using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Entidades;
using MeConta.Privacidade.Motor;
using MeConta.UnitTests.Servicos;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Remediacao;

/// <summary>
/// IA na remediação: alertas para a família, para o painel e para a auditoria do código.
/// Memórias e números fictícios; o CPF 123.456.789-09 é o número de teste usado em todo o projeto.
/// </summary>
public sealed class RemediacaoServiceTests
{
    private const string CpfDeTeste = "123.456.789-09";

    private readonly BancoFake _banco = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly ExplicadorFake _ia = new();
    private readonly Idoso _lourdes;
    private int _proximoEnvio = 10;

    public RemediacaoServiceTests()
    {
        Membro bia = BancoFake.ComId(new Membro("usuario-bia", "Bia Souza", Agora), BiaId);
        _banco.Membros.Add(bia);
        _lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        _banco.Idosos.Add(_lourdes);
    }

    private RemediacaoService CriarServico(
        ExplicadorFake? ia = null,
        RelatoriosFake? relatorios = null,
        RiscoCidadaoFake? painel = null,
        TradutorDeRisco? tradutor = null)
    {
        Membro bia = _banco.Membros[0];
        return new RemediacaoService(
            new ControleDeAcessoFake(_banco, bia, 1),
            new PaginaRepositoryFake(_banco),
            MotorPii.CriarPadrao(),
            tradutor ?? new TradutorDeRisco(ia ?? _ia, MotorPii.CriarPadrao(), _relogio),
            painel ?? new RiscoCidadaoFake(),
            relatorios ?? new RelatoriosFake(null, null),
            _relogio);
    }

    private PaginaCaderno CriarPagina(string titulo, string? transcricao)
    {
        EnvioPergunta envio = BancoFake.ComId(
            new EnvioPergunta(_lourdes, CriarPerguntaDoCatalogo(), BiaId, Agora),
            _proximoEnvio++);
        BancoFake.ComNavegacao(envio, nameof(EnvioPergunta.Idoso), _lourdes);

        PaginaCaderno pagina = BancoFake.ComId(
            envio.Responder(titulo, $"audios/pagina-{envio.Id}.opus", 30, null, BiaId, Guid.NewGuid(), Agora),
            envio.Id + 100);
        pagina.DefinirTranscricao(transcricao);
        _banco.Paginas.Add(pagina);
        return pagina;
    }

    // ---------- Explicação para a família ----------

    [Fact]
    public async Task ExplicarPagina_ComCpf_AlertaMascaradoEVersaoSemONumero()
    {
        PaginaCaderno pagina = CriarPagina(
            "A fábrica de tecidos",
            $"Entrei na fábrica em 1962. No crachá tinha meu CPF {CpfDeTeste} e o número da seção.");

        ExplicacaoDeRiscoResponse explicacao = await CriarServico(new ExplicadorFake(disponivel: false)).ExplicarPaginaAsync(pagina.Id);

        Assert.Equal("Bloqueada", explicacao.Resultado);
        Assert.Equal(70, explicacao.IndiceRiscoCidadao);
        Assert.Contains("não pode ir para o mapa", explicacao.Resumo);
        AlertaDeRiscoResponse alerta = Assert.Single(explicacao.Alertas);
        Assert.Equal("CPF", alerta.Rotulo);
        Assert.Equal("Alto", alerta.Nivel);
        Assert.Equal(["***.***.***-09"], alerta.Trechos);
        Assert.Equal(TradutorDeRisco.FonteLocal, explicacao.Fonte);
        Assert.Contains("[CPF removido]", explicacao.VersaoSugerida);
        Assert.DoesNotContain(CpfDeTeste, explicacao.VersaoSugerida);
    }

    [Fact]
    public async Task ExplicarPagina_ComIa_NenhumDadoPessoalSaiDaApi()
    {
        PaginaCaderno pagina = CriarPagina(
            $"Meu CPF é {CpfDeTeste}",
            $"Moro na Rua das Flores, 123 e fico sozinha de tarde. Meu CPF é {CpfDeTeste}, liga no (11) 98765-4321.");

        ExplicacaoDeRiscoResponse explicacao = await CriarServico().ExplicarPaginaAsync(pagina.Id);

        // O que foi para a IA: tipos, níveis, trechos mascarados e texto com avisos. Nunca o dado.
        string enviado = _ia.TudoQueSaiu;
        Assert.DoesNotContain("456.789", enviado);
        Assert.DoesNotContain("98765", enviado);
        Assert.DoesNotContain("Rua das Flores, 123", enviado);
        Assert.Contains("[CPF removido]", Assert.Single(_ia.Pedidos).TextoMascarado);
        Assert.StartsWith("Gemini", explicacao.Fonte);
        Assert.Equal("Memória reescrita pela IA.", explicacao.VersaoSugerida);
    }

    [Fact]
    public async Task ExplicarPagina_EnderecoERotina_TemAlertaDaCombinacao()
    {
        PaginaCaderno pagina = CriarPagina(
            "A casa",
            "A casa fica na Rua das Flores, 123. Moro sozinha e de tarde fico sozinha em casa.");

        ExplicacaoDeRiscoResponse explicacao = await CriarServico(new ExplicadorFake(disponivel: false)).ExplicarPaginaAsync(pagina.Id);

        Assert.Equal("PrecisaConfirmar", explicacao.Resultado);
        AlertaDeRiscoResponse combinacao = Assert.Single(explicacao.Alertas, alerta => alerta.Rotulo == "Combinação perigosa");
        Assert.Contains("Endereço junto com a rotina", combinacao.RiscoReal);
        Assert.Contains(explicacao.Alertas, alerta => alerta.Rotulo == "Endereço");
    }

    [Fact]
    public async Task ExplicarPagina_SemDadoPessoal_NemChamaAIa()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo", "Três ovos caipiras e uma xícara de açúcar.");

        ExplicacaoDeRiscoResponse explicacao = await CriarServico().ExplicarPaginaAsync(pagina.Id);

        Assert.Equal("Liberada", explicacao.Resultado);
        Assert.Empty(explicacao.Alertas);
        Assert.Equal(TradutorDeRisco.FonteMotor, explicacao.Fonte);
        Assert.Empty(_ia.Pedidos);
    }

    [Fact]
    public async Task ExplicarPagina_Inexistente_404()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => CriarServico().ExplicarPaginaAsync(999));
    }

    // ---------- Painel executivo ----------

    [Fact]
    public async Task AlertasDoPainel_UmAlertaPorTipoSemQuantidadesNoPedido()
    {
        RiscoCidadaoFake painel = new(
            new RiscoPorTipo("Saude", "Dado de saúde", "Medio", 3),
            new RiscoPorTipo("Cpf", "CPF", "Alto", 1));

        TradutorDeRisco tradutor = new(_ia, MotorPii.CriarPadrao(), _relogio);

        AlertasDoPainelResponse alertas = await CriarServico(painel: painel, tradutor: tradutor).AlertasDoPainelAsync(14);

        Assert.Equal(["Saude", "Cpf"], alertas.Alertas.Select(alerta => alerta.Tipo));
        Assert.Equal(3, alertas.Alertas[0].Quantidade);
        Assert.Equal("Alerta da IA Saude-1", alertas.Alertas[0].Alerta);
        Assert.Equal(1, alertas.Ia.Chamadas);

        // A quantidade não vai no pedido: mais uma memória com CPF não gera outra chamada à IA.
        RiscoCidadaoFake painelDepois = new(
            new RiscoPorTipo("Saude", "Dado de saúde", "Medio", 4),
            new RiscoPorTipo("Cpf", "CPF", "Alto", 2));
        AlertasDoPainelResponse depois = await CriarServico(painel: painelDepois, tradutor: tradutor).AlertasDoPainelAsync(14);

        Assert.Single(_ia.Pedidos);
        Assert.Equal(2, depois.Alertas[1].Quantidade);
        Assert.Equal(1, depois.Ia.RespostasDoCache);
    }

    // ---------- Auditoria do código ----------

    [Fact]
    public async Task Auditoria_RelatoriosReais_CincoAchadosViramDuasFalhasCorrigidas()
    {
        RelatoriosFake relatorios = new(
            Repositorio.Ler("relatorios", "antes-semgrep.sarif"),
            Repositorio.Ler("relatorios", "depois-semgrep.sarif"));

        AuditoriaDeCodigoResponse auditoria = await CriarServico(new ExplicadorFake(disponivel: false), relatorios).AuditoriaAsync();

        Assert.StartsWith("Semgrep", auditoria.Ferramenta);
        Assert.Equal(5, auditoria.AchadosAntes);
        Assert.Equal(0, auditoria.AchadosDepois);
        Assert.Equal(2, auditoria.Corrigidas);
        Assert.Equal(0, auditoria.Abertas);

        FalhaDeCodigoResponse portaRetrato = Assert.Single(auditoria.Falhas, falha => falha.Regra == "meconta-pii-para-porta-retrato");
        Assert.Equal([54, 61, 104, 111], portaRetrato.Linhas);
        Assert.Equal(RemediacaoService.SituacaoCorrigida, portaRetrato.Situacao);

        FalhaDeCodigoResponse log = Assert.Single(auditoria.Falhas, falha => falha.Regra == "meconta-pii-para-log");
        Assert.StartsWith("CWE-532", log.Cwe);
        Assert.Contains("registro de erros", log.Alerta);
    }

    [Fact]
    public async Task Auditoria_FalhaQueContinua_ApareceComoAberta()
    {
        string antes = Repositorio.Ler("relatorios", "antes-semgrep.sarif");

        AuditoriaDeCodigoResponse auditoria = await CriarServico(relatorios: new RelatoriosFake(antes, antes)).AuditoriaAsync();

        Assert.Equal(2, auditoria.Abertas);
        Assert.All(auditoria.Falhas, falha => Assert.Equal(RemediacaoService.SituacaoAberta, falha.Situacao));
    }

    [Fact]
    public async Task Auditoria_SemRelatorios_404ComOrientacao()
    {
        RecursoNaoEncontradoException erro = await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => CriarServico().AuditoriaAsync());

        Assert.Equal(RemediacaoService.MensagemSemRelatorio, erro.Message);
    }
}
