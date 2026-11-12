using System.Security.Claims;
using MeConta.Application.Adapters;

namespace MeConta.Api.Adapters;

/// <summary>
/// Lê o usuário logado a partir do token Bearer do pedido HTTP.
/// </summary>
public sealed class UsuarioAtualHttp(IHttpContextAccessor httpContextAccessor) : IUsuarioAtual
{
    public string? UsuarioId =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
