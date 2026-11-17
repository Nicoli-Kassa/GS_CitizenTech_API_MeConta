using MeConta.Application.Adapters;
using MeConta.Application.Servicos;
using MeConta.Privacidade.Motor;
using MeConta.UnitTests.Servicos;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Remediacao;

/// <summary>
/// A IA fica entre duas conferências do motor de dados pessoais: nada com dado pessoal sai para ela,
/// e nada com dado pessoal volta dela. Sem IA, valem as explicações locais.
/// Todos os números são fictícios.
/// </summary>
public sealed class TradutorDeRiscoTests
{
    private readonly ExplicadorFake _ia = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly TradutorDeRisco _tradutor;

    public TradutorDeRiscoTests()
    {
        _tradutor = new TradutorDeRisco(_ia, MotorPii.CriarPadrao(), _relogio);
    }

    private static PedidoDeTraducao PedidoDeCpf(string exemplo = "***.***.***-09", string? texto = "Trabalhei na fábrica. Meu [CPF removido] ficava na carteira.") =>
        new(
            PublicoDoAlerta.Familia,
            [new FalhaParaTraduzir("Cpf", ExplicacoesLocais.OrigemDadoPessoal, "CPF", "Alto", "Regra PII-CPF-001", exemplo)],
            texto);

    [Fact]
    public async Task SemChave_UsaExplicacoesLocaisSemChamarAIa()
    {
        TradutorDeRisco tradutor = new(new ExplicadorFake(disponivel: false), MotorPii.CriarPadrao(), _relogio);

        ResultadoDaTraducao resultado = await tradutor.TraduzirAsync(PedidoDeCpf());

        Assert.Equal(TradutorDeRisco.FonteLocal, resultado.Fonte);
        AlertaTraduzido alerta = Assert.Single(resultado.Alertas);
        Assert.Equal("Tem um CPF no texto.", alerta.Alerta);
        Assert.Contains("INSS", alerta.RiscoReal);
        Assert.Contains("[CPF removido]", resultado.VersaoSugerida);
        Assert.Equal(1, tradutor.Estatisticas().UsosDaReserva);
    }

    [Fact]
    public async Task IaRespondeSemDadoPessoal_UsaOTextoDaIa()
    {
        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(PedidoDeCpf());

        Assert.Equal("Gemini (modelo-teste)", resultado.Fonte);
        Assert.Null(resultado.Observacao);
        Assert.Equal("Alerta da IA Cpf", Assert.Single(resultado.Alertas).Alerta);
        Assert.Equal("Memória reescrita pela IA.", resultado.VersaoSugerida);
        Assert.Equal(1, _tradutor.Estatisticas().RespostasAproveitadas);
    }

    [Fact]
    public async Task IaDevolveUmCpfNoAlerta_FraseDescartadaETrocadaPelaLocal()
    {
        _ia.Responder = pedido => new TraducaoDaIa(
            [new AlertaTraduzido("Cpf", "Seu CPF 529.982.247-25 está exposto.", "Golpes.", "Apague.")],
            "Memória reescrita pela IA.",
            "modelo-teste");

        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(PedidoDeCpf());

        AlertaTraduzido alerta = Assert.Single(resultado.Alertas);
        Assert.Equal("Tem um CPF no texto.", alerta.Alerta);
        Assert.DoesNotContain("529", alerta.Alerta);
        Assert.Equal("Gemini (modelo-teste) + Local", resultado.Fonte);
        Assert.NotNull(resultado.Observacao);
        Assert.Equal(1, _tradutor.Estatisticas().FrasesDescartadas);
    }

    [Fact]
    public async Task IaDevolveTelefoneNaVersaoSugerida_FicaAVersaoMascarada()
    {
        _ia.Responder = pedido => new TraducaoDaIa(
            [new AlertaTraduzido("Cpf", "Tem CPF no texto.", "Golpe do falso INSS.", "Apague o número.")],
            "Trabalhei na fábrica. Liga pra mim no (11) 98765-4321.",
            "modelo-teste");

        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(PedidoDeCpf());

        Assert.Equal("Trabalhei na fábrica. Meu [CPF removido] ficava na carteira.", resultado.VersaoSugerida);
        Assert.Equal("Tem CPF no texto.", Assert.Single(resultado.Alertas).Alerta);
        Assert.EndsWith("+ Local", resultado.Fonte);
    }

    [Fact]
    public async Task AlertaQueFalaDeSaudeComoAssunto_EAproveitado()
    {
        PedidoDeTraducao pedido = new(
            PublicoDoAlerta.Familia,
            [new FalhaParaTraduzir("Saude", ExplicacoesLocais.OrigemDadoPessoal, "Dado de saúde", "Medio", "Regra PII-SAUDE-002", "r****** p*** p******")]);
        _ia.Responder = _ => new TraducaoDaIa(
            [new AlertaTraduzido("Saude", "A memória conta qual remédio a vó toma.", "Golpistas oferecem remédio milagroso para quem mora sozinha.", "Combine com a vó se quer contar.")],
            null,
            "modelo-teste");

        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(pedido);

        Assert.Equal("A memória conta qual remédio a vó toma.", Assert.Single(resultado.Alertas).Alerta);
        Assert.Equal("Gemini (modelo-teste)", resultado.Fonte);
    }

    [Fact]
    public async Task PedidoComDadoPessoalNoExemplo_NaoSaiParaAIa()
    {
        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(PedidoDeCpf(exemplo: "123.456.789-09"));

        Assert.Empty(_ia.Pedidos);
        Assert.Equal(TradutorDeRisco.FonteLocal, resultado.Fonte);
        Assert.Contains("barrado", resultado.Observacao);
        Assert.Equal(1, _tradutor.Estatisticas().EnviosBarrados);
    }

    [Fact]
    public async Task TextoMascaradoComDadoQueEscapou_NaoSaiParaAIa()
    {
        await _tradutor.TraduzirAsync(PedidoDeCpf(texto: "Meu e-mail é vo.lourdes@example.com"));

        Assert.Empty(_ia.Pedidos);
        Assert.Equal(1, _tradutor.Estatisticas().EnviosBarrados);
    }

    [Fact]
    public async Task IaFalha_UsaLocalEDescansaUmMinuto()
    {
        _ia.Erro = new HttpRequestException("429");

        ResultadoDaTraducao primeiro = await _tradutor.TraduzirAsync(PedidoDeCpf());
        ResultadoDaTraducao segundo = await _tradutor.TraduzirAsync(PedidoDeCpf(texto: "Outra memória."));

        Assert.Equal(TradutorDeRisco.FonteLocal, primeiro.Fonte);
        Assert.Equal(TradutorDeRisco.FonteLocal, segundo.Fonte);
        Assert.Single(_ia.Pedidos);
        Assert.True(_tradutor.Estatisticas().EmPausa);

        _ia.Erro = null;
        _relogio.Avancar(TradutorDeRisco.PausaDepoisDeFalha + TimeSpan.FromSeconds(1));
        ResultadoDaTraducao depoisDaPausa = await _tradutor.TraduzirAsync(PedidoDeCpf(texto: "Outra memória."));

        Assert.Equal("Gemini (modelo-teste)", depoisDaPausa.Fonte);
        Assert.Equal(2, _ia.Pedidos.Count);
    }

    [Fact]
    public async Task MesmoPedido_UsaARespostaGuardadaSemChamarAIaDeNovo()
    {
        await _tradutor.TraduzirAsync(PedidoDeCpf());
        ResultadoDaTraducao segunda = await _tradutor.TraduzirAsync(PedidoDeCpf());

        Assert.Single(_ia.Pedidos);
        Assert.Equal("Gemini (modelo-teste)", segunda.Fonte);
        Assert.Equal(1, _tradutor.Estatisticas().RespostasDoCache);
    }

    [Fact]
    public async Task IaEsqueceUmaFalha_CompletaComOTextoLocal()
    {
        PedidoDeTraducao pedido = new(
            PublicoDoAlerta.Gestor,
            [
                new FalhaParaTraduzir("Cpf-1", ExplicacoesLocais.OrigemDadoPessoal, "CPF", "Alto", "risco"),
                new FalhaParaTraduzir("Telefone-2", ExplicacoesLocais.OrigemDadoPessoal, "Telefone", "Medio", "risco"),
            ]);
        _ia.Responder = _ => new TraducaoDaIa(
            [new AlertaTraduzido("Cpf-1", "CPF aparece nas memórias.", "Golpe do falso INSS.", "Orientar as famílias.")],
            null,
            "modelo-teste");

        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(pedido);

        Assert.Equal(["Cpf-1", "Telefone-2"], resultado.Alertas.Select(alerta => alerta.Id));
        Assert.Equal("CPF aparece nas memórias.", resultado.Alertas[0].Alerta);
        Assert.Equal("Tem um número de telefone no texto.", resultado.Alertas[1].Alerta);
    }

    [Fact]
    public async Task SemFalhas_NemChamaAIa()
    {
        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(new PedidoDeTraducao(PublicoDoAlerta.Gestor, []));

        Assert.Empty(resultado.Alertas);
        Assert.Equal(TradutorDeRisco.FonteMotor, resultado.Fonte);
        Assert.Empty(_ia.Pedidos);
    }

    [Fact]
    public async Task FraseLongaDemais_EDescartada()
    {
        _ia.Responder = _ => new TraducaoDaIa(
            [new AlertaTraduzido("Cpf", new string('a', TradutorDeRisco.TamanhoMaximoDoCampo + 1), "Golpes.", "Apague.")],
            null,
            "modelo-teste");

        ResultadoDaTraducao resultado = await _tradutor.TraduzirAsync(PedidoDeCpf(texto: null));

        Assert.Equal("Tem um CPF no texto.", Assert.Single(resultado.Alertas).Alerta);
    }
}
