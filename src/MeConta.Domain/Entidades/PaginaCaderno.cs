using MeConta.Domain.Comum;
using MeConta.Domain.Excecoes;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Uma memória guardada no caderno da família: a resposta em áudio,
/// com título e foto opcional (US08 e US09).
/// </summary>
public sealed class PaginaCaderno : Entidade
{
    public const int TamanhoMaximoTitulo = 200;

    // Construtor usado pelo Entity Framework ao ler do banco.
    private PaginaCaderno()
    {
    }

    /// <summary>
    /// Só o <see cref="EnvioPergunta"/> cria páginas, porque é ele que controla
    /// se a pergunta já foi respondida.
    /// </summary>
    internal PaginaCaderno(
        EnvioPergunta envio,
        string? titulo,
        string audioCaminho,
        int duracaoSegundos,
        string? fotoCaminho,
        int criadaPorMembroId,
        Guid chaveIdempotencia,
        DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(envio);

        if (string.IsNullOrWhiteSpace(audioCaminho))
        {
            throw new ArgumentException("O caminho do áudio é obrigatório.", nameof(audioCaminho));
        }

        if (chaveIdempotencia == Guid.Empty)
        {
            throw new ArgumentException("A chave de idempotência é obrigatória.", nameof(chaveIdempotencia));
        }

        if (duracaoSegundos <= 0)
        {
            throw new RegraDeNegocioException("Não conseguimos ler a duração do áudio. Tente de novo.");
        }

        // Título em branco usa o texto da pergunta.
        string tituloFinal = string.IsNullOrWhiteSpace(titulo) ? envio.TextoPergunta : titulo.Trim();
        if (tituloFinal.Length > TamanhoMaximoTitulo)
        {
            throw new RegraDeNegocioException($"Use até {TamanhoMaximoTitulo} caracteres no título");
        }

        Envio = envio;
        EnvioId = envio.Id;
        Titulo = tituloFinal;
        AudioCaminho = audioCaminho;
        DuracaoSegundos = duracaoSegundos;
        FotoCaminho = string.IsNullOrWhiteSpace(fotoCaminho) ? null : fotoCaminho;
        CriadaPorMembroId = criadaPorMembroId;
        ChaveIdempotencia = chaveIdempotencia;
        CriadaEm = agora;
    }

    public int EnvioId { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    /// <summary>Caminho relativo do áudio no armazenamento. Ex.: "audios/3f2a...opus".</summary>
    public string AudioCaminho { get; private set; } = string.Empty;

    public int DuracaoSegundos { get; private set; }

    public string? FotoCaminho { get; private set; }

    public int CriadaPorMembroId { get; private set; }

    /// <summary>
    /// Identificador gerado pelo celular. Se o app reenviar a mesma página depois de ficar sem internet (US10),
    /// a API reconhece a chave e não cria uma página duplicada.
    /// </summary>
    public Guid ChaveIdempotencia { get; private set; }

    public DateTime CriadaEm { get; private set; }

    public EnvioPergunta? Envio { get; private set; }

    public bool TemFoto => FotoCaminho is not null;

    /// <summary>
    /// Só quem criou a página pode apagá-la (US11).
    /// </summary>
    public bool PodeSerApagadaPor(int membroId) => CriadaPorMembroId == membroId;
}
