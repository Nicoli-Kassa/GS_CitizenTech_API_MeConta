using MeConta.Domain.Excecoes;

namespace MeConta.Domain.ValueObjects;

/// <summary>
/// Número de WhatsApp no formato internacional usado pelo link wa.me: 55 + DDD + número.
/// Aceita o número digitado de vários jeitos ("(11) 98765-4321", "+55 11 98765-4321")
/// e guarda só os dígitos.
/// </summary>
public sealed record Telefone
{
    public const string MensagemInvalido = "Confira o número do WhatsApp";

    private const string CodigoDoBrasil = "55";

    public Telefone(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new RegraDeNegocioException(MensagemInvalido);
        }

        string digitos = new(numero.Where(char.IsAsciiDigit).ToArray());

        // 10 ou 11 dígitos: DDD + número (fixo ou celular), sem o código do país.
        // 12 ou 13 dígitos começando com 55: número já no formato internacional.
        string internacional = digitos.Length switch
        {
            10 or 11 => CodigoDoBrasil + digitos,
            12 or 13 when digitos.StartsWith(CodigoDoBrasil, StringComparison.Ordinal) => digitos,
            _ => throw new RegraDeNegocioException(MensagemInvalido),
        };

        // Nenhum DDD brasileiro começa com 0.
        if (internacional[2] == '0')
        {
            throw new RegraDeNegocioException(MensagemInvalido);
        }

        Valor = internacional;
    }

    /// <summary>
    /// Só dígitos, já com o 55 na frente. Ex.: 5511987654321.
    /// </summary>
    public string Valor { get; }

    public override string ToString() => Valor;
}
