using MeConta.Application.Abstracoes;
using MeConta.Domain.Comum;
using MeConta.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace MeConta.Infrastructure.Repositorios;

/// <summary>
/// Implementação genérica de <see cref="IRepository{T, TId}"/> com Entity Framework.
/// Os repositórios específicos herdam daqui e acrescentam as consultas próprias.
/// </summary>
public abstract class RepositorioEf<T>(AppDbContext db) : IRepository<T, int>
    where T : Entidade
{
    protected AppDbContext Db { get; } = db;

    protected DbSet<T> Conjunto => Db.Set<T>();

    /// <summary>Busca pela chave. A entidade volta rastreada, pronta para ser alterada.</summary>
    public virtual async Task<T?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Conjunto.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ObterTodosAsync(CancellationToken cancellationToken = default) =>
        await Conjunto.AsNoTracking().OrderBy(entidade => entidade.Id).ToListAsync(cancellationToken);

    public async Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default) =>
        await Conjunto.AddAsync(entidade, cancellationToken);

    public void Remover(T entidade) => Conjunto.Remove(entidade);
}
