using MeConta.Application.Adapters;

namespace MeConta.Infrastructure.Adapters;

/// <summary>
/// Lê os relatórios SARIF do Semgrep da pasta seguranca/relatorios
/// (antes-semgrep.sarif e depois-semgrep.sarif, gerados como em docs/cyber/taint-analysis.md).
/// </summary>
public sealed class RelatoriosDeAuditoriaEmDisco(string pasta) : IRelatoriosDeAuditoria
{
    public const string ArquivoAntes = "antes-semgrep.sarif";
    public const string ArquivoDepois = "depois-semgrep.sarif";

    /// <summary>Relatórios maiores que isso não são lidos (proteção contra arquivo trocado por um enorme).</summary>
    public const long TamanhoMaximo = 5 * 1024 * 1024;

    public async Task<RelatoriosSarif> CarregarAsync(CancellationToken cancellationToken = default) =>
        new(await LerAsync(ArquivoAntes, cancellationToken), await LerAsync(ArquivoDepois, cancellationToken));

    private async Task<string?> LerAsync(string nome, CancellationToken cancellationToken)
    {
        FileInfo arquivo = new(Path.Combine(pasta, nome));
        if (!arquivo.Exists || arquivo.Length > TamanhoMaximo)
        {
            return null;
        }

        return await File.ReadAllTextAsync(arquivo.FullName, cancellationToken);
    }
}
