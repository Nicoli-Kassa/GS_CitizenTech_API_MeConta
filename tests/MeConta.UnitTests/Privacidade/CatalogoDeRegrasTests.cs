using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;

namespace MeConta.UnitTests.Privacidade;

public sealed class CatalogoDeRegrasTests
{
    private const string RegraBase = """
        "id": "PII-PLACA-001", "tipo": "PlacaDeCarro", "rotulo": "Placa de carro", "nivel": "Medio",
        "padrao": "\\b[a-z]{3}-?\\d[a-z0-9]\\d{2}\\b", "contexto": ["placa", "carro"], "mascara": "Digitos",
        "substituicao": "[placa removida]", "explicacao": "A placa do carro mostra onde a pessoa costuma estar."
        """;

    [Fact]
    public void CarregarPadrao_RegrasDoModulo_CarregaTodasComIdsUnicos()
    {
        CatalogoDeRegras catalogo = CatalogoDeRegras.CarregarPadrao();

        Assert.Equal(17, catalogo.Regras.Count);
        Assert.Equal(catalogo.Regras.Count, catalogo.Regras.Select(regra => regra.Id).Distinct().Count());
        Assert.Equal(3, catalogo.Combinacoes.Count);
        Assert.False(string.IsNullOrWhiteSpace(catalogo.Versao));
    }

    [Fact]
    public void CarregarPadrao_CobreCpfSaudeESenha()
    {
        // Os três exemplos do brief: CPF, saúde e senhas.
        CatalogoDeRegras catalogo = CatalogoDeRegras.CarregarPadrao();
        HashSet<string> tipos = catalogo.Regras.Select(regra => regra.Tipo).ToHashSet();

        Assert.Contains("Cpf", tipos);
        Assert.Contains("Saude", tipos);
        Assert.Contains("Senha", tipos);
    }

    [Fact]
    public void CarregarDeJson_RegraNovaNoArquivo_MotorPassaAEncontrarODadoSemMudarCodigo()
    {
        string json = $$"""{ "versao": "teste", "regras": [{ {{RegraBase}} }] }""";
        MotorPii motor = new(CatalogoDeRegras.CarregarDeJson(json));

        ResultadoAnalisePii resultado = motor.Analisar("A placa do carro do seu avô era ABC-1D23.");

        AchadoPii achado = Assert.Single(resultado.Achados);
        Assert.Equal("PlacaDeCarro", achado.Tipo);
        Assert.Equal("A placa do carro do seu avô era [placa removida].", resultado.TextoSeguro);
    }

    [Theory]
    [InlineData("\"nivel\": \"Medio\"", "\"nivel\": \"Gravissimo\"", "nível")]
    [InlineData("\"mascara\": \"Digitos\"", "\"mascara\": \"Borrao\"", "máscara")]
    [InlineData("\"padrao\": \"\\\\b[a-z]{3}", "\"validador\": \"placa\", \"padrao\": \"\\\\b[a-z]{3}", "validador")]
    [InlineData("\\\\d{2}\\\\b\"", "\\\\d{2}(\\\\b\"", "expressão inválida")]
    [InlineData("\"substituicao\": \"[placa removida]\"", "\"substituicao\": \"\"", "substituicao")]
    public void CarregarDeJson_RegraComErro_RecusaDizendoARegraEOCampo(string trecho, string troca, string mensagem)
    {
        string regra = RegraBase.Replace(trecho, troca, StringComparison.Ordinal);
        string json = $$"""{ "versao": "teste", "regras": [{ {{regra}} }] }""";

        ConfiguracaoDeRegrasInvalidaException erro =
            Assert.Throws<ConfiguracaoDeRegrasInvalidaException>(() => CatalogoDeRegras.CarregarDeJson(json));

        Assert.Contains("PII-PLACA-001", erro.Message);
        Assert.Contains(mensagem, erro.Message);
    }

    [Fact]
    public void CarregarDeJson_IdRepetido_Recusa()
    {
        string json = $$"""{ "versao": "teste", "regras": [{ {{RegraBase}} }, { {{RegraBase}} }] }""";

        ConfiguracaoDeRegrasInvalidaException erro =
            Assert.Throws<ConfiguracaoDeRegrasInvalidaException>(() => CatalogoDeRegras.CarregarDeJson(json));

        Assert.Contains("repetido", erro.Message);
    }

    [Fact]
    public void CarregarDeJson_CombinacaoComTipoQueNaoExiste_Recusa()
    {
        string json = $$"""
            {
              "versao": "teste",
              "regras": [{ {{RegraBase}} }],
              "combinacoes": [{ "id": "COMB-X", "tipos": ["PlacaDeCarro", "Cpf"], "bonus": 10, "explicacao": "x" }]
            }
            """;

        ConfiguracaoDeRegrasInvalidaException erro =
            Assert.Throws<ConfiguracaoDeRegrasInvalidaException>(() => CatalogoDeRegras.CarregarDeJson(json));

        Assert.Contains("Cpf", erro.Message);
    }

    [Theory]
    [InlineData("isto não é json")]
    [InlineData("{ \"versao\": \"teste\", \"regras\": [] }")]
    public void CarregarDeJson_ArquivoInvalidoOuSemRegras_Recusa(string json)
    {
        Assert.Throws<ConfiguracaoDeRegrasInvalidaException>(() => CatalogoDeRegras.CarregarDeJson(json));
    }
}
