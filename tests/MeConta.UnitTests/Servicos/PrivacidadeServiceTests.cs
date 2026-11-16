using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Privacidade.Motor;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Servicos;

/// <summary>
/// Filtro de dados pessoais antes do mapa do bairro (US16, US17, US18).
/// Todas as memórias e números são fictícios.
/// </summary>
public sealed class PrivacidadeServiceTests
{
    private readonly BancoFake _banco = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly ArmazenamentoFake _armazenamento = new();
    private readonly Idoso _lourdes;
    private readonly PrivacidadeService _servico;
    private int _proximoEnvio = 10;

    public PrivacidadeServiceTests()
    {
        Membro bia = BancoFake.ComId(new Membro("usuario-bia", "Bia Souza", Agora), BiaId);
        _banco.Membros.Add(bia);
        _lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        _banco.Idosos.Add(_lourdes);

        _servico = new PrivacidadeService(
            new ControleDeAcessoFake(_banco, bia, 1),
            new PaginaRepositoryFake(_banco),
            new MemoriaDoBairroRepositoryFake(_banco),
            new RegistroDePrivacidadeRepositoryFake(_banco),
            _armazenamento,
            MotorPii.CriarPadrao(),
            new UnidadeDeTrabalhoFake(_banco),
            _relogio);
    }

    /// <summary>Página do caderno da Vó Lourdes, com áudio guardado no armazenamento falso.</summary>
    private PaginaCaderno CriarPagina(string titulo, string? transcricao = null)
    {
        EnvioPergunta envio = BancoFake.ComId(
            new EnvioPergunta(_lourdes, CriarPerguntaDoCatalogo(), BiaId, Agora),
            _proximoEnvio++);
        BancoFake.ComNavegacao(envio, nameof(EnvioPergunta.Idoso), _lourdes);

        string audio = $"audios/pagina-{envio.Id}.opus";
        _armazenamento.Arquivos[audio] = [1, 2, 3];

        PaginaCaderno pagina = BancoFake.ComId(
            envio.Responder(titulo, audio, 30, null, BiaId, Guid.NewGuid(), Agora),
            envio.Id + 100);
        pagina.DefinirTranscricao(transcricao);
        _banco.Paginas.Add(pagina);
        return pagina;
    }

    private static PublicarMemoriaRequest Pedido(bool confirmo = false, string bairro = "Pirituba") =>
        new(bairro, IdosoAutorizou: true, ConfirmoDadosPessoais: confirmo);

    // ---------- Análise ----------

    [Fact]
    public async Task Analisar_MemoriaSemDadoPessoal_LiberadaERegistradaNoHistorico()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo", "Três ovos caipiras e uma xícara de açúcar.");

        AnalisePrivacidadeResponse analise = await _servico.AnalisarPaginaAsync(pagina.Id);

        Assert.Equal("Liberada", analise.Resultado);
        Assert.Equal(0, analise.IndiceRiscoCidadao);
        Assert.Empty(analise.Achados);
        RegistroDePrivacidade registro = Assert.Single(_banco.Registros);
        Assert.Equal(OrigemDaVerificacao.Analise, registro.Origem);
        Assert.Equal(ResultadoDaVerificacao.Liberada, registro.Resultado);
        Assert.Null(registro.Bairro);
    }

    [Fact]
    public async Task Analisar_CpfNaTranscricao_BloqueadaComTrechoMascaradoEVersaoSegura()
    {
        PaginaCaderno pagina = CriarPagina("O primeiro emprego", "Na fábrica pediram meu CPF, 123.456.789-09, e eu comecei na segunda.");

        AnalisePrivacidadeResponse analise = await _servico.AnalisarPaginaAsync(pagina.Id);

        Assert.Equal("Bloqueada", analise.Resultado);
        Assert.True(analise.Bloqueada);
        Assert.Equal("Alto", analise.Faixa);
        AchadoResponse achado = Assert.Single(analise.Achados);
        Assert.Equal("Cpf", achado.Tipo);
        Assert.Equal("Transcricao", achado.Campo);
        Assert.Equal("***.***.***-09", achado.TrechoMascarado);
        Assert.Equal("Na fábrica pediram meu CPF, [CPF removido], e eu comecei na segunda.", analise.VersaoSegura.Transcricao);
        Assert.Equal("O primeiro emprego", analise.VersaoSegura.Titulo);
    }

    [Fact]
    public async Task Analisar_PaginaSemTranscricao_VersaoSeguraSemTranscricao()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");

        AnalisePrivacidadeResponse analise = await _servico.AnalisarPaginaAsync(pagina.Id);

        Assert.Null(analise.VersaoSegura.Transcricao);
    }

    [Fact]
    public async Task Analisar_PaginaQueNaoExiste_NaoEncontrada()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.AnalisarPaginaAsync(999));
    }

    // ---------- Publicação no mapa ----------

    [Fact]
    public async Task Publicar_MemoriaSemDadoPessoal_VaiParaOMapaSoComApelidoTituloETexto()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo", "Três ovos caipiras.");

        PublicacaoResponse publicacao = await _servico.PublicarAsync(pagina.Id, Pedido());

        MemoriaDoBairro memoria = Assert.Single(_banco.Memorias);
        Assert.Equal("Pirituba", memoria.Bairro);
        Assert.Equal("Vó Lourdes", publicacao.Memoria.Apelido);
        Assert.Equal("O bolo de domingo", publicacao.Memoria.Titulo);
        Assert.Equal("Três ovos caipiras.", publicacao.Memoria.Transcricao);
        Assert.Equal($"/api/v1/memorias/{memoria.Id}/audio", publicacao.Memoria.AudioUrl);
        Assert.Equal("Publicada", publicacao.Analise.Resultado);
        Assert.Equal(ResultadoDaVerificacao.Publicada, Assert.Single(_banco.Registros).Resultado);
    }

    [Fact]
    public async Task Publicar_SemAutorizacaoDoIdoso_RecusaSemAnalisarNemRegistrar()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");

        RegraDeNegocioException erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.PublicarAsync(pagina.Id, new PublicarMemoriaRequest("Pirituba", IdosoAutorizou: false)));

        Assert.Equal(MemoriaDoBairro.MensagemSemAutorizacao("Vó Lourdes"), erro.Message);
        Assert.Empty(_banco.Memorias);
        Assert.Empty(_banco.Registros);
    }

    [Theory]
    [InlineData("", MemoriaDoBairro.MensagemBairroVazio)]
    [InlineData("Rua das Flores, 123", PrivacidadeService.MensagemBairroComDadoPessoal)]
    [InlineData("02998-000 cep", PrivacidadeService.MensagemBairroComDadoPessoal)]
    public async Task Publicar_BairroVazioOuComEndereco_Recusa(string bairro, string mensagem)
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");

        RegraDeNegocioException erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.PublicarAsync(pagina.Id, Pedido(bairro: bairro)));

        Assert.Equal(mensagem, erro.Message);
        Assert.Empty(_banco.Memorias);
    }

    [Fact]
    public async Task Publicar_CpfNaTranscricao_Recusa422RegistraOBloqueioENaoPublica()
    {
        PaginaCaderno pagina = CriarPagina("O primeiro emprego", "meu CPF é 12345678909");

        PublicacaoRecusadaException erro = await Assert.ThrowsAsync<PublicacaoRecusadaException>(
            () => _servico.PublicarAsync(pagina.Id, Pedido(confirmo: true)));

        Assert.True(erro.Analise.Bloqueada);
        Assert.Equal("Bloqueada", erro.Analise.Resultado);
        Assert.Empty(_banco.Memorias);

        RegistroDePrivacidade registro = Assert.Single(_banco.Registros);
        Assert.Equal(OrigemDaVerificacao.Publicacao, registro.Origem);
        Assert.Equal(ResultadoDaVerificacao.Bloqueada, registro.Resultado);
        Assert.Equal("Pirituba", registro.Bairro);
        AchadoRegistrado achado = Assert.Single(registro.Achados);
        Assert.Equal("Alto", achado.Nivel);
        Assert.DoesNotContain("456", achado.TrechoMascarado);
    }

    [Fact]
    public async Task Publicar_CpfNoTitulo_TambemBloqueia()
    {
        PaginaCaderno pagina = CriarPagina("CPF da vó: 123.456.789-09", "Guardado na gaveta.");

        PublicacaoRecusadaException erro = await Assert.ThrowsAsync<PublicacaoRecusadaException>(
            () => _servico.PublicarAsync(pagina.Id, Pedido()));

        Assert.Equal("Titulo", Assert.Single(erro.Analise.Achados).Campo);
    }

    [Fact]
    public async Task Publicar_DadoDeSaudeSemConfirmar_PedeConfirmacaoEDepoisPublica()
    {
        PaginaCaderno pagina = CriarPagina("Os remédios da vó", "Tomo remédio para pressão todo dia, depois do café.");

        PublicacaoRecusadaException erro = await Assert.ThrowsAsync<PublicacaoRecusadaException>(
            () => _servico.PublicarAsync(pagina.Id, Pedido(confirmo: false)));
        Assert.Equal("PrecisaConfirmar", erro.Analise.Resultado);
        Assert.False(erro.Analise.Bloqueada);
        Assert.Empty(_banco.Memorias);

        PublicacaoResponse publicacao = await _servico.PublicarAsync(pagina.Id, Pedido(confirmo: true));

        Assert.Equal("Tomo remédio para pressão todo dia, depois do café.", publicacao.Memoria.Transcricao);
        string historico = string.Join(", ", _banco.Registros.Select(registro => registro.Resultado));
        Assert.Equal("PrecisaConfirmar, Publicada", historico);
    }

    [Fact]
    public async Task Publicar_EnderecoNoTituloEMoraSozinhaNaTranscricao_CombinacaoApareceNaAnalise()
    {
        PaginaCaderno pagina = CriarPagina("A casa da Rua das Flores, 123", "Hoje eu moro sozinha lá.");

        PublicacaoRecusadaException erro = await Assert.ThrowsAsync<PublicacaoRecusadaException>(
            () => _servico.PublicarAsync(pagina.Id, Pedido()));

        CombinacaoResponse combinacao = Assert.Single(erro.Analise.Combinacoes);
        Assert.Equal("COMB-ENDERECO-ROTINA", combinacao.Id);
        Assert.Equal("Alto", erro.Analise.Faixa);
    }

    [Fact]
    public async Task Publicar_MemoriaQueJaEstaNoMapa_Conflito()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");
        await _servico.PublicarAsync(pagina.Id, Pedido());

        ConflitoException erro = await Assert.ThrowsAsync<ConflitoException>(() => _servico.PublicarAsync(pagina.Id, Pedido()));

        Assert.Equal(MemoriaDoBairro.MensagemJaNoMapa, erro.Message);
    }

    // ---------- Mapa do bairro ----------

    [Fact]
    public async Task ListarEOuvir_MemoriaNoMapa_AparecePeloBairroSemDiferenciarMaiusculas()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");
        PublicacaoResponse publicacao = await _servico.PublicarAsync(pagina.Id, Pedido());
        CriarPagina("Outra memória, ainda no caderno");

        IReadOnlyList<MemoriaDoBairroResponse> lista = await _servico.ListarDoBairroAsync("pirituba");
        ArquivoParaDownload audio = await _servico.AbrirAudioDaMemoriaAsync(publicacao.Memoria.Id);

        Assert.Equal(publicacao.Memoria.Id, Assert.Single(lista).Id);
        Assert.Equal("audio/ogg", audio.ContentType);
        Assert.Empty(await _servico.ListarDoBairroAsync("Lapa"));
    }

    [Fact]
    public async Task TirarDoMapa_MemoriaPublicada_SomeDaListaEOAudioDeixaDeTocar()
    {
        PaginaCaderno pagina = CriarPagina("O bolo de domingo");
        PublicacaoResponse publicacao = await _servico.PublicarAsync(pagina.Id, Pedido());

        await _servico.TirarDoMapaAsync(pagina.Id);

        Assert.Empty(await _servico.ListarDoBairroAsync("Pirituba"));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.AbrirAudioDaMemoriaAsync(publicacao.Memoria.Id));
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => _servico.TirarDoMapaAsync(pagina.Id));
    }

    [Fact]
    public async Task Historico_VariasVerificacoes_NuncaGuardaODadoEmClaro()
    {
        PaginaCaderno pagina = CriarPagina(
            "Anotações da vó",
            "CPF 123.456.789-09, telefone (11) 98765-4321, senha do banco é 4521 e moro sozinha.");

        await _servico.AnalisarPaginaAsync(pagina.Id);
        await Assert.ThrowsAsync<PublicacaoRecusadaException>(() => _servico.PublicarAsync(pagina.Id, Pedido(confirmo: true)));

        IEnumerable<string> trechos = _banco.Registros.SelectMany(registro => registro.Achados).Select(achado => achado.TrechoMascarado);
        Assert.All(trechos, trecho =>
        {
            Assert.DoesNotContain("456.789", trecho);
            Assert.DoesNotContain("98765", trecho);
            Assert.DoesNotContain("4521", trecho);
        });
    }
}
