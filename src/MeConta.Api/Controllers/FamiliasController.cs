using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Família e convites (US01 e US03).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Famílias")]
public sealed class FamiliasController(IFamiliaService familias) : ControllerBase
{
    /// <summary>Cria uma família. Quem cria entra como "Criador" (US01).</summary>
    /// <response code="201">Família criada.</response>
    /// <response code="400">Nome em branco ou com mais de 60 caracteres.</response>
    [HttpPost("familias")]
    [ProducesResponseType<FamiliaResumoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FamiliaResumoResponse>> CriarAsync(
        CriarFamiliaRequest request,
        CancellationToken cancellationToken)
    {
        FamiliaResumoResponse familia = await familias.CriarAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterAsync), new { familiaId = familia.Id }, familia);
    }

    /// <summary>Famílias de que o usuário logado participa.</summary>
    [HttpGet("familias")]
    [ProducesResponseType<IReadOnlyList<FamiliaResumoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FamiliaResumoResponse>>> ListarMinhasAsync(
        CancellationToken cancellationToken) =>
        Ok(await familias.ListarMinhasAsync(cancellationToken));

    /// <summary>Detalhe da família: membros e idosos.</summary>
    /// <response code="403">Quem está logado não é da família.</response>
    /// <response code="404">Família não encontrada.</response>
    [HttpGet("familias/{familiaId:int}")]
    [ProducesResponseType<FamiliaDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FamiliaDetalheResponse>> ObterAsync(int familiaId, CancellationToken cancellationToken) =>
        Ok(await familias.ObterAsync(familiaId, cancellationToken));

    /// <summary>Gera um código de convite de 6 caracteres, válido por 48 horas (US03).</summary>
    [HttpPost("familias/{familiaId:int}/convites")]
    [ProducesResponseType<ConviteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ConviteResponse>> GerarConviteAsync(int familiaId, CancellationToken cancellationToken)
    {
        ConviteResponse convite = await familias.GerarConviteAsync(familiaId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, convite);
    }

    /// <summary>Entra na família usando o código de convite (US03).</summary>
    /// <response code="200">Entrou na família.</response>
    /// <response code="400">Código inválido ou expirado.</response>
    [HttpPost("convites/{codigo}/aceitar")]
    [ProducesResponseType<FamiliaDetalheResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FamiliaDetalheResponse>> AceitarConviteAsync(
        string codigo,
        CancellationToken cancellationToken) =>
        Ok(await familias.AceitarConviteAsync(codigo, cancellationToken));
}
