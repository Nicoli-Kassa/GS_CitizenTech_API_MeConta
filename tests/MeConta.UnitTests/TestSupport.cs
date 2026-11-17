using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Regras;
using MeConta.Domain.ValueObjects;

namespace MeConta.UnitTests;

/// <summary>
/// Dados fictícios e objetos prontos usados por vários testes.
/// Nenhum dado pessoal real: as personas são as hipóteses do projeto.
/// </summary>
internal static class TestSupport
{
    /// <summary>Terça-feira, 10/11/2026, 15h UTC (12h em Brasília).</summary>
    public static readonly DateTime Agora = new(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);

    public const int BiaId = 1;
    public const int ClaudiaId = 2;

    public static Familia CriarFamilia() => new("Família Souza", BiaId, Agora);

    public static Idoso CriarVoLourdes(Familia? familia = null) =>
        (familia ?? CriarFamilia()).CadastrarIdoso(
            "Lourdes Souza",
            "Vó Lourdes",
            new Telefone("(11) 98765-4321"),
            Agora);

    public static PerguntaCatalogo CriarPerguntaDoCatalogo(
        string texto = "Qual era o segredo do seu bolo de domingo?") =>
        new(texto, categoriaId: 1);
}

/// <summary>
/// Pergunta falsa para testar regras que recebem <see cref="IPergunta"/>
/// sem depender do Id gerado pelo banco.
/// </summary>
internal sealed class PerguntaFake(int id, TipoPergunta tipo = TipoPergunta.Catalogo, string? texto = null) : IPergunta
{
    public int Id { get; } = id;

    public string Texto { get; } = texto ?? $"Pergunta de teste número {id}";

    public int CategoriaId { get; } = 1;

    public TipoPergunta Tipo { get; } = tipo;

    public bool VisivelPara(int familiaId) => true;
}

/// <summary>
/// Filtro falso com resposta fixa, para testar a entidade isolada do filtro real.
/// </summary>
internal sealed class FiltroFake(bool suspeito) : IFiltroAntiGolpe
{
    public bool EhSuspeito(string texto) => suspeito;
}
