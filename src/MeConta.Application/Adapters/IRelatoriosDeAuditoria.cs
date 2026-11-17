namespace MeConta.Application.Adapters;

/// <summary>
/// Relatórios SARIF da auditoria do código (Semgrep): o de antes e o de depois das correções.
/// A implementação da API lê da pasta seguranca/relatorios do repositório.
/// </summary>
public interface IRelatoriosDeAuditoria
{
    /// <summary>Conteúdo dos dois relatórios. Nulo quando o arquivo não existe.</summary>
    Task<RelatoriosSarif> CarregarAsync(CancellationToken cancellationToken = default);
}

/// <param name="Antes">SARIF da auditoria antes das correções (antes-semgrep.sarif).</param>
/// <param name="Depois">SARIF da auditoria depois das correções (depois-semgrep.sarif).</param>
public sealed record RelatoriosSarif(string? Antes, string? Depois);
