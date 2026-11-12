namespace MeConta.Application.Abstracoes;

/// <summary>
/// Operações básicas de leitura e escrita de uma entidade.
/// Os repositórios específicos herdam este contrato e acrescentam as consultas de que precisam.
/// </summary>
/// <typeparam name="T">Tipo da entidade.</typeparam>
/// <typeparam name="TId">Tipo do identificador.</typeparam>
public interface IRepository<T, in TId>
    where T : class
{
    Task<T?> ObterPorIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ObterTodosAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default);

    void Remover(T entidade);
}
