namespace MeConta.Application.Abstracoes;

/// <summary>
/// Grava de uma vez tudo o que os repositórios alteraram no mesmo pedido.
/// Se algo falhar, nada fica pela metade no banco.
/// </summary>
public interface IUnidadeDeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
