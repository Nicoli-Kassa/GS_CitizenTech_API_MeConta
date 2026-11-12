using MeConta.Domain.Excecoes;

namespace MeConta.Domain.ValueObjects;

/// <summary>
/// Arquivo enviado pelo app (áudio ou foto), validado antes de ser salvo.
/// Cada tipo concreto define os formatos aceitos, o tamanho máximo e as mensagens de erro.
/// </summary>
public abstract class ArquivoDeMidia
{
    private const string MensagemArquivoVazio = "O arquivo está vazio. Escolha outro arquivo.";

    protected ArquivoDeMidia(string nomeArquivo, long tamanhoBytes)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo))
        {
            throw new RegraDeNegocioException(MensagemFormatoInvalido);
        }

        string extensao = Path.GetExtension(nomeArquivo.Trim()).ToLowerInvariant();
        if (!FormatosAceitos.TryGetValue(extensao, out string? contentType))
        {
            throw new RegraDeNegocioException(MensagemFormatoInvalido);
        }

        if (tamanhoBytes <= 0)
        {
            throw new RegraDeNegocioException(MensagemArquivoVazio);
        }

        if (tamanhoBytes > TamanhoMaximoBytes)
        {
            throw new ArquivoMuitoGrandeException(MensagemTamanhoExcedido);
        }

        Extensao = extensao;
        ContentType = contentType;
        TamanhoBytes = tamanhoBytes;
    }

    /// <summary>Extensão em minúsculas, com o ponto. Ex.: ".opus".</summary>
    public string Extensao { get; }

    /// <summary>Tipo MIME usado para devolver o arquivo. Ex.: "audio/ogg".</summary>
    public string ContentType { get; }

    public long TamanhoBytes { get; }

    /// <summary>Pasta onde o tipo de arquivo é guardado. Ex.: "audios".</summary>
    public abstract string Pasta { get; }

    /// <summary>Extensões aceitas e o tipo MIME de cada uma.</summary>
    protected abstract IReadOnlyDictionary<string, string> FormatosAceitos { get; }

    protected abstract long TamanhoMaximoBytes { get; }

    protected abstract string MensagemFormatoInvalido { get; }

    protected abstract string MensagemTamanhoExcedido { get; }
}
