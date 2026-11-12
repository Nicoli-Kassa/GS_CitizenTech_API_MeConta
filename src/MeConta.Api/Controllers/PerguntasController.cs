using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Categorias, catálogo de perguntas (US04), pergunta da semana (US05) e perguntas da família (US06).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Perguntas")]
public sealed class PerguntasController(IPerguntaService perguntas) : ControllerBase
{
    /// <summary>As cinco categorias: receitas, história da família, ofícios, o bairro e conselhos.</summary>
    [HttpGet("categorias")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<CategoriaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaResponse>>> ListarCategoriasAsync(
        CancellationToken cancellationToken) =>
        Ok(await perguntas.ListarCategoriasAsync(cancellationToken));

    /// <summary>
    /// Perguntas que a família pode enviar a este idoso, com a marca "jaPerguntada" (US04).
    /// </summary>
    /// <param name="idosoId">Id do idoso.</param>
    /// <param name="categoria">Slug da categoria (opcional). Ex.: receitas.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    [HttpGet("idosos/{idosoId:int}/perguntas")]
    [ProducesResponseType<IReadOnlyList<PerguntaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PerguntaResponse>>> ListarParaIdosoAsync(
        int idosoId,
        [FromQuery] string? categoria,
        CancellationToken cancellationToken) =>
        Ok(await perguntas.ListarParaIdosoAsync(idosoId, categoria, cancellationToken));

    /// <summary>Cria uma pergunta própria da família (US06). Pedidos de dinheiro, PIX ou senha são barrados.</summary>
    /// <response code="201">Pergunta criada.</response>
    /// <response code="400">Texto curto, longo, sem categoria ou com pedido suspeito.</response>
    [HttpPost("familias/{familiaId:int}/perguntas")]
    [ProducesResponseType<PerguntaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PerguntaResponse>> CriarDaFamiliaAsync(
        int familiaId,
        CriarPerguntaRequest request,
        CancellationToken cancellationToken)
    {
        PerguntaResponse pergunta = await perguntas.CriarDaFamiliaAsync(familiaId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, pergunta);
    }

    /// <summary>Pergunta sugerida para a semana atual, de segunda a domingo (US05).</summary>
    /// <response code="200">Sugestão da semana.</response>
    /// <response code="204">Todas as perguntas do catálogo já foram enviadas: o app convida a escrever uma pergunta própria.</response>
    [HttpGet("idosos/{idosoId:int}/pergunta-da-semana")]
    [ProducesResponseType<PerguntaDaSemanaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<PerguntaDaSemanaResponse>> ObterPerguntaDaSemanaAsync(
        int idosoId,
        CancellationToken cancellationToken)
    {
        PerguntaDaSemanaResponse? sugestao = await perguntas.ObterPerguntaDaSemanaAsync(idosoId, cancellationToken);
        return sugestao is null ? NoContent() : Ok(sugestao);
    }
}
