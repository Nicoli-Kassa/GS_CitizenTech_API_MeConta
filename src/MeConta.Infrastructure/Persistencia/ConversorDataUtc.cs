using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// O banco não guarda o "tipo" da data (UTC ou local). Este conversor marca como UTC
/// toda data lida do banco, já que a aplicação só grava datas em UTC.
/// </summary>
public sealed class ConversorDataUtc : ValueConverter<DateTime, DateTime>
{
    public ConversorDataUtc()
        : base(
            data => data,
            data => DateTime.SpecifyKind(data, DateTimeKind.Utc))
    {
    }
}
