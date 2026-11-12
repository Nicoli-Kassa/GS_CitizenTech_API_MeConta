namespace MeConta.Domain.Comum;

/// <summary>
/// Fuso usado para decidir em que dia e semana "está" a família.
/// O banco guarda tudo em UTC; regras como "toda segunda-feira" usam o horário de Brasília.
/// </summary>
public static class FusoHorario
{
    /// <summary>
    /// Horário de Brasília. O identificador IANA funciona no Windows (a partir do .NET 6) e no Linux.
    /// </summary>
    public static TimeZoneInfo Brasilia { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
}
