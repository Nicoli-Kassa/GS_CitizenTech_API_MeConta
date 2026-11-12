namespace MeConta.Domain.ValueObjects;

/// <summary>
/// Áudio de resposta do idoso (US08): mp3, m4a, ogg ou opus (formato do WhatsApp), até 20 MB.
/// </summary>
public sealed class ArquivoDeAudio : ArquivoDeMidia
{
    public const long LimiteBytes = 20L * 1024 * 1024;

    // Estático para já existir quando o construtor da classe base consultar os formatos.
    private static readonly Dictionary<string, string> Formatos = new()
    {
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".ogg"] = "audio/ogg",
        [".opus"] = "audio/ogg",
    };

    public ArquivoDeAudio(string nomeArquivo, long tamanhoBytes)
        : base(nomeArquivo, tamanhoBytes)
    {
    }

    public override string Pasta => "audios";

    /// <summary>
    /// Tipo MIME de um áudio já salvo, a partir da extensão do caminho.
    /// </summary>
    public static string ObterContentType(string caminho) =>
        Formatos.GetValueOrDefault(Path.GetExtension(caminho).ToLowerInvariant(), "application/octet-stream");

    protected override IReadOnlyDictionary<string, string> FormatosAceitos => Formatos;

    protected override long TamanhoMaximoBytes => LimiteBytes;

    protected override string MensagemFormatoInvalido =>
        "Escolha um arquivo de áudio (mp3, m4a, ogg ou opus)";

    protected override string MensagemTamanhoExcedido =>
        "Áudio muito grande: o limite é 20 MB";
}
