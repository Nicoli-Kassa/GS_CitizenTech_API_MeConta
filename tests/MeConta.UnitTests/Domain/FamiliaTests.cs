using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Domain;

public sealed class FamiliaTests
{
    private const string CodigoValido = "AB3K9Z";

    [Fact]
    public void Construtor_Valido_NormalizaNomeEColocaCriadorComoMembro()
    {
        Familia familia = new("  Família Souza  ", BiaId, Agora);

        Assert.Equal("Família Souza", familia.Nome);
        Assert.Equal(Agora, familia.CriadaEm);
        Participacao criador = Assert.Single(familia.Participacoes);
        Assert.Equal(BiaId, criador.MembroId);
        Assert.Equal(PapelNaFamilia.Criador, criador.Papel);
        Assert.True(familia.EhMembro(BiaId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construtor_NomeEmBranco_PedeNome(string nome)
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new Familia(nome, BiaId, Agora));

        Assert.Equal("Dê um nome para a família", erro.Message);
    }

    [Fact]
    public void Construtor_NomeCom61Caracteres_PedeAte60()
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new Familia(new string('a', 61), BiaId, Agora));

        Assert.Equal("Use até 60 caracteres", erro.Message);
    }

    [Fact]
    public void Construtor_NomeComExatos60Caracteres_EhAceito()
    {
        Familia familia = new(new string('a', 60), BiaId, Agora);

        Assert.Equal(60, familia.Nome.Length);
    }

    [Fact]
    public void EhMembro_QuemNaoEntrou_RetornaFalso()
    {
        Familia familia = CriarFamilia();

        Assert.False(familia.EhMembro(ClaudiaId));
    }

    [Fact]
    public void GerarConvite_Valido_VenceEm48Horas()
    {
        Familia familia = CriarFamilia();

        Convite convite = familia.GerarConvite("ab3k9z", BiaId, Agora);

        Assert.Equal(CodigoValido, convite.Codigo);
        Assert.Equal(Agora.AddHours(48), convite.ExpiraEm);
        Assert.Single(familia.Convites);
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFG")]
    [InlineData("AB0K9Z")]
    public void GerarConvite_CodigoForaDoPadrao_LancaExcecao(string codigo)
    {
        Familia familia = CriarFamilia();

        Assert.Throws<ArgumentException>(() => familia.GerarConvite(codigo, BiaId, Agora));
    }

    [Fact]
    public void AceitarConvite_CodigoNoPrazo_AdicionaComoMembro()
    {
        Familia familia = CriarFamilia();
        familia.GerarConvite(CodigoValido, BiaId, Agora);

        familia.AceitarConvite(" ab3k9z ", ClaudiaId, Agora.AddHours(47));

        Participacao claudia = familia.Participacoes.Single(item => item.MembroId == ClaudiaId);
        Assert.Equal(PapelNaFamilia.Membro, claudia.Papel);
        Assert.True(familia.EhMembro(ClaudiaId));
    }

    [Fact]
    public void AceitarConvite_ExatamenteNas48Horas_EstaVencido()
    {
        Familia familia = CriarFamilia();
        familia.GerarConvite(CodigoValido, BiaId, Agora);

        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => familia.AceitarConvite(CodigoValido, ClaudiaId, Agora.AddHours(48)));

        Assert.Equal(Familia.MensagemConviteInvalido, erro.Message);
        Assert.False(familia.EhMembro(ClaudiaId));
    }

    [Fact]
    public void AceitarConvite_CodigoInexistente_NaoDaAcesso()
    {
        Familia familia = CriarFamilia();
        familia.GerarConvite(CodigoValido, BiaId, Agora);

        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => familia.AceitarConvite("ZZZZZZ", ClaudiaId, Agora));

        Assert.Equal(Familia.MensagemConviteInvalido, erro.Message);
        Assert.False(familia.EhMembro(ClaudiaId));
    }

    [Fact]
    public void AceitarConvite_QuemJaEhMembro_NaoDuplica()
    {
        Familia familia = CriarFamilia();
        familia.GerarConvite(CodigoValido, BiaId, Agora);

        familia.AceitarConvite(CodigoValido, BiaId, Agora);

        Assert.Single(familia.Participacoes);
    }

    [Fact]
    public void CadastrarIdoso_DadosValidos_GuardaTelefoneInternacional()
    {
        Familia familia = CriarFamilia();

        Idoso idoso = familia.CadastrarIdoso(" Lourdes Souza ", " Vó Lourdes ", new Telefone("(11) 98765-4321"), Agora);

        Assert.Equal("Lourdes Souza", idoso.Nome);
        Assert.Equal("Vó Lourdes", idoso.Apelido);
        Assert.Equal("5511987654321", idoso.Telefone!.Valor);
        Assert.True(idoso.TemWhatsApp);
        Assert.False(idoso.RespondeSozinho);
        Assert.Single(familia.Idosos);
    }

    [Fact]
    public void CadastrarIdoso_TelefoneJaNaFamilia_LancaConflito()
    {
        Familia familia = CriarFamilia();
        CriarVoLourdes(familia);

        ConflitoException erro = Assert.Throws<ConflitoException>(
            () => familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", new Telefone("11 98765 4321"), Agora));

        Assert.Equal(Familia.MensagemTelefoneRepetido, erro.Message);
        Assert.Single(familia.Idosos);
    }

    [Fact]
    public void CadastrarIdoso_DoisSemWhatsApp_SaoAceitos()
    {
        Familia familia = CriarFamilia();

        familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", null, Agora);
        familia.CadastrarIdoso("Lourdes Souza", "Vó Lourdes", null, Agora);

        Assert.Equal(2, familia.Idosos.Count);
        Assert.All(familia.Idosos, idoso => Assert.False(idoso.TemWhatsApp));
    }

    [Theory]
    [InlineData("", "Vó Lourdes")]
    [InlineData("Lourdes Souza", "  ")]
    public void CadastrarIdoso_NomeOuApelidoEmBranco_LancaExcecao(string nome, string apelido)
    {
        Familia familia = CriarFamilia();

        Assert.Throws<RegraDeNegocioException>(() => familia.CadastrarIdoso(nome, apelido, null, Agora));
        Assert.Empty(familia.Idosos);
    }

    [Fact]
    public void AtualizarIdoso_MantendoOProprioTelefone_EhAceito()
    {
        Familia familia = CriarFamilia();
        Idoso lourdes = CriarVoLourdes(familia);

        familia.AtualizarIdoso(lourdes, "Lourdes Souza", "Vovó Lourdes", new Telefone("11987654321"));

        Assert.Equal("Vovó Lourdes", lourdes.Apelido);
    }

    [Fact]
    public void AtualizarIdoso_TelefoneDeOutroIdoso_LancaConflito()
    {
        Familia familia = CriarFamilia();
        CriarVoLourdes(familia);
        Idoso antonio = familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", new Telefone("11912345678"), Agora);

        Assert.Throws<ConflitoException>(
            () => familia.AtualizarIdoso(antonio, "Antônio Souza", "Vô Antônio", new Telefone("11987654321")));
        Assert.Equal("5511912345678", antonio.Telefone!.Valor);
    }

    [Fact]
    public void MarcarRespondeSozinho_AposRoteiro_FicaVerdadeiro()
    {
        Idoso lourdes = CriarVoLourdes();

        lourdes.MarcarRespondeSozinho();

        Assert.True(lourdes.RespondeSozinho);
    }
}
