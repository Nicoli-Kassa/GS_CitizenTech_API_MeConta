namespace MeConta.Domain.Excecoes;

/// <summary>
/// Violação de uma regra de negócio do Me Conta.
/// A mensagem é escrita para quem usa o app e pode ir direto para a tela.
/// A API transforma esta exceção em resposta 400 (Bad Request).
/// </summary>
public class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem)
        : base(mensagem)
    {
    }
}
