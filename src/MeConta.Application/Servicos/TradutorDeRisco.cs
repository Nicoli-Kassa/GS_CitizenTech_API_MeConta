using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Resultados;

namespace MeConta.Application.Servicos;

/// <summary>
/// Traduz falhas técnicas em alertas de risco real para o cidadão (missão 03).
///
/// A IA fica entre duas conferências do motor de dados pessoais:
/// <list type="number">
/// <item>Antes de enviar: se algum campo do pedido ainda tiver dado pessoal, o envio é barrado
/// e valem as explicações locais. O pedido só leva tipo, nível e trecho mascarado.</item>
/// <item>Depois de receber: cada frase da IA passa pelo motor. A que trouxer dado pessoal
/// (um número inventado, um telefone "de exemplo") é descartada e trocada pela explicação local.</item>
/// </list>
/// Sem chave, com o limite do plano gratuito estourado ou com a IA fora do ar, a resposta vem das
/// explicações locais e o painel mostra a fonte. Respostas iguais ficam guardadas em memória,
/// para não gastar o limite do plano gratuito com a mesma pergunta.
/// </summary>
public sealed class TradutorDeRisco(IExplicadorDeRisco ia, IMotorPii motor, TimeProvider relogio) : ITradutorDeRisco
{
    public const string FonteLocal = "Local";
    public const string FonteMotor = "Motor";
    public const int TamanhoMaximoDoCampo = 400;
    public const int TamanhoMaximoDaVersao = 4_500;
    public const int TamanhoDoCache = 200;
    public static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(25);
    public static readonly TimeSpan PausaDepoisDeFalha = TimeSpan.FromSeconds(60);

    /// <summary>Tipos que, numa frase de alerta, são assunto e não dado de alguém.</summary>
    private static readonly HashSet<string> TiposDeAssunto = new(StringComparer.OrdinalIgnoreCase) { "Saude", "Vulnerabilidade" };

    private readonly ConcurrentDictionary<string, ResultadoDaTraducao> _cache = new();
    private readonly object _trava = new();
    private DateTimeOffset _pausadaAte = DateTimeOffset.MinValue;
    private string? _ultimoModelo;
    private int _chamadas;
    private int _respostasAproveitadas;
    private int _frasesDescartadas;
    private int _enviosBarrados;
    private int _usosDaReserva;
    private int _respostasDoCache;

    public async Task<ResultadoDaTraducao> TraduzirAsync(PedidoDeTraducao pedido, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        IReadOnlyList<AlertaTraduzido> locais = ExplicacoesLocais.Traduzir(pedido);

        if (pedido.Falhas.Count == 0)
        {
            return new ResultadoDaTraducao([], pedido.TextoMascarado, FonteMotor, null);
        }

        if (!ia.Disponivel)
        {
            return Reserva(locais, pedido, "Sem chave do Gemini configurada: alertas escritos pela equipe.");
        }

        // 1ª conferência: nada com dado pessoal sai para a IA.
        if (PedidoTemDadoPessoal(pedido))
        {
            Interlocked.Increment(ref _enviosBarrados);
            return Reserva(locais, pedido, "Envio à IA barrado: o pedido ainda tinha dado pessoal.");
        }

        string chave = ChaveDoCache(pedido);
        if (_cache.TryGetValue(chave, out ResultadoDaTraducao? guardado))
        {
            Interlocked.Increment(ref _respostasDoCache);
            return guardado;
        }

        if (relogio.GetUtcNow() < LerPausa())
        {
            return Reserva(locais, pedido, "IA em pausa depois de uma falha: alertas escritos pela equipe.");
        }

        TraducaoDaIa resposta;
        try
        {
            Interlocked.Increment(ref _chamadas);
            using CancellationTokenSource limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(TempoLimite);
            resposta = await ia.ExplicarAsync(pedido, limite.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Rede, limite do plano gratuito, chave recusada, resposta fora do formato: o cidadão
            // continua recebendo o alerta, só que o local. O adapter registra o motivo no log.
            Pausar();
            return Reserva(locais, pedido, "IA indisponível agora: alertas escritos pela equipe.");
        }

        // 2ª conferência: cada frase da IA passa pelo motor.
        int descartadas = 0;
        List<AlertaTraduzido> alertas = [];
        foreach (AlertaTraduzido local in locais)
        {
            AlertaTraduzido? daIa = resposta.Alertas.FirstOrDefault(alerta => alerta.Id == local.Id);
            if (daIa is not null && FraseSegura(daIa.Alerta) && FraseSegura(daIa.RiscoReal) && FraseSegura(daIa.OQueFazer))
            {
                alertas.Add(new AlertaTraduzido(local.Id, daIa.Alerta.Trim(), daIa.RiscoReal.Trim(), daIa.OQueFazer.Trim()));
            }
            else
            {
                descartadas++;
                alertas.Add(local);
            }
        }

        string? versao = pedido.TextoMascarado;
        if (pedido.TextoMascarado is not null)
        {
            if (VersaoSegura(resposta.VersaoSugerida))
            {
                versao = resposta.VersaoSugerida!.Trim();
            }
            else
            {
                descartadas++;
            }
        }

        Interlocked.Add(ref _frasesDescartadas, descartadas);
        if (descartadas < locais.Count + (pedido.TextoMascarado is null ? 0 : 1))
        {
            Interlocked.Increment(ref _respostasAproveitadas);
        }

        _ultimoModelo = resposta.Modelo ?? _ultimoModelo;
        string nome = resposta.Modelo is null ? ia.Nome : $"{ia.Nome} ({resposta.Modelo})";
        string fonte = descartadas == 0 ? nome : $"{nome} + {FonteLocal}";
        string? observacao = descartadas == 0
            ? null
            : $"{descartadas} resposta(s) da IA descartada(s) pela conferência do motor e trocada(s) pelo texto local.";

        ResultadoDaTraducao resultado = new(alertas, versao, fonte, observacao);
        Guardar(chave, resultado);
        return resultado;
    }

    public EstatisticasDaIaResponse Estatisticas() =>
        new(
            ia.Nome,
            ia.Disponivel,
            _ultimoModelo,
            relogio.GetUtcNow() < LerPausa(),
            Volatile.Read(ref _chamadas),
            Volatile.Read(ref _respostasAproveitadas),
            Volatile.Read(ref _frasesDescartadas),
            Volatile.Read(ref _enviosBarrados),
            Volatile.Read(ref _usosDaReserva),
            Volatile.Read(ref _respostasDoCache));

    private ResultadoDaTraducao Reserva(IReadOnlyList<AlertaTraduzido> locais, PedidoDeTraducao pedido, string observacao)
    {
        Interlocked.Increment(ref _usosDaReserva);
        return new ResultadoDaTraducao(locais, pedido.TextoMascarado, FonteLocal, observacao);
    }

    /// <summary>
    /// Confere os campos que vieram do texto da família: o trecho mascarado e o texto mascarado.
    /// Rótulo, descrição técnica e referência são escritos pelo motor ou pelo Semgrep, não pela família.
    /// </summary>
    private bool PedidoTemDadoPessoal(PedidoDeTraducao pedido)
    {
        IEnumerable<string?> campos = pedido.Falhas
            .Select(falha => falha.Exemplo)
            .Append(pedido.TextoMascarado);

        return campos.Any(campo => !string.IsNullOrWhiteSpace(campo) && !TextoLimpo(campo, todosOsTipos: true));
    }

    /// <summary>
    /// Uma frase de alerta pode falar de saúde ou de "morar sozinha" como assunto ("não conte que a pessoa mora sozinha"),
    /// mas não pode trazer nenhum dado que identifique alguém: número, telefone, e-mail, endereço, data...
    /// </summary>
    private bool FraseSegura(string? frase) =>
        !string.IsNullOrWhiteSpace(frase)
        && frase.Length <= TamanhoMaximoDoCampo
        && TextoLimpo(frase, todosOsTipos: false);

    /// <summary>A versão sugerida é a memória da pessoa: aí nenhum tipo de dado pessoal pode voltar.</summary>
    private bool VersaoSegura(string? versao) =>
        !string.IsNullOrWhiteSpace(versao)
        && versao.Length <= TamanhoMaximoDaVersao
        && TextoLimpo(versao, todosOsTipos: true);

    /// <summary>O motor conseguiu analisar o texto inteiro e não encontrou dado pessoal.</summary>
    private bool TextoLimpo(string texto, bool todosOsTipos)
    {
        ResultadoAnalisePii resultado = motor.Analisar(texto);
        return resultado.AnaliseCompleta
            && !resultado.Achados.Any(achado => todosOsTipos || !TiposDeAssunto.Contains(achado.Tipo));
    }

    private DateTimeOffset LerPausa()
    {
        lock (_trava)
        {
            return _pausadaAte;
        }
    }

    private void Pausar()
    {
        lock (_trava)
        {
            _pausadaAte = relogio.GetUtcNow() + PausaDepoisDeFalha;
        }
    }

    private void Guardar(string chave, ResultadoDaTraducao resultado)
    {
        if (_cache.Count >= TamanhoDoCache)
        {
            _cache.Clear();
        }

        _cache[chave] = resultado;
    }

    private static string ChaveDoCache(PedidoDeTraducao pedido)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(pedido);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}

/// <summary>Alertas prontos para mostrar, já conferidos pelo motor.</summary>
/// <param name="Alertas">Um alerta por falha, na ordem do pedido.</param>
/// <param name="VersaoSugerida">Memória sem os dados pessoais (da IA, se passou na conferência; senão, a versão mascarada).</param>
/// <param name="Fonte">"Gemini (modelo)", "Gemini (modelo) + Local", "Local" ou "Motor".</param>
/// <param name="Observacao">Por que a reserva local foi usada, quando foi.</param>
public sealed record ResultadoDaTraducao(
    IReadOnlyList<AlertaTraduzido> Alertas,
    string? VersaoSugerida,
    string Fonte,
    string? Observacao);
