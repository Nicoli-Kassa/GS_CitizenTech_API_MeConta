using MeConta.Application.Abstracoes;
using MeConta.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// Contexto do Entity Framework. Herda as tabelas de login do ASP.NET Identity
/// e acrescenta as tabelas do Me Conta.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options), IUnidadeDeTrabalho
{
    public DbSet<Membro> Membros => Set<Membro>();

    public DbSet<Familia> Familias => Set<Familia>();

    public DbSet<Participacao> Participacoes => Set<Participacao>();

    public DbSet<Convite> Convites => Set<Convite>();

    public DbSet<Idoso> Idosos => Set<Idoso>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Pergunta> Perguntas => Set<Pergunta>();

    public DbSet<EnvioPergunta> Envios => Set<EnvioPergunta>();

    public DbSet<PaginaCaderno> Paginas => Set<PaginaCaderno>();

    public DbSet<PortaRetrato> PortaRetratos => Set<PortaRetrato>();

    public DbSet<AvisoQueroContar> Avisos => Set<AvisoQueroContar>();

    public DbSet<MemoriaDoBairro> MemoriasDoBairro => Set<MemoriaDoBairro>();

    public DbSet<RegistroDePrivacidade> RegistrosDePrivacidade => Set<RegistroDePrivacidade>();

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tabelas do Identity (AspNetUsers, AspNetRoles...).
        base.OnModelCreating(modelBuilder);

        // Lê todas as classes IEntityTypeConfiguration da pasta Configuracoes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Toda data é gravada em UTC e volta marcada como UTC,
        // para o JSON sair com "Z" e o app converter certo para o horário local.
        // Vale também para as datas opcionais (DateTime?).
        configurationBuilder.Properties<DateTime>().HaveConversion<ConversorDataUtc>();
    }
}
