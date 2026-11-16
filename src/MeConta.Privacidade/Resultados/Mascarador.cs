using System.Text;
using MeConta.Privacidade.Regras;

namespace MeConta.Privacidade.Resultados;

/// <summary>
/// Esconde o dado pessoal para mostrar no alerta e no painel, sem revelar o valor.
/// </summary>
public static class Mascarador
{
    private const char Mascara = '*';
    private const int DigitosVisiveis = 2;

    public static string Mascarar(string trecho, EstiloDeMascara estilo)
    {
        ArgumentNullException.ThrowIfNull(trecho);

        return estilo switch
        {
            EstiloDeMascara.Digitos => MascararDigitos(trecho),
            EstiloDeMascara.Palavras => MascararPalavras(trecho),
            _ => MascararTudo(trecho),
        };
    }

    /// <summary>"123.456.789-09" → "***.***.***-09". Letras (número falado) também somem.</summary>
    private static string MascararDigitos(string trecho)
    {
        int totalDeDigitos = trecho.Count(char.IsAsciiDigit);
        int digitosVistos = 0;
        StringBuilder saida = new(trecho.Length);

        foreach (char letra in trecho)
        {
            if (char.IsAsciiDigit(letra))
            {
                digitosVistos++;
                bool estaNoFinal = totalDeDigitos > 6 && digitosVistos > totalDeDigitos - DigitosVisiveis;
                saida.Append(estaNoFinal ? letra : Mascara);
            }
            else
            {
                saida.Append(char.IsLetter(letra) ? Mascara : letra);
            }
        }

        return saida.ToString();
    }

    /// <summary>"remédio para pressão" → "r****** p*** p******".</summary>
    private static string MascararPalavras(string trecho)
    {
        StringBuilder saida = new(trecho.Length);
        bool inicioDePalavra = true;

        foreach (char letra in trecho)
        {
            if (char.IsLetterOrDigit(letra))
            {
                saida.Append(inicioDePalavra && char.IsLetter(letra) ? letra : Mascara);
                inicioDePalavra = false;
            }
            else
            {
                saida.Append(letra);
                inicioDePalavra = true;
            }
        }

        return saida.ToString();
    }

    private static string MascararTudo(string trecho) =>
        new(trecho.Select(letra => char.IsLetterOrDigit(letra) ? Mascara : letra).ToArray());
}
