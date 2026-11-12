using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;

namespace MeConta.Application.Servicos;

/// <summary>
/// Criação da família e entrada de parentes por convite (US01 e US03).
/// </summary>
public sealed class FamiliaService(
    IControleDeAcesso acesso,
    IFamiliaRepository familias,
    IGeradorDeCodigoConvite geradorDeCodigo,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IFamiliaService
{
    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<FamiliaResumoResponse> CriarAsync(
        CriarFamiliaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);

        Familia familia = new(request.Nome, membro.Id, Agora);
        await familias.AdicionarAsync(familia, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new FamiliaResumoResponse(familia.Id, familia.Nome, PapelNaFamilia.Criador, 0, familia.CriadaEm);
    }

    public async Task<IReadOnlyList<FamiliaResumoResponse>> ListarMinhasAsync(CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        return await familias.ListarDoMembroAsync(membro.Id, cancellationToken);
    }

    public async Task<FamiliaDetalheResponse> ObterAsync(int familiaId, CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);
        Familia familia = await ObterCompletaAsync(familiaId, cancellationToken);
        return FamiliaDetalheResponse.De(familia);
    }

    public async Task<ConviteResponse> GerarConviteAsync(int familiaId, CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);
        Familia familia = await familias.ObterPorIdAsync(familiaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Família não encontrada.");

        Convite convite = familia.GerarConvite(geradorDeCodigo.Gerar(), membro.Id, Agora);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new ConviteResponse(convite.Codigo, convite.ExpiraEm);
    }

    public async Task<FamiliaDetalheResponse> AceitarConviteAsync(string codigo, CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        string normalizado = Convite.Normalizar(codigo);

        // Código que não existe recebe a mesma mensagem de código vencido,
        // para não revelar quais códigos existem.
        Familia familia = await familias.ObterPorCodigoConviteAsync(normalizado, cancellationToken)
            ?? throw new RegraDeNegocioException(Familia.MensagemConviteInvalido);

        familia.AceitarConvite(normalizado, membro.Id, Agora);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        Familia completa = await ObterCompletaAsync(familia.Id, cancellationToken);
        return FamiliaDetalheResponse.De(completa);
    }

    private async Task<Familia> ObterCompletaAsync(int familiaId, CancellationToken cancellationToken) =>
        await familias.ObterCompletaAsync(familiaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Família não encontrada.");
}
