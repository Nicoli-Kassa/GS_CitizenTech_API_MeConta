using System.Reflection;
using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Comum;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.UnitTests.Servicos;

/// <summary>
/// "Banco" em memória para testar os serviços sem Entity Framework.
/// Ao salvar, dá um Id para as entidades novas, como o banco faria.
/// </summary>
internal sealed class BancoFake
{
    private static readonly PropertyInfo PropriedadeId = typeof(Entidade).GetProperty(nameof(Entidade.Id))!;

    private int _proximoId = 100;

    public List<Membro> Membros { get; } = [];

    public List<Idoso> Idosos { get; } = [];

    public List<EnvioPergunta> Envios { get; } = [];

    public List<PortaRetrato> PortaRetratos { get; } = [];

    public List<AvisoQueroContar> Avisos { get; } = [];

    public List<PaginaCaderno> Paginas { get; } = [];

    public List<MemoriaDoBairro> Memorias { get; } = [];

    public List<RegistroDePrivacidade> Registros { get; } = [];

    public int Salvamentos { get; private set; }

    /// <summary>Dá um Id fixo a uma entidade criada no teste (o Id tem setter protegido).</summary>
    public static T ComId<T>(T entidade, int id)
        where T : Entidade
    {
        PropriedadeId.SetValue(entidade, id);
        return entidade;
    }

    /// <summary>
    /// Preenche uma navegação que o Entity Framework carregaria do banco (ex.: o idoso de um envio).
    /// </summary>
    public static T ComNavegacao<T>(T entidade, string propriedade, object valor)
        where T : Entidade
    {
        typeof(T).GetProperty(propriedade)!.SetValue(entidade, valor);
        return entidade;
    }

    public void Salvar()
    {
        Salvamentos++;
        IEnumerable<Entidade> todas = Membros.Cast<Entidade>()
            .Concat(Idosos)
            .Concat(Envios)
            .Concat(PortaRetratos)
            .Concat(Avisos)
            .Concat(Paginas)
            .Concat(Memorias)
            .Concat(Registros);

        foreach (Entidade entidade in todas.Where(entidade => entidade.Id == 0))
        {
            ComId(entidade, _proximoId++);
        }
    }
}

internal sealed class UnidadeDeTrabalhoFake(BancoFake banco) : IUnidadeDeTrabalho
{
    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        banco.Salvar();
        return Task.FromResult(1);
    }
}

internal class RepositorioFake<T>(List<T> itens) : IRepository<T, int>
    where T : Entidade
{
    protected List<T> Itens { get; } = itens;

    public Task<T?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<T>> ObterTodosAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<T>>(Itens.ToList());

    public Task AdicionarAsync(T entidade, CancellationToken cancellationToken = default)
    {
        Itens.Add(entidade);
        return Task.CompletedTask;
    }

    public void Remover(T entidade) => Itens.Remove(entidade);
}

internal sealed class MembroRepositoryFake(BancoFake banco) : RepositorioFake<Membro>(banco.Membros), IMembroRepository
{
    public Task<Membro?> ObterPorUsuarioIdAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(membro => membro.UsuarioId == usuarioId));
}

internal sealed class IdosoRepositoryFake(BancoFake banco) : RepositorioFake<Idoso>(banco.Idosos), IIdosoRepository
{
    public Task<IReadOnlyList<Idoso>> ListarDaFamiliaAsync(int familiaId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Idoso>>(Itens.Where(idoso => idoso.FamiliaId == familiaId).ToList());
}

internal sealed class EnvioRepositoryFake(BancoFake banco) : RepositorioFake<EnvioPergunta>(banco.Envios), IEnvioRepository
{
    public Task<EnvioPergunta?> ObterComIdosoAsync(int id, CancellationToken cancellationToken = default) =>
        ObterPorIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<EnvioPergunta>> ListarDoIdosoAsync(
        int idosoId,
        StatusEnvio? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EnvioPergunta>>(Itens
            .Where(envio => envio.IdosoId == idosoId && (status is null || envio.Status == status))
            .OrderByDescending(envio => envio.EnviadoEm)
            .ToList());

    public Task<IReadOnlySet<int>> ObterIdsPerguntasEnviadasAsync(int idosoId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<int>>(Itens
            .Where(envio => envio.IdosoId == idosoId)
            .Select(envio => envio.PerguntaId)
            .ToHashSet());
}

internal sealed class PortaRetratoRepositoryFake(BancoFake banco)
    : RepositorioFake<PortaRetrato>(banco.PortaRetratos), IPortaRetratoRepository
{
    public Task<PortaRetrato?> ObterPorChaveHashAsync(string chaveHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(portaRetrato => portaRetrato.ChaveHash == chaveHash));

    public Task<PortaRetrato?> ObterDoIdosoAsync(int idosoId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(portaRetrato => portaRetrato.IdosoId == idosoId));

    public Task<bool> CodigoEmUsoAsync(string codigo, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.Any(portaRetrato => portaRetrato.Codigo == codigo));
}

internal sealed class AvisoRepositoryFake(BancoFake banco) : RepositorioFake<AvisoQueroContar>(banco.Avisos), IAvisoRepository
{
    public Task<IReadOnlySet<int>> ObterIdsEnviosComAvisoAsync(int idosoId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlySet<int>>(Itens
            .Where(aviso => aviso.IdosoId == idosoId && aviso.EnvioId is not null)
            .Select(aviso => aviso.EnvioId!.Value)
            .ToHashSet());

    public Task<AvisoQueroContar?> ObterDoEnvioAsync(int envioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(aviso => aviso.EnvioId == envioId));

    public Task<AvisoQueroContar?> ObterUltimoSemEnvioAsync(
        int idosoId,
        DateTime desdeUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens
            .Where(aviso => aviso.IdosoId == idosoId && aviso.EnvioId is null && aviso.CriadoEm >= desdeUtc)
            .OrderByDescending(aviso => aviso.CriadoEm)
            .FirstOrDefault());

    public Task<IReadOnlyList<AvisoResponse>> ListarDaFamiliaAsync(
        int familiaId,
        bool somenteNaoVistos,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Consulta coberta pelos testes de integração.");
}

internal sealed class PaginaRepositoryFake(BancoFake banco) : RepositorioFake<PaginaCaderno>(banco.Paginas), IPaginaRepository
{
    public Task<PaginaCaderno?> ObterPorChaveAsync(Guid chaveIdempotencia, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(pagina => pagina.ChaveIdempotencia == chaveIdempotencia));

    public Task<PaginaCaderno?> ObterComEnvioAsync(int id, CancellationToken cancellationToken = default) =>
        ObterPorIdAsync(id, cancellationToken);

    public Task<(IReadOnlyList<PaginaCaderno> Itens, int Total)> ListarDaFamiliaAsync(
        int familiaId,
        int? idosoId,
        int? categoriaId,
        int pagina,
        int tamanho,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Consulta coberta pelos testes de integração.");
}

internal sealed class MemoriaDoBairroRepositoryFake(BancoFake banco)
    : RepositorioFake<MemoriaDoBairro>(banco.Memorias), IMemoriaDoBairroRepository
{
    public Task<MemoriaDoBairro?> ObterNoMapaDaPaginaAsync(int paginaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Itens.FirstOrDefault(memoria => memoria.PaginaId == paginaId && memoria.EstaNoMapa));

    public Task<MemoriaDoBairro?> ObterNoMapaComPaginaAsync(int id, CancellationToken cancellationToken = default)
    {
        MemoriaDoBairro? memoria = Itens.FirstOrDefault(item => item.Id == id && item.EstaNoMapa);
        PaginaCaderno? pagina = memoria is null ? null : banco.Paginas.FirstOrDefault(item => item.Id == memoria.PaginaId);
        return Task.FromResult(memoria is null || pagina is null
            ? memoria
            : BancoFake.ComNavegacao(memoria, nameof(MemoriaDoBairro.Pagina), pagina));
    }

    public Task<IReadOnlyList<MemoriaDoBairro>> ListarNoMapaDoBairroAsync(string bairro, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MemoriaDoBairro>>(Itens
            .Where(memoria => memoria.EstaNoMapa && string.Equals(memoria.Bairro, bairro, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(memoria => memoria.PublicadaEm)
            .ToList());
}

internal sealed class RegistroDePrivacidadeRepositoryFake(BancoFake banco)
    : RepositorioFake<RegistroDePrivacidade>(banco.Registros), IRegistroDePrivacidadeRepository
{
    public Task<IReadOnlyList<RegistroDePrivacidade>> ListarDesdeAsync(DateTime desdeUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RegistroDePrivacidade>>(Itens
            .Where(registro => registro.CriadoEm >= desdeUtc)
            .OrderBy(registro => registro.CriadoEm)
            .ToList());
}

/// <summary>Auditoria em memória: guarda as linhas que iriam para o log do servidor.</summary>
internal sealed class AuditoriaFake : IRegistroDeAuditoria
{
    public List<EventoDePrivacidade> Eventos { get; } = [];

    public void Registrar(EventoDePrivacidade evento) => Eventos.Add(evento);
}

/// <summary>Armazenamento em memória: guarda os bytes por caminho.</summary>
internal sealed class ArmazenamentoFake : IArmazenamentoDeArquivos
{
    public Dictionary<string, byte[]> Arquivos { get; } = [];

    public async Task<string> SalvarAsync(Stream conteudo, MeConta.Domain.ValueObjects.ArquivoDeMidia arquivo, CancellationToken cancellationToken = default)
    {
        using MemoryStream copia = new();
        await conteudo.CopyToAsync(copia, cancellationToken);
        string caminho = $"audios/{Guid.NewGuid():N}{arquivo.Extensao}";
        Arquivos[caminho] = copia.ToArray();
        return caminho;
    }

    public Task<Stream?> AbrirAsync(string caminho, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(Arquivos.TryGetValue(caminho, out byte[]? bytes) ? new MemoryStream(bytes) : null);

    public Task RemoverAsync(string caminho, CancellationToken cancellationToken = default)
    {
        Arquivos.Remove(caminho);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Controle de acesso falso: o membro logado pode acessar só os idosos da lista.
/// </summary>
internal sealed class ControleDeAcessoFake(BancoFake banco, Membro membro, params int[] idososPermitidos) : IControleDeAcesso
{
    public Task<Membro> ObterMembroAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult(membro);

    public Task<Membro> GarantirAcessoAFamiliaAsync(int familiaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(membro);

    public Task<AcessoAoIdoso> GarantirAcessoAoIdosoAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        Idoso idoso = banco.Idosos.FirstOrDefault(item => item.Id == idosoId)
            ?? throw new RecursoNaoEncontradoException("Idoso não encontrado.");

        return idososPermitidos.Contains(idosoId)
            ? Task.FromResult(new AcessoAoIdoso(membro, idoso))
            : throw new AcessoNegadoException(ControleDeAcesso.MensagemForaDaFamilia);
    }
}

/// <summary>Gerador com valores fixos, na ordem da fila.</summary>
internal sealed class GeradorDeCredenciaisFake(params string[] codigos) : IGeradorDeCredenciais
{
    private readonly Queue<string> _codigos = new(codigos);
    private int _chavesGeradas;

    public string GerarCodigo() => _codigos.Dequeue();

    public string GerarChave() => $"chave-de-teste-numero-{++_chavesGeradas:D4}-abcdefgh";
}

/// <summary>Relógio parado numa hora fixa, que o teste pode adiantar.</summary>
internal sealed class RelogioFake(DateTime agoraUtc) : TimeProvider
{
    private DateTimeOffset _agora = new(agoraUtc, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _agora;

    public void Avancar(TimeSpan tempo) => _agora = _agora.Add(tempo);
}
