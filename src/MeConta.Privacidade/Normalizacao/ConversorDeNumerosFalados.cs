using System.Text;

namespace MeConta.Privacidade.Normalizacao;

/// <summary>
/// Troca números falados por dígitos, como aparecem numa transcrição de áudio:
/// "um dois três, quatro cinco seis" → "123456";
/// "cento e vinte e três, quatrocentos e cinquenta e seis" → "123456";
/// "nove meia meia sete" → "9667" ("meia" é o seis dito ao telefone).
/// Só converte sequências com pelo menos 3 dígitos, para não mexer em frases como "dois filhos".
/// </summary>
internal static class ConversorDeNumerosFalados
{
    private const int MinimoDeDigitos = 3;

    private static readonly Dictionary<string, int> Unidades = new()
    {
        ["zero"] = 0, ["um"] = 1, ["uma"] = 1, ["dois"] = 2, ["duas"] = 2, ["tres"] = 3,
        ["quatro"] = 4, ["cinco"] = 5, ["seis"] = 6, ["meia"] = 6, ["sete"] = 7, ["oito"] = 8, ["nove"] = 9,
    };

    private static readonly Dictionary<string, int> DeDezADezenove = new()
    {
        ["dez"] = 10, ["onze"] = 11, ["doze"] = 12, ["treze"] = 13, ["quatorze"] = 14, ["catorze"] = 14,
        ["quinze"] = 15, ["dezesseis"] = 16, ["dezasseis"] = 16, ["dezessete"] = 17, ["dezassete"] = 17,
        ["dezoito"] = 18, ["dezenove"] = 19, ["dezanove"] = 19,
    };

    private static readonly Dictionary<string, int> Dezenas = new()
    {
        ["vinte"] = 20, ["trinta"] = 30, ["quarenta"] = 40, ["cinquenta"] = 50, ["sessenta"] = 60,
        ["setenta"] = 70, ["oitenta"] = 80, ["noventa"] = 90,
    };

    private static readonly Dictionary<string, int> Centenas = new()
    {
        ["cem"] = 100, ["cento"] = 100, ["duzentos"] = 200, ["duzentas"] = 200, ["trezentos"] = 300,
        ["trezentas"] = 300, ["quatrocentos"] = 400, ["quatrocentas"] = 400, ["quinhentos"] = 500,
        ["quinhentas"] = 500, ["seiscentos"] = 600, ["seiscentas"] = 600, ["setecentos"] = 700,
        ["setecentas"] = 700, ["oitocentos"] = 800, ["oitocentas"] = 800, ["novecentos"] = 900,
        ["novecentas"] = 900,
    };

    /// <summary>Palavras que as pessoas falam entre os grupos de um documento: "ponto", "traço"...</summary>
    private static readonly HashSet<string> PalavrasSeparadoras = ["ponto", "traco", "hifen", "barra", "virgula"];

    private sealed record Palavra(string Texto, int Inicio, int Fim);

    public static ConstrutorDeTexto Converter(ConstrutorDeTexto entrada)
    {
        string texto = entrada.ToString();
        List<Palavra> palavras = ExtrairPalavras(texto);
        List<(int Inicio, int Fim, string Digitos)> trocas = [];

        int indice = 0;
        while (indice < palavras.Count)
        {
            if (TentarLerSequencia(texto, palavras, indice, out int proxima, out string digitos)
                && digitos.Length >= MinimoDeDigitos)
            {
                trocas.Add((palavras[indice].Inicio, palavras[proxima - 1].Fim, digitos));
                indice = proxima;
            }
            else
            {
                indice++;
            }
        }

        if (trocas.Count == 0)
        {
            return entrada;
        }

        ConstrutorDeTexto saida = new(entrada.Tamanho);
        int posicao = 0;

        foreach ((int inicio, int fim, string digitos) in trocas)
        {
            for (; posicao < inicio; posicao++)
            {
                saida.Adicionar(entrada[posicao], entrada.InicioNoOriginal(posicao), entrada.FimNoOriginal(posicao));
            }

            // Cada dígito aponta para a frase inteira no original ("um dois três" → 3 dígitos, mesma origem).
            int inicioOriginal = entrada.InicioNoOriginal(inicio);
            int fimOriginal = entrada.FimNoOriginal(fim - 1);
            foreach (char digito in digitos)
            {
                saida.Adicionar(digito, inicioOriginal, fimOriginal);
            }

            posicao = fim;
        }

        for (; posicao < entrada.Tamanho; posicao++)
        {
            saida.Adicionar(entrada[posicao], entrada.InicioNoOriginal(posicao), entrada.FimNoOriginal(posicao));
        }

        return saida;
    }

    private static List<Palavra> ExtrairPalavras(string texto)
    {
        List<Palavra> palavras = [];
        int inicio = -1;

        for (int i = 0; i <= texto.Length; i++)
        {
            bool ehLetra = i < texto.Length && texto[i] is >= 'a' and <= 'z';

            if (ehLetra && inicio < 0)
            {
                inicio = i;
            }
            else if (!ehLetra && inicio >= 0)
            {
                palavras.Add(new Palavra(texto[inicio..i], inicio, i));
                inicio = -1;
            }
        }

        return palavras;
    }

    /// <summary>
    /// Lê grupos numéricos seguidos ("cento e vinte e três, quatrocentos...") e junta os dígitos.
    /// </summary>
    private static bool TentarLerSequencia(
        string texto, List<Palavra> palavras, int inicio, out int proxima, out string digitos)
    {
        proxima = inicio;
        digitos = string.Empty;

        if (!TentarLerGrupo(texto, palavras, inicio, out int depois, out string grupo))
        {
            return false;
        }

        StringBuilder sequencia = new(grupo);
        int fimDaSequencia = depois;

        while (fimDaSequencia < palavras.Count)
        {
            int candidata = fimDaSequencia;

            if (!SoSeparadoresEntre(texto, palavras[candidata - 1].Fim, palavras[candidata].Inicio))
            {
                break;
            }

            // Pula palavras como "ponto" e "traço" entre os grupos.
            while (candidata + 1 < palavras.Count
                   && PalavrasSeparadoras.Contains(palavras[candidata].Texto)
                   && SoSeparadoresEntre(texto, palavras[candidata].Fim, palavras[candidata + 1].Inicio))
            {
                candidata++;
            }

            if (!TentarLerGrupo(texto, palavras, candidata, out depois, out grupo))
            {
                break;
            }

            sequencia.Append(grupo);
            fimDaSequencia = depois;
        }

        proxima = fimDaSequencia;
        digitos = sequencia.ToString();
        return true;
    }

    /// <summary>
    /// Lê um número de 0 a 999 escrito por extenso: "sete", "dezoito", "vinte e um", "cento e vinte e três".
    /// </summary>
    private static bool TentarLerGrupo(
        string texto, List<Palavra> palavras, int indice, out int depois, out string grupo)
    {
        depois = indice;
        grupo = string.Empty;

        if (indice >= palavras.Count)
        {
            return false;
        }

        string palavra = palavras[indice].Texto;
        int proxima = indice + 1;
        int valor;

        if (Centenas.TryGetValue(palavra, out valor))
        {
            if (TentarLerComplemento(texto, palavras, proxima, ComplementoDaCentena, out int indiceDepois, out int complemento))
            {
                valor += complemento;
                proxima = indiceDepois;

                bool complementoEhDezenaRedonda = complemento >= 20 && complemento % 10 == 0;
                if (complementoEhDezenaRedonda
                    && TentarLerComplemento(texto, palavras, proxima, UnidadeDeComplemento, out indiceDepois, out int unidade))
                {
                    valor += unidade;
                    proxima = indiceDepois;
                }
            }
        }
        else if (Dezenas.TryGetValue(palavra, out valor))
        {
            if (TentarLerComplemento(texto, palavras, proxima, UnidadeDeComplemento, out int indiceDepois, out int unidade))
            {
                valor += unidade;
                proxima = indiceDepois;
            }
        }
        else if (!DeDezADezenove.TryGetValue(palavra, out valor) && !Unidades.TryGetValue(palavra, out valor))
        {
            return false;
        }

        depois = proxima;
        grupo = valor.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>Depois de "cento e": uma dezena, de dez a dezenove ou uma unidade.</summary>
    private static int? ComplementoDaCentena(string palavra) =>
        Dezenas.TryGetValue(palavra, out int dezena) ? dezena
        : DeDezADezenove.TryGetValue(palavra, out int dezADezenove) ? dezADezenove
        : UnidadeDeComplemento(palavra);

    /// <summary>Depois de "vinte e": de um a nove ("meia" não entra aqui).</summary>
    private static int? UnidadeDeComplemento(string palavra) =>
        palavra != "meia" && palavra != "zero" && Unidades.TryGetValue(palavra, out int unidade) ? unidade : null;

    /// <summary>Lê "e" + número, como em "vinte e três".</summary>
    private static bool TentarLerComplemento(
        string texto, List<Palavra> palavras, int indice, Func<string, int?> valorDe, out int depois, out int valor)
    {
        depois = indice;
        valor = 0;

        if (indice + 1 >= palavras.Count || palavras[indice].Texto != "e")
        {
            return false;
        }

        if (!SoEspacosEntre(texto, palavras[indice - 1].Fim, palavras[indice].Inicio)
            || !SoEspacosEntre(texto, palavras[indice].Fim, palavras[indice + 1].Inicio))
        {
            return false;
        }

        int? encontrado = valorDe(palavras[indice + 1].Texto);
        if (encontrado is null)
        {
            return false;
        }

        valor = encontrado.Value;
        depois = indice + 2;
        return true;
    }

    private static bool SoEspacosEntre(string texto, int inicio, int fim) =>
        fim - inicio is >= 1 and <= 3 && texto[inicio..fim].All(letra => letra == ' ');

    private static bool SoSeparadoresEntre(string texto, int inicio, int fim) =>
        fim - inicio is >= 1 and <= 4 && texto[inicio..fim].All(letra => letra is ' ' or '.' or ',' or '-' or '/');
}
