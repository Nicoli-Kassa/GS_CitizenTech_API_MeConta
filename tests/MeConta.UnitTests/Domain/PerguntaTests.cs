using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Domain;

public sealed class PerguntaTests
{
    private const int FamiliaSouzaId = 7;
    private const int OutraFamiliaId = 8;

    [Fact]
    public void PerguntaCatalogo_QualquerFamilia_PodeVer()
    {
        Pergunta pergunta = CriarPerguntaDoCatalogo();

        Assert.Equal(TipoPergunta.Catalogo, pergunta.Tipo);
        Assert.True(pergunta.VisivelPara(FamiliaSouzaId));
        Assert.True(pergunta.VisivelPara(OutraFamiliaId));
    }

    [Fact]
    public void PerguntaDaFamilia_SoAFamiliaQueCriou_PodeVer()
    {
        Pergunta pergunta = CriarPerguntaDaFamilia("Vó, como a senhora conheceu o vô?");

        Assert.Equal(TipoPergunta.DaFamilia, pergunta.Tipo);
        Assert.True(pergunta.VisivelPara(FamiliaSouzaId));
        Assert.False(pergunta.VisivelPara(OutraFamiliaId));
    }

    [Fact]
    public void Polimorfismo_ListaDeIPergunta_CadaTipoDecideAVisibilidade()
    {
        IPergunta[] perguntas =
        [
            CriarPerguntaDoCatalogo(),
            CriarPerguntaDaFamilia("Vó, como a senhora conheceu o vô?"),
        ];

        int visiveisParaOutraFamilia = perguntas.Count(pergunta => pergunta.VisivelPara(OutraFamiliaId));

        Assert.Equal(1, visiveisParaOutraFamilia);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Receita?")]
    [InlineData("   Bolo?   ")]
    public void Construtor_TextoComMenosDe10Caracteres_PedePerguntaMaior(string texto)
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => CriarPerguntaDaFamilia(texto));

        Assert.Equal("Escreva uma pergunta um pouco maior", erro.Message);
    }

    [Fact]
    public void Construtor_TextoCom201Caracteres_LancaExcecao()
    {
        Assert.Throws<RegraDeNegocioException>(() => CriarPerguntaDaFamilia(new string('a', 201)));
    }

    [Fact]
    public void Construtor_TextoComExatos10E200Caracteres_SaoAceitos()
    {
        Pergunta minima = CriarPerguntaDaFamilia(new string('a', 10));
        Pergunta maxima = CriarPerguntaDaFamilia(new string('a', 200));

        Assert.Equal(10, minima.Texto.Length);
        Assert.Equal(200, maxima.Texto.Length);
    }

    [Fact]
    public void Construtor_SemCategoria_PedeCategoria()
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => new PerguntaCatalogo("Qual era o segredo do bolo?", categoriaId: 0));

        Assert.Equal("Escolha uma categoria para a pergunta", erro.Message);
    }

    [Fact]
    public void PerguntaDaFamilia_FiltroAcusaGolpe_NaoEhCriada()
    {
        RegraDeNegocioException erro = Assert.Throws<RegraDeNegocioException>(
            () => CriarPerguntaDaFamilia("Vó, me manda um PIX?", new FiltroFake(suspeito: true)));

        Assert.Equal(PerguntaDaFamilia.MensagemPedidoSuspeito, erro.Message);
    }

    private static PerguntaDaFamilia CriarPerguntaDaFamilia(string texto, FiltroFake? filtro = null) =>
        new(texto, categoriaId: 2, FamiliaSouzaId, BiaId, Agora, filtro ?? new FiltroFake(suspeito: false));
}
