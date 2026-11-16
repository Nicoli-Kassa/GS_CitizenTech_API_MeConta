using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MeConta.Privacidade.Validadores;

namespace MeConta.Privacidade.Regras;

/// <summary>
/// Regras e combinações carregadas do arquivo regras-pii.json e conferidas antes do uso.
/// Uma regra com erro (expressão inválida, nível desconhecido, validador que não existe)
/// impede o carregamento inteiro: é melhor a API não subir do que rodar sem uma regra.
/// </summary>
public sealed class CatalogoDeRegras
{
    private const string RecursoPadrao = "MeConta.Privacidade.regras-pii.json";

    /// <summary>Tempo máximo de cada expressão por texto. Protege contra textos feitos para travar o motor.</summary>
    public static readonly TimeSpan TempoMaximoPadrao = TimeSpan.FromMilliseconds(250);

    private const RegexOptions Opcoes = RegexOptions.CultureInvariant | RegexOptions.Compiled;

    private CatalogoDeRegras(string versao, IReadOnlyList<RegraPii> regras, IReadOnlyList<CombinacaoDeRisco> combinacoes)
    {
        Versao = versao;
        Regras = regras;
        Combinacoes = combinacoes;
    }

    public string Versao { get; }

    public IReadOnlyList<RegraPii> Regras { get; }

    public IReadOnlyList<CombinacaoDeRisco> Combinacoes { get; }

    /// <summary>Regras que vêm dentro do módulo.</summary>
    public static CatalogoDeRegras CarregarPadrao(TimeSpan? tempoMaximo = null)
    {
        Assembly modulo = typeof(CatalogoDeRegras).Assembly;
        using Stream recurso = modulo.GetManifestResourceStream(RecursoPadrao)
            ?? throw new ConfiguracaoDeRegrasInvalidaException($"Recurso {RecursoPadrao} não encontrado no módulo.");
        using StreamReader leitor = new(recurso);
        return CarregarDeJson(leitor.ReadToEnd(), tempoMaximo);
    }

    /// <summary>Regras de um arquivo externo, para trocar as regras sem recompilar.</summary>
    public static CatalogoDeRegras CarregarDeArquivo(string caminho, TimeSpan? tempoMaximo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caminho);
        return CarregarDeJson(File.ReadAllText(caminho), tempoMaximo);
    }

    public static CatalogoDeRegras CarregarDeJson(
        string json,
        TimeSpan? tempoMaximo = null,
        RegistroDeValidadores? validadores = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ArquivoDeRegras arquivo;
        try
        {
            arquivo = JsonSerializer.Deserialize<ArquivoDeRegras>(json)
                ?? throw new ConfiguracaoDeRegrasInvalidaException("O arquivo de regras está vazio.");
        }
        catch (JsonException erro)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"O arquivo de regras não é um JSON válido: {erro.Message}");
        }

        if (arquivo.Regras.Count == 0)
        {
            throw new ConfiguracaoDeRegrasInvalidaException("O arquivo não tem nenhuma regra.");
        }

        TimeSpan limite = tempoMaximo ?? TempoMaximoPadrao;
        RegistroDeValidadores registro = validadores ?? RegistroDeValidadores.Padrao();

        List<RegraPii> regras = [];
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);

        foreach (DefinicaoDeRegra definicao in arquivo.Regras)
        {
            if (!ids.Add(definicao.Id))
            {
                throw new ConfiguracaoDeRegrasInvalidaException($"Id repetido: {definicao.Id}.");
            }

            regras.Add(Compilar(definicao, limite, registro));
        }

        HashSet<string> tipos = regras.Select(regra => regra.Tipo).ToHashSet(StringComparer.Ordinal);
        List<CombinacaoDeRisco> combinacoes = arquivo.Combinacoes
            .Select(combinacao => CompilarCombinacao(combinacao, tipos))
            .ToList();

        return new CatalogoDeRegras(arquivo.Versao, regras, combinacoes);
    }

    private static RegraPii Compilar(DefinicaoDeRegra definicao, TimeSpan limite, RegistroDeValidadores registro)
    {
        string nome = string.IsNullOrWhiteSpace(definicao.Id) ? "(sem id)" : definicao.Id;

        ExigirTexto(nome, "id", definicao.Id);
        ExigirTexto(nome, "tipo", definicao.Tipo);
        ExigirTexto(nome, "rotulo", definicao.Rotulo);
        ExigirTexto(nome, "padrao", definicao.Padrao);
        ExigirTexto(nome, "substituicao", definicao.Substituicao);
        ExigirTexto(nome, "explicacao", definicao.Explicacao);

        if (!Enum.TryParse(definicao.Nivel, ignoreCase: true, out NivelDeRisco nivel) || !Enum.IsDefined(nivel))
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: nível \"{definicao.Nivel}\" desconhecido. Use Baixo, Medio ou Alto.");
        }

        if (!Enum.TryParse(definicao.Mascara, ignoreCase: true, out EstiloDeMascara mascara) || !Enum.IsDefined(mascara))
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: máscara \"{definicao.Mascara}\" desconhecida. Use Digitos, Palavras ou Total.");
        }

        IValidadorPii? validador = null;
        if (!string.IsNullOrWhiteSpace(definicao.Validador) && !registro.TentarObter(definicao.Validador, out validador))
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: validador \"{definicao.Validador}\" não existe.");
        }

        if (definicao.ExigeContexto && definicao.Contexto.Count == 0)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: exige contexto, mas não lista nenhuma palavra de contexto.");
        }

        Regex padrao = CriarExpressao(nome, definicao.Padrao, limite);

        Regex? contexto = null;
        if (definicao.Contexto.Count > 0)
        {
            string alternativas = string.Join('|', definicao.Contexto.Select(Regex.Escape));
            contexto = CriarExpressao(nome, $@"\b(?:{alternativas})\b", limite);
        }

        return new RegraPii(definicao, nivel, mascara, padrao, contexto, validador);
    }

    private static CombinacaoDeRisco CompilarCombinacao(DefinicaoDeCombinacao definicao, HashSet<string> tipos)
    {
        string nome = string.IsNullOrWhiteSpace(definicao.Id) ? "(combinação sem id)" : definicao.Id;
        ExigirTexto(nome, "id", definicao.Id);
        ExigirTexto(nome, "explicacao", definicao.Explicacao);

        if (definicao.Tipos.Count < 2)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: uma combinação precisa de pelo menos dois tipos.");
        }

        string? desconhecido = definicao.Tipos.FirstOrDefault(tipo => !tipos.Contains(tipo));
        if (desconhecido is not null)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: nenhuma regra tem o tipo \"{desconhecido}\".");
        }

        if (definicao.Bonus is < 0 or > 100)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: o bônus precisa estar entre 0 e 100.");
        }

        return new CombinacaoDeRisco(definicao.Id, definicao.Tipos.ToHashSet(StringComparer.Ordinal), definicao.Bonus, definicao.Explicacao);
    }

    private static Regex CriarExpressao(string nome, string padrao, TimeSpan limite)
    {
        try
        {
            return new Regex(padrao, Opcoes, limite);
        }
        catch (ArgumentException erro)
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: expressão inválida ({erro.Message}).");
        }
    }

    private static void ExigirTexto(string nome, string campo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ConfiguracaoDeRegrasInvalidaException($"{nome}: o campo \"{campo}\" é obrigatório.");
        }
    }
}

/// <summary>
/// Tipos de dado que, encontrados juntos no mesmo texto, aumentam o risco.
/// </summary>
public sealed record CombinacaoDeRisco(string Id, IReadOnlySet<string> Tipos, int Bonus, string Explicacao);

/// <summary>
/// Erro no arquivo de regras. A mensagem diz qual regra e qual campo corrigir.
/// </summary>
public sealed class ConfiguracaoDeRegrasInvalidaException(string mensagem) : Exception(mensagem);
