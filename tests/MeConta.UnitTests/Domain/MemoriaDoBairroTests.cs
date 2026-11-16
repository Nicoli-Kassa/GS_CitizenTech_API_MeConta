using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.UnitTests.Servicos;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Domain;

public sealed class MemoriaDoBairroTests
{
    private readonly PaginaCaderno _pagina;

    public MemoriaDoBairroTests()
    {
        Idoso lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        EnvioPergunta envio = BancoFake.ComId(new EnvioPergunta(lourdes, CriarPerguntaDoCatalogo(), BiaId, Agora), 10);
        BancoFake.ComNavegacao(envio, nameof(EnvioPergunta.Idoso), lourdes);
        _pagina = BancoFake.ComId(envio.Responder("O bolo de domingo", "audios/bolo.opus", 30, null, BiaId, Guid.NewGuid(), Agora), 20);
    }

    private MemoriaDoBairro Publicar(string? bairro = "Pirituba", bool autorizou = true) =>
        new(_pagina, bairro, autorizou, _pagina.Titulo, "Três ovos caipiras.", 0, BiaId, Agora);

    [Fact]
    public void Criar_ComAutorizacao_GuardaApelidoEBairroSemNomeCompleto()
    {
        MemoriaDoBairro memoria = Publicar(" Pirituba ");

        Assert.Equal("Pirituba", memoria.Bairro);
        Assert.Equal("Vó Lourdes", memoria.Apelido);
        Assert.Equal(20, memoria.PaginaId);
        Assert.True(memoria.AutorizadaPeloIdoso);
        Assert.True(memoria.EstaNoMapa);
    }

    [Fact]
    public void Criar_SemAutorizacaoDoIdoso_Recusa()
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(() => Publicar(autorizou: false));

        Assert.Equal("Marque que Vó Lourdes autorizou a publicação no mapa do bairro", erro.Message);
    }

    [Theory]
    [InlineData(null, MemoriaDoBairro.MensagemBairroVazio)]
    [InlineData("  ", MemoriaDoBairro.MensagemBairroVazio)]
    [InlineData("Um bairro com um nome comprido demais para caber no mapa do Me Conta", MemoriaDoBairro.MensagemBairroLongo)]
    public void Criar_BairroVazioOuLongo_Recusa(string? bairro, string mensagem)
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(() => Publicar(bairro));

        Assert.Equal(mensagem, erro.Message);
    }

    [Fact]
    public void TirarDoMapa_DuasVezes_GuardaQuemTirouPrimeiro()
    {
        MemoriaDoBairro memoria = Publicar();

        memoria.TirarDoMapa(ClaudiaId, Agora.AddHours(1));
        memoria.TirarDoMapa(BiaId, Agora.AddHours(2));

        Assert.False(memoria.EstaNoMapa);
        Assert.Equal(ClaudiaId, memoria.RemovidaPorMembroId);
        Assert.Equal(Agora.AddHours(1), memoria.RemovidaEm);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  Três ovos.  ", "Três ovos.")]
    public void DefinirTranscricao_TextoEmBrancoApagaETextoNormalSemEspacosNasPontas(string? texto, string? esperado)
    {
        _pagina.DefinirTranscricao("Algo antes");

        _pagina.DefinirTranscricao(texto);

        Assert.Equal(esperado, _pagina.Transcricao);
    }

    [Fact]
    public void DefinirTranscricao_AcimaDoLimite_Recusa()
    {
        string longa = new('a', PaginaCaderno.TamanhoMaximoTranscricao + 1);

        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(() => _pagina.DefinirTranscricao(longa));

        Assert.Equal("Use até 4000 caracteres na transcrição", erro.Message);
    }

    [Fact]
    public void RegistroDePrivacidade_TrechoGrande_CortadoNoLimiteEBairroSemEspacos()
    {
        AchadoRegistrado achado = new("PII-END-001", "Endereco", "Endereço", "Medio", "Transcricao", new string('*', 250));

        RegistroDePrivacidade registro = new(
            20, 1, 1, BiaId, OrigemDaVerificacao.Publicacao, ResultadoDaVerificacao.PrecisaConfirmar,
            " Pirituba ", 35, true, "1.0.0", [achado], Agora);

        Assert.Equal(AchadoRegistrado.TamanhoMaximoTrecho, achado.TrechoMascarado.Length);
        Assert.Equal("Pirituba", registro.Bairro);
        Assert.Single(registro.Achados);
    }
}
