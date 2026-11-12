using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;

namespace MeConta.UnitTests.Domain;

public sealed class ArquivoDeMidiaTests
{
    private const long UmMega = 1024 * 1024;

    [Theory]
    [InlineData("resposta.mp3", "audio/mpeg")]
    [InlineData("resposta.m4a", "audio/mp4")]
    [InlineData("resposta.ogg", "audio/ogg")]
    [InlineData("PTT-20261110-WA0003.opus", "audio/ogg")]
    [InlineData("RESPOSTA.OPUS", "audio/ogg")]
    public void Audio_FormatoAceito_GuardaExtensaoETipo(string nome, string contentType)
    {
        ArquivoDeAudio audio = new(nome, UmMega);

        Assert.Equal(contentType, audio.ContentType);
        Assert.Equal(Path.GetExtension(nome).ToLowerInvariant(), audio.Extensao);
        Assert.Equal("audios", audio.Pasta);
    }

    [Theory]
    [InlineData("documento.pdf")]
    [InlineData("foto.jpg")]
    [InlineData("sem-extensao")]
    [InlineData("")]
    public void Audio_ArquivoQueNaoEhAudio_PedeAudio(string nome)
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new ArquivoDeAudio(nome, UmMega));

        Assert.Equal("Escolha um arquivo de áudio (mp3, m4a, ogg ou opus)", erro.Message);
    }

    [Fact]
    public void Audio_Exatamente20MB_EhAceito()
    {
        ArquivoDeAudio audio = new("resposta.opus", 20 * UmMega);

        Assert.Equal(ArquivoDeAudio.LimiteBytes, audio.TamanhoBytes);
    }

    [Fact]
    public void Audio_AcimaDe20MB_LancaArquivoMuitoGrande()
    {
        ArquivoMuitoGrandeException erro = Assert.Throws<ArquivoMuitoGrandeException>(
            () => new ArquivoDeAudio("resposta.opus", (20 * UmMega) + 1));

        Assert.Equal("Áudio muito grande: o limite é 20 MB", erro.Message);
    }

    [Fact]
    public void Audio_ArquivoVazio_LancaExcecao()
    {
        Assert.Throws<RegraDeNegocioException>(() => new ArquivoDeAudio("resposta.opus", 0));
    }

    [Theory]
    [InlineData("bolo.jpg")]
    [InlineData("bolo.JPEG")]
    [InlineData("bolo.png")]
    [InlineData("bolo.heic")]
    public void Foto_FormatoAceito_VaiParaPastaDeFotos(string nome)
    {
        ArquivoDeFoto foto = new(nome, UmMega);

        Assert.Equal("fotos", foto.Pasta);
    }

    [Fact]
    public void Foto_AcimaDe10MB_LancaArquivoMuitoGrande()
    {
        Assert.Throws<ArquivoMuitoGrandeException>(() => new ArquivoDeFoto("bolo.jpg", (10 * UmMega) + 1));
    }

    [Fact]
    public void Polimorfismo_MesmoArquivo_CadaTipoAplicaASuaRegra()
    {
        // Um .opus é aceito como áudio, mas não como foto.
        Assert.Equal("audios", new ArquivoDeAudio("resposta.opus", UmMega).Pasta);
        Assert.Throws<RegraDeNegocioException>(() => new ArquivoDeFoto("resposta.opus", UmMega));
    }
}
