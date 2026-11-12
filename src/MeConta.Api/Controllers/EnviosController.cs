using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using MeConta.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Envio da pergunta pelo WhatsApp (US07).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/idosos/{idosoId:int}/envios")]
[Produces("application/json")]
[Tags("Envios")]
public sealed class EnviosController(IEnvioService envios) : ControllerBase
{
    /// <summary>
    /// Registra o envio e devolve a mensagem pronta e o link wa.me para o app abrir o WhatsApp.
    /// </summary>
    /// <response code="201">Envio registrado.</response>
    /// <response code="404">Idoso ou pergunta não encontrados.</response>
    /// <response code="409">O idoso não tem WhatsApp cadastrado ("Cadastre o WhatsApp").</response>
    [HttpPost]
    [ProducesResponseType<EnvioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnvioResponse>> EnviarAsync(
        int idosoId,
        EnviarPerguntaRequest request,
        CancellationToken cancellationToken)
    {
        EnvioResponse envio = await envios.EnviarAsync(idosoId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, envio);
    }

    /// <summary>Perguntas enviadas a este idoso, da mais recente para a mais antiga.</summary>
    /// <param name="idosoId">Id do idoso.</param>
    /// <param name="status">Filtro opcional: Enviada ou Respondida.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EnvioResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EnvioResponse>>> ListarAsync(
        int idosoId,
        [FromQuery] StatusEnvio? status,
        CancellationToken cancellationToken) =>
        Ok(await envios.ListarAsync(idosoId, status, cancellationToken));
}
