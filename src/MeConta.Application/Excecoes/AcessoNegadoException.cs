namespace MeConta.Application.Excecoes;

/// <summary>
/// Quem está logado não pode acessar o item (ex.: caderno de outra família).
/// A API transforma esta exceção em resposta 403 (Forbidden).
/// </summary>
public sealed class AcessoNegadoException : Exception
{
    public AcessoNegadoException(string mensagem)
        : base(mensagem)
    {
    }
}
