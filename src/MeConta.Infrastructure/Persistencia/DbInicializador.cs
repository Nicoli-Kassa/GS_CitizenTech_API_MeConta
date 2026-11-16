using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// Prepara o banco quando a API sobe.
/// </summary>
public static class DbInicializador
{
    /// <summary>
    /// SQL Server: aplica as migrações pendentes. SQLite (testes): cria o banco a partir do modelo.
    /// Em seguida, se pedido, cria os dados fictícios de demonstração.
    /// </summary>
    /// <param name="services">Serviços da API.</param>
    /// <param name="criarDadosDeDemonstracao">Cria a Família Souza, a Bia, a Cláudia, os idosos e o porta-retrato.</param>
    /// <param name="criarPaginasDoFiltro">Cria também as páginas para testar o filtro de dados pessoais.</param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public static async Task InicializarAsync(
        IServiceProvider services,
        bool criarDadosDeDemonstracao,
        bool criarPaginasDoFiltro = false,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope escopo = services.CreateAsyncScope();
        AppDbContext db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.IsSqlServer())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await db.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (criarDadosDeDemonstracao)
        {
            await DadosDeDemonstracao.CriarAsync(escopo.ServiceProvider, criarPaginasDoFiltro, cancellationToken);
        }
    }
}
