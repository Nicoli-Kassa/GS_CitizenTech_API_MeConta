using System.Globalization;
using System.Text;

namespace MeConta.Privacidade.Normalizacao;

/// <summary>
/// Prepara o texto para as regras em duas etapas:
/// 1. caractere a caractere: minúsculas, sem acentos, sem caracteres invisíveis, travessões viram "-";
/// 2. números falados viram dígitos ("um dois três quatro" → "1234"), porque a transcrição vem de áudio.
/// As duas etapas mantêm o mapa de posições até o texto original.
/// </summary>
public static class NormalizadorDeTexto
{
    /// <summary>Caracteres que não aparecem na tela, mas podem ser usados para esconder um número do filtro.</summary>
    private static readonly HashSet<char> Invisiveis = ['​', '‌', '‍', '⁠', '﻿', '­'];

    public static TextoNormalizado Normalizar(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);

        ConstrutorDeTexto caracteres = NormalizarCaracteres(texto);
        ConstrutorDeTexto comNumeros = ConversorDeNumerosFalados.Converter(caracteres);
        return comNumeros.Construir(texto);
    }

    private static ConstrutorDeTexto NormalizarCaracteres(string texto)
    {
        ConstrutorDeTexto saida = new(texto.Length);

        for (int i = 0; i < texto.Length; i++)
        {
            char letra = texto[i];

            if (Invisiveis.Contains(letra))
            {
                continue;
            }

            // Emojis e outros símbolos fora do plano básico viram espaço.
            if (char.IsSurrogate(letra) || char.IsWhiteSpace(letra))
            {
                saida.Adicionar(' ', i, i + 1);
                continue;
            }

            // Hífens e travessões tipográficos (‐ ‑ ‒ – — ―) e o sinal de menos (−).
            if (letra is >= '‐' and <= '―' or '−')
            {
                saida.Adicionar('-', i, i + 1);
                continue;
            }

            // FormKD separa o acento da letra e também troca dígitos "de largura cheia" e sobrescritos
            // pelos dígitos comuns, para que "１２３" não escape das regras.
            string decomposto = letra.ToString().Normalize(NormalizationForm.FormKD);
            foreach (char parte in decomposto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(parte) != UnicodeCategory.NonSpacingMark)
                {
                    saida.Adicionar(char.ToLowerInvariant(parte), i, i + 1);
                }
            }
        }

        return saida;
    }
}

/// <summary>
/// Monta o texto normalizado junto com o mapa de posições até o original.
/// </summary>
internal sealed class ConstrutorDeTexto(int capacidade)
{
    private readonly StringBuilder _texto = new(capacidade);
    private readonly List<int> _inicio = new(capacidade);
    private readonly List<int> _fim = new(capacidade);

    public int Tamanho => _texto.Length;

    public char this[int indice] => _texto[indice];

    public int InicioNoOriginal(int indice) => _inicio[indice];

    public int FimNoOriginal(int indice) => _fim[indice];

    public void Adicionar(char letra, int inicioNoOriginal, int fimNoOriginal)
    {
        _texto.Append(letra);
        _inicio.Add(inicioNoOriginal);
        _fim.Add(fimNoOriginal);
    }

    public override string ToString() => _texto.ToString();

    public TextoNormalizado Construir(string original) =>
        new(original, _texto.ToString(), _inicio.ToArray(), _fim.ToArray());
}
