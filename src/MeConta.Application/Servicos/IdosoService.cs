using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.ValueObjects;

namespace MeConta.Application.Servicos;

/// <summary>
/// Cadastro do idoso (US02) e conclusão do roteiro "Ensine sua avó" (US14).
/// </summary>
public sealed class IdosoService(
    IControleDeAcesso acesso,
    IFamiliaRepository familias,
    IIdosoRepository idosos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IIdosoService
{
    public async Task<IdosoResponse> CadastrarAsync(
        int familiaId,
        IdosoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);
        Familia familia = await ObterFamiliaCompletaAsync(familiaId, cancellationToken);

        Idoso idoso = familia.CadastrarIdoso(
            request.Nome,
            request.Apelido,
            CriarTelefone(request.WhatsApp),
            relogio.GetUtcNow().UtcDateTime);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return IdosoResponse.De(idoso);
    }

    public async Task<IReadOnlyList<IdosoResponse>> ListarAsync(int familiaId, CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);
        IReadOnlyList<Idoso> lista = await idosos.ListarDaFamiliaAsync(familiaId, cancellationToken);
        return lista.Select(IdosoResponse.De).ToList();
    }

    public async Task<IdosoResponse> ObterAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);
        return IdosoResponse.De(acessoAoIdoso.Idoso);
    }

    public async Task<IdosoResponse> AtualizarAsync(
        int idosoId,
        IdosoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);

        // A família é quem garante que o WhatsApp não se repete, então a alteração passa por ela.
        Familia familia = await ObterFamiliaCompletaAsync(acessoAoIdoso.Idoso.FamiliaId, cancellationToken);
        Idoso idoso = familia.Idosos.Single(item => item.Id == idosoId);

        familia.AtualizarIdoso(idoso, request.Nome, request.Apelido, CriarTelefone(request.WhatsApp));
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return IdosoResponse.De(idoso);
    }

    public async Task<IdosoResponse> ConcluirRoteiroAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);

        acessoAoIdoso.Idoso.MarcarRespondeSozinho();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return IdosoResponse.De(acessoAoIdoso.Idoso);
    }

    /// <summary>WhatsApp vazio vira "sem telefone"; preenchido passa pela validação do <see cref="Telefone"/>.</summary>
    private static Telefone? CriarTelefone(string? whatsApp) =>
        string.IsNullOrWhiteSpace(whatsApp) ? null : new Telefone(whatsApp);

    private async Task<Familia> ObterFamiliaCompletaAsync(int familiaId, CancellationToken cancellationToken) =>
        await familias.ObterCompletaAsync(familiaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Família não encontrada.");
}
