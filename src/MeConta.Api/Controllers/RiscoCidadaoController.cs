using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Indicador "Risco Cidadão" para o painel executivo.
/// Só números agregados e trechos já mascarados: nenhum título, nome ou família.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/risco-cidadao")]
[Produces("application/json")]
[Tags("Risco Cidadão")]
public sealed class RiscoCidadaoController(IRiscoCidadaoService riscoCidadao) : ControllerBase
{
    /// <summary>
    /// Índice Risco Cidadão (média das memórias verificadas), memórias publicadas e bloqueadas,
    /// vazamentos evitados, dados encontrados por tipo, evolução por dia e por bairro, e os últimos alertas.
    /// </summary>
    /// <param name="dias">Período em dias, de 1 a 90 (padrão: 14).</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="400">Período fora de 1 a 90 dias.</response>
    [HttpGet]
    [ProducesResponseType<RiscoCidadaoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RiscoCidadaoResponse>> ObterAsync(
        [FromQuery] int dias = RiscoCidadaoService.PeriodoPadraoDias,
        CancellationToken cancellationToken = default) =>
        Ok(await riscoCidadao.ObterAsync(dias, cancellationToken));
}
