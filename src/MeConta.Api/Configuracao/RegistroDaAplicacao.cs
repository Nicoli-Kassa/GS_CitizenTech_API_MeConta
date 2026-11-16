using MeConta.Application.Servicos;
using MeConta.Domain.Regras;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;

namespace MeConta.Api.Configuracao;

public static class RegistroDaAplicacao
{
    /// <summary>
    /// Registra os serviços da camada Application e as regras do domínio no contêiner de injeção de dependência.
    /// </summary>
    /// <param name="services">Contêiner de injeção de dependência.</param>
    /// <param name="arquivoDeRegrasPii">
    /// Arquivo de regras de dados pessoais fora da DLL (configuração "Privacidade:ArquivoDeRegras").
    /// Vazio: usa as regras que vêm dentro do módulo MeConta.Privacidade.
    /// </param>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services, string? arquivoDeRegrasPii = null)
    {
        // Relógio injetável: em produção, a hora do sistema; nos testes, uma hora fixa.
        services.AddSingleton(TimeProvider.System);

        // Regras do domínio sem estado: uma instância para a aplicação inteira.
        services.AddSingleton<IFiltroAntiGolpe, FiltroAntiGolpe>();
        services.AddSingleton<ISugestaoSemanal, SugestaoSemanal>();

        // Motor de dados pessoais: as regras são lidas e conferidas uma vez, quando a API sobe.
        // Uma regra com erro impede a API de subir, em vez de rodar sem ela.
        CatalogoDeRegras catalogo = string.IsNullOrWhiteSpace(arquivoDeRegrasPii)
            ? CatalogoDeRegras.CarregarPadrao()
            : CatalogoDeRegras.CarregarDeArquivo(arquivoDeRegrasPii);
        services.AddSingleton<IMotorPii>(new MotorPii(catalogo));

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
        services.AddScoped<IPrivacidadeService, PrivacidadeService>();
        services.AddScoped<IRiscoCidadaoService, RiscoCidadaoService>();

        return services;
    }
}
