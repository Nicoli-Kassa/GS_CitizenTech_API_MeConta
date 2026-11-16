using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Infrastructure.Adapters;
using MeConta.Infrastructure.Persistencia;
using MeConta.Infrastructure.Repositorios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeConta.Infrastructure;

/// <summary>
/// Banco usado pela API. SQL Server é o padrão; SQLite serve para os testes de integração.
/// </summary>
public enum ProvedorDeBanco
{
    SqlServer,
    Sqlite,
}

public static class InjecaoDeDependencia
{
    /// <summary>
    /// Registra o DbContext, os repositórios e os adapters de disco e de código de convite.
    /// </summary>
    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection services,
        ProvedorDeBanco provedor,
        string connectionString,
        string pastaDeArquivos)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure a connection string 'DefaultConnection'.");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provedor == ProvedorDeBanco.Sqlite)
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        // O próprio DbContext é a unidade de trabalho: um SaveChanges por pedido.
        services.AddScoped<IUnidadeDeTrabalho>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IMembroRepository, MembroRepository>();
        services.AddScoped<IFamiliaRepository, FamiliaRepository>();
        services.AddScoped<IIdosoRepository, IdosoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IPerguntaRepository, PerguntaRepository>();
        services.AddScoped<IEnvioRepository, EnvioRepository>();
        services.AddScoped<IPaginaRepository, PaginaRepository>();
        services.AddScoped<IIndicadorConsulta, IndicadorConsulta>();
        services.AddScoped<IPortaRetratoRepository, PortaRetratoRepository>();
        services.AddScoped<IAvisoRepository, AvisoRepository>();
        services.AddScoped<IMemoriaDoBairroRepository, MemoriaDoBairroRepository>();
        services.AddScoped<IRegistroDePrivacidadeRepository, RegistroDePrivacidadeRepository>();

        services.AddSingleton<IArmazenamentoDeArquivos>(new ArmazenamentoEmDiscoAdapter(pastaDeArquivos));
        services.AddSingleton<IGeradorDeCodigoConvite, GeradorDeCodigoConvite>();
        services.AddSingleton<IGeradorDeCredenciais, GeradorDeCredenciais>();

        return services;
    }
}
