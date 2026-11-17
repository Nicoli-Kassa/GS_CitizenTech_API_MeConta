using Microsoft.AspNetCore.StaticFiles;

namespace MeConta.Api.Configuracao;

/// <summary>
/// Cabeçalhos de segurança das páginas estáticas (painel Risco Cidadão).
/// A política de conteúdo só deixa rodar o script e o estilo do próprio servidor: um texto que chegasse
/// ao painel com "&lt;script&gt;" não seria executado, e a página não pode ser aberta dentro de outro site.
/// </summary>
public static class CabecalhosDoPainel
{
    public const string PoliticaDeConteudo =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; " +
        "font-src 'self'; object-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'";

    public static void Aplicar(StaticFileResponseContext contexto)
    {
        IHeaderDictionary cabecalhos = contexto.Context.Response.Headers;
        cabecalhos.ContentSecurityPolicy = PoliticaDeConteudo;
        cabecalhos.XContentTypeOptions = "nosniff";
        cabecalhos.XFrameOptions = "DENY";
        cabecalhos["Referrer-Policy"] = "no-referrer";
        cabecalhos.CacheControl = "no-cache";
    }
}
