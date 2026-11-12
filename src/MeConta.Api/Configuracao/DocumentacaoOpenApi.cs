using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MeConta.Api.Configuracao;

/// <summary>
/// Completa o documento OpenAPI: título, descrição e o esquema de login Bearer,
/// que faz aparecer o botão "Authorize" no Swagger UI.
/// </summary>
internal sealed class DocumentacaoOpenApi : IOpenApiDocumentTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Me Conta API",
            Version = "v1",
            Description =
                "Backend do Me Conta: o app que troca o 👍 da família por uma pergunta que só os avós sabem responder. " +
                "Para testar: faça login em POST /api/v1/auth/login (bia@example.com / MeConta@2026), " +
                "copie o accessToken e cole em Authorize. Todos os dados de demonstração são fictícios.",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Token devolvido por POST /api/v1/auth/login (campo accessToken).",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, document)] = [],
        });

        return Task.CompletedTask;
    }
}
