using System.Text.Json;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Servicos;

namespace MeConta.UnitTests.Remediacao;

/// <summary>
/// IA falsa: devolve o que o teste mandar e guarda cada pedido, para o teste conferir o que "saiu" da API.
/// </summary>
internal sealed class ExplicadorFake(bool disponivel = true) : IExplicadorDeRisco
{
    public List<PedidoDeTraducao> Pedidos { get; } = [];

    /// <summary>Monta a resposta a partir do pedido. Padrão: um alerta limpo por falha.</summary>
    public Func<PedidoDeTraducao, TraducaoDaIa> Responder { get; set; } = pedido => new TraducaoDaIa(
        pedido.Falhas.Select(falha => new AlertaTraduzido(falha.Id, $"Alerta da IA {falha.Id}", "Risco explicado pela IA.", "Faça isto.")).ToList(),
        pedido.TextoMascarado is null ? null : "Memória reescrita pela IA.",
        "modelo-teste");

    public Exception? Erro { get; set; }

    public string Nome => "Gemini";

    public bool Disponivel => disponivel;

    /// <summary>Tudo o que foi enviado à IA, como JSON (para procurar dados que não podiam sair).</summary>
    public string TudoQueSaiu => string.Join("\n", Pedidos.Select(pedido => JsonSerializer.Serialize(pedido)));

    public Task<TraducaoDaIa> ExplicarAsync(PedidoDeTraducao pedido, CancellationToken cancellationToken)
    {
        Pedidos.Add(pedido);
        return Erro is null ? Task.FromResult(Responder(pedido)) : Task.FromException<TraducaoDaIa>(Erro);
    }
}

/// <summary>Relatórios SARIF em memória.</summary>
internal sealed class RelatoriosFake(string? antes, string? depois) : IRelatoriosDeAuditoria
{
    public Task<RelatoriosSarif> CarregarAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new RelatoriosSarif(antes, depois));
}

/// <summary>Painel Risco Cidadão com os tipos que o teste escolher.</summary>
internal sealed class RiscoCidadaoFake(params RiscoPorTipo[] tipos) : IRiscoCidadaoService
{
    public Task<RiscoCidadaoResponse> ObterAsync(int dias = RiscoCidadaoService.PeriodoPadraoDias, CancellationToken cancellationToken = default) =>
        Task.FromResult(new RiscoCidadaoResponse(
            TestSupport.Agora,
            dias,
            50,
            "Medio",
            new TotaisRiscoCidadao(0, 0, 0, 0, 0, tipos.Sum(tipo => tipo.Quantidade), 0),
            tipos,
            [],
            [],
            [],
            "1.0.0"));
}

/// <summary>
/// Arquivos do repositório copiados para a pasta dos testes (ver o .csproj):
/// relatórios do Semgrep em "Seguranca/relatorios" e regras em "Seguranca/semgrep".
/// </summary>
internal static class Repositorio
{
    public static string Ler(params string[] caminho) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Seguranca", .. caminho]));

    public static IEnumerable<string> Arquivos(string pasta, string padrao) =>
        Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Seguranca", pasta), padrao);
}
