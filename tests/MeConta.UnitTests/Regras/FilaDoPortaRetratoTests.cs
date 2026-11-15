using MeConta.Domain.Entidades;
using MeConta.Domain.Regras;
using MeConta.UnitTests.Servicos;
using static MeConta.UnitTests.TestSupport;

namespace MeConta.UnitTests.Regras;

public sealed class FilaDoPortaRetratoTests
{
    [Fact]
    public void PerguntasEsperando_SemEnvios_ListaVazia()
    {
        IReadOnlyList<EnvioPergunta> fila = FilaDoPortaRetrato.PerguntasEsperando([], new HashSet<int>());

        Assert.Empty(fila);
    }

    [Fact]
    public void PerguntasEsperando_TiraRespondidasEAsQueJaTemAviso_DaMaisRecenteParaAMaisAntiga()
    {
        Idoso lourdes = BancoFake.ComId(CriarVoLourdes(), 1);
        EnvioPergunta antiga = CriarEnvio(lourdes, id: 10, Agora);
        EnvioPergunta comAviso = CriarEnvio(lourdes, id: 11, Agora.AddHours(1));
        EnvioPergunta respondida = CriarEnvio(lourdes, id: 12, Agora.AddHours(2));
        EnvioPergunta recente = CriarEnvio(lourdes, id: 13, Agora.AddHours(3));
        respondida.Responder(null, "audios/r.opus", 30, null, BiaId, Guid.NewGuid(), Agora.AddHours(4));

        IReadOnlyList<EnvioPergunta> fila = FilaDoPortaRetrato.PerguntasEsperando(
            [antiga, comAviso, respondida, recente],
            new HashSet<int> { 11 });

        Assert.Equal(new[] { 13, 10 }, fila.Select(envio => envio.Id));
    }

    [Theory]
    [InlineData("Bia", "Bia")]
    [InlineData("Bia Souza", "Bia")]
    [InlineData("  Cláudia   Souza ", "Cláudia")]
    [InlineData("", "Sua família")]
    [InlineData("   ", "Sua família")]
    public void PrimeiroNome_SoOPrimeiroNomeVaiParaOAparelho(string nome, string esperado)
    {
        Assert.Equal(esperado, FilaDoPortaRetrato.PrimeiroNome(nome));
    }

    private static EnvioPergunta CriarEnvio(Idoso idoso, int id, DateTime enviadoEm) =>
        BancoFake.ComId(new EnvioPergunta(idoso, new PerguntaFake(id), BiaId, enviadoEm), id);
}
