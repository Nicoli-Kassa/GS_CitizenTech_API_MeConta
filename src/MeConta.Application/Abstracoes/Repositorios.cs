using MeConta.Application.Dtos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Abstracoes;

public interface IMembroRepository : IRepository<Membro, int>
{
    Task<Membro?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default);
}

public interface IFamiliaRepository : IRepository<Familia, int>
{
    /// <summary>Família com membros (e seus nomes) e idosos, pronta para ser alterada.</summary>
    Task<Familia?> ObterCompletaAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Família dona do código de convite, com os membros e esse convite carregados.</summary>
    Task<Familia?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> EhMembroAsync(int familiaId, int membroId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamiliaResumoResponse>> ListarDoMembroAsync(int membroId, CancellationToken cancellationToken = default);
}

public interface IIdosoRepository : IRepository<Idoso, int>
{
    Task<IReadOnlyList<Idoso>> ListarDaFamiliaAsync(int familiaId, CancellationToken cancellationToken = default);
}

public interface ICategoriaRepository : IRepository<Categoria, int>
{
    Task<Categoria?> ObterPorSlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IPerguntaRepository : IRepository<Pergunta, int>
{
    /// <summary>Perguntas do catálogo e as da própria família, opcionalmente de uma categoria.</summary>
    Task<IReadOnlyList<Pergunta>> ListarVisiveisParaFamiliaAsync(
        int familiaId,
        int? categoriaId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PerguntaCatalogo>> ListarCatalogoAsync(CancellationToken cancellationToken = default);
}

public interface IEnvioRepository : IRepository<EnvioPergunta, int>
{
    Task<EnvioPergunta?> ObterComIdosoAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvioPergunta>> ListarDoIdosoAsync(
        int idosoId,
        StatusEnvio? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> ObterIdsPerguntasEnviadasAsync(int idosoId, CancellationToken cancellationToken = default);
}

public interface IPaginaRepository : IRepository<PaginaCaderno, int>
{
    Task<PaginaCaderno?> ObterPorChaveAsync(Guid chaveIdempotencia, CancellationToken cancellationToken = default);

    /// <summary>Página com o envio e o idoso carregados, pronta para ser alterada.</summary>
    Task<PaginaCaderno?> ObterComEnvioAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PaginaCaderno> Itens, int Total)> ListarDaFamiliaAsync(
        int familiaId,
        int? idosoId,
        int? categoriaId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default);
}

public interface IPortaRetratoRepository : IRepository<PortaRetrato, int>
{
    /// <summary>Aparelho dono da chave (busca pelo hash), com o idoso carregado.</summary>
    Task<PortaRetrato?> ObterPorChaveHashAsync(string chaveHash, CancellationToken cancellationToken = default);

    Task<PortaRetrato?> ObterDoIdosoAsync(int idosoId, CancellationToken cancellationToken = default);

    Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken = default);
}

public interface IAvisoRepository : IRepository<AvisoQueroContar, int>
{
    /// <summary>Ids dos envios deste idoso que já receberam "quero contar".</summary>
    Task<IReadOnlySet<int>> ObterIdsEnviosComAvisoAsync(int idosoId, CancellationToken cancellationToken = default);

    Task<AvisoQueroContar?> ObterDoEnvioAsync(int envioId, CancellationToken cancellationToken = default);

    /// <summary>Aviso mais recente sem pergunta ligada, criado a partir de <paramref name="desdeUtc"/>.</summary>
    Task<AvisoQueroContar?> ObterUltimoSemEnvioAsync(
        int idosoId,
        DateTime desdeUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Avisos dos idosos da família, do mais recente para o mais antigo, já com apelido e pergunta.</summary>
    Task<IReadOnlyList<AvisoResponse>> ListarDaFamiliaAsync(
        int familiaId,
        bool somenteNaoVistos,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Consulta agregada do indicador "Conversas de verdade" (US12).
/// </summary>
public interface IIndicadorConsulta
{
    Task<DadosIndicador> ObterAsync(DateTime inicioUtc, DateTime fimUtcExclusivo, CancellationToken cancellationToken = default);
}

/// <summary>Contagens brutas usadas no cálculo do indicador.</summary>
public sealed record DadosIndicador(
    int IdososCadastrados,
    int IdososQueResponderam,
    int SegundosDeAudio,
    int IdososQueRespondemSozinhos);
