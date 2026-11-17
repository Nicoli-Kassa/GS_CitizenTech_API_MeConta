using MeConta.Application.Adapters;

namespace MeConta.Api.Adapters;

/// <summary>
/// Escreve o log de auditoria do filtro de dados pessoais no log da API (console e, no servidor, journald).
/// Exemplo: "Privacidade: Publicacao da página 4 por membro 1 → Bloqueada, Risco Cidadão 70, dados: CPF ***.***.***-09".
/// </summary>
public sealed class AuditoriaEmLog(ILogger<AuditoriaEmLog> logger) : IRegistroDeAuditoria
{
    public void Registrar(EventoDePrivacidade evento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        string dados = evento.Dados.Count == 0 ? "nenhum" : string.Join("; ", evento.Dados);
        logger.LogInformation(
            "Privacidade: {Acao} da página {PaginaId} por membro {MembroId} → {Resultado}, Risco Cidadão {Indice}, dados: {Dados}",
            evento.Acao,
            evento.PaginaId,
            evento.MembroId,
            evento.Resultado,
            evento.IndiceRiscoCidadao,
            dados);
    }
}
