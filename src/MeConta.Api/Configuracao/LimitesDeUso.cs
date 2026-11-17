using System.Threading.RateLimiting;

namespace MeConta.Api.Configuracao;

/// <summary>
/// Limites de pedidos por endereço IP (janela fixa de um minuto). Passou do limite: HTTP 429.
/// </summary>
public static class LimitesDeUso
{
    /// <summary>Rotas abertas do painel Risco Cidadão (o painel consulta a cada 5 segundos).</summary>
    public const string Painel = "painel";

    /// <summary>Rotas que podem chamar a IA.</summary>
    public const string Ia = "ia";

    public const int PedidosDoPainelPorMinuto = 120;
    public const int PedidosDaIaPorMinuto = 20;

    public static RateLimitPartition<string> PorEndereco(HttpContext contexto, int pedidosPorMinuto) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "sem-endereco",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = pedidosPorMinuto,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
}
