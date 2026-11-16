using MeConta.Privacidade.Normalizacao;

namespace MeConta.UnitTests.Privacidade;

public sealed class NormalizadorDeTextoTests
{
    [Theory]
    [InlineData("Remédio para PRESSÃO", "remedio para pressao")]
    [InlineData("Avó, coração, Ação", "avo, coracao, acao")]
    [InlineData("１２３", "123")]
    [InlineData("12​3­4", "1234")]
    [InlineData("98765–4321", "98765-4321")]
    public void Normalizar_AcentosMaiusculasEInvisiveis_ViramTextoSimples(string texto, string esperado)
    {
        Assert.Equal(esperado, NormalizadorDeTexto.Normalizar(texto).Valor);
    }

    [Theory]
    [InlineData("um dois três quatro", "1234")]
    [InlineData("nove meia meia sete", "9667")]
    [InlineData("cento e vinte e três", "123")]
    [InlineData("quatrocentos e cinquenta e seis, setecentos e oitenta e nove", "456789")]
    [InlineData("sete oito nove traço zero nove", "78909")]
    [InlineData("vinte e um, dezoito, trezentos", "2118300")]
    public void Normalizar_NumerosFalados_ViramDigitos(string texto, string esperado)
    {
        Assert.Equal(esperado, NormalizadorDeTexto.Normalizar(texto).Valor);
    }

    [Theory]
    [InlineData("tenho dois filhos e três netos")]
    [InlineData("meia hora depois")]
    [InlineData("uma xícara e meia")]
    [InlineData("dois e três")]
    public void Normalizar_NumerosSoltosNaFrase_FicamComoPalavras(string texto)
    {
        Assert.Equal(texto.Replace("ê", "e", StringComparison.Ordinal).Replace("í", "i", StringComparison.Ordinal),
            NormalizadorDeTexto.Normalizar(texto).Valor);
    }

    [Fact]
    public void ParaOriginal_TrechoDeDigitosFalados_ApontaParaAFraseInteira()
    {
        string texto = "CPF: um dois três quatro";
        TextoNormalizado normalizado = NormalizadorDeTexto.Normalizar(texto);
        int inicio = normalizado.Valor.IndexOf("1234", StringComparison.Ordinal);

        Trecho trecho = normalizado.ParaOriginal(inicio, 4);

        Assert.Equal("um dois três quatro", texto.Substring(trecho.Inicio, trecho.Tamanho));
    }

    [Fact]
    public void ParaOriginal_TextoComCaracterInvisivel_ApontaParaAPosicaoCerta()
    {
        string texto = "x 12​34 y";
        TextoNormalizado normalizado = NormalizadorDeTexto.Normalizar(texto);

        Trecho trecho = normalizado.ParaOriginal(2, 4);

        Assert.Equal("12​34", texto.Substring(trecho.Inicio, trecho.Tamanho));
    }
}
