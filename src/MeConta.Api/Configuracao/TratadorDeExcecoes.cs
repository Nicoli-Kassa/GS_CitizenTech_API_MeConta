using MeConta.Application.Excecoes;
using MeConta.Domain.Excecoes;
using MeConta.Privacidade.Taint;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MeConta.Api.Configuracao;

/// <summary>
/// Converte as exceções da aplicação em respostas HTTP com ProblemDetails.
/// A mensagem das regras de negócio vai no campo "detail", pronta para o app mostrar.
/// </summary>
public sealed class TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A ordem importa: as classes mais específicas vêm antes da classe base.
        (int status, string titulo) = exception switch
        {
            PublicacaoRecusadaException or PublicacaoBloqueadaException
                => (StatusCodes.Status422UnprocessableEntity, "Publicação recusada pelo filtro de dados pessoais"),
            ArquivoMuitoGrandeException => (StatusCodes.Status413PayloadTooLarge, "Arquivo muito grande"),
            ConflitoException => (StatusCodes.Status409Conflict, "Conflito"),
            RegraDeNegocioException => (StatusCodes.Status400BadRequest, "Regra de negócio"),
            CredencialInvalidaException => (StatusCodes.Status401Unauthorized, "Não autorizado"),
            AcessoNegadoException => (StatusCodes.Status403Forbidden, "Acesso negado"),
            RecursoNaoEncontradoException => (StatusCodes.Status404NotFound, "Não encontrado"),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro inesperado em {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);
        }

        ProblemDetails problema = new()
        {
            Status = status,
            Title = titulo,
            // Erros inesperados não expõem detalhes internos.
            Detail = status == StatusCodes.Status500InternalServerError
                ? "Algo deu errado. Tente de novo em instantes."
                : exception.Message,
            Instance = httpContext.Request.Path,
        };

        // A análise vai junto, só com trechos mascarados, para o app mostrar o alerta e a versão segura.
        if (exception is PublicacaoRecusadaException recusa)
        {
            problema.Extensions["analise"] = recusa.Analise;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);
        return true;
    }
}
