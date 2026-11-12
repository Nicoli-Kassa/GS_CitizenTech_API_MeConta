using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MeConta.Domain.Regras;

/// <summary>
/// Filtro por padrões de texto. Antes de comparar, tira acentos e passa para minúsculas,
/// para que "Código", "codigo" e "CÓDIGO" sejam tratados igual.
/// Falar de dinheiro não é bloqueado ("como a senhora juntava dinheiro?");
/// pedir dinheiro é ("me empresta dinheiro?").
/// </summary>
public sealed class FiltroAntiGolpe : IFiltroAntiGolpe
{
    private const RegexOptions Opcoes = RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private static readonly Regex[] Padroes =
    [
        // Qualquer menção a PIX ou senha.
        new(@"\bpix\b", Opcoes),
        new(@"\bsenhas?\b", Opcoes),

        // Códigos que chegam por SMS ou WhatsApp.
        new(@"\bcodigo\s+(de\s+)?(verificacao|seguranca|confirmacao|acesso|sms)\b", Opcoes),
        new(@"\bcodigo\s+que\s+(chegou|recebeu|apareceu|veio)\b", Opcoes),

        // Dados de cartão.
        new(@"\b(numero|dados|foto|frente|verso)\s+do\s+(seu\s+)?cartao\b", Opcoes),

        // Pedido de dinheiro: verbo de pedido seguido de dinheiro, transferência, depósito ou empréstimo.
        new(
            @"\b(manda|mande|envia|envie|transfere|transfira|empresta|empreste|deposita|deposite|passa|passe|faz|faca|arruma|arrume)\s+(me\s+)?((um|uma|o|a|uns|umas)\s+)?(dinheiro|grana|transferencia|deposito|emprestimo)\b",
            Opcoes),
        new(@"\bme\s+empresta", Opcoes),
    ];

    /// <inheritdoc />
    public bool EhSuspeito(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        string normalizado = Normalizar(texto);
        return Padroes.Any(padrao => padrao.IsMatch(normalizado));
    }

    /// <summary>
    /// Minúsculas e sem acentos: "Transferência" vira "transferencia".
    /// </summary>
    private static string Normalizar(string texto)
    {
        string decomposto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder semAcentos = new(decomposto.Length);

        foreach (char letra in decomposto)
        {
            // Na forma decomposta, o acento vira um caractere separado (NonSpacingMark), que é descartado.
            if (CharUnicodeInfo.GetUnicodeCategory(letra) != UnicodeCategory.NonSpacingMark)
            {
                semAcentos.Append(letra);
            }
        }

        return semAcentos.ToString().Normalize(NormalizationForm.FormC);
    }
}
