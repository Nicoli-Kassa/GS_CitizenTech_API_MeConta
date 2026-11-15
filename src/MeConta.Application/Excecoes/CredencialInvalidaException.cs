namespace MeConta.Application.Excecoes;

/// <summary>
/// O aparelho (porta-retrato) não mandou a chave ou mandou uma chave que não existe.
/// A API transforma esta exceção em resposta 401 (Unauthorized).
/// </summary>
public sealed class CredencialInvalidaException : Exception
{
    public CredencialInvalidaException(string mensagem)
        : base(mensagem)
    {
    }
}
