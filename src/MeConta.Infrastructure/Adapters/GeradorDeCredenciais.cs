using System.Security.Cryptography;
using MeConta.Application.Adapters;
using MeConta.Domain.Entidades;

namespace MeConta.Infrastructure.Adapters;

/// <summary>
/// Sorteia o código e a chave do porta-retrato com um gerador criptográfico.
/// </summary>
public sealed class GeradorDeCredenciais : IGeradorDeCredenciais
{
    /// <summary>32 bytes aleatórios = 256 bits, impossível de adivinhar.</summary>
    private const int BytesDaChave = 32;

    public string GerarCodigo() =>
        RandomNumberGenerator.GetString(PortaRetrato.AlfabetoCodigo, PortaRetrato.TamanhoCodigo);

    /// <summary>
    /// Chave em Base64 "para URL" (sem +, / e =), fácil de colar num cabeçalho HTTP.
    /// </summary>
    public string GerarChave() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(BytesDaChave))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
