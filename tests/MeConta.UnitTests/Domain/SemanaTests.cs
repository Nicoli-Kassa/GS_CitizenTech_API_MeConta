using MeConta.Domain.Comum;
using MeConta.Domain.ValueObjects;

namespace MeConta.UnitTests.Domain;

public sealed class SemanaTests
{
    private static readonly DateOnly Segunda = new(2026, 11, 9);

    [Theory]
    [InlineData(2026, 11, 9)]   // segunda
    [InlineData(2026, 11, 11)]  // quarta
    [InlineData(2026, 11, 15)]  // domingo
    public void Contendo_DiaDaSemana_ComecaNaSegunda(int ano, int mes, int dia)
    {
        Semana semana = Semana.Contendo(new DateOnly(ano, mes, dia));

        Assert.Equal(Segunda, semana.Inicio);
        Assert.Equal(new DateOnly(2026, 11, 15), semana.Fim);
    }

    [Fact]
    public void Numero_SemanasSeguidas_AvancaUm()
    {
        Semana atual = Semana.Contendo(Segunda);

        Assert.Equal(atual.Numero + 1, atual.Seguinte().Numero);
        Assert.Equal(atual.Numero - 1, atual.Anterior().Numero);
    }

    [Fact]
    public void Numero_ViradaDoAno_ContinuaSeguido()
    {
        Semana ultimaDe2026 = Semana.Contendo(new DateOnly(2026, 12, 31));
        Semana primeiraDe2027 = Semana.Contendo(new DateOnly(2027, 1, 7));

        Assert.Equal(ultimaDe2026.Numero + 1, primeiraDe2027.Numero);
    }

    [Fact]
    public void Contendo_SegundaAs2hUtc_AindaEhDomingoEmBrasilia()
    {
        // 16/11/2026 02h UTC = 15/11/2026 23h em Brasília (domingo).
        DateTime instanteUtc = new(2026, 11, 16, 2, 0, 0, DateTimeKind.Utc);

        Semana semana = Semana.Contendo(instanteUtc, FusoHorario.Brasilia);

        Assert.Equal(Segunda, semana.Inicio);
    }

    [Fact]
    public void InicioEFimUtc_SemanaEmBrasilia_ConvertemPara3hUtc()
    {
        Semana semana = Semana.Contendo(Segunda);

        Assert.Equal(new DateTime(2026, 11, 9, 3, 0, 0, DateTimeKind.Utc), semana.InicioUtc(FusoHorario.Brasilia));
        Assert.Equal(new DateTime(2026, 11, 16, 3, 0, 0, DateTimeKind.Utc), semana.FimUtcExclusivo(FusoHorario.Brasilia));
    }
}
