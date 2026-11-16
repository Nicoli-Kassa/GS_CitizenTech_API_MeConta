using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Taint;

namespace MeConta.UnitTests.Privacidade;

public sealed class TextosRastreadosTests
{
    private readonly MotorPii _motor = MotorPii.CriarPadrao();

    [Fact]
    public void TextoDoUsuario_EscritoNumLogPorEngano_NaoMostraOConteudo()
    {
        TextoDoUsuario texto = new("meu CPF é 123.456.789-09");

        string linhaDeLog = $"Página recebida: {texto}";

        Assert.DoesNotContain("123", linhaDeLog);
        Assert.Contains("não verificado", linhaDeLog);
    }

    [Fact]
    public void Verificar_TextoComCpf_ParaRegistroSaiMascaradoEPublicacaoEhRecusada()
    {
        TextoVerificado verificado = _motor.Verificar(new TextoDoUsuario("meu CPF é 123.456.789-09"));

        Assert.Equal("meu CPF é [CPF removido]", verificado.ParaRegistro);
        Assert.Equal(verificado.ParaRegistro, verificado.ToString());
        Assert.False(verificado.PodePublicar(familiaConfirmou: true));

        PublicacaoBloqueadaException erro =
            Assert.Throws<PublicacaoBloqueadaException>(() => verificado.ConteudoParaPublicar(familiaConfirmou: true));
        Assert.True(erro.Resultado.DeveBloquear);
        Assert.DoesNotContain("123", erro.Message);
    }

    [Fact]
    public void Verificar_TextoComDadoDeSaude_SoPublicaComConfirmacaoDaFamilia()
    {
        const string original = "tomo remédio para pressão todo dia";
        TextoVerificado verificado = _motor.Verificar(new TextoDoUsuario(original));

        Assert.Throws<PublicacaoBloqueadaException>(() => verificado.ConteudoParaPublicar(familiaConfirmou: false));
        Assert.Equal(original, verificado.ConteudoParaPublicar(familiaConfirmou: true));
    }

    [Fact]
    public void Verificar_TextoSemDadoPessoal_PublicaOOriginal()
    {
        const string original = "O segredo do bolo era bater as claras em neve.";
        TextoVerificado verificado = _motor.Verificar(new TextoDoUsuario(original));

        Assert.True(verificado.PodePublicar(familiaConfirmou: false));
        Assert.Equal(original, verificado.ConteudoParaPublicar(familiaConfirmou: false));
    }
}
