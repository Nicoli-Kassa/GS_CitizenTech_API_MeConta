namespace MeConta.Application.Excecoes;

/// <summary>
/// O item pedido não existe. A API transforma esta exceção em resposta 404 (Not Found).
/// </summary>
public sealed class RecursoNaoEncontradoException : Exception
{
    public RecursoNaoEncontradoException(string mensagem)
        : base(mensagem)
    {
    }
}
