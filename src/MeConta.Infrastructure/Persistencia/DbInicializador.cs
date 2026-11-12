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
    public static async Task InicializarAsync(
        IServiceProvider services,
        bool criarDadosDeDemonstracao,
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
            await DadosDeDemonstracao.CriarAsync(escopo.ServiceProvider, cancellationToken);
        }
    }
}
