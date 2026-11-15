using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Entidades;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Servicos;

public sealed class PortaRetratoServiceTests
{
    private readonly BancoFake _banco = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly Idoso _lourdes;
    private readonly Membro _bia;

    public PortaRetratoServiceTests()
    {
        _bia = BancoFake.ComId(new Membro("usuario-bia", "Bia", Agora), BiaId);
        _banco.Membros.Add(_bia);
        _lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        _banco.Idosos.Add(_lourdes);
    }

    [Fact]
    public async Task Parear_PrimeiraVez_DevolveAChaveEGuardaSoOHash()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake("k7m2q9xd"));

        PortaRetratoPareadoResponse pareado = await servico.ParearAsync(_lourdes.Id);

        PortaRetrato guardado = Assert.Single(_banco.PortaRetratos);
        Assert.Equal("k7m2q9xd", pareado.Codigo);
        Assert.Equal("meconta/k7m2q9xd", pareado.TopicoBase);
        Assert.Equal(_lourdes.Id, pareado.IdosoId);
        Assert.NotEqual(pareado.Chave, guardado.ChaveHash);
        Assert.True(guardado.ConfereChave(pareado.Chave));
        Assert.Equal(BiaId, guardado.CriadoPorMembroId);
    }

    [Fact]
    public async Task Parear_DeNovo_ChaveAntigaParaDeFuncionarNoAparelho()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake("k7m2q9xd", "abcd2345"));
        PortaRetratoPareadoResponse primeiro = await servico.ParearAsync(_lourdes.Id);

        PortaRetratoPareadoResponse segundo = await servico.ParearAsync(_lourdes.Id);

        Assert.Single(_banco.PortaRetratos);
        Assert.Equal("abcd2345", segundo.Codigo);
        AparelhoPortaRetratoService aparelho = CriarAparelho();
        await Assert.ThrowsAsync<CredencialInvalidaException>(() => aparelho.ObterEstadoAsync(primeiro.Chave));
        EstadoPortaRetratoResponse estado = await aparelho.ObterEstadoAsync(segundo.Chave);
        Assert.Equal("abcd2345", estado.Codigo);
    }

    [Fact]
    public async Task Parear_CodigoSorteadoJaEmUso_SorteiaOutro()
    {
        Familia familia = CriarFamilia();
        Idoso outro = BancoFake.ComId(
            familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", telefone: null, Agora),
            2);
        _banco.Idosos.Add(outro);
        PortaRetratoService servico = CriarServico(
            new GeradorDeCredenciaisFake("k7m2q9xd", "k7m2q9xd", "zz22xx33"),
            idososPermitidos: [1, 2]);
        await servico.ParearAsync(_lourdes.Id);

        PortaRetratoPareadoResponse doOutro = await servico.ParearAsync(outro.Id);

        Assert.Equal("zz22xx33", doOutro.Codigo);
    }

    [Fact]
    public async Task Parear_IdosoDeOutraFamilia_Recusa()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake("k7m2q9xd"), idososPermitidos: []);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.ParearAsync(_lourdes.Id));

        Assert.Empty(_banco.PortaRetratos);
    }

    [Fact]
    public async Task Obter_SemPortaRetrato_Lanca404()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake());

        RecursoNaoEncontradoException erro = await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => servico.ObterAsync(_lourdes.Id));

        Assert.Equal(PortaRetratoService.MensagemSemPortaRetrato, erro.Message);
    }

    [Fact]
    public async Task Obter_DepoisDoSinalDeVida_ApareceConectado()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake("k7m2q9xd"));
        PortaRetratoPareadoResponse pareado = await servico.ParearAsync(_lourdes.Id);
        await CriarAparelho().RegistrarConexaoAsync(pareado.Chave, new ConexaoPortaRetratoRequest(Online: true));

        PortaRetratoResponse agora = await servico.ObterAsync(_lourdes.Id);
        _relogio.Avancar(TimeSpan.FromMinutes(2));
        PortaRetratoResponse semSinal = await servico.ObterAsync(_lourdes.Id);

        Assert.True(agora.Conectado);
        Assert.False(semSinal.Conectado);
    }

    [Fact]
    public async Task Desparear_RemoveOPortaRetrato()
    {
        PortaRetratoService servico = CriarServico(new GeradorDeCredenciaisFake("k7m2q9xd"));
        await servico.ParearAsync(_lourdes.Id);

        await servico.DesparearAsync(_lourdes.Id);

        Assert.Empty(_banco.PortaRetratos);
    }

    private PortaRetratoService CriarServico(GeradorDeCredenciaisFake gerador, int[]? idososPermitidos = null) =>
        new(
            new ControleDeAcessoFake(_banco, _bia, idososPermitidos ?? [1]),
            new PortaRetratoRepositoryFake(_banco),
            gerador,
            new UnidadeDeTrabalhoFake(_banco),
            _relogio);

    private AparelhoPortaRetratoService CriarAparelho() =>
        new(
            new PortaRetratoRepositoryFake(_banco),
            new IdosoRepositoryFake(_banco),
            new EnvioRepositoryFake(_banco),
            new AvisoRepositoryFake(_banco),
            new MembroRepositoryFake(_banco),
            new UnidadeDeTrabalhoFake(_banco),
            _relogio);
}
