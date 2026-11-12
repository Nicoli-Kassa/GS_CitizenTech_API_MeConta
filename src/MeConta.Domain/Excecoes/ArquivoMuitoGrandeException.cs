namespace MeConta.Domain.Excecoes;

/// <summary>
/// O arquivo enviado passa do tamanho máximo permitido.
/// A API transforma esta exceção em resposta 413 (Content Too Large).
/// </summary>
public sealed class ArquivoMuitoGrandeException : RegraDeNegocioException
{
    public ArquivoMuitoGrandeException(string mensagem)
        : base(mensagem)
    {
    }
}
