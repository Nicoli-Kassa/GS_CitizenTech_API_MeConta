using MeConta.Api.Configuracao;
using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeConta.Api.Controllers;

/// <summary>
/// Indicador "Risco Cidadão" para o painel executivo (página /risco-cidadao/).
/// Só números agregados e trechos já mascarados: nenhum título, nome ou família.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(LimitesDeUso.Painel)]
[Route("api/v1/risco-cidadao")]
[Produces("application/json")]
[Tags("Risco Cidadão")]
public sealed class RiscoCidadaoController(IRiscoCidadaoService riscoCidadao, IRemediacaoService remediacao) : ControllerBase
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

    /// <summary>
    /// Um alerta em linguagem simples para cada tipo de dado pessoal encontrado no período:
    /// o problema, o risco real para a pessoa idosa e o que fazer. Escrito pela IA (Gemini) e conferido
    /// pelo motor de dados pessoais; sem IA, vale o texto da equipe ("fonte" = "Local").
    /// </summary>
    /// <param name="dias">Período em dias, de 1 a 90 (padrão: 14).</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="400">Período fora de 1 a 90 dias.</response>
    /// <response code="429">Muitos pedidos no mesmo minuto.</response>
    [HttpGet("alertas")]
    [ProducesResponseType<AlertasDoPainelResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AlertasDoPainelResponse>> AlertasAsync(
        [FromQuery] int dias = RiscoCidadaoService.PeriodoPadraoDias,
        CancellationToken cancellationToken = default) =>
        Ok(await remediacao.AlertasDoPainelAsync(dias, cancellationToken));

    /// <summary>
    /// Auditoria do código (taint analysis com Semgrep): falhas encontradas antes e depois das correções,
    /// cada uma traduzida em risco real para o cidadão.
    /// </summary>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="404">Os relatórios da auditoria não estão na pasta seguranca/relatorios.</response>
    /// <response code="429">Muitos pedidos no mesmo minuto.</response>
    [HttpGet("auditoria")]
    [ProducesResponseType<AuditoriaDeCodigoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuditoriaDeCodigoResponse>> AuditoriaAsync(CancellationToken cancellationToken) =>
        Ok(await remediacao.AuditoriaAsync(cancellationToken));
}
