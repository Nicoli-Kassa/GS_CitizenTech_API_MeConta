using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace MeConta.Infrastructure.Repositorios;

public sealed class MembroRepository(AppDbContext db) : RepositorioEf<Membro>(db), IMembroRepository
{
    public Task<Membro?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        Db.Membros.FirstOrDefaultAsync(membro => membro.UsuarioId == usuarioId, cancellationToken);
}

public sealed class FamiliaRepository(AppDbContext db) : RepositorioEf<Familia>(db), IFamiliaRepository
{
    public Task<Familia?> ObterCompletaAsync(int id, CancellationToken cancellationToken = default) =>
        Db.Familias
            .Include(familia => familia.Participacoes)
                .ThenInclude(participacao => participacao.Membro)
            .Include(familia => familia.Idosos)
            .AsSplitQuery()
            .FirstOrDefaultAsync(familia => familia.Id == id, cancellationToken);

    public Task<Familia?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken = default) =>
        Db.Familias
            .Include(familia => familia.Participacoes)
            .Include(familia => familia.Convites.Where(convite => convite.Codigo == codigo))
            .FirstOrDefaultAsync(
                familia => familia.Convites.Any(convite => convite.Codigo == codigo),
                cancellationToken);

    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        Db.Familias.AnyAsync(familia => familia.Id == id, cancellationToken);

    public Task<bool> EhMembroAsync(int familiaId, int membroId, CancellationToken cancellationToken = default) =>
        Db.Participacoes.AnyAsync(
            participacao => participacao.FamiliaId == familiaId && participacao.MembroId == membroId,
            cancellationToken);

    public async Task<IReadOnlyList<FamiliaResumoResponse>> ListarDoMembroAsync(
        int membroId,
        CancellationToken cancellationToken = default)
    {
        // LINQ em sintaxe de consulta: junta participação e família e já monta o resumo.
        IQueryable<FamiliaResumoResponse> consulta =
            from participacao in Db.Participacoes.AsNoTracking()
            join familia in Db.Familias on participacao.FamiliaId equals familia.Id
            where participacao.MembroId == membroId
            orderby familia.Nome
            select new FamiliaResumoResponse(
                familia.Id,
                familia.Nome,
                participacao.Papel,
                familia.Idosos.Count(),
                familia.CriadaEm);

        return await consulta.ToListAsync(cancellationToken);
    }
}

public sealed class IdosoRepository(AppDbContext db) : RepositorioEf<Idoso>(db), IIdosoRepository
{
    public async Task<IReadOnlyList<Idoso>> ListarDaFamiliaAsync(int familiaId, CancellationToken cancellationToken = default) =>
        await Db.Idosos
            .AsNoTracking()
            .Where(idoso => idoso.FamiliaId == familiaId)
            .OrderBy(idoso => idoso.Apelido)
            .ToListAsync(cancellationToken);
}

public sealed class CategoriaRepository(AppDbContext db) : RepositorioEf<Categoria>(db), ICategoriaRepository
{
    public Task<Categoria?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        string normalizado = slug.Trim().ToLowerInvariant();
        return Db.Categorias.AsNoTracking().FirstOrDefaultAsync(categoria => categoria.Slug == normalizado, cancellationToken);
    }

    public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default) =>
        Db.Categorias.AnyAsync(categoria => categoria.Id == id, cancellationToken);
}

public sealed class PerguntaRepository(AppDbContext db) : RepositorioEf<Pergunta>(db), IPerguntaRepository
{
    public async Task<IReadOnlyList<Pergunta>> ListarVisiveisParaFamiliaAsync(
        int familiaId,
        int? categoriaId,
        CancellationToken cancellationToken = default)
    {
        // Duas consultas na mesma tabela (TPH): o catálogo inteiro e as perguntas desta família.
        IQueryable<PerguntaCatalogo> catalogo = Db.Set<PerguntaCatalogo>().AsNoTracking();
        IQueryable<PerguntaDaFamilia> daFamilia = Db.Set<PerguntaDaFamilia>()
            .AsNoTracking()
            .Where(pergunta => pergunta.FamiliaId == familiaId);

        if (categoriaId is int id)
        {
            catalogo = catalogo.Where(pergunta => pergunta.CategoriaId == id);
            daFamilia = daFamilia.Where(pergunta => pergunta.CategoriaId == id);
        }

        List<PerguntaCatalogo> doCatalogo = await catalogo.OrderBy(pergunta => pergunta.Id).ToListAsync(cancellationToken);
        List<PerguntaDaFamilia> criadas = await daFamilia.OrderBy(pergunta => pergunta.Id).ToListAsync(cancellationToken);

        // As perguntas da família aparecem primeiro: são as mais pessoais.
        return criadas.Cast<Pergunta>().Concat(doCatalogo).ToList();
    }

    public async Task<IReadOnlyList<PerguntaCatalogo>> ListarCatalogoAsync(CancellationToken cancellationToken = default) =>
        await Db.Set<PerguntaCatalogo>()
            .AsNoTracking()
            .OrderBy(pergunta => pergunta.Id)
            .ToListAsync(cancellationToken);
}

public sealed class EnvioRepository(AppDbContext db) : RepositorioEf<EnvioPergunta>(db), IEnvioRepository
{
    public Task<EnvioPergunta?> ObterComIdosoAsync(int id, CancellationToken cancellationToken = default) =>
        Db.Envios
            .Include(envio => envio.Idoso)
            .FirstOrDefaultAsync(envio => envio.Id == id, cancellationToken);

    public async Task<IReadOnlyList<EnvioPergunta>> ListarDoIdosoAsync(
        int idosoId,
        StatusEnvio? status,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EnvioPergunta> consulta = Db.Envios
            .AsNoTracking()
            .Include(envio => envio.Pagina)
            .Where(envio => envio.IdosoId == idosoId);

        if (status is StatusEnvio filtro)
        {
            consulta = consulta.Where(envio => envio.Status == filtro);
        }

        return await consulta
            .OrderByDescending(envio => envio.EnviadoEm)
            .ThenByDescending(envio => envio.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<int>> ObterIdsPerguntasEnviadasAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        List<int> ids = await Db.Envios
            .Where(envio => envio.IdosoId == idosoId)
            .Select(envio => envio.PerguntaId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}

public sealed class PaginaRepository(AppDbContext db) : RepositorioEf<PaginaCaderno>(db), IPaginaRepository
{
    public Task<PaginaCaderno?> ObterPorChaveAsync(Guid chaveIdempotencia, CancellationToken cancellationToken = default) =>
        ComEnvioEIdoso().FirstOrDefaultAsync(pagina => pagina.ChaveIdempotencia == chaveIdempotencia, cancellationToken);

    public Task<PaginaCaderno?> ObterComEnvioAsync(int id, CancellationToken cancellationToken = default) =>
        ComEnvioEIdoso().FirstOrDefaultAsync(pagina => pagina.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<PaginaCaderno> Itens, int Total)> ListarDaFamiliaAsync(
        int familiaId,
        int? idosoId,
        int? categoriaId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default)
    {
        IQueryable<PaginaCaderno> consulta = ComEnvioEIdoso()
            .AsNoTracking()
            .Where(item => item.Envio!.Idoso!.FamiliaId == familiaId);

        if (idosoId is int idoso)
        {
            consulta = consulta.Where(item => item.Envio!.IdosoId == idoso);
        }

        if (categoriaId is int categoria)
        {
            consulta = consulta.Where(item => item.Envio!.CategoriaId == categoria);
        }

        int total = await consulta.CountAsync(cancellationToken);

        // Da mais recente para a mais antiga (US09), com paginação.
        List<PaginaCaderno> itens = await consulta
            .OrderByDescending(item => item.CriadaEm)
            .ThenByDescending(item => item.Id)
            .Skip((pagina - 1) * tamanho)
            .Take(tamanho)
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    private IQueryable<PaginaCaderno> ComEnvioEIdoso() =>
        Db.Paginas
            .Include(pagina => pagina.Envio)
                .ThenInclude(envio => envio!.Idoso);
}

public sealed class PortaRetratoRepository(AppDbContext db) : RepositorioEf<PortaRetrato>(db), IPortaRetratoRepository
{
    public Task<PortaRetrato?> ObterPorChaveHashAsync(string chaveHash, CancellationToken cancellationToken = default) =>
        Db.PortaRetratos
            .Include(portaRetrato => portaRetrato.Idoso)
            .FirstOrDefaultAsync(portaRetrato => portaRetrato.ChaveHash == chaveHash, cancellationToken);

    public Task<PortaRetrato?> ObterDoIdosoAsync(int idosoId, CancellationToken cancellationToken = default) =>
        Db.PortaRetratos.FirstOrDefaultAsync(portaRetrato => portaRetrato.IdosoId == idosoId, cancellationToken);

    public Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken = default) =>
        Db.PortaRetratos.AnyAsync(portaRetrato => portaRetrato.Codigo == codigo, cancellationToken);
}

public sealed class AvisoRepository(AppDbContext db) : RepositorioEf<AvisoQueroContar>(db), IAvisoRepository
{
    public async Task<IReadOnlySet<int>> ObterIdsEnviosComAvisoAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        List<int> ids = await Db.Avisos
            .Where(aviso => aviso.IdosoId == idosoId && aviso.EnvioId != null)
            .Select(aviso => aviso.EnvioId!.Value)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public Task<AvisoQueroContar?> ObterDoEnvioAsync(int envioId, CancellationToken cancellationToken = default) =>
        Db.Avisos.FirstOrDefaultAsync(aviso => aviso.EnvioId == envioId, cancellationToken);

    public Task<AvisoQueroContar?> ObterUltimoSemEnvioAsync(
        int idosoId,
        DateTime desdeUtc,
        CancellationToken cancellationToken = default) =>
        Db.Avisos
            .Where(aviso => aviso.IdosoId == idosoId && aviso.EnvioId == null && aviso.CriadoEm >= desdeUtc)
            .OrderByDescending(aviso => aviso.CriadoEm)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AvisoResponse>> ListarDaFamiliaAsync(
        int familiaId,
        bool somenteNaoVistos,
        CancellationToken cancellationToken = default)
    {
        // LINQ em sintaxe de consulta: aviso + idoso, com a pergunta (left join, porque ela é opcional).
        IQueryable<AvisoResponse> consulta =
            from aviso in Db.Avisos.AsNoTracking()
            join idoso in Db.Idosos on aviso.IdosoId equals idoso.Id
            join envio in Db.Envios on aviso.EnvioId equals (int?)envio.Id into enviosDoAviso
            from envio in enviosDoAviso.DefaultIfEmpty()
            where idoso.FamiliaId == familiaId && (!somenteNaoVistos || aviso.VistoEm == null)
            orderby aviso.CriadoEm descending, aviso.Id descending
            select new AvisoResponse(
                aviso.Id,
                aviso.IdosoId,
                idoso.Apelido,
                aviso.EnvioId,
                envio == null ? null : envio.TextoPergunta,
                aviso.CriadoEm,
                aviso.VistoEm != null,
                aviso.VistoEm);

        return await consulta.ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Consultas agregadas do indicador "Conversas de verdade" (US12).
/// </summary>
public sealed class IndicadorConsulta(AppDbContext db) : IIndicadorConsulta
{
    public async Task<DadosIndicador> ObterAsync(
        DateTime inicioUtc,
        DateTime fimUtcExclusivo,
        CancellationToken cancellationToken = default)
    {
        int cadastrados = await db.Idosos
            .CountAsync(idoso => idoso.CadastradoEm < fimUtcExclusivo, cancellationToken);

        // Sintaxe de consulta: idosos diferentes com pelo menos uma página guardada na semana.
        int responderam = await (
                from pagina in db.Paginas
                join envio in db.Envios on pagina.EnvioId equals envio.Id
                where pagina.CriadaEm >= inicioUtc && pagina.CriadaEm < fimUtcExclusivo
                select envio.IdosoId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Sintaxe de métodos: soma da duração dos áudios da semana.
        int segundos = await db.Paginas
            .Where(pagina => pagina.CriadaEm >= inicioUtc && pagina.CriadaEm < fimUtcExclusivo)
            .SumAsync(pagina => (int?)pagina.DuracaoSegundos, cancellationToken) ?? 0;

        int respondemSozinhos = await db.Idosos
            .CountAsync(idoso => idoso.RespondeSozinho && idoso.CadastradoEm < fimUtcExclusivo, cancellationToken);

        return new DadosIndicador(cadastrados, responderam, segundos, respondemSozinhos);
    }
}
