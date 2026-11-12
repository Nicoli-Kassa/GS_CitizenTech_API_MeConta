using MeConta.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Grava o <see cref="Telefone"/> como texto (só dígitos) e o recria ao ler.
/// </summary>
public sealed class ConversorTelefone : ValueConverter<Telefone, string>
{
    public const int TamanhoMaximo = 13;

    public ConversorTelefone()
        : base(
            telefone => telefone.Valor,
            valor => new Telefone(valor))
    {
    }
}
