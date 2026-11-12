using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Idosos da família (US02) e roteiro "Ensine sua avó" (US14).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Idosos")]
public sealed class IdososController(IIdosoService idosos) : ControllerBase
{
    /// <summary>Cadastra um idoso na família, com nome, apelido e WhatsApp (US02).</summary>
    /// <response code="201">Idoso cadastrado.</response>
    /// <response code="400">Dados inválidos (ex.: "Confira o número do WhatsApp").</response>
    /// <response code="409">"Esse número já está na família".</response>
    [HttpPost("familias/{familiaId:int}/idosos")]
    [ProducesResponseType<IdosoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdosoResponse>> CadastrarAsync(
        int familiaId,
        IdosoRequest request,
        CancellationToken cancellationToken)
    {
        IdosoResponse idoso = await idosos.CadastrarAsync(familiaId, request, cancellationToken);
        return CreatedAtAction(nameof(ObterAsync), new { idosoId = idoso.Id }, idoso);
    }

    /// <summary>Idosos da família.</summary>
    [HttpGet("familias/{familiaId:int}/idosos")]
    [ProducesResponseType<IReadOnlyList<IdosoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IdosoResponse>>> ListarAsync(
        int familiaId,
        CancellationToken cancellationToken) =>
        Ok(await idosos.ListarAsync(familiaId, cancellationToken));

    /// <summary>Dados de um idoso.</summary>
    [HttpGet("idosos/{idosoId:int}")]
    [ProducesResponseType<IdosoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IdosoResponse>> ObterAsync(int idosoId, CancellationToken cancellationToken) =>
        Ok(await idosos.ObterAsync(idosoId, cancellationToken));

    /// <summary>Atualiza nome, apelido e WhatsApp do idoso.</summary>
    [HttpPut("idosos/{idosoId:int}")]
    [ProducesResponseType<IdosoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdosoResponse>> AtualizarAsync(
        int idosoId,
        IdosoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await idosos.AtualizarAsync(idosoId, request, cancellationToken));

    /// <summary>Marca que o idoso concluiu o roteiro e já responde sozinho (US14).</summary>
    [HttpPost("idosos/{idosoId:int}/roteiro-concluido")]
    [ProducesResponseType<IdosoResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IdosoResponse>> ConcluirRoteiroAsync(int idosoId, CancellationToken cancellationToken) =>
        Ok(await idosos.ConcluirRoteiroAsync(idosoId, cancellationToken));
}
