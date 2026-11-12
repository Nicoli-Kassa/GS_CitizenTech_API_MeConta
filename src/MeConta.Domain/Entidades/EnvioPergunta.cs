using MeConta.Domain.Comum;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Uma pergunta enviada a um idoso pelo WhatsApp (US07).
/// Guarda uma cópia do texto e do telefone do momento do envio,
/// para o histórico não mudar se a pergunta ou o idoso forem editados depois.
/// </summary>
public sealed class EnvioPergunta : Entidade
{
    public const string MensagemSemWhatsApp = "Cadastre o WhatsApp";
    public const string MensagemPerguntaIndisponivel = "Essa pergunta não está disponível para esta família";
    public const string MensagemJaRespondida = "Essa pergunta já foi respondida";

    // Construtor usado pelo Entity Framework ao ler do banco.
    private EnvioPergunta()
    {
    }

    public EnvioPergunta(Idoso idoso, IPergunta pergunta, int enviadoPorMembroId, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(idoso);
        ArgumentNullException.ThrowIfNull(pergunta);

        if (idoso.Telefone is null)
        {
            throw new ConflitoException(MensagemSemWhatsApp);
        }

        if (!pergunta.VisivelPara(idoso.FamiliaId))
        {
            throw new RegraDeNegocioException(MensagemPerguntaIndisponivel);
        }

        IdosoId = idoso.Id;
        PerguntaId = pergunta.Id;
        CategoriaId = pergunta.CategoriaId;
        TextoPergunta = pergunta.Texto;
        TelefoneDestino = idoso.Telefone;
        Mensagem = $"{idoso.Apelido}, me conta: {pergunta.Texto}";
        EnviadoPorMembroId = enviadoPorMembroId;
        EnviadoEm = agora;
        Status = StatusEnvio.Enviada;
    }

    public int IdosoId { get; private set; }

    public int PerguntaId { get; private set; }

    public int CategoriaId { get; private set; }

    /// <summary>Cópia do texto da pergunta no momento do envio.</summary>
    public string TextoPergunta { get; private set; } = string.Empty;

    /// <summary>Texto que vai no WhatsApp. Ex.: "Vó Lourdes, me conta: qual era o segredo do bolo?"</summary>
    public string Mensagem { get; private set; } = string.Empty;

    public Telefone TelefoneDestino { get; private set; } = null!;

    public int EnviadoPorMembroId { get; private set; }

    public DateTime EnviadoEm { get; private set; }

    public StatusEnvio Status { get; private set; }

    public DateTime? RespondidoEm { get; private set; }

    public Idoso? Idoso { get; private set; }

    public PaginaCaderno? Pagina { get; private set; }

    /// <summary>
    /// Link que abre a conversa com o idoso já com a mensagem escrita.
    /// O texto é codificado para que acentos e emojis cheguem certos.
    /// </summary>
    public string ObterLinkWhatsApp() =>
        $"https://wa.me/{TelefoneDestino.Valor}?text={Uri.EscapeDataString(Mensagem)}";

    /// <summary>
    /// Guarda a resposta em áudio como página do caderno (US08) e marca a pergunta como respondida.
    /// </summary>
    /// <exception cref="ConflitoException">A pergunta já tem uma página.</exception>
    public PaginaCaderno Responder(
        string? titulo,
        string audioCaminho,
        int duracaoSegundos,
        string? fotoCaminho,
        int criadaPorMembroId,
        Guid chaveIdempotencia,
        DateTime agora)
    {
        if (Status == StatusEnvio.Respondida)
        {
            throw new ConflitoException(MensagemJaRespondida);
        }

        PaginaCaderno pagina = new(
            this,
            titulo,
            audioCaminho,
            duracaoSegundos,
            fotoCaminho,
            criadaPorMembroId,
            chaveIdempotencia,
            agora);

        Pagina = pagina;
        Status = StatusEnvio.Respondida;
        RespondidoEm = agora;
        return pagina;
    }

    /// <summary>
    /// Volta a pergunta para "Enviada" quando a página do caderno é apagada (US11),
    /// para que a família possa guardar outra resposta.
    /// </summary>
    public void ReabrirAposExclusao()
    {
        Pagina = null;
        Status = StatusEnvio.Enviada;
        RespondidoEm = null;
    }
}
