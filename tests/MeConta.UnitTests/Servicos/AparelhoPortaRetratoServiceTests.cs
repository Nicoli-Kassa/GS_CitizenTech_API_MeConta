using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Application.Servicos;
using MeConta.Domain.Entidades;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Servicos;

public sealed class AparelhoPortaRetratoServiceTests
{
    private const string Chave = "demo-porta-retrato-vo-lourdes-2026";

    private readonly BancoFake _banco = new();
    private readonly RelogioFake _relogio = new(Agora);
    private readonly Idoso _lourdes;
    private readonly Idoso _tuca;
    private readonly Membro _bia;
    private readonly AparelhoPortaRetratoService _servico;

    public AparelhoPortaRetratoServiceTests()
    {
        _bia = BancoFake.ComId(new Membro("usuario-bia", "Bia Souza", Agora), BiaId);
        _banco.Membros.Add(_bia);

        Familia familia = CriarFamilia();
        _lourdes = BancoFake.ComId(CriarVoLourdes(familia), 1);
        _tuca = BancoFake.ComId(
            familia.CadastrarIdoso("Antônia Ribeiro", "Vó Tuca", new Telefone("(11) 98888-0001"), Agora),
            2);
        _banco.Idosos.AddRange([_lourdes, _tuca]);

        _banco.PortaRetratos.Add(BancoFake.ComId(new PortaRetrato(_lourdes, "k7m2q9xd", Chave, BiaId, Agora), 1));

        _servico = new AparelhoPortaRetratoService(
            new PortaRetratoRepositoryFake(_banco),
            new IdosoRepositoryFake(_banco),
            new EnvioRepositoryFake(_banco),
            new AvisoRepositoryFake(_banco),
            new MembroRepositoryFake(_banco),
            new UnidadeDeTrabalhoFake(_banco),
            _relogio);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("chave-que-nao-existe-em-lugar-nenhum")]
    public async Task ObterEstado_SemChaveOuChaveErrada_Recusa(string? chave)
    {
        CredencialInvalidaException erro = await Assert.ThrowsAsync<CredencialInvalidaException>(
            () => _servico.ObterEstadoAsync(chave));

        Assert.Equal(AparelhoPortaRetratoService.MensagemChaveInvalida, erro.Message);
    }

    [Fact]
    public async Task ObterEstado_SemPerguntaEsperando_MolduraApagada()
    {
        EstadoPortaRetratoResponse estado = await _servico.ObterEstadoAsync(Chave);

        Assert.False(estado.TemPerguntaNova);
        Assert.Equal(0, estado.PerguntasEsperando);
        Assert.Null(estado.Pergunta);
        Assert.Equal("Vó Lourdes", estado.Apelido);
        Assert.Equal("k7m2q9xd", estado.Codigo);
    }

    [Fact]
    public async Task ObterEstado_PerguntaEnviada_AcendeComPrimeiroNomeDeQuemPerguntou()
    {
        EnvioPergunta envio = Enviar(_lourdes, perguntaId: 7, Agora);

        EstadoPortaRetratoResponse estado = await _servico.ObterEstadoAsync(Chave);

        Assert.True(estado.TemPerguntaNova);
        Assert.Equal(1, estado.PerguntasEsperando);
        Assert.NotNull(estado.Pergunta);
        Assert.Equal(envio.Id, estado.Pergunta.EnvioId);
        Assert.Equal("Bia", estado.Pergunta.QuemPerguntou);
        Assert.Equal(envio.TextoPergunta, estado.Pergunta.Texto);
    }

    [Fact]
    public async Task ObterEstado_DuasPerguntas_MostraAMaisRecenteEContaAsDuas()
    {
        Enviar(_lourdes, perguntaId: 7, Agora);
        EnvioPergunta recente = Enviar(_lourdes, perguntaId: 8, Agora.AddHours(1));

        EstadoPortaRetratoResponse estado = await _servico.ObterEstadoAsync(Chave);

        Assert.Equal(2, estado.PerguntasEsperando);
        Assert.Equal(recente.Id, estado.Pergunta!.EnvioId);
    }

    [Fact]
    public async Task ObterEstado_PerguntaDeOutroIdoso_NaoAcende()
    {
        Enviar(_tuca, perguntaId: 7, Agora);

        EstadoPortaRetratoResponse estado = await _servico.ObterEstadoAsync(Chave);

        Assert.False(estado.TemPerguntaNova);
    }

    [Fact]
    public async Task ObterEstado_PerguntaJaRespondidaNoCaderno_NaoAcende()
    {
        EnvioPergunta envio = Enviar(_lourdes, perguntaId: 7, Agora);
        envio.Responder("O bolo de domingo", "audios/r.opus", 30, null, BiaId, Guid.NewGuid(), Agora.AddHours(1));

        EstadoPortaRetratoResponse estado = await _servico.ObterEstadoAsync(Chave);

        Assert.False(estado.TemPerguntaNova);
    }

    [Fact]
    public async Task QueroContar_PerguntaNaTela_CriaAvisoEApagaAMoldura()
    {
        EnvioPergunta envio = Enviar(_lourdes, perguntaId: 7, Agora);

        ResultadoQueroContar resultado = await _servico.QueroContarAsync(Chave, new QueroContarRequest(envio.Id));
        EstadoPortaRetratoResponse depois = await _servico.ObterEstadoAsync(Chave);

        Assert.True(resultado.Criado);
        Assert.Equal(envio.Id, resultado.Aviso.EnvioId);
        Assert.Equal("Vó Lourdes", resultado.Aviso.Apelido);
        Assert.Equal(envio.TextoPergunta, resultado.Aviso.Pergunta);
        Assert.False(resultado.Aviso.Visto);
        Assert.Single(_banco.Avisos);
        Assert.False(depois.TemPerguntaNova);
    }

    [Fact]
    public async Task QueroContar_MesmaPerguntaDuasVezes_NaoDuplicaOAviso()
    {
        EnvioPergunta envio = Enviar(_lourdes, perguntaId: 7, Agora);

        ResultadoQueroContar primeiro = await _servico.QueroContarAsync(Chave, new QueroContarRequest(envio.Id));
        ResultadoQueroContar segundo = await _servico.QueroContarAsync(Chave, new QueroContarRequest(envio.Id));

        Assert.False(segundo.Criado);
        Assert.Equal(primeiro.Aviso.Id, segundo.Aviso.Id);
        Assert.Single(_banco.Avisos);
    }

    [Fact]
    public async Task QueroContar_PerguntaDeOutroIdoso_Recusa()
    {
        EnvioPergunta daTuca = Enviar(_tuca, perguntaId: 7, Agora);

        RegraDeNegocioException erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.QueroContarAsync(Chave, new QueroContarRequest(daTuca.Id)));

        Assert.Equal(AvisoQueroContar.MensagemEnvioDeOutroIdoso, erro.Message);
        Assert.Empty(_banco.Avisos);
    }

    [Fact]
    public async Task QueroContar_PerguntaQueNaoExiste_RecusaComAMesmaMensagem()
    {
        RegraDeNegocioException erro = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => _servico.QueroContarAsync(Chave, new QueroContarRequest(999)));

        Assert.Equal(AvisoQueroContar.MensagemEnvioDeOutroIdoso, erro.Message);
    }

    [Fact]
    public async Task QueroContar_SemPerguntaApertandoVariasVezes_UmAvisoAteAJanelaPassar()
    {
        ResultadoQueroContar primeiro = await _servico.QueroContarAsync(Chave, new QueroContarRequest(null));
        _relogio.Avancar(TimeSpan.FromSeconds(30));
        ResultadoQueroContar repetido = await _servico.QueroContarAsync(Chave, new QueroContarRequest(null));
        _relogio.Avancar(AparelhoPortaRetratoService.JanelaDoAvisoSemPergunta);
        ResultadoQueroContar depoisDaJanela = await _servico.QueroContarAsync(Chave, new QueroContarRequest(null));

        Assert.True(primeiro.Criado);
        Assert.Null(primeiro.Aviso.EnvioId);
        Assert.Null(primeiro.Aviso.Pergunta);
        Assert.False(repetido.Criado);
        Assert.Equal(primeiro.Aviso.Id, repetido.Aviso.Id);
        Assert.True(depoisDaJanela.Criado);
        Assert.Equal(2, _banco.Avisos.Count);
    }

    [Fact]
    public async Task QueroContar_ChaveErrada_NaoCriaAviso()
    {
        await Assert.ThrowsAsync<CredencialInvalidaException>(
            () => _servico.QueroContarAsync("chave-que-nao-existe-em-lugar-nenhum", new QueroContarRequest(null)));

        Assert.Empty(_banco.Avisos);
    }

    [Fact]
    public async Task RegistrarConexao_Online_GuardaHoraDoContato()
    {
        await _servico.RegistrarConexaoAsync(Chave, new ConexaoPortaRetratoRequest(Online: true));

        PortaRetrato portaRetrato = _banco.PortaRetratos[0];
        Assert.True(portaRetrato.Online);
        Assert.Equal(Agora, portaRetrato.UltimoContatoEm);
        Assert.Equal(1, _banco.Salvamentos);
    }

    private EnvioPergunta Enviar(Idoso idoso, int perguntaId, DateTime enviadoEm)
    {
        EnvioPergunta envio = new(idoso, new PerguntaFake(perguntaId), BiaId, enviadoEm);
        _banco.Envios.Add(envio);
        _banco.Salvar();
        return envio;
    }
}
