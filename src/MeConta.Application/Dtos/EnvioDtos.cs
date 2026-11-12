using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Dtos;

/// <summary>Pergunta a enviar pelo WhatsApp (US07).</summary>
/// <param name="PerguntaId">Id de uma pergunta do catálogo ou da própria família.</param>
public sealed record EnviarPerguntaRequest(int PerguntaId);

/// <summary>
/// Pergunta enviada. O app abre o <c>LinkWhatsApp</c> para a mensagem sair pronta.
/// </summary>
public sealed record EnvioResponse(
    int Id,
    int IdosoId,
    int PerguntaId,
    string Pergunta,
    string Mensagem,
    string LinkWhatsApp,
    StatusEnvio Status,
    DateTime EnviadoEm,
    DateTime? RespondidoEm,
    int? PaginaId)
{
    public static EnvioResponse De(EnvioPergunta envio) => new(
        envio.Id,
        envio.IdosoId,
        envio.PerguntaId,
        envio.TextoPergunta,
        envio.Mensagem,
        envio.ObterLinkWhatsApp(),
        envio.Status,
        envio.EnviadoEm,
        envio.RespondidoEm,
        envio.Pagina?.Id);
}
