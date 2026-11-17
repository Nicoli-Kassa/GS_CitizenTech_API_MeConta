using MeConta.Api.Configuracao;
using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MeConta.Api.Controllers;

/// <summary>
/// Filtro de dados pessoais (US18) e mapa de memórias do bairro (US16, US17).
/// Antes de uma página do caderno ir para o mapa, título e transcrição passam pelo motor de PII:
/// dado de risco alto (CPF, senha, cartão...) bloqueia; dado de risco médio (saúde, telefone, endereço...)
/// exige a confirmação da família.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Mapa do bairro")]
public sealed class MapaDoBairroController(IPrivacidadeService privacidade, IRemediacaoService remediacao) : ControllerBase
{
    /// <summary>
    /// Passa título e transcrição da página pelo filtro de dados pessoais, sem publicar.
    /// Mostra os dados encontrados (mascarados), o índice Risco Cidadão e a versão segura do texto.
    /// </summary>
    /// <response code="200">Resultado da análise: Liberada, PrecisaConfirmar ou Bloqueada.</response>
    /// <response code="403">Quem não é da família não vê nenhum dado.</response>
    /// <response code="404">Página não encontrada.</response>
    [HttpPost("paginas/{paginaId:int}/analise-privacidade")]
    [ProducesResponseType<AnalisePrivacidadeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnalisePrivacidadeResponse>> AnalisarAsync(int paginaId, CancellationToken cancellationToken) =>
        Ok(await privacidade.AnalisarPaginaAsync(paginaId, cancellationToken));

    /// <summary>
    /// Explica para a família, em linguagem simples, por que a página não pode ir (ou precisa de confirmação)
    /// para o mapa do bairro: um alerta por tipo de dado, o risco real e o que fazer, e uma versão
    /// da memória sem os dados pessoais. A IA (Gemini) só recebe o que o motor já mascarou,
    /// e a resposta dela passa de novo pelo motor antes de chegar aqui.
    /// </summary>
    /// <response code="200">Explicação. "fonte" diz quem escreveu: Gemini, Local (texto da equipe) ou Motor (nada a explicar).</response>
    /// <response code="403">Quem não é da família não vê nenhum dado.</response>
    /// <response code="404">Página não encontrada.</response>
    /// <response code="429">Muitos pedidos no mesmo minuto.</response>
    [HttpGet("paginas/{paginaId:int}/explicacao-de-risco")]
    [EnableRateLimiting(LimitesDeUso.Ia)]
    [ProducesResponseType<ExplicacaoDeRiscoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ExplicacaoDeRiscoResponse>> ExplicarAsync(int paginaId, CancellationToken cancellationToken) =>
        Ok(await remediacao.ExplicarPaginaAsync(paginaId, cancellationToken));

    /// <summary>
    /// Leva a página ao mapa do bairro (US16), com a autorização da pessoa idosa e se o filtro liberar (US18).
    /// </summary>
    /// <response code="201">Memória publicada no mapa.</response>
    /// <response code="400">Sem a autorização da pessoa idosa, sem bairro, ou bairro com endereço.</response>
    /// <response code="409">A memória já está no mapa.</response>
    /// <response code="422">O filtro encontrou dado pessoal. O campo "analise" traz os dados mascarados e a versão segura.</response>
    [HttpPost("paginas/{paginaId:int}/publicacao")]
    [ProducesResponseType<PublicacaoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PublicacaoResponse>> PublicarAsync(
        int paginaId,
        PublicarMemoriaRequest request,
        CancellationToken cancellationToken)
    {
        PublicacaoResponse publicacao = await privacidade.PublicarAsync(paginaId, request, cancellationToken);
        return Created($"/api/v1/memorias/{publicacao.Memoria.Id}/audio", publicacao);
    }

    /// <summary>Tira a memória do mapa do bairro na hora (US16).</summary>
    /// <response code="204">Memória fora do mapa.</response>
    /// <response code="404">A página não está no mapa.</response>
    [HttpDelete("paginas/{paginaId:int}/publicacao")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TirarDoMapaAsync(int paginaId, CancellationToken cancellationToken)
    {
        await privacidade.TirarDoMapaAsync(paginaId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Memórias publicadas no bairro (US17): título, apelido, transcrição liberada e áudio.
    /// Aberto a qualquer morador; nunca traz telefone, nome completo ou família.
    /// </summary>
    /// <param name="bairro">Nome do bairro. Ex.: Pirituba.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    [HttpGet("bairros/{bairro}/memorias")]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<MemoriaDoBairroResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MemoriaDoBairroResponse>>> ListarDoBairroAsync(
        string bairro,
        CancellationToken cancellationToken) =>
        Ok(await privacidade.ListarDoBairroAsync(bairro, cancellationToken));

    /// <summary>Áudio de uma memória do mapa do bairro. Memória tirada do mapa responde 404.</summary>
    [HttpGet("memorias/{memoriaId:int}/audio")]
    [AllowAnonymous]
    [Produces("audio/mpeg", "audio/mp4", "audio/ogg")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OuvirAsync(int memoriaId, CancellationToken cancellationToken)
    {
        ArquivoParaDownload arquivo = await privacidade.AbrirAudioDaMemoriaAsync(memoriaId, cancellationToken);
        return File(arquivo.Conteudo, arquivo.ContentType, enableRangeProcessing: true);
    }
}
