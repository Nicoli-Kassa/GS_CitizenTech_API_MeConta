using MeConta.Privacidade.Validadores;

namespace MeConta.UnitTests.Privacidade;

public sealed class ValidadoresTests
{
    [Theory]
    [InlineData("123.456.789-09")]
    [InlineData("12345678909")]
    [InlineData("083.016.613-05")]
    [InlineData("52601815906")]
    public void Cpf_DigitoVerificadorCerto_EhValido(string cpf)
    {
        Assert.True(new ValidadorCpf().EhValido(cpf));
    }

    [Theory]
    [InlineData("123.456.789-00")]
    [InlineData("111.111.111-11")]
    [InlineData("1234567890")]
    [InlineData("123456789091")]
    public void Cpf_DigitoErradoRepetidoOuTamanhoErrado_NaoEhValido(string cpf)
    {
        Assert.False(new ValidadorCpf().EhValido(cpf));
    }

    [Theory]
    [InlineData("4111 1111 1111 1111", true)]
    [InlineData("5555-5555-5555-4444", true)]
    [InlineData("4111 1111 1111 1112", false)]
    [InlineData("0000 0000 0000 0000", false)]
    [InlineData("4111 1111 111", false)]
    public void Cartao_AlgoritmoDeLuhn(string numero, bool esperado)
    {
        Assert.Equal(esperado, new ValidadorCartao().EhValido(numero));
    }

    [Theory]
    [InlineData("(11) 98765-4321", true)]
    [InlineData("+55 11 98765-4321", true)]
    [InlineData("(11) 3901-2345", true)]
    [InlineData("(20) 98765-4321", false)]
    [InlineData("(11) 88765-4321", false)]
    [InlineData("(11) 9901-2345", false)]
    [InlineData("44 11 98765-4321", false)]
    public void Telefone_DddETipoDeNumero(string numero, bool esperado)
    {
        Assert.Equal(esperado, new ValidadorTelefone().EhValido(numero));
    }

    [Fact]
    public void Registro_Padrao_TemOsTresValidadores()
    {
        RegistroDeValidadores registro = RegistroDeValidadores.Padrao();

        Assert.True(registro.TentarObter("cpf", out _));
        Assert.True(registro.TentarObter("CARTAO", out _));
        Assert.True(registro.TentarObter("telefone", out _));
        Assert.False(registro.TentarObter("placa", out _));
    }
}
