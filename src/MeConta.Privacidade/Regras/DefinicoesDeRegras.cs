using System.Text.Json.Serialization;

namespace MeConta.Privacidade.Regras;

/// <summary>
/// Conteúdo do arquivo de regras (regras-pii.json). Para ensinar o motor a reconhecer
/// um dado novo, basta acrescentar uma regra no arquivo, sem mexer no código.
/// </summary>
public sealed class ArquivoDeRegras
{
    [JsonPropertyName("versao")]
    public string Versao { get; init; } = string.Empty;

    [JsonPropertyName("regras")]
    public List<DefinicaoDeRegra> Regras { get; init; } = [];

    [JsonPropertyName("combinacoes")]
    public List<DefinicaoDeCombinacao> Combinacoes { get; init; } = [];
}

/// <summary>
/// Uma regra do arquivo, ainda como texto.
/// </summary>
public sealed class DefinicaoDeRegra
{
    /// <summary>Identificador único, que aparece nos achados e no painel (ex.: "PII-CPF-001").</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Tipo de dado, usado para agrupar no painel (ex.: "Cpf", "Saude").</summary>
    [JsonPropertyName("tipo")]
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Nome do dado em linguagem simples (ex.: "CPF", "Dado de saúde").</summary>
    [JsonPropertyName("rotulo")]
    public string Rotulo { get; init; } = string.Empty;

    /// <summary>"Baixo", "Medio" ou "Alto".</summary>
    [JsonPropertyName("nivel")]
    public string Nivel { get; init; } = string.Empty;

    /// <summary>
    /// Expressão regular aplicada ao texto normalizado (minúsculas, sem acentos, números falados em dígitos).
    /// Se tiver um grupo chamado "valor", só esse pedaço é tratado como dado pessoal.
    /// </summary>
    [JsonPropertyName("padrao")]
    public string Padrao { get; init; } = string.Empty;

    /// <summary>Validador opcional ("cpf", "cartao", "telefone").</summary>
    [JsonPropertyName("validador")]
    public string? Validador { get; init; }

    /// <summary>Palavras que, perto do trecho, confirmam o dado (ex.: "cpf", "documento").</summary>
    [JsonPropertyName("contexto")]
    public List<string> Contexto { get; init; } = [];

    /// <summary>Só aceita o trecho se uma palavra de contexto estiver por perto.</summary>
    [JsonPropertyName("exigeContexto")]
    public bool ExigeContexto { get; init; }

    /// <summary>
    /// Aceita o trecho mesmo quando o validador recusa, desde que haja contexto
    /// (ex.: "meu CPF é ..." com um dígito errado continua sendo uma tentativa de expor o CPF).
    /// </summary>
    [JsonPropertyName("aceitaSemValidacaoComContexto")]
    public bool AceitaSemValidacaoComContexto { get; init; }

    /// <summary>"Digitos", "Palavras" ou "Total".</summary>
    [JsonPropertyName("mascara")]
    public string Mascara { get; init; } = "Total";

    /// <summary>Texto que entra no lugar do dado na versão segura da memória (ex.: "[CPF removido]").</summary>
    [JsonPropertyName("substituicao")]
    public string Substituicao { get; init; } = string.Empty;

    /// <summary>Explicação do risco em linguagem simples, usada quando a IA não está disponível.</summary>
    [JsonPropertyName("explicacao")]
    public string Explicacao { get; init; } = string.Empty;
}

/// <summary>
/// Dados que, juntos, são mais perigosos que separados (ex.: endereço + "moro sozinha").
/// </summary>
public sealed class DefinicaoDeCombinacao
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("tipos")]
    public List<string> Tipos { get; init; } = [];

    /// <summary>Pontos somados ao índice Risco Cidadão.</summary>
    [JsonPropertyName("bonus")]
    public int Bonus { get; init; }

    [JsonPropertyName("explicacao")]
    public string Explicacao { get; init; } = string.Empty;
}
