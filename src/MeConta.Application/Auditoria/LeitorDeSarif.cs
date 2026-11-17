using System.Text.Json;

namespace MeConta.Application.Auditoria;

/// <summary>Um achado do Semgrep no código.</summary>
/// <param name="Regra">Id curto da regra. Ex.: "meconta-pii-para-log".</param>
/// <param name="Arquivo">Caminho do arquivo no repositório.</param>
/// <param name="Linha">Linha do achado.</param>
/// <param name="Descricao">Mensagem da regra.</param>
/// <param name="Cwe">Classificação CWE da regra, quando houver.</param>
public sealed record AchadoDeCodigo(string Regra, string Arquivo, int Linha, string Descricao, string? Cwe);

/// <summary>Um relatório SARIF lido.</summary>
/// <param name="Ferramenta">Ex.: "Semgrep OSS 1.185.0".</param>
/// <param name="Achados">Achados do relatório.</param>
public sealed record RelatorioDeCodigo(string Ferramenta, IReadOnlyList<AchadoDeCodigo> Achados);

/// <summary>
/// Lê relatórios SARIF 2.1.0 (o formato que o Semgrep, o GitHub e outras ferramentas usam).
/// Pega só regra, arquivo, linha, mensagem e CWE: o trecho de código não é usado.
/// </summary>
public static class LeitorDeSarif
{
    /// <exception cref="FormatException">O texto não é um SARIF válido.</exception>
    public static RelatorioDeCodigo Ler(string sarif)
    {
        ArgumentNullException.ThrowIfNull(sarif);

        try
        {
            using JsonDocument documento = JsonDocument.Parse(sarif);
            JsonElement runs = documento.RootElement.GetProperty("runs");

            string ferramenta = "SARIF";
            List<AchadoDeCodigo> achados = [];
            foreach (JsonElement run in runs.EnumerateArray())
            {
                JsonElement driver = run.GetProperty("tool").GetProperty("driver");
                ferramenta = MontarNomeDaFerramenta(driver);
                Dictionary<string, string?> cwePorRegra = LerCwes(driver);

                if (!run.TryGetProperty("results", out JsonElement resultados))
                {
                    continue;
                }

                foreach (JsonElement resultado in resultados.EnumerateArray())
                {
                    string regraCompleta = Texto(resultado, "ruleId") ?? "sem-regra";
                    JsonElement local = resultado.GetProperty("locations")[0].GetProperty("physicalLocation");
                    string arquivo = Texto(local.GetProperty("artifactLocation"), "uri") ?? "?";
                    int linha = local.TryGetProperty("region", out JsonElement regiao)
                        && regiao.TryGetProperty("startLine", out JsonElement inicio)
                        ? inicio.GetInt32()
                        : 0;
                    string descricao = resultado.GetProperty("message").GetProperty("text").GetString() ?? string.Empty;

                    achados.Add(new AchadoDeCodigo(
                        IdCurto(regraCompleta),
                        arquivo,
                        linha,
                        descricao.Trim(),
                        cwePorRegra.GetValueOrDefault(regraCompleta)));
                }
            }

            return new RelatorioDeCodigo(ferramenta, achados);
        }
        catch (Exception erro) when (erro is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new FormatException("O relatório não está no formato SARIF.", erro);
        }
    }

    /// <summary>"seguranca.semgrep.meconta-pii-para-log" → "meconta-pii-para-log".</summary>
    public static string IdCurto(string regra)
    {
        int ponto = regra.LastIndexOf('.');
        return ponto >= 0 ? regra[(ponto + 1)..] : regra;
    }

    private static string MontarNomeDaFerramenta(JsonElement driver)
    {
        string nome = Texto(driver, "name") ?? "SARIF";
        string? versao = Texto(driver, "semanticVersion") ?? Texto(driver, "version");
        return versao is null ? nome : $"{nome} {versao}";
    }

    private static Dictionary<string, string?> LerCwes(JsonElement driver)
    {
        Dictionary<string, string?> cwes = [];
        if (!driver.TryGetProperty("rules", out JsonElement regras))
        {
            return cwes;
        }

        foreach (JsonElement regra in regras.EnumerateArray())
        {
            string? id = Texto(regra, "id");
            if (id is null)
            {
                continue;
            }

            string? cwe = null;
            if (regra.TryGetProperty("properties", out JsonElement propriedades)
                && propriedades.TryGetProperty("tags", out JsonElement tags))
            {
                cwe = tags.EnumerateArray()
                    .Select(tag => tag.GetString())
                    .FirstOrDefault(tag => tag?.StartsWith("CWE-", StringComparison.OrdinalIgnoreCase) == true);
            }

            cwes[id] = cwe;
        }

        return cwes;
    }

    private static string? Texto(JsonElement elemento, string propriedade) =>
        elemento.TryGetProperty(propriedade, out JsonElement valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString()
            : null;
}
