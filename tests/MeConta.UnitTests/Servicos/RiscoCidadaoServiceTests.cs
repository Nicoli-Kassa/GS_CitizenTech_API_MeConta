using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Excecoes;
using MeConta.Privacidade.Motor;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Servicos;

/// <summary>
/// Painel "Risco Cidadão" calculado a partir do histórico real do filtro.
/// </summary>
public sealed class RiscoCidadaoServiceTests
{
    private readonly BancoFake _banco = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly AuditoriaFake _auditoria = new();
    private readonly Idoso _lourdes;
    private readonly PrivacidadeService _privacidade;
    private readonly RiscoCidadaoService _painel;
    private int _proximoEnvio = 10;

    public RiscoCidadaoServiceTests()
    {
        Membro bia = BancoFake.ComId(new Membro("usuario-bia", "Bia Souza", Agora), BiaId);
        _banco.Membros.Add(bia);
        _lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        _banco.Idosos.Add(_lourdes);

        MotorPii motor = MotorPii.CriarPadrao();
        _privacidade = new PrivacidadeService(
            new ControleDeAcessoFake(_banco, bia, 1),
            new PaginaRepositoryFake(_banco),
            new MemoriaDoBairroRepositoryFake(_banco),
            new RegistroDePrivacidadeRepositoryFake(_banco),
            new ArmazenamentoFake(),
            motor,
            new UnidadeDeTrabalhoFake(_banco),
            _auditoria,
            _relogio);
        _painel = new RiscoCidadaoService(new RegistroDePrivacidadeRepositoryFake(_banco), motor, _relogio);
    }

    private PaginaCaderno CriarPagina(string titulo, string? transcricao = null)
    {
        EnvioPergunta envio = BancoFake.ComId(new EnvioPergunta(_lourdes, CriarPerguntaDoCatalogo(), BiaId, Agora), _proximoEnvio++);
        BancoFake.ComNavegacao(envio, nameof(EnvioPergunta.Idoso), _lourdes);
        PaginaCaderno pagina = BancoFake.ComId(
            envio.Responder(titulo, "audios/x.opus", 30, null, BiaId, Guid.NewGuid(), Agora),
            envio.Id + 100);
        pagina.DefinirTranscricao(transcricao);
        _banco.Paginas.Add(pagina);
        return pagina;
    }

    private async Task PublicarAsync(PaginaCaderno pagina, string bairro = "Pirituba", bool confirmo = false)
    {
        try
        {
            await _privacidade.PublicarAsync(pagina.Id, new PublicarMemoriaRequest(bairro, true, confirmo));
        }
        catch (PublicacaoRecusadaException)
        {
            // A recusa também entra no histórico; o teste só olha o painel.
        }
    }

    [Fact]
    public async Task Obter_SemHistorico_IndiceZeroEUmDiaPorItemDoPeriodo()
    {
        RiscoCidadaoResponse painel = await _painel.ObterAsync();

        Assert.Equal(0, painel.IndiceRiscoCidadao);
        Assert.Equal("Baixo", painel.Faixa);
        Assert.Equal(0, painel.Totais.Verificacoes);
        Assert.Equal(RiscoCidadaoService.PeriodoPadraoDias, painel.PorDia.Count);
        Assert.Equal(new DateOnly(2026, 11, 10), painel.PorDia[^1].Dia);
        Assert.Empty(painel.UltimosAlertas);
        Assert.False(string.IsNullOrWhiteSpace(painel.VersaoDasRegras));
    }

    [Fact]
    public async Task Obter_PublicacoesEBloqueios_TotaisEVazamentosEvitados()
    {
        await PublicarAsync(CriarPagina("O bolo de domingo"));
        await PublicarAsync(CriarPagina("O primeiro emprego", "meu CPF é 123.456.789-09 e a senha do banco é 4521"));
        await PublicarAsync(CriarPagina("Os remédios", "tomo remédio para pressão"), "Lapa");
        await _privacidade.AnalisarPaginaAsync(CriarPagina("Liga pra mim", "(11) 98765-4321").Id);

        RiscoCidadaoResponse painel = await _painel.ObterAsync();

        Assert.Equal(4, painel.Totais.Verificacoes);
        Assert.Equal(1, painel.Totais.Publicadas);
        Assert.Equal(1, painel.Totais.Bloqueadas);
        Assert.Equal(2, painel.Totais.PrecisaramConfirmar);
        Assert.Equal(1, painel.Totais.SemRisco);
        // CPF e senha; "remédios" no título e "remédio para pressão" na transcrição; telefone.
        Assert.Equal(5, painel.Totais.DadosEncontrados);
        Assert.Equal(2, painel.Totais.VazamentosEvitados);
        Assert.InRange(painel.IndiceRiscoCidadao, 1, 100);
    }

    [Fact]
    public async Task Obter_PorTipoPorBairroEAlertas_SoComTrechosMascarados()
    {
        await PublicarAsync(CriarPagina("Primeiro", "CPF 123.456.789-09"));
        await PublicarAsync(CriarPagina("Segundo", "CPF 526.018.159-06"));
        await PublicarAsync(CriarPagina("Terceiro", "tomo remédio para pressão"), "Lapa");

        RiscoCidadaoResponse painel = await _painel.ObterAsync();

        RiscoPorTipo maisComum = painel.PorTipo[0];
        Assert.Equal("Cpf", maisComum.Tipo);
        Assert.Equal(2, maisComum.Quantidade);
        RiscoPorBairro pirituba = Assert.Single(painel.PorBairro, bairro => bairro.Bairro == "Pirituba");
        Assert.Equal(2, pirituba.Tentativas);
        Assert.Equal(2, pirituba.Bloqueadas);
        Assert.Equal(3, painel.UltimosAlertas.Count);
        Assert.Equal("Dado de saúde", painel.UltimosAlertas[0].Rotulo);
        Assert.All(painel.UltimosAlertas, alerta => Assert.DoesNotContain("456.789", alerta.TrechoMascarado));
    }

    [Fact]
    public async Task Obter_VerificacaoDeMadrugadaEmUtc_ContaNoDiaDeBrasilia()
    {
        // 11/11, 01h UTC = 10/11, 22h em Brasília.
        _relogio.Avancar(TimeSpan.FromHours(10));
        await _privacidade.AnalisarPaginaAsync(CriarPagina("O bolo", "CPF 123.456.789-09").Id);

        RiscoCidadaoResponse painel = await _painel.ObterAsync(dias: 2);

        Assert.Equal(new DateOnly(2026, 11, 10), painel.PorDia[^1].Dia);
        Assert.Equal(1, painel.PorDia[^1].Verificacoes);
        Assert.Equal(1, painel.PorDia[^1].Bloqueadas);
    }

    [Fact]
    public async Task Obter_RegistroForaDoPeriodo_NaoEntra()
    {
        await _privacidade.AnalisarPaginaAsync(CriarPagina("Antigo", "CPF 123.456.789-09").Id);
        _relogio.Avancar(TimeSpan.FromDays(20));

        RiscoCidadaoResponse painel = await _painel.ObterAsync(dias: 14);

        Assert.Equal(0, painel.Totais.Verificacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public async Task Obter_PeriodoForaDoLimite_Recusa(int dias)
    {
        await Assert.ThrowsAsync<RegraDeNegocioException>(() => _painel.ObterAsync(dias));
    }
}
