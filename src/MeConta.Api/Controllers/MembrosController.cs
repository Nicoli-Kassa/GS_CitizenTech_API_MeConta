using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Perfil de quem usa o app. Primeiro a pessoa se cadastra em /api/v1/auth/register,
/// faz login e depois cria o perfil aqui.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/membros")]
[Produces("application/json")]
[Tags("Membros")]
public sealed class MembrosController(IMembroService membros) : ControllerBase
{
    /// <summary>Cria o perfil do usuário logado.</summary>
    /// <response code="201">Perfil criado.</response>
    /// <response code="400">Nome vazio ou longo demais.</response>
    /// <response code="409">O perfil já existe.</response>
    [HttpPost]
    [ProducesResponseType<MembroResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MembroResponse>> CriarPerfilAsync(
        CriarPerfilRequest request,
        CancellationToken cancellationToken)
    {
        MembroResponse membro = await membros.CriarPerfilAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterPerfilAsync), null, membro);
    }

    /// <summary>Perfil do usuário logado.</summary>
    /// <response code="200">Perfil encontrado.</response>
    /// <response code="403">O perfil ainda não foi criado.</response>
    [HttpGet("eu")]
    [ProducesResponseType<MembroResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MembroResponse>> ObterPerfilAsync(CancellationToken cancellationToken) =>
        Ok(await membros.ObterPerfilAsync(cancellationToken));
}
