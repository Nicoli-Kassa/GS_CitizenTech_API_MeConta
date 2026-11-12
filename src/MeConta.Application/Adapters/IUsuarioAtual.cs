namespace MeConta.Application.Adapters;

/// <summary>
/// Quem fez o pedido. Na API, vem do token de login; nos testes, de um valor fixo.
/// </summary>
public interface IUsuarioAtual
{
    /// <summary>Id do usuário no ASP.NET Identity, ou <see langword="null"/> se ninguém estiver logado.</summary>
    string? UsuarioId { get; }
}
