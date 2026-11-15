using MeConta.Domain.Entidades;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;
using MeConta.UnitTests.Servicos;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Domain;

public sealed class PortaRetratoTests
{
    private const string Codigo = "k7m2q9xd";
    private const string Chave = "demo-porta-retrato-vo-lourdes-2026";

    [Fact]
    public void Construtor_DadosValidos_GuardaSoOHashDaChave()
    {
        PortaRetrato portaRetrato = CriarPortaRetrato();

        Assert.Equal(Codigo, portaRetrato.Codigo);
        Assert.Equal(64, portaRetrato.ChaveHash.Length);
        Assert.DoesNotContain(Chave, portaRetrato.ChaveHash);
        Assert.Equal(PortaRetrato.CalcularHash(Chave), portaRetrato.ChaveHash);
        Assert.Equal("meconta/k7m2q9xd", portaRetrato.TopicoBase);
        Assert.False(portaRetrato.Online);
        Assert.Null(portaRetrato.UltimoContatoEm);
    }

    [Fact]
    public void Construtor_CodigoEmMaiusculas_GuardaEmMinusculas()
    {
        PortaRetrato portaRetrato = new(CriarVoLourdes(), " K7M2Q9XD ", Chave, BiaId, Agora);

        Assert.Equal(Codigo, portaRetrato.Codigo);
    }

    [Theory]
    [InlineData("k7m2q9x")]
    [InlineData("k7m2q9xdz")]
    [InlineData("k7m2q9x0")]
    [InlineData("k7m2-9xd")]
    [InlineData("")]
    public void Construtor_CodigoInvalido_LancaExcecao(string codigo)
    {
        Assert.Throws<ArgumentException>(() => new PortaRetrato(CriarVoLourdes(), codigo, Chave, BiaId, Agora));
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta-demais")]
    public void Construtor_ChaveCurta_LancaExcecao(string chave)
    {
        Assert.Throws<ArgumentException>(() => new PortaRetrato(CriarVoLourdes(), Codigo, chave, BiaId, Agora));
    }

    [Fact]
    public void ConfereChave_ChaveCerta_Aceita()
    {
        Assert.True(CriarPortaRetrato().ConfereChave(Chave));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("demo-porta-retrato-vo-lourdes-2025")]
    public void ConfereChave_ChaveErradaOuVazia_Recusa(string? chave)
    {
        Assert.False(CriarPortaRetrato().ConfereChave(chave));
    }

    [Fact]
    public void TrocarCredenciais_ChaveAntiga_ParaDeFuncionar()
    {
        PortaRetrato portaRetrato = CriarPortaRetrato();
        portaRetrato.RegistrarConexao(online: true, Agora);

        portaRetrato.TrocarCredenciais("abcd2345", "outra-chave-bem-comprida-para-teste", ClaudiaId, Agora.AddDays(1));

        Assert.False(portaRetrato.ConfereChave(Chave));
        Assert.True(portaRetrato.ConfereChave("outra-chave-bem-comprida-para-teste"));
        Assert.Equal("abcd2345", portaRetrato.Codigo);
        Assert.Equal(ClaudiaId, portaRetrato.CriadoPorMembroId);
        Assert.False(portaRetrato.Online);
        Assert.Null(portaRetrato.UltimoContatoEm);
    }

    [Fact]
    public void EstaConectado_OnlineHaPouco_Conectado()
    {
        PortaRetrato portaRetrato = CriarPortaRetrato();
        portaRetrato.RegistrarConexao(online: true, Agora);

        Assert.True(portaRetrato.EstaConectado(Agora.AddSeconds(90)));
    }

    [Fact]
    public void EstaConectado_SemSinalDeVidaHaMaisDe90Segundos_Desconectado()
    {
        PortaRetrato portaRetrato = CriarPortaRetrato();
        portaRetrato.RegistrarConexao(online: true, Agora);

        Assert.False(portaRetrato.EstaConectado(Agora.AddSeconds(91)));
    }

    [Fact]
    public void EstaConectado_PonteAvisouOffline_Desconectado()
    {
        PortaRetrato portaRetrato = CriarPortaRetrato();
        portaRetrato.RegistrarConexao(online: false, Agora);

        Assert.False(portaRetrato.EstaConectado(Agora));
    }

    [Fact]
    public void AvisoQueroContar_EnvioDeOutroIdoso_LancaExcecao()
    {
        Familia familia = CriarFamilia();
        Idoso lourdes = BancoFake.ComId(CriarVoLourdes(familia), 1);
        Idoso tuca = BancoFake.ComId(
            familia.CadastrarIdoso("Antônia Ribeiro", "Vó Tuca", new Telefone("(11) 98888-0001"), Agora),
            2);
        PortaRetrato daLourdes = new(lourdes, Codigo, Chave, BiaId, Agora);
        EnvioPergunta paraTuca = new(tuca, new PerguntaFake(7), BiaId, Agora);

        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new AvisoQueroContar(daLourdes, paraTuca, Agora));

        Assert.Equal(AvisoQueroContar.MensagemEnvioDeOutroIdoso, erro.Message);
    }

    [Fact]
    public void AvisoQueroContar_MarcarVistoDuasVezes_GuardaQuemViuPrimeiro()
    {
        AvisoQueroContar aviso = new(CriarPortaRetrato(), envio: null, Agora);

        aviso.MarcarVisto(ClaudiaId, Agora.AddMinutes(5));
        aviso.MarcarVisto(BiaId, Agora.AddMinutes(9));

        Assert.True(aviso.FoiVisto);
        Assert.Equal(ClaudiaId, aviso.VistoPorMembroId);
        Assert.Equal(Agora.AddMinutes(5), aviso.VistoEm);
        Assert.Null(aviso.EnvioId);
    }

    private static PortaRetrato CriarPortaRetrato() =>
        new(BancoFake.ComId(CriarVoLourdes(), 1), Codigo, Chave, BiaId, Agora);
}
