using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Avisos "quero contar" do porta-retrato, para a família ver no app (US15).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Porta-retrato")]
public sealed class AvisosController(IAvisoService avisos) : ControllerBase
{
    /// <summary>Avisos dos idosos da família, do mais recente para o mais antigo.</summary>
    /// <param name="familiaId">Id da família.</param>
    /// <param name="somenteNaoVistos">Verdadeiro para trazer só os avisos que ninguém viu ainda.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    [HttpGet("familias/{familiaId:int}/avisos")]
    [ProducesResponseType<IReadOnlyList<AvisoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AvisoResponse>>> ListarAsync(
        int familiaId,
        [FromQuery] bool somenteNaoVistos,
        CancellationToken cancellationToken) =>
        Ok(await avisos.ListarAsync(familiaId, somenteNaoVistos, cancellationToken));

    /// <summary>Marca o aviso como visto por quem está logado.</summary>
    /// <response code="200">Aviso marcado como visto.</response>
    /// <response code="403">O aviso é de outra família.</response>
    /// <response code="404">Aviso não encontrado.</response>
    [HttpPost("avisos/{avisoId:int}/visto")]
    [ProducesResponseType<AvisoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvisoResponse>> MarcarVistoAsync(int avisoId, CancellationToken cancellationToken) =>
        Ok(await avisos.MarcarVistoAsync(avisoId, cancellationToken));
}
