using MeConta.Application.Dtos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Servicos;

/// <summary>
/// Regras de acesso usadas por todos os serviços: quem está logado e de que família faz parte.
/// </summary>
public interface IControleDeAcesso
{
    /// <summary>Membro logado. Lança 403 se não houver login ou se o perfil ainda não foi criado.</summary>
    Task<Membro> ObterMembroAtualAsync(CancellationToken cancellationToken = default);

    /// <summary>Garante que o membro logado é da família (404 se ela não existe, 403 se não for membro).</summary>
    Task<Membro> GarantirAcessoAFamiliaAsync(int familiaId, CancellationToken cancellationToken = default);

    /// <summary>Carrega o idoso e garante que o membro logado é da família dele.</summary>
    Task<AcessoAoIdoso> GarantirAcessoAoIdosoAsync(int idosoId, CancellationToken cancellationToken = default);
}

/// <summary>Membro logado e idoso que ele pode acessar.</summary>
public sealed record AcessoAoIdoso(Membro Membro, Idoso Idoso);

public interface IMembroService
{
    Task<MembroResponse> CriarPerfilAsync(CriarPerfilRequest request, CancellationToken cancellationToken = default);

    Task<MembroResponse> ObterPerfilAsync(CancellationToken cancellationToken = default);
}

public interface IFamiliaService
{
    Task<FamiliaResumoResponse> CriarAsync(CriarFamiliaRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FamiliaResumoResponse>> ListarMinhasAsync(CancellationToken cancellationToken = default);

    Task<FamiliaDetalheResponse> ObterAsync(int familiaId, CancellationToken cancellationToken = default);

    Task<ConviteResponse> GerarConviteAsync(int familiaId, CancellationToken cancellationToken = default);

    Task<FamiliaDetalheResponse> AceitarConviteAsync(string codigo, CancellationToken cancellationToken = default);
}

public interface IIdosoService
{
    Task<IdosoResponse> CadastrarAsync(int familiaId, IdosoRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdosoResponse>> ListarAsync(int familiaId, CancellationToken cancellationToken = default);

    Task<IdosoResponse> ObterAsync(int idosoId, CancellationToken cancellationToken = default);

    Task<IdosoResponse> AtualizarAsync(int idosoId, IdosoRequest request, CancellationToken cancellationToken = default);

    Task<IdosoResponse> ConcluirRoteiroAsync(int idosoId, CancellationToken cancellationToken = default);
}

public interface IPerguntaService
{
    Task<IReadOnlyList<CategoriaResponse>> ListarCategoriasAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PerguntaResponse>> ListarParaIdosoAsync(
        int idosoId,
        string? categoria,
        CancellationToken cancellationToken = default);

    Task<PerguntaResponse> CriarDaFamiliaAsync(
        int familiaId,
        CriarPerguntaRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sugestão da semana, ou <see langword="null"/> quando o catálogo acabou para esse idoso.</summary>
    Task<PerguntaDaSemanaResponse?> ObterPerguntaDaSemanaAsync(int idosoId, CancellationToken cancellationToken = default);
}

public interface IEnvioService
{
    Task<EnvioResponse> EnviarAsync(int idosoId, EnviarPerguntaRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvioResponse>> ListarAsync(
        int idosoId,
        StatusEnvio? status,
        CancellationToken cancellationToken = default);
}

public interface ICadernoService
{
    Task<ResultadoGuardarResposta> GuardarRespostaAsync(
        int envioId,
        NovaResposta resposta,
        CancellationToken cancellationToken = default);

    Task<ListaPaginada<PaginaResponse>> ListarAsync(
        int familiaId,
        FiltroCaderno filtro,
        CancellationToken cancellationToken = default);

    Task<PaginaResponse> ObterAsync(int paginaId, CancellationToken cancellationToken = default);

    Task<ArquivoParaDownload> AbrirAudioAsync(int paginaId, CancellationToken cancellationToken = default);

    Task<ArquivoParaDownload> AbrirFotoAsync(int paginaId, CancellationToken cancellationToken = default);

    Task ApagarAsync(int paginaId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Pareamento do porta-retrato pela família (US15).
/// </summary>
public interface IPortaRetratoService
{
    /// <summary>Gera código e chave novos. Se o idoso já tinha porta-retrato, a chave antiga para de funcionar.</summary>
    Task<PortaRetratoPareadoResponse> ParearAsync(int idosoId, CancellationToken cancellationToken = default);

    Task<PortaRetratoResponse> ObterAsync(int idosoId, CancellationToken cancellationToken = default);

    /// <summary>Desliga o porta-retrato do idoso. Os avisos já recebidos continuam no histórico.</summary>
    Task DesparearAsync(int idosoId, CancellationToken cancellationToken = default);
}

/// <summary>
/// O que o próprio aparelho (pela ponte Node-RED) pode fazer, autenticado pela chave do porta-retrato.
/// </summary>
public interface IAparelhoPortaRetratoService
{
    Task<EstadoPortaRetratoResponse> ObterEstadoAsync(string? chave, CancellationToken cancellationToken = default);

    Task<ResultadoQueroContar> QueroContarAsync(
        string? chave,
        QueroContarRequest request,
        CancellationToken cancellationToken = default);

    Task RegistrarConexaoAsync(
        string? chave,
        ConexaoPortaRetratoRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Avisos "quero contar" vistos pela família no app.
/// </summary>
public interface IAvisoService
{
    Task<IReadOnlyList<AvisoResponse>> ListarAsync(
        int familiaId,
        bool somenteNaoVistos,
        CancellationToken cancellationToken = default);

    Task<AvisoResponse> MarcarVistoAsync(int avisoId, CancellationToken cancellationToken = default);
}

public interface IIndicadorService
{
    /// <summary>Indicador da semana que contém a data (ou da semana atual, se a data não vier).</summary>
    Task<IndicadorConversasResponse> ObterConversasDeVerdadeAsync(
        DateOnly? dataNaSemana,
        CancellationToken cancellationToken = default);
}
