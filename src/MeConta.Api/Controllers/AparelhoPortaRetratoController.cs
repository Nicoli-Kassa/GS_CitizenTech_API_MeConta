using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Rotas usadas pelo porta-retrato (ESP32), por meio da ponte Node-RED (US15).
/// Não usam o login da família: o aparelho se identifica pela chave no cabeçalho <c>X-Chave-PortaRetrato</c>.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/porta-retrato")]
[Produces("application/json")]
[Tags("Porta-retrato (aparelho)")]
public sealed class AparelhoPortaRetratoController(IAparelhoPortaRetratoService aparelho) : ControllerBase
{
    /// <summary>Cabeçalho com a chave secreta do porta-retrato.</summary>
    public const string CabecalhoChave = "X-Chave-PortaRetrato";

    /// <summary>
    /// O que o porta-retrato deve mostrar agora: se há pergunta nova (a moldura acende),
    /// quem perguntou e o texto. A ponte consulta esta rota a cada 5 segundos.
    /// </summary>
    /// <param name="chave">Chave do porta-retrato (demonstração: demo-porta-retrato-vo-lourdes-2026).</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="200">Estado atual.</response>
    /// <response code="401">Chave ausente ou inválida.</response>
    [HttpGet("estado")]
    [ProducesResponseType<EstadoPortaRetratoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EstadoPortaRetratoResponse>> ObterEstadoAsync(
        [FromHeader(Name = CabecalhoChave)] string? chave,
        CancellationToken cancellationToken) =>
        Ok(await aparelho.ObterEstadoAsync(chave, cancellationToken));

    /// <summary>
    /// O idoso apertou "quero contar". Cria o aviso para a família e a moldura apaga.
    /// Apertar de novo para a mesma pergunta devolve o aviso que já existe (200).
    /// </summary>
    /// <param name="chave">Chave do porta-retrato.</param>
    /// <param name="request">Pergunta que estava na tela (vazio quando não havia pergunta).</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="201">Aviso criado.</response>
    /// <response code="200">O aviso já existia.</response>
    /// <response code="400">A pergunta não é deste porta-retrato.</response>
    /// <response code="401">Chave ausente ou inválida.</response>
    [HttpPost("quero-contar")]
    [ProducesResponseType<AvisoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<AvisoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AvisoResponse>> QueroContarAsync(
        [FromHeader(Name = CabecalhoChave)] string? chave,
        QueroContarRequest request,
        CancellationToken cancellationToken)
    {
        ResultadoQueroContar resultado = await aparelho.QueroContarAsync(chave, request, cancellationToken);
        return resultado.Criado
            ? StatusCode(StatusCodes.Status201Created, resultado.Aviso)
            : Ok(resultado.Aviso);
    }

    /// <summary>
    /// A ponte informa se o aparelho está conectado ao broker MQTT (mensagem de status ou "last will").
    /// Também serve de sinal de vida: a ponte repete a cada 30 segundos.
    /// </summary>
    /// <param name="chave">Chave do porta-retrato.</param>
    /// <param name="request">Online ou offline.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="204">Conexão registrada.</response>
    /// <response code="401">Chave ausente ou inválida.</response>
    [HttpPut("conexao")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegistrarConexaoAsync(
        [FromHeader(Name = CabecalhoChave)] string? chave,
        ConexaoPortaRetratoRequest request,
        CancellationToken cancellationToken)
    {
        await aparelho.RegistrarConexaoAsync(chave, request, cancellationToken);
        return NoContent();
    }
}
