namespace MeConta.Domain.Excecoes;

/// <summary>
/// A operação conflita com o estado atual dos dados
/// (ex.: número de WhatsApp já cadastrado na família, pergunta já respondida).
/// A API transforma esta exceção em resposta 409 (Conflict).
/// </summary>
public sealed class ConflitoException : RegraDeNegocioException
{
    public ConflitoException(string mensagem)
        : base(mensagem)
    {
    }
}
