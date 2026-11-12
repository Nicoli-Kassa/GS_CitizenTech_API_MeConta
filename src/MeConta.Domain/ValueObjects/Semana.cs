namespace MeConta.Domain.ValueObjects;

/// <summary>
/// Semana de segunda a domingo, usada na pergunta da semana (US05)
/// e no indicador "Conversas de verdade" (US12).
/// </summary>
public readonly record struct Semana
{
    /// <summary>
    /// Uma segunda-feira qualquer, usada como ponto zero para numerar as semanas.
    /// </summary>
    private static readonly DateOnly SegundaDeReferencia = new(2024, 1, 1);

    private Semana(DateOnly inicio)
    {
        Inicio = inicio;
    }

    /// <summary>Segunda-feira da semana.</summary>
    public DateOnly Inicio { get; }

    /// <summary>Domingo da semana.</summary>
    public DateOnly Fim => Inicio.AddDays(6);

    /// <summary>
    /// Número sequencial da semana: duas semanas seguidas sempre têm números seguidos,
    /// inclusive na virada do ano.
    /// </summary>
    public int Numero => (Inicio.DayNumber - SegundaDeReferencia.DayNumber) / 7;

    /// <summary>
    /// Semana que contém a data informada.
    /// </summary>
    public static Semana Contendo(DateOnly data)
    {
        // DayOfWeek começa no domingo (0). Esta conta transforma segunda em 0 e domingo em 6.
        int diasDesdeSegunda = ((int)data.DayOfWeek + 6) % 7;
        return new Semana(data.AddDays(-diasDesdeSegunda));
    }

    /// <summary>
    /// Semana que contém um instante em UTC, olhando o calendário do fuso informado.
    /// Ex.: segunda às 02h UTC ainda é domingo em Brasília, então pertence à semana anterior.
    /// </summary>
    public static Semana Contendo(DateTime instanteUtc, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);

        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc),
            fuso);
        return Contendo(DateOnly.FromDateTime(local));
    }

    /// <summary>
    /// Instante UTC em que a semana começa (segunda, 00h no fuso informado).
    /// </summary>
    public DateTime InicioUtc(TimeZoneInfo fuso) => ParaUtc(Inicio, fuso);

    /// <summary>
    /// Instante UTC em que a semana seguinte começa. Use com "menor que" nas consultas.
    /// </summary>
    public DateTime FimUtcExclusivo(TimeZoneInfo fuso) => ParaUtc(Inicio.AddDays(7), fuso);

    public Semana Anterior() => new(Inicio.AddDays(-7));

    public Semana Seguinte() => new(Inicio.AddDays(7));

    private static DateTime ParaUtc(DateOnly data, TimeZoneInfo fuso)
    {
        ArgumentNullException.ThrowIfNull(fuso);
        return TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(TimeOnly.MinValue), fuso);
    }
}
