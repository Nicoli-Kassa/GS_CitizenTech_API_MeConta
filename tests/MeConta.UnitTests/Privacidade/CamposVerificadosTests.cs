using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;
using MeConta.Privacidade.Taint;

namespace MeConta.UnitTests.Privacidade;

/// <summary>
/// Verificação de vários campos de uma vez (título e transcrição da mesma memória).
/// </summary>
public sealed class CamposVerificadosTests
{
    private readonly MotorPii _motor = MotorPii.CriarPadrao();

    private CamposVerificados Verificar(string titulo, string transcricao) =>
        _motor.VerificarCampos(
        [
            new CampoDoUsuario("Titulo", new TextoDoUsuario(titulo)),
            new CampoDoUsuario("Transcricao", new TextoDoUsuario(transcricao)),
        ]);

    [Fact]
    public void VerificarCampos_CpfSoNaTranscricao_BloqueiaOTituloTambem()
    {
        CamposVerificados verificados = Verificar("O primeiro emprego", "Pra me registrar pediram o CPF 123.456.789-09.");

        Assert.True(verificados.Analise.DeveBloquear);
        Assert.Throws<PublicacaoBloqueadaException>(() => verificados.ConteudoParaPublicar("Titulo", familiaConfirmou: true));
        Assert.Throws<PublicacaoBloqueadaException>(() => verificados.ConteudoParaPublicar("Transcricao", familiaConfirmou: true));
    }

    [Fact]
    public void VerificarCampos_Achados_TrazemOCampoDeOrigemEPosicaoDentroDoCampo()
    {
        const string transcricao = "Meu telefone é (11) 98765-4321.";
        CamposVerificados verificados = Verificar("Tomo remédio para pressão", transcricao);

        AchadoPii saude = Assert.Single(verificados.Analise.Achados, achado => achado.Tipo == "Saude");
        AchadoPii telefone = Assert.Single(verificados.Analise.Achados, achado => achado.Tipo == "Telefone");
        Assert.Equal("Titulo", saude.Campo);
        Assert.Equal("Transcricao", telefone.Campo);
        Assert.Equal("(11) 98765-4321", transcricao.Substring(telefone.Inicio, telefone.Tamanho));
    }

    [Fact]
    public void VerificarCampos_EnderecoNoTituloEMoraSozinhaNaTranscricao_EncontraACombinacaoEntreOsCampos()
    {
        CamposVerificados verificados = Verificar("A casa da Rua das Flores, 123", "Hoje eu moro sozinha lá.");

        CombinacaoEncontrada combinacao = Assert.Single(verificados.Analise.Combinacoes);
        Assert.Equal("COMB-ENDERECO-ROTINA", combinacao.Id);
        Assert.Equal(NivelDeRisco.Alto, verificados.Analise.Faixa);
        Assert.False(verificados.Analise.DeveBloquear);
        Assert.True(verificados.Analise.ExigeConfirmacao);
    }

    [Fact]
    public void VerificarCampos_DadoMedio_PublicaSoComConfirmacaoEEntregaOTextoOriginal()
    {
        CamposVerificados verificados = Verificar("Tomo remédio para pressão", "Mas o bolo continua igual.");

        Assert.False(verificados.PodePublicar(familiaConfirmou: false));
        Assert.Equal("Tomo remédio para pressão", verificados.ConteudoParaPublicar("Titulo", familiaConfirmou: true));
        Assert.Equal("Tomo [dado de saúde removido]", verificados.ParaRegistro("Titulo"));
    }

    [Fact]
    public void VerificarCampos_SemDadoPessoal_IndiceZeroEPublicaSemConfirmacao()
    {
        CamposVerificados verificados = Verificar("O bolo de domingo", "Três ovos caipiras e muito carinho.");

        Assert.Empty(verificados.Analise.Achados);
        Assert.Equal(0, verificados.Analise.IndiceRiscoCidadao);
        Assert.Equal("O bolo de domingo", verificados.ConteudoParaPublicar("Titulo", familiaConfirmou: false));
    }

    [Fact]
    public void VerificarCampos_CampoGrandeDemais_AnaliseIncompletaBloqueiaComIndice100()
    {
        MotorPii motor = new(CatalogoDeRegras.CarregarPadrao(), new OpcoesDoMotorPii { TamanhoMaximoDoTexto = 10 });

        CamposVerificados verificados = motor.VerificarCampos(
        [
            new CampoDoUsuario("Titulo", new TextoDoUsuario("Bolo")),
            new CampoDoUsuario("Transcricao", new TextoDoUsuario("Um texto bem maior que o limite.")),
        ]);

        Assert.False(verificados.Analise.AnaliseCompleta);
        Assert.True(verificados.Analise.DeveBloquear);
        Assert.Equal(100, verificados.Analise.IndiceRiscoCidadao);
    }

    [Fact]
    public void VerificarCampos_NomesRepetidosOuCampoNaoVerificado_Recusa()
    {
        TextoDoUsuario texto = new("Bolo de domingo");

        Assert.Throws<ArgumentException>(() => _motor.VerificarCampos(
            [new CampoDoUsuario("Titulo", texto), new CampoDoUsuario("Titulo", texto)]));
        Assert.Throws<ArgumentException>(() => _motor.VerificarCampos([]));
        Assert.Throws<ArgumentException>(() =>
            Verificar("Bolo", "Ovos").ConteudoParaPublicar("Bairro", familiaConfirmou: false));
    }
}
