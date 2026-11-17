using System.Text.RegularExpressions;
using MeConta.Application.Adapters;
using MeConta.Application.Auditoria;
using MeConta.Application.Servicos;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;

namespace MeConta.UnitTests.Remediacao;

/// <summary>
/// As explicações locais são a reserva da IA: precisam cobrir todo tipo do catálogo
/// e toda regra do Semgrep, e não podem trazer dado pessoal.
/// </summary>
public sealed partial class ExplicacoesLocaisTests
{
    [Fact]
    public void TodoTipoDoCatalogoDeRegras_TemTextoProprio()
    {
        IEnumerable<string> tipos = CatalogoDeRegras.CarregarPadrao().Regras.Select(regra => regra.Tipo).Distinct();

        Assert.All(tipos, tipo => Assert.Contains(tipo, ExplicacoesLocais.TiposConhecidos));
    }

    [Fact]
    public void TodaRegraDoSemgrep_TemTextoProprio()
    {
        List<string> regras = Repositorio.Arquivos("semgrep", "*.yml")
            .SelectMany(arquivo => IdDaRegra().Matches(File.ReadAllText(arquivo)).Select(achado => achado.Groups[1].Value))
            .ToList();

        Assert.Equal(7, regras.Count);
        Assert.All(regras, regra => Assert.Contains(regra, ExplicacoesLocais.RegrasConhecidas));
    }

    /// <summary>
    /// Mesma regra da conferência das frases da IA: saúde e rotina da casa podem aparecer como assunto
    /// ("golpes de remédios milagrosos"), mas nenhum dado que identifique alguém.
    /// </summary>
    [Fact]
    public void TextosLocais_NaoTemDadoPessoal()
    {
        MotorPii motor = MotorPii.CriarPadrao();
        IEnumerable<FalhaParaTraduzir> falhas = ExplicacoesLocais.TiposConhecidos
            .Select(tipo => new FalhaParaTraduzir(tipo, ExplicacoesLocais.OrigemDadoPessoal, tipo, "Alto", "-"))
            .Concat(ExplicacoesLocais.RegrasConhecidas
                .Select(regra => new FalhaParaTraduzir("Codigo-1", ExplicacoesLocais.OrigemCodigo, regra, "Alto", "-")));

        string[] assuntos = ["Saude", "Vulnerabilidade"];
        IEnumerable<string> frases = falhas
            .Select(ExplicacoesLocais.Traduzir)
            .SelectMany(alerta => new[] { alerta.Alerta, alerta.RiscoReal, alerta.OQueFazer });

        foreach (string frase in frases)
        {
            Assert.False(motor.Analisar(frase).Achados.Any(achado => !assuntos.Contains(achado.Tipo)), frase);
        }
    }

    [Fact]
    public void TipoDesconhecido_UsaOTextoGenerico()
    {
        AlertaTraduzido alerta = ExplicacoesLocais.Traduzir(
            new FalhaParaTraduzir("Placa-1", ExplicacoesLocais.OrigemDadoPessoal, "Placa do carro", "Medio", "-"));

        Assert.Equal("Placa-1", alerta.Id);
        Assert.Equal("O texto tem um dado pessoal.", alerta.Alerta);
    }

    [Fact]
    public void Sarif_RelatorioDeAntes_CincoAchadosComRegraArquivoLinhaECwe()
    {
        RelatorioDeCodigo relatorio = LeitorDeSarif.Ler(Repositorio.Ler("relatorios", "antes-semgrep.sarif"));

        Assert.Equal("Semgrep OSS 1.185.0", relatorio.Ferramenta);
        Assert.Equal(5, relatorio.Achados.Count);
        AchadoDeCodigo log = Assert.Single(relatorio.Achados, achado => achado.Regra == "meconta-pii-para-log");
        Assert.Equal("src/MeConta.Api/Configuracao/TratadorDeExcecoes.cs", log.Arquivo);
        Assert.Equal(36, log.Linha);
        Assert.StartsWith("CWE-532", log.Cwe);
    }

    [Fact]
    public void Sarif_RelatorioDeDepois_SemAchados()
    {
        Assert.Empty(LeitorDeSarif.Ler(Repositorio.Ler("relatorios", "depois-semgrep.sarif")).Achados);
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("{\"version\":\"2.1.0\"}")]
    public void Sarif_ForaDoFormato_FormatException(string conteudo)
    {
        Assert.Throws<FormatException>(() => LeitorDeSarif.Ler(conteudo));
    }

    [GeneratedRegex(@"^\s*-\s*id:\s*(meconta-[a-z0-9-]+)", RegexOptions.Multiline)]
    private static partial Regex IdDaRegra();
}
