using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.Regras;

namespace MeConta.Application.Servicos;

/// <summary>
/// Pedidos feitos em nome do porta-retrato pela ponte (Node-RED), autenticados pela chave do aparelho (US15).
/// A chave só dá acesso ao estado do próprio idoso e ao botão "quero contar":
/// nada de caderno, áudios, telefone ou dados de outras pessoas.
/// </summary>
public sealed class AparelhoPortaRetratoService(
    IPortaRetratoRepository portaRetratos,
    IIdosoRepository idosos,
    IEnvioRepository envios,
    IAvisoRepository avisos,
    IMembroRepository membros,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IAparelhoPortaRetratoService
{
    public const string MensagemChaveInvalida = "Chave do porta-retrato ausente ou inválida.";

    /// <summary>
    /// Apertar o botão de novo, sem pergunta na tela, dentro deste tempo não cria outro aviso
    /// (evita encher o app da família quando o botão é apertado várias vezes).
    /// </summary>
    public static readonly TimeSpan JanelaDoAvisoSemPergunta = TimeSpan.FromMinutes(2);

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<EstadoPortaRetratoResponse> ObterEstadoAsync(
        string? chave,
        CancellationToken cancellationToken = default)
    {
        PortaRetrato portaRetrato = await AutenticarAsync(chave, cancellationToken);
        Idoso idoso = await ObterIdosoAsync(portaRetrato, cancellationToken);

        IReadOnlyList<EnvioPergunta> enviados = await envios.ListarDoIdosoAsync(
            portaRetrato.IdosoId,
            StatusEnvio.Enviada,
            cancellationToken);
        IReadOnlySet<int> comAviso = await avisos.ObterIdsEnviosComAvisoAsync(portaRetrato.IdosoId, cancellationToken);
        IReadOnlyList<EnvioPergunta> esperando = FilaDoPortaRetrato.PerguntasEsperando(enviados, comAviso);

        PerguntaNoPortaRetrato? naTela = null;
        if (esperando.Count > 0)
        {
            EnvioPergunta envio = esperando[0];
            Membro? quemEnviou = await membros.ObterPorIdAsync(envio.EnviadoPorMembroId, cancellationToken);
            naTela = new PerguntaNoPortaRetrato(
                envio.Id,
                FilaDoPortaRetrato.PrimeiroNome(quemEnviou?.Nome ?? string.Empty),
                envio.TextoPergunta,
                envio.EnviadoEm);
        }

        return new EstadoPortaRetratoResponse(
            portaRetrato.Codigo,
            idoso.Apelido,
            naTela is not null,
            esperando.Count,
            naTela);
    }

    public async Task<ResultadoQueroContar> QueroContarAsync(
        string? chave,
        QueroContarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PortaRetrato portaRetrato = await AutenticarAsync(chave, cancellationToken);
        Idoso idoso = await ObterIdosoAsync(portaRetrato, cancellationToken);

        EnvioPergunta? envio = null;
        AvisoQueroContar? existente;

        if (request.EnvioId is int envioId)
        {
            // Envio inexistente ou de outro idoso recebe a mesma mensagem,
            // para a chave de um aparelho não revelar dados de outra família.
            envio = await envios.ObterPorIdAsync(envioId, cancellationToken);
            if (envio is null || envio.IdosoId != portaRetrato.IdosoId)
            {
                throw new RegraDeNegocioException(AvisoQueroContar.MensagemEnvioDeOutroIdoso);
            }

            existente = await avisos.ObterDoEnvioAsync(envioId, cancellationToken);
        }
        else
        {
            existente = await avisos.ObterUltimoSemEnvioAsync(
                portaRetrato.IdosoId,
                Agora - JanelaDoAvisoSemPergunta,
                cancellationToken);
        }

        // Botão apertado de novo para a mesma pergunta: devolve o aviso que já existe.
        if (existente is not null)
        {
            return new ResultadoQueroContar(AvisoResponse.De(existente, idoso.Apelido, envio?.TextoPergunta), Criado: false);
        }

        AvisoQueroContar aviso = new(portaRetrato, envio, Agora);
        await avisos.AdicionarAsync(aviso, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new ResultadoQueroContar(AvisoResponse.De(aviso, idoso.Apelido, envio?.TextoPergunta), Criado: true);
    }

    public async Task RegistrarConexaoAsync(
        string? chave,
        ConexaoPortaRetratoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PortaRetrato portaRetrato = await AutenticarAsync(chave, cancellationToken);

        portaRetrato.RegistrarConexao(request.Online, Agora);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    /// <summary>
    /// Procura o aparelho pelo hash da chave. Chave vazia e chave desconhecida recebem a mesma resposta (401).
    /// </summary>
    private async Task<PortaRetrato> AutenticarAsync(string? chave, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chave))
        {
            throw new CredencialInvalidaException(MensagemChaveInvalida);
        }

        PortaRetrato? portaRetrato = await portaRetratos.ObterPorChaveHashAsync(
            PortaRetrato.CalcularHash(chave),
            cancellationToken);

        return portaRetrato is not null && portaRetrato.ConfereChave(chave)
            ? portaRetrato
            : throw new CredencialInvalidaException(MensagemChaveInvalida);
    }

    private async Task<Idoso> ObterIdosoAsync(PortaRetrato portaRetrato, CancellationToken cancellationToken) =>
        await idosos.ObterPorIdAsync(portaRetrato.IdosoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Idoso não encontrado.");
}
