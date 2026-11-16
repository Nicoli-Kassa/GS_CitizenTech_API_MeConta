using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Resultados;

namespace MeConta.UnitTests.Privacidade;

/// <summary>
/// Memória simulada com os tipos de dado pessoal que ela realmente contém (o "gabarito").
/// </summary>
internal sealed record MemoriaRotulada(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("texto")] string Texto,
    [property: JsonPropertyName("tipos")] List<string> Tipos,
    [property: JsonPropertyName("deveBloquear")] bool DeveBloquear);

/// <summary>
/// Acertos e erros do motor para um tipo de dado.
/// Precisão: dos alertas dados, quantos estavam certos. Recall: dos dados que existiam, quantos o motor achou.
/// </summary>
internal sealed record MetricasPorTipo(string Tipo, int VerdadeirosPositivos, int FalsosPositivos, int FalsosNegativos)
{
    public double Precisao => VerdadeirosPositivos + FalsosPositivos == 0
        ? 1.0
        : (double)VerdadeirosPositivos / (VerdadeirosPositivos + FalsosPositivos);

    public double Recall => VerdadeirosPositivos + FalsosNegativos == 0
        ? 1.0
        : (double)VerdadeirosPositivos / (VerdadeirosPositivos + FalsosNegativos);
}

internal sealed record ResultadoDaAvaliacao(
    IReadOnlyList<MetricasPorTipo> PorTipo,
    MetricasPorTipo Geral,
    int Memorias,
    int MemoriasComDadoPessoal,
    int AcertosDeBloqueio,
    IReadOnlyList<string> Divergencias);

/// <summary>
/// Roda o motor em todas as memórias do corpus e compara com o gabarito, tipo por tipo.
/// Usado pelos testes e pela ferramenta que gera a tabela de evidências.
/// </summary>
internal static class AvaliacaoDoCorpus
{
    public const string NomeDoArquivo = "corpus-memorias.json";
    public const string NomeDaValidacao = "corpus-validacao.json";

    public static List<MemoriaRotulada> Carregar(string caminho) =>
        JsonSerializer.Deserialize<List<MemoriaRotulada>>(File.ReadAllText(caminho))
        ?? throw new InvalidOperationException("Corpus vazio.");

    public static ResultadoDaAvaliacao Avaliar(IMotorPii motor, IReadOnlyList<MemoriaRotulada> memorias)
    {
        Dictionary<string, (int Vp, int Fp, int Fn)> contagem = new(StringComparer.Ordinal);
        List<string> divergencias = [];
        int acertosDeBloqueio = 0;

        foreach (MemoriaRotulada memoria in memorias)
        {
            ResultadoAnalisePii resultado = motor.Analisar(memoria.Texto);
            HashSet<string> encontrados = resultado.Achados.Select(achado => achado.Tipo).ToHashSet(StringComparer.Ordinal);
            HashSet<string> esperados = memoria.Tipos.ToHashSet(StringComparer.Ordinal);

            foreach (string tipo in encontrados.Union(esperados))
            {
                (int vp, int fp, int fn) = contagem.GetValueOrDefault(tipo);
                bool achou = encontrados.Contains(tipo);
                bool devia = esperados.Contains(tipo);

                contagem[tipo] = (vp + (achou && devia ? 1 : 0), fp + (achou && !devia ? 1 : 0), fn + (!achou && devia ? 1 : 0));

                if (achou != devia)
                {
                    divergencias.Add($"{memoria.Id}: {(achou ? "alerta a mais" : "não encontrou")} {tipo}");
                }
            }

            if (resultado.DeveBloquear == memoria.DeveBloquear)
            {
                acertosDeBloqueio++;
            }
            else
            {
                divergencias.Add($"{memoria.Id}: bloqueio esperado {memoria.DeveBloquear}, obtido {resultado.DeveBloquear}");
            }
        }

        List<MetricasPorTipo> porTipo = contagem
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => new MetricasPorTipo(item.Key, item.Value.Vp, item.Value.Fp, item.Value.Fn))
            .ToList();

        MetricasPorTipo geral = new(
            "Geral",
            porTipo.Sum(metrica => metrica.VerdadeirosPositivos),
            porTipo.Sum(metrica => metrica.FalsosPositivos),
            porTipo.Sum(metrica => metrica.FalsosNegativos));

        return new ResultadoDaAvaliacao(
            porTipo,
            geral,
            memorias.Count,
            memorias.Count(memoria => memoria.Tipos.Count > 0),
            acertosDeBloqueio,
            divergencias);
    }

    public static string ParaMarkdown(ResultadoDaAvaliacao avaliacao)
    {
        CultureInfo pt = CultureInfo.GetCultureInfo("pt-BR");
        StringBuilder texto = new();

        texto.AppendLine("| Tipo de dado | Acertos | Alertas a mais | Não encontrados | Precisão | Recall |");
        texto.AppendLine("|---|---:|---:|---:|---:|---:|");

        foreach (MetricasPorTipo metrica in avaliacao.PorTipo.Append(avaliacao.Geral))
        {
            string nome = metrica.Tipo == "Geral" ? "**Geral**" : metrica.Tipo;
            texto.AppendLine(string.Create(pt,
                $"| {nome} | {metrica.VerdadeirosPositivos} | {metrica.FalsosPositivos} | {metrica.FalsosNegativos} | {metrica.Precisao:P0} | {metrica.Recall:P0} |"));
        }

        texto.AppendLine();
        texto.AppendLine(string.Create(pt,
            $"Decisão de bloqueio correta em {avaliacao.AcertosDeBloqueio} de {avaliacao.Memorias} memórias ({avaliacao.MemoriasComDadoPessoal} com dado pessoal e {avaliacao.Memorias - avaliacao.MemoriasComDadoPessoal} sem)."));

        if (avaliacao.Divergencias.Count > 0)
        {
            texto.AppendLine();
            texto.AppendLine("Divergências:");
            foreach (string divergencia in avaliacao.Divergencias)
            {
                texto.AppendLine($"- {divergencia}");
            }
        }

        return texto.ToString();
    }
}
