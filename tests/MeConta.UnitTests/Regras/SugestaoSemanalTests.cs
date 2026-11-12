using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Regras;
using MeConta.Domain.ValueObjects;

namespace MeConta.UnitTests.Regras;

public sealed class SugestaoSemanalTests
{
    private const int LourdesId = 3;

    private static readonly Semana EstaSemana = Semana.Contendo(new DateOnly(2026, 11, 9));

    private readonly SugestaoSemanal _sugestao = new();

    [Fact]
    public void Sugerir_MesmaSemana_SempreAMesmaPergunta()
    {
        IPergunta[] catalogo = CriarCatalogo(5);

        IPergunta? primeira = _sugestao.Sugerir(LourdesId, catalogo, new HashSet<int>(), EstaSemana);
        IPergunta[] emOutraOrdem = catalogo.OrderByDescending(pergunta => pergunta.Id).ToArray();
        IPergunta? segunda = _sugestao.Sugerir(LourdesId, emOutraOrdem, new HashSet<int>(), EstaSemana);

        Assert.NotNull(primeira);
        Assert.Equal(primeira.Id, segunda!.Id);
    }

    [Fact]
    public void Sugerir_SemanaSeguinte_MudaAPergunta()
    {
        IPergunta[] catalogo = CriarCatalogo(5);

        IPergunta? agora = _sugestao.Sugerir(LourdesId, catalogo, new HashSet<int>(), EstaSemana);
        IPergunta? depois = _sugestao.Sugerir(LourdesId, catalogo, new HashSet<int>(), EstaSemana.Seguinte());

        Assert.NotEqual(agora!.Id, depois!.Id);
    }

    [Fact]
    public void Sugerir_VariasSemanas_NuncaRepeteJaEnviada()
    {
        IPergunta[] catalogo = CriarCatalogo(10);
        HashSet<int> enviadas = [1, 2, 3, 4, 5, 6, 7];
        Semana semana = EstaSemana;

        for (int vez = 0; vez < 20; vez++)
        {
            IPergunta? sugerida = _sugestao.Sugerir(LourdesId, catalogo, enviadas, semana);

            Assert.NotNull(sugerida);
            Assert.DoesNotContain(sugerida.Id, enviadas);
            semana = semana.Seguinte();
        }
    }

    [Fact]
    public void Sugerir_TodasJaEnviadas_RetornaNulo()
    {
        IPergunta[] catalogo = CriarCatalogo(3);

        IPergunta? sugerida = _sugestao.Sugerir(LourdesId, catalogo, new HashSet<int> { 1, 2, 3 }, EstaSemana);

        Assert.Null(sugerida);
    }

    [Fact]
    public void Sugerir_PerguntasDaFamilia_NaoEntramNaSugestao()
    {
        IPergunta[] perguntas =
        [
            new PerguntaFake(1, TipoPergunta.DaFamilia),
            new PerguntaFake(2, TipoPergunta.DaFamilia),
            new PerguntaFake(3, TipoPergunta.Catalogo),
        ];

        IPergunta? sugerida = _sugestao.Sugerir(LourdesId, perguntas, new HashSet<int>(), EstaSemana);

        Assert.Equal(3, sugerida!.Id);
    }

    [Fact]
    public void Sugerir_SemPerguntas_RetornaNulo()
    {
        Assert.Null(_sugestao.Sugerir(LourdesId, [], new HashSet<int>(), EstaSemana));
    }

    private static IPergunta[] CriarCatalogo(int quantidade) =>
        Enumerable.Range(1, quantidade)
            .Select(id => (IPergunta)new PerguntaFake(id))
            .ToArray();
}
