using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;

namespace MeConta.UnitTests.Privacidade;

public sealed class CalculadoraDeRiscoCidadaoTests
{
    private static AchadoPii Achado(NivelDeRisco nivel, double confianca = 0.9) =>
        new("R", "Tipo", "Rótulo", nivel, 0, 1, "*", confianca, false, "explicação");

    [Fact]
    public void Calcular_SemAchados_Zero()
    {
        Assert.Equal(0, CalculadoraDeRiscoCidadao.Calcular([], []));
    }

    [Fact]
    public void Calcular_UmAchadoAlto_PeloMenos70()
    {
        int indice = CalculadoraDeRiscoCidadao.Calcular([Achado(NivelDeRisco.Alto, 0.75)], []);

        Assert.Equal(70, indice);
        Assert.Equal(NivelDeRisco.Alto, CalculadoraDeRiscoCidadao.FaixaDe(indice));
    }

    [Fact]
    public void Calcular_UmAchadoMedio_FicaNaFaixaMedia()
    {
        int indice = CalculadoraDeRiscoCidadao.Calcular([Achado(NivelDeRisco.Medio)], []);

        Assert.InRange(indice, CalculadoraDeRiscoCidadao.InicioDaFaixaMedia, CalculadoraDeRiscoCidadao.InicioDaFaixaAlta - 1);
    }

    [Fact]
    public void Calcular_DoisAchadosMedios_PesamMaisQueUm()
    {
        int um = CalculadoraDeRiscoCidadao.Calcular([Achado(NivelDeRisco.Medio)], []);
        int dois = CalculadoraDeRiscoCidadao.Calcular([Achado(NivelDeRisco.Medio), Achado(NivelDeRisco.Medio)], []);

        Assert.True(dois > um);
    }

    [Fact]
    public void Calcular_MuitosAchadosEBonus_NuncaPassaDe100()
    {
        AchadoPii[] achados = Enumerable.Repeat(Achado(NivelDeRisco.Alto, 0.99), 10).ToArray();
        CombinacaoEncontrada[] combinacoes = [new("C1", 40, "x"), new("C2", 40, "x")];

        Assert.Equal(100, CalculadoraDeRiscoCidadao.Calcular(achados, combinacoes));
    }

    [Theory]
    [InlineData(0, NivelDeRisco.Baixo)]
    [InlineData(29, NivelDeRisco.Baixo)]
    [InlineData(30, NivelDeRisco.Medio)]
    [InlineData(69, NivelDeRisco.Medio)]
    [InlineData(70, NivelDeRisco.Alto)]
    [InlineData(100, NivelDeRisco.Alto)]
    public void FaixaDe_LimitesDasFaixas(int indice, NivelDeRisco esperada)
    {
        Assert.Equal(esperada, CalculadoraDeRiscoCidadao.FaixaDe(indice));
    }
}
