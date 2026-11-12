using System.Security.Cryptography;
using MeConta.Application.Adapters;
using MeConta.Domain.Entidades;

namespace MeConta.Infrastructure.Adapters;

/// <summary>
/// Sorteia o código com um gerador criptográfico, para que não dê para adivinhar o próximo código.
/// </summary>
public sealed class GeradorDeCodigoConvite : IGeradorDeCodigoConvite
{
    public string Gerar() => RandomNumberGenerator.GetString(Convite.Alfabeto, Convite.TamanhoCodigo);
}
