using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Comum;
using MeConta.Domain.Entidades;
using MeConta.Domain.Excecoes;
using MeConta.Domain.Regras;
using MeConta.Domain.ValueObjects;

namespace MeConta.Application.Servicos;

/// <summary>
/// Catálogo por categoria (US04), pergunta da semana (US05) e perguntas da família (US06).
/// </summary>
public sealed class PerguntaService(
    IControleDeAcesso acesso,
    ICategoriaRepository categorias,
    IPerguntaRepository perguntas,
    IEnvioRepository envios,
    ISugestaoSemanal sugestaoSemanal,
    IFiltroAntiGolpe filtroAntiGolpe,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IPerguntaService
{
    public async Task<IReadOnlyList<CategoriaResponse>> ListarCategoriasAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Categoria> lista = await categorias.ObterTodosAsync(cancellationToken);
        return lista.Select(CategoriaResponse.De).ToList();
    }

    public async Task<IReadOnlyList<PerguntaResponse>> ListarParaIdosoAsync(
        int idosoId,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);
        int? categoriaId = await ResolverCategoriaAsync(categoria, cancellationToken);

        IReadOnlyList<Pergunta> visiveis = await perguntas.ListarVisiveisParaFamiliaAsync(
            acessoAoIdoso.Idoso.FamiliaId,
            categoriaId,
            cancellationToken);
        IReadOnlySet<int> jaEnviadas = await envios.ObterIdsPerguntasEnviadasAsync(idosoId, cancellationToken);

        return visiveis
            .Select(pergunta => PerguntaResponse.De(pergunta, jaEnviadas.Contains(pergunta.Id)))
            .ToList();
    }

    public async Task<PerguntaResponse> CriarDaFamiliaAsync(
        int familiaId,
        CriarPerguntaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Membro membro = await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);

        if (!await categorias.ExisteAsync(request.CategoriaId, cancellationToken))
        {
            throw new RegraDeNegocioException("Escolha uma categoria para a pergunta");
        }

        PerguntaDaFamilia pergunta = new(
            request.Texto,
            request.CategoriaId,
            familiaId,
            membro.Id,
            relogio.GetUtcNow().UtcDateTime,
            filtroAntiGolpe);
        await perguntas.AdicionarAsync(pergunta, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return PerguntaResponse.De(pergunta, jaPerguntada: false);
    }

    public async Task<PerguntaDaSemanaResponse?> ObterPerguntaDaSemanaAsync(
        int idosoId,
        CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);

        IReadOnlyList<PerguntaCatalogo> catalogo = await perguntas.ListarCatalogoAsync(cancellationToken);
        IReadOnlySet<int> jaEnviadas = await envios.ObterIdsPerguntasEnviadasAsync(idosoId, cancellationToken);
        Semana semana = Semana.Contendo(relogio.GetUtcNow().UtcDateTime, FusoHorario.Brasilia);

        IPergunta? sugerida = sugestaoSemanal.Sugerir(idosoId, catalogo, jaEnviadas, semana);
        if (sugerida is null)
        {
            return null;
        }

        return new PerguntaDaSemanaResponse(
            idosoId,
            semana.Inicio,
            semana.Fim,
            PerguntaResponse.De(sugerida, jaPerguntada: false));
    }

    private async Task<int?> ResolverCategoriaAsync(string? slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        Categoria categoria = await categorias.ObterPorSlugAsync(slug, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Categoria não encontrada.");
        return categoria.Id;
    }
}
