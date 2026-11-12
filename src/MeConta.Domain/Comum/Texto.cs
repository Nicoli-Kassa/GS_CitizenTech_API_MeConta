using MeConta.Domain.Excecoes;

namespace MeConta.Domain.Comum;

/// <summary>
/// Validações de texto reaproveitadas pelas entidades.
/// </summary>
internal static class Texto
{
    /// <summary>
    /// Confere se o texto foi preenchido e cabe no limite, e devolve o texto sem espaços nas pontas.
    /// </summary>
    /// <exception cref="RegraDeNegocioException">Quando o texto está vazio ou é longo demais.</exception>
    public static string Obrigatorio(
        string? valor,
        int tamanhoMaximo,
        string mensagemSeVazio,
        string mensagemSeLongo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new RegraDeNegocioException(mensagemSeVazio);
        }

        string normalizado = valor.Trim();
        if (normalizado.Length > tamanhoMaximo)
        {
            throw new RegraDeNegocioException(mensagemSeLongo);
        }

        return normalizado;
    }
}
