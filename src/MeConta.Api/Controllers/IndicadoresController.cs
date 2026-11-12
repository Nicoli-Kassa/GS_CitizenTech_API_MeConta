using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Indicador de impacto da Ficha da CitizenTech (US12).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/indicadores")]
[Produces("application/json")]
[Tags("Indicadores")]
public sealed class IndicadoresController(IIndicadorService indicadores) : ControllerBase
{
    /// <summary>
    /// "Conversas de verdade": % de idosos que responderam pelo menos uma pergunta na semana,
    /// minutos de áudio guardados e idosos que já respondem sozinhos.
    /// </summary>
    /// <param name="data">Qualquer dia da semana desejada (aaaa-mm-dd). Sem data, usa a semana atual.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    [HttpGet("conversas-de-verdade")]
    [ProducesResponseType<IndicadorConversasResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IndicadorConversasResponse>> ObterConversasDeVerdadeAsync(
        [FromQuery] DateOnly? data,
        CancellationToken cancellationToken) =>
        Ok(await indicadores.ObterConversasDeVerdadeAsync(data, cancellationToken));
}
