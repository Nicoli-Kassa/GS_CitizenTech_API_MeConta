using System.ComponentModel.DataAnnotations;
using MeConta.Application.Dtos;
using MeConta.Application.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Controllers;

/// <summary>
/// Caderno da família: guardar a resposta em áudio (US08, US10), ver e ouvir (US09), apagar (US11)
/// e guardar a transcrição que o filtro de dados pessoais confere (US18).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1")]
[Produces("application/json")]
[Tags("Caderno")]
public sealed class CadernoController(ICadernoService caderno) : ControllerBase
{
    /// <summary>Áudio de até 20 MB + foto de até 10 MB + campos do formulário.</summary>
    private const long LimiteDoFormulario = 32L * 1024 * 1024;

    /// <summary>
    /// Guarda a resposta em áudio como página do caderno (US08).
    /// Se a mesma chaveIdempotencia for reenviada, devolve a página que já existe (US10).
    /// </summary>
    /// <response code="201">Página criada.</response>
    /// <response code="200">A página com essa chave já existia (reenvio do modo offline).</response>
    /// <response code="400">Arquivo que não é áudio, duração inválida ou título longo.</response>
    /// <response code="409">A pergunta já foi respondida.</response>
    /// <response code="413">Áudio acima de 20 MB ou foto acima de 10 MB.</response>
    [HttpPost("envios/{envioId:int}/pagina")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteDoFormulario)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteDoFormulario)]
    [ProducesResponseType<PaginaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<PaginaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<PaginaResponse>> GuardarRespostaAsync(
        int envioId,
        [FromForm] RespostaForm form,
        CancellationToken cancellationToken)
    {
        NovaResposta resposta = new(
            form.Titulo,
            form.DuracaoSegundos,
            form.ChaveIdempotencia,
            ParaArquivo(form.Audio),
            form.Foto is null ? null : ParaArquivo(form.Foto),
            form.Transcricao);

        ResultadoGuardarResposta resultado = await caderno.GuardarRespostaAsync(envioId, resposta, cancellationToken);

        return resultado.Criada
            ? CreatedAtAction(nameof(ObterPaginaAsync), new { paginaId = resultado.Pagina.Id }, resultado.Pagina)
            : Ok(resultado.Pagina);
    }

    /// <summary>Caderno da família, da página mais recente para a mais antiga (US09).</summary>
    /// <param name="familiaId">Id da família.</param>
    /// <param name="idosoId">Filtro opcional por idoso.</param>
    /// <param name="categoria">Filtro opcional por categoria (slug). Ex.: receitas.</param>
    /// <param name="pagina">Número da página da lista, a partir de 1.</param>
    /// <param name="tamanho">Itens por página, de 1 a 50.</param>
    /// <param name="cancellationToken">Cancelamento do pedido.</param>
    /// <response code="403">Quem não é da família não vê nenhum dado.</response>
    [HttpGet("familias/{familiaId:int}/caderno")]
    [ProducesResponseType<ListaPaginada<PaginaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListaPaginada<PaginaResponse>>> ListarAsync(
        int familiaId,
        [FromQuery] int? idosoId,
        [FromQuery] string? categoria,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanho = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await caderno.ListarAsync(
            familiaId,
            new FiltroCaderno(idosoId, categoria, pagina, tamanho),
            cancellationToken));

    /// <summary>Uma página do caderno.</summary>
    [HttpGet("paginas/{paginaId:int}")]
    [ProducesResponseType<PaginaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginaResponse>> ObterPaginaAsync(int paginaId, CancellationToken cancellationToken) =>
        Ok(await caderno.ObterAsync(paginaId, cancellationToken));

    /// <summary>Áudio da resposta, para o botão "Ouvir". Aceita pedidos parciais (avançar e voltar o áudio).</summary>
    [HttpGet("paginas/{paginaId:int}/audio")]
    [Produces("audio/mpeg", "audio/mp4", "audio/ogg")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> OuvirAsync(int paginaId, CancellationToken cancellationToken)
    {
        ArquivoParaDownload arquivo = await caderno.AbrirAudioAsync(paginaId, cancellationToken);
        return File(arquivo.Conteudo, arquivo.ContentType, enableRangeProcessing: true);
    }

    /// <summary>Foto da página, se houver.</summary>
    [HttpGet("paginas/{paginaId:int}/foto")]
    [Produces("image/jpeg", "image/png", "image/webp", "image/heic")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerFotoAsync(int paginaId, CancellationToken cancellationToken)
    {
        ArquivoParaDownload arquivo = await caderno.AbrirFotoAsync(paginaId, cancellationToken);
        return File(arquivo.Conteudo, arquivo.ContentType);
    }

    /// <summary>Apaga a página. Só quem a criou pode apagar (US11).</summary>
    /// <response code="204">Página apagada do banco e do disco.</response>
    /// <response code="403">Quem está logado não criou a página.</response>
    [HttpDelete("paginas/{paginaId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApagarAsync(int paginaId, CancellationToken cancellationToken)
    {
        await caderno.ApagarAsync(paginaId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Guarda ou troca a transcrição do áudio (o que a pessoa idosa disse). Texto em branco apaga.
    /// É o texto que o filtro de dados pessoais confere antes de a memória ir para o mapa do bairro.
    /// </summary>
    /// <response code="200">Página com a transcrição nova.</response>
    /// <response code="400">Transcrição acima de 4000 caracteres.</response>
    [HttpPut("paginas/{paginaId:int}/transcricao")]
    [ProducesResponseType<PaginaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginaResponse>> DefinirTranscricaoAsync(
        int paginaId,
        TranscricaoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await caderno.DefinirTranscricaoAsync(paginaId, request, cancellationToken));

    private static ArquivoRecebido ParaArquivo(IFormFile arquivo) =>
        new(arquivo.FileName, arquivo.Length, arquivo.OpenReadStream);
}

/// <summary>
/// Formulário multipart da resposta em áudio.
/// </summary>
public sealed class RespostaForm
{
    /// <summary>Áudio da resposta: mp3, m4a, ogg ou opus (formato do WhatsApp), até 20 MB.</summary>
    [Required]
    public IFormFile Audio { get; set; } = null!;

    /// <summary>Foto opcional: jpg, png, webp ou heic, até 10 MB.</summary>
    public IFormFile? Foto { get; set; }

    /// <summary>Título da página. Em branco, usa o texto da pergunta.</summary>
    [StringLength(200)]
    public string? Titulo { get; set; }

    /// <summary>Duração do áudio em segundos, lida pelo app.</summary>
    [Range(1, 7200)]
    public int DuracaoSegundos { get; set; }

    /// <summary>Identificador gerado pelo celular para não duplicar a página no modo offline.</summary>
    public Guid? ChaveIdempotencia { get; set; }

    /// <summary>O que a pessoa idosa disse no áudio, escrito pela família (opcional, até 4000 caracteres).</summary>
    [StringLength(4000)]
    public string? Transcricao { get; set; }
}
