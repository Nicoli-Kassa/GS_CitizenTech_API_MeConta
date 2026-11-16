using System.Text;
using System.Text.RegularExpressions;
using MeConta.Privacidade.Normalizacao;
using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;
using MeConta.Privacidade.Taint;

namespace MeConta.Privacidade.Motor;

/// <summary>
/// Motor de detecção de dados pessoais (PII) do Me Conta.
/// </summary>
public interface IMotorPii
{
    string VersaoDasRegras { get; }

    /// <summary>Procura dados pessoais no texto e calcula o Risco Cidadão.</summary>
    ResultadoAnalisePii Analisar(string texto);

    /// <summary>
    /// Única forma de transformar um <see cref="TextoDoUsuario"/> (não verificado)
    /// num <see cref="TextoVerificado"/>, que as saídas do sistema aceitam.
    /// </summary>
    TextoVerificado Verificar(TextoDoUsuario texto);

    /// <summary>
    /// Verifica vários campos do mesmo conteúdo de uma vez (ex.: título e transcrição),
    /// com uma decisão e um índice Risco Cidadão para o conjunto.
    /// </summary>
    CamposVerificados VerificarCampos(IReadOnlyList<CampoDoUsuario> campos);
}

/// <summary>
/// Limites do motor.
/// </summary>
public sealed class OpcoesDoMotorPii
{
    /// <summary>Textos maiores que isso não são analisados e ficam bloqueados.</summary>
    public int TamanhoMaximoDoTexto { get; init; } = 50_000;
}

/// <summary>
/// Passo a passo da análise:
/// 1. normaliza o texto (minúsculas, sem acentos, números falados em dígitos);
/// 2. aplica todas as regras do catálogo;
/// 3. quando duas regras pegam o mesmo trecho, fica a mais grave e mais confiável;
/// 4. mascara os trechos, monta a versão segura do texto e calcula o Risco Cidadão.
/// Se algo impedir a análise completa, o resultado bloqueia a publicação (falha fechada).
/// </summary>
public sealed class MotorPii : IMotorPii
{
    public const string AvisoTextoNaoVerificado = "[texto não verificado]";

    private readonly CatalogoDeRegras _catalogo;
    private readonly OpcoesDoMotorPii _opcoes;

    public MotorPii(CatalogoDeRegras catalogo, OpcoesDoMotorPii? opcoes = null)
    {
        ArgumentNullException.ThrowIfNull(catalogo);
        _catalogo = catalogo;
        _opcoes = opcoes ?? new OpcoesDoMotorPii();
    }

    /// <summary>Motor com as regras que vêm dentro do módulo.</summary>
    public static MotorPii CriarPadrao() => new(CatalogoDeRegras.CarregarPadrao());

    public string VersaoDasRegras => _catalogo.Versao;

    public TextoVerificado Verificar(TextoDoUsuario texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return new TextoVerificado(texto.ValorBruto, Analisar(texto.ValorBruto));
    }

    public CamposVerificados VerificarCampos(IReadOnlyList<CampoDoUsuario> campos)
    {
        ArgumentNullException.ThrowIfNull(campos);

        if (campos.Count == 0)
        {
            throw new ArgumentException("Informe pelo menos um campo.", nameof(campos));
        }

        if (campos.Select(campo => campo.Nome).Distinct(StringComparer.Ordinal).Count() != campos.Count)
        {
            throw new ArgumentException("Os nomes dos campos não podem se repetir.", nameof(campos));
        }

        List<ResultadoDoCampo> resultados = campos
            .Select(campo => new ResultadoDoCampo(campo.Nome, Analisar(campo.Texto.ValorBruto)))
            .ToList();

        // Cada achado leva o nome do campo, e a posição continua relativa ao texto do próprio campo.
        List<AchadoPii> achados = resultados
            .SelectMany(campo => campo.Resultado.Achados.Select(achado => achado with { Campo = campo.Nome }))
            .ToList();

        bool completa = resultados.All(campo => campo.Resultado.AnaliseCompleta);
        List<CombinacaoEncontrada> combinacoes = completa ? EncontrarCombinacoes(achados) : [];
        int indice = completa ? CalculadoraDeRiscoCidadao.Calcular(achados, combinacoes) : 100;

        AnaliseDeCampos analise = new(resultados, achados, combinacoes, indice, VersaoDasRegras);
        Dictionary<string, string> originais = campos.ToDictionary(campo => campo.Nome, campo => campo.Texto.ValorBruto);
        return new CamposVerificados(originais, analise);
    }

    public ResultadoAnalisePii Analisar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        if (string.IsNullOrWhiteSpace(texto))
        {
            return new ResultadoAnalisePii([], [], 0, texto, analiseCompleta: true, VersaoDasRegras);
        }

        if (texto.Length > _opcoes.TamanhoMaximoDoTexto)
        {
            return Incompleto([]);
        }

        TextoNormalizado normalizado = NormalizadorDeTexto.Normalizar(texto);
        List<Candidato> candidatos = [];
        bool completa = true;

        foreach (RegraPii regra in _catalogo.Regras)
        {
            try
            {
                candidatos.AddRange(regra.Encontrar(normalizado));
            }
            catch (RegexMatchTimeoutException)
            {
                // Texto montado para travar a expressão: segue com as outras regras, mas bloqueia no final.
                completa = false;
            }
        }

        List<AchadoPii> achados = ResolverSobreposicoes(candidatos)
            .Select(candidato => CriarAchado(texto, candidato))
            .ToList();

        if (!completa)
        {
            return Incompleto(achados);
        }

        List<CombinacaoEncontrada> combinacoes = EncontrarCombinacoes(achados);
        int indice = CalculadoraDeRiscoCidadao.Calcular(achados, combinacoes);
        string textoSeguro = MontarTextoSeguro(texto, achados);

        return new ResultadoAnalisePii(achados, combinacoes, indice, textoSeguro, analiseCompleta: true, VersaoDasRegras);
    }

    /// <summary>
    /// Quando duas regras pegam trechos que se sobrepõem, fica a de nível mais alto;
    /// no empate, a de maior confiança; depois, a que pegou o trecho maior.
    /// </summary>
    private static IEnumerable<Candidato> ResolverSobreposicoes(List<Candidato> candidatos)
    {
        List<Candidato> aceitos = [];

        IEnumerable<Candidato> emOrdemDePrioridade = candidatos
            .OrderByDescending(candidato => candidato.Regra.Nivel)
            .ThenByDescending(candidato => candidato.Confianca)
            .ThenByDescending(candidato => candidato.Trecho.Tamanho)
            .ThenBy(candidato => candidato.Trecho.Inicio);

        foreach (Candidato candidato in emOrdemDePrioridade)
        {
            if (!aceitos.Any(aceito => aceito.Trecho.SeSobrepoe(candidato.Trecho)))
            {
                aceitos.Add(candidato);
            }
        }

        return aceitos.OrderBy(aceito => aceito.Trecho.Inicio);
    }

    private static AchadoPii CriarAchado(string texto, Candidato candidato)
    {
        RegraPii regra = candidato.Regra;
        Trecho trecho = candidato.Trecho;
        string mascarado = Mascarador.Mascarar(texto.Substring(trecho.Inicio, trecho.Tamanho), regra.Mascara);

        return new AchadoPii(
            regra.Id,
            regra.Tipo,
            regra.Rotulo,
            regra.Nivel,
            trecho.Inicio,
            trecho.Tamanho,
            mascarado,
            candidato.Confianca,
            candidato.TemContexto,
            regra.Explicacao);
    }

    private List<CombinacaoEncontrada> EncontrarCombinacoes(List<AchadoPii> achados)
    {
        HashSet<string> tipos = achados.Select(achado => achado.Tipo).ToHashSet(StringComparer.Ordinal);

        return _catalogo.Combinacoes
            .Where(combinacao => combinacao.Tipos.All(tipos.Contains))
            .Select(combinacao => new CombinacaoEncontrada(combinacao.Id, combinacao.Bonus, combinacao.Explicacao))
            .ToList();
    }

    /// <summary>Troca cada dado pessoal pelo aviso da regra: "meu CPF é [CPF removido]".</summary>
    private string MontarTextoSeguro(string texto, List<AchadoPii> achados)
    {
        Dictionary<string, string> substituicoes = _catalogo.Regras.ToDictionary(regra => regra.Id, regra => regra.Substituicao);
        StringBuilder seguro = new(texto.Length);
        int posicao = 0;

        foreach (AchadoPii achado in achados)
        {
            seguro.Append(texto, posicao, achado.Inicio - posicao);
            seguro.Append(substituicoes[achado.RegraId]);
            posicao = achado.Inicio + achado.Tamanho;
        }

        seguro.Append(texto, posicao, texto.Length - posicao);
        return seguro.ToString();
    }

    private ResultadoAnalisePii Incompleto(IReadOnlyList<AchadoPii> achados) =>
        new(achados, [], 100, AvisoTextoNaoVerificado, analiseCompleta: false, VersaoDasRegras);
}
