using MeConta.Privacidade.Motor;
using MeConta.UnitTests.Privacidade;

// Roda o motor de PII nos dois conjuntos de memórias simuladas e imprime as tabelas em Markdown.
MotorPii motor = MotorPii.CriarPadrao();
Console.WriteLine($"Regras: versão {motor.VersaoDasRegras}");

foreach ((string titulo, string arquivo) in new[]
{
    ("Corpus de desenvolvimento", AvaliacaoDoCorpus.NomeDoArquivo),
    ("Conjunto de validação", AvaliacaoDoCorpus.NomeDaValidacao),
})
{
    List<MemoriaRotulada> memorias = AvaliacaoDoCorpus.Carregar(Path.Combine(AppContext.BaseDirectory, "Privacidade", arquivo));
    Console.WriteLine();
    Console.WriteLine($"## {titulo} ({memorias.Count} memórias)");
    Console.WriteLine();
    Console.Write(AvaliacaoDoCorpus.ParaMarkdown(AvaliacaoDoCorpus.Avaliar(motor, memorias)));
}
