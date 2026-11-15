using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Pareamento do porta-retrato com o idoso, feito pela família (US15).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/idosos/{idosoId:int}/porta-retrato")]
[Produces("application/json")]
[Tags("Porta-retrato")]
public sealed class PortaRetratoController(IPortaRetratoService portaRetratos) : ControllerBase
{
    /// <summary>
    /// Pareia um porta-retrato com o idoso e devolve o código e a chave do aparelho.
    /// A chave aparece só nesta resposta. Se o idoso já tinha porta-retrato, a chave antiga para de funcionar.
    /// </summary>
    /// <response code="201">Porta-retrato pareado.</response>
    /// <response code="403">O idoso não é da família de quem está logado.</response>
    /// <response code="404">Idoso não encontrado.</response>
    [HttpPost]
    [ProducesResponseType<PortaRetratoPareadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortaRetratoPareadoResponse>> ParearAsync(
        int idosoId,
        CancellationToken cancellationToken)
    {
        PortaRetratoPareadoResponse pareado = await portaRetratos.ParearAsync(idosoId, cancellationToken);
        return CreatedAtAction(nameof(ObterAsync), new { idosoId }, pareado);
    }

    /// <summary>Porta-retrato do idoso e se ele está conectado agora.</summary>
    /// <response code="200">Dados do porta-retrato (sem a chave).</response>
    /// <response code="404">O idoso não tem porta-retrato.</response>
    [HttpGet]
    [ProducesResponseType<PortaRetratoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PortaRetratoResponse>> ObterAsync(int idosoId, CancellationToken cancellationToken) =>
        Ok(await portaRetratos.ObterAsync(idosoId, cancellationToken));

    /// <summary>Desliga o porta-retrato do idoso. Os avisos já recebidos continuam no histórico.</summary>
    /// <response code="204">Porta-retrato removido.</response>
    /// <response code="404">O idoso não tem porta-retrato.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesparearAsync(int idosoId, CancellationToken cancellationToken)
    {
        await portaRetratos.DesparearAsync(idosoId, cancellationToken);
        return NoContent();
    }
}
