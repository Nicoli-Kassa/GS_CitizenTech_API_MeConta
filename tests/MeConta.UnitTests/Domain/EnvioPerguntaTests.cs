using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Domain;

public sealed class EnvioPerguntaTests
{
    private const string CaminhoAudio = "audios/resposta.opus";

    [Fact]
    public void Construtor_IdosoComWhatsApp_MontaMensagemComApelido()
    {
        EnvioPergunta envio = CriarEnvio();

        Assert.Equal("Vó Lourdes, me conta: Qual era o segredo do seu bolo de domingo?", envio.Mensagem);
        Assert.Equal("Qual era o segredo do seu bolo de domingo?", envio.TextoPergunta);
        Assert.Equal("5511987654321", envio.TelefoneDestino.Valor);
        Assert.Equal(StatusEnvio.Enviada, envio.Status);
        Assert.Equal(Agora, envio.EnviadoEm);
        Assert.Null(envio.RespondidoEm);
    }

    [Fact]
    public void ObterLinkWhatsApp_TextoComAcentosEEmoji_ChegaCodificado()
    {
        Idoso lourdes = CriarVoLourdes();
        EnvioPergunta envio = new(lourdes, CriarPerguntaDoCatalogo("Como era o São João na sua rua? 🎉"), BiaId, Agora);

        string link = envio.ObterLinkWhatsApp();

        Assert.StartsWith("https://wa.me/5511987654321?text=", link);
        Assert.Contains("S%C3%A3o%20Jo%C3%A3o", link);
        Assert.Contains("%F0%9F%8E%89", link);
        Assert.DoesNotContain(" ", link);

        // Decodificando, a mensagem volta exatamente ao original.
        string textoCodificado = link[(link.IndexOf("text=", StringComparison.Ordinal) + 5)..];
        Assert.Equal(envio.Mensagem, Uri.UnescapeDataString(textoCodificado));
    }

    [Fact]
    public void Construtor_IdosoSemWhatsApp_PedeCadastroDoNumero()
    {
        Familia familia = CriarFamilia();
        Idoso semCelular = familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", null, Agora);

        ConflitoException erro = Assert.Throws<ConflitoException>(
            () => new EnvioPergunta(semCelular, CriarPerguntaDoCatalogo(), BiaId, Agora));

        Assert.Equal(EnvioPergunta.MensagemSemWhatsApp, erro.Message);
    }

    [Fact]
    public void Construtor_PerguntaDeOutraFamilia_LancaExcecao()
    {
        Idoso lourdes = CriarVoLourdes();
        PerguntaDaFamilia deOutraFamilia = new(
            "Vó, como a senhora conheceu o vô?",
            categoriaId: 2,
            familiaId: 99,
            criadaPorMembroId: ClaudiaId,
            Agora,
            new FiltroFake(suspeito: false));

        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new EnvioPergunta(lourdes, deOutraFamilia, BiaId, Agora));

        Assert.Equal(EnvioPergunta.MensagemPerguntaIndisponivel, erro.Message);
    }

    [Fact]
    public void Responder_PrimeiraVez_CriaPaginaEMarcaRespondida()
    {
        EnvioPergunta envio = CriarEnvio();
        Guid chave = Guid.NewGuid();
        DateTime depois = Agora.AddHours(3);

        PaginaCaderno pagina = envio.Responder(
            "O bolo de fubá da Vó Lourdes",
            CaminhoAudio,
            duracaoSegundos: 95,
            fotoCaminho: "fotos/bolo.jpg",
            criadaPorMembroId: BiaId,
            chave,
            depois);

        Assert.Equal(StatusEnvio.Respondida, envio.Status);
        Assert.Equal(depois, envio.RespondidoEm);
        Assert.Same(pagina, envio.Pagina);
        Assert.Same(envio, pagina.Envio);
        Assert.Equal("O bolo de fubá da Vó Lourdes", pagina.Titulo);
        Assert.Equal(95, pagina.DuracaoSegundos);
        Assert.True(pagina.TemFoto);
        Assert.Equal(chave, pagina.ChaveIdempotencia);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Responder_TituloEmBranco_UsaOTextoDaPergunta(string? titulo)
    {
        EnvioPergunta envio = CriarEnvio();

        PaginaCaderno pagina = Responder(envio, titulo);

        Assert.Equal(envio.TextoPergunta, pagina.Titulo);
        Assert.False(pagina.TemFoto);
    }

    [Fact]
    public void Responder_SegundaVez_LancaConflito()
    {
        EnvioPergunta envio = CriarEnvio();
        Responder(envio);

        ConflitoException erro = Assert.Throws<ConflitoException>(() => Responder(envio));

        Assert.Equal(EnvioPergunta.MensagemJaRespondida, erro.Message);
    }

    [Fact]
    public void Responder_DuracaoZero_NaoMudaOStatus()
    {
        EnvioPergunta envio = CriarEnvio();

        Assert.Throws<RegraDeNegocioException>(
            () => envio.Responder(null, CaminhoAudio, 0, null, BiaId, Guid.NewGuid(), Agora));
        Assert.Equal(StatusEnvio.Enviada, envio.Status);
        Assert.Null(envio.Pagina);
    }

    [Fact]
    public void ReabrirAposExclusao_PaginaApagada_PermiteResponderDeNovo()
    {
        EnvioPergunta envio = CriarEnvio();
        Responder(envio);

        envio.ReabrirAposExclusao();
        PaginaCaderno novaPagina = Responder(envio);

        Assert.Equal(StatusEnvio.Respondida, envio.Status);
        Assert.Same(novaPagina, envio.Pagina);
    }

    [Fact]
    public void PodeSerApagadaPor_SoQuemCriou_RetornaVerdadeiro()
    {
        PaginaCaderno pagina = Responder(CriarEnvio());

        Assert.True(pagina.PodeSerApagadaPor(BiaId));
        Assert.False(pagina.PodeSerApagadaPor(ClaudiaId));
    }

    private static EnvioPergunta CriarEnvio() =>
        new(CriarVoLourdes(), CriarPerguntaDoCatalogo(), BiaId, Agora);

    private static PaginaCaderno Responder(EnvioPergunta envio, string? titulo = null) =>
        envio.Responder(titulo, CaminhoAudio, 60, null, BiaId, Guid.NewGuid(), Agora);
}
