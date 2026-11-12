using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;

namespace MeConta.UnitTests.Domain;

public sealed class TelefoneTests
{
    [Theory]
    [InlineData("(11) 98765-4321", "5511987654321")]
    [InlineData("11987654321", "5511987654321")]
    [InlineData("+55 11 98765-4321", "5511987654321")]
    [InlineData("5511987654321", "5511987654321")]
    [InlineData("(11) 3456-7890", "551134567890")]
    [InlineData("(55) 99123-4567", "5555991234567")]
    public void Construtor_FormatosAceitos_GuardaNoFormatoInternacional(string digitado, string esperado)
    {
        Telefone telefone = new(digitado);

        Assert.Equal(esperado, telefone.Valor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("98765-4321")]
    [InlineData("123456789")]
    [InlineData("(01) 98765-4321")]
    [InlineData("44 11 98765-4321")]
    [InlineData("abc")]
    public void Construtor_NumeroInvalido_LancaMensagemDoApp(string digitado)
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(() => new Telefone(digitado));

        Assert.Equal(Telefone.MensagemInvalido, erro.Message);
    }

    [Fact]
    public void Igualdade_MesmoNumeroDigitadoDiferente_SaoIguais()
    {
        Telefone primeiro = new("(11) 98765-4321");
        Telefone segundo = new("+55 11 987654321");

        Assert.Equal(primeiro, segundo);
    }
}
