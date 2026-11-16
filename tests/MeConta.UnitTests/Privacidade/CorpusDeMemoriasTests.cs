using MeConta.Privacidade.Motor;

namespace MeConta.UnitTests.Privacidade;

/// <summary>
/// Mede o motor em dois conjuntos de memórias simuladas:
/// o corpus de desenvolvimento (64 memórias, 38 com dado pessoal e 26 sem, incluindo armadilhas como
/// "panela de pressão", "me conta 1950" e "sou de câncer") e o conjunto de validação (30 memórias
/// escritas depois das regras, usadas para achar erros que o primeiro conjunto não mostrava).
/// </summary>
public sealed class CorpusDeMemoriasTests
{
    private static readonly string CaminhoDoCorpus = Path.Combine(AppContext.BaseDirectory, "Privacidade", AvaliacaoDoCorpus.NomeDoArquivo);
    private static readonly string CaminhoDaValidacao = Path.Combine(AppContext.BaseDirectory, "Privacidade", AvaliacaoDoCorpus.NomeDaValidacao);

    [Fact]
    public void Corpus_TodasAsMemorias_RecallEPrecisaoGeraisDePeloMenos95PorCento()
    {
        List<MemoriaRotulada> memorias = AvaliacaoDoCorpus.Carregar(CaminhoDoCorpus);

        ResultadoDaAvaliacao avaliacao = AvaliacaoDoCorpus.Avaliar(MotorPii.CriarPadrao(), memorias);

        Assert.True(avaliacao.Geral.Recall >= 0.95, AvaliacaoDoCorpus.ParaMarkdown(avaliacao));
        Assert.True(avaliacao.Geral.Precisao >= 0.95, AvaliacaoDoCorpus.ParaMarkdown(avaliacao));
    }

    [Fact]
    public void Corpus_DecisaoDeBloqueio_AcertaTodasAsMemorias()
    {
        List<MemoriaRotulada> memorias = AvaliacaoDoCorpus.Carregar(CaminhoDoCorpus);

        ResultadoDaAvaliacao avaliacao = AvaliacaoDoCorpus.Avaliar(MotorPii.CriarPadrao(), memorias);

        Assert.Equal(memorias.Count, avaliacao.AcertosDeBloqueio);
    }

    [Fact]
    public void Corpus_DadosDeRiscoAlto_NenhumFicaSemSerEncontrado()
    {
        string[] tiposDeRiscoAlto = ["Cpf", "Rg", "CartaoDeCredito", "DadosBancarios", "ChavePix", "Senha", "CodigoDeVerificacao"];
        List<MemoriaRotulada> memorias = AvaliacaoDoCorpus.Carregar(CaminhoDoCorpus);

        ResultadoDaAvaliacao avaliacao = AvaliacaoDoCorpus.Avaliar(MotorPii.CriarPadrao(), memorias);

        Assert.All(
            avaliacao.PorTipo.Where(metrica => tiposDeRiscoAlto.Contains(metrica.Tipo)),
            metrica => Assert.Equal(0, metrica.FalsosNegativos));
    }

    [Fact]
    public void Validacao_MemoriasEscritasDepoisDasRegras_RecallEPrecisaoDePeloMenos95PorCentoEBloqueioCerto()
    {
        List<MemoriaRotulada> memorias = AvaliacaoDoCorpus.Carregar(CaminhoDaValidacao);

        ResultadoDaAvaliacao avaliacao = AvaliacaoDoCorpus.Avaliar(MotorPii.CriarPadrao(), memorias);

        Assert.True(avaliacao.Geral.Recall >= 0.95, AvaliacaoDoCorpus.ParaMarkdown(avaliacao));
        Assert.True(avaliacao.Geral.Precisao >= 0.95, AvaliacaoDoCorpus.ParaMarkdown(avaliacao));
        Assert.Equal(memorias.Count, avaliacao.AcertosDeBloqueio);
    }
}
