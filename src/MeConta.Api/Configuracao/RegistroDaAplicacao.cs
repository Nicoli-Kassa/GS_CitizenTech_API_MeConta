using MeConta.Application.Servicos;
using MeConta.Domain.Regras;

namespace MeConta.Api.Configuracao;

public static class RegistroDaAplicacao
{
    /// <summary>
    /// Registra os serviços da camada Application e as regras do domínio no contêiner de injeção de dependência.
    /// </summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        // Relógio injetável: em produção, a hora do sistema; nos testes, uma hora fixa.
        services.AddSingleton(TimeProvider.System);

        // Regras do domínio sem estado: uma instância para a aplicação inteira.
        services.AddSingleton<IFiltroAntiGolpe, FiltroAntiGolpe>();
        services.AddSingleton<ISugestaoSemanal, SugestaoSemanal>();

        // Serviços: uma instância por pedido HTTP, junto com o DbContext.
        services.AddScoped<IControleDeAcesso, ControleDeAcesso>();
        services.AddScoped<IMembroService, MembroService>();
        services.AddScoped<IFamiliaService, FamiliaService>();
        services.AddScoped<IIdosoService, IdosoService>();
        services.AddScoped<IPerguntaService, PerguntaService>();
        services.AddScoped<IEnvioService, EnvioService>();
        services.AddScoped<ICadernoService, CadernoService>();
        services.AddScoped<IIndicadorService, IndicadorService>();
        services.AddScoped<IPortaRetratoService, PortaRetratoService>();
        services.AddScoped<IAparelhoPortaRetratoService, AparelhoPortaRetratoService>();
        services.AddScoped<IAvisoService, AvisoService>();

        return services;
    }
}
