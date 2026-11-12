using System.Text.Json.Serialization;
using MeConta.Api.Adapters;
using MeConta.Api.Configuracao;
using MeConta.Application.Adapters;
using MeConta.Infrastructure;
using MeConta.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

namespace MeConta.Api;

/// <summary>
/// Ponto de entrada da Web API do Me Conta.
/// A classe é "partial" para que os testes de integração possam subir a API com WebApplicationFactory.
/// </summary>
public partial class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Banco, repositórios e adapters (camada Infrastructure).
        ProvedorDeBanco provedor = builder.Configuration.GetValue("Banco:Provedor", ProvedorDeBanco.SqlServer);
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        string pastaDeArquivos = builder.Configuration["Armazenamento:Pasta"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "storage");
        builder.Services.AdicionarInfraestrutura(provedor, connectionString, pastaDeArquivos);

        // Casos de uso (camada Application) e regras do domínio.
        builder.Services.AdicionarAplicacao();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUsuarioAtual, UsuarioAtualHttp>();

        // Login com ASP.NET Identity: /api/v1/auth/register e /api/v1/auth/login devolvem token Bearer.
        builder.Services.AddAuthorization();
        builder.Services
            .AddIdentityApiEndpoints<IdentityUser>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services
            .AddControllers(options =>
            {
                // Mantém o sufixo "Async" no nome das ações, para o CreatedAtAction encontrar a rota.
                options.SuppressAsyncSuffixInActionNames = false;
            })
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Erros viram ProblemDetails (RFC 9457) com a mensagem da regra de negócio.
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<TratadorDeExcecoes>();

        // Documentação automática: um documento OpenAPI, exibido no Swagger UI e no Scalar.
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<DocumentacaoOpenApi>());

        builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("banco");

        // Libera chamadas do app rodando no navegador (Expo Web) durante o desenvolvimento.
        builder.Services.AddCors(options => options.AddPolicy(
            "app",
            policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        WebApplication app = builder.Build();

        app.UseExceptionHandler();
        app.UseCors("app");
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Me Conta API v1");
            options.DocumentTitle = "Me Conta API";
        });
        app.MapScalarApiReference(options => options.WithTitle("Me Conta API"));

        app.MapGroup("/api/v1/auth")
            .MapIdentityApi<IdentityUser>()
            .WithTags("Autenticação");
        app.MapControllers();
        app.MapHealthChecks("/health");

        bool dadosDeDemonstracao = builder.Configuration.GetValue("Banco:DadosDeDemonstracao", false);
        await DbInicializador.InicializarAsync(app.Services, dadosDeDemonstracao);

        await app.RunAsync();
    }
}
