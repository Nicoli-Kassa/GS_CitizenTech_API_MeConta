using MeConta.Domain.Regras;

namespace MeConta.UnitTests.Regras;

public sealed class FiltroAntiGolpeTests
{
    private readonly FiltroAntiGolpe _filtro = new();

    [Theory]
    [InlineData("Vó, me manda um PIX de 50 reais?")]
    [InlineData("Qual a sua senha do banco?")]
    [InlineData("Me passa o código que chegou no seu celular")]
    [InlineData("Qual é o CÓDIGO DE VERIFICAÇÃO?")]
    [InlineData("Vó, a senhora pode me emprestar dinheiro?")]
    [InlineData("Faz uma transferência pra mim?")]
    [InlineData("Deposita um dinheiro na minha conta")]
    [InlineData("Manda a foto do seu cartão")]
    public void EhSuspeito_PedidoDeDinheiroOuDeAcesso_RetornaVerdadeiro(string texto)
    {
        Assert.True(_filtro.EhSuspeito(texto));
    }

    [Theory]
    [InlineData("Qual era o segredo do seu bolo de domingo?")]
    [InlineData("Como a senhora juntava dinheiro quando era jovem?")]
    [InlineData("O que a senhora fazia quando faltava dinheiro em casa?")]
    [InlineData("Como se passava o tempo antes da televisão?")]
    [InlineData("Qual foi o melhor conselho que a senhora já recebeu?")]
    [InlineData("")]
    public void EhSuspeito_PerguntaDeMemoria_RetornaFalso(string texto)
    {
        Assert.False(_filtro.EhSuspeito(texto));
    }
}
