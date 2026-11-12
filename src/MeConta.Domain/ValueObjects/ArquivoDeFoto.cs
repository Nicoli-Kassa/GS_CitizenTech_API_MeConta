namespace MeConta.Domain.ValueObjects;

/// <summary>
/// Foto opcional de uma página do caderno: jpg, png, webp ou heic, até 10 MB.
/// </summary>
public sealed class ArquivoDeFoto : ArquivoDeMidia
{
    public const long LimiteBytes = 10L * 1024 * 1024;

    private static readonly Dictionary<string, string> Formatos = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".heic"] = "image/heic",
    };

    public ArquivoDeFoto(string nomeArquivo, long tamanhoBytes)
        : base(nomeArquivo, tamanhoBytes)
    {
    }

    public override string Pasta => "fotos";

    protected override IReadOnlyDictionary<string, string> FormatosAceitos => Formatos;

    protected override long TamanhoMaximoBytes => LimiteBytes;

    protected override string MensagemFormatoInvalido =>
        "Escolha uma foto (jpg, png, webp ou heic)";

    protected override string MensagemTamanhoExcedido =>
        "Foto muito grande: o limite é 10 MB";
}
