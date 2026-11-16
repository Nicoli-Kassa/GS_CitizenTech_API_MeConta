using MeConta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

internal sealed class MemoriaDoBairroConfiguracao : IEntityTypeConfiguration<MemoriaDoBairro>
{
    public void Configure(EntityTypeBuilder<MemoriaDoBairro> builder)
    {
        builder.ToTable("MemoriasDoBairro");
        builder.HasKey(memoria => memoria.Id);

        builder.Property(memoria => memoria.Bairro).HasMaxLength(MemoriaDoBairro.TamanhoMaximoBairro).IsRequired();
        builder.Property(memoria => memoria.Apelido).HasMaxLength(Idoso.TamanhoMaximoApelido).IsRequired();
        builder.Property(memoria => memoria.Titulo).HasMaxLength(PaginaCaderno.TamanhoMaximoTitulo).IsRequired();
        builder.Property(memoria => memoria.Transcricao).HasMaxLength(PaginaCaderno.TamanhoMaximoTranscricao);
        builder.Ignore(memoria => memoria.EstaNoMapa);

        // Apagar a página do caderno (US11) tira a memória do mapa junto.
        builder.HasOne(memoria => memoria.Pagina)
            .WithMany()
            .HasForeignKey(memoria => memoria.PaginaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(memoria => memoria.PublicadaPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(memoria => memoria.RemovidaPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cada página aparece no mapa no máximo uma vez; depois de tirada, pode voltar.
        builder.HasIndex(memoria => memoria.PaginaId)
            .IsUnique()
            .HasFilter("[RemovidaEm] IS NULL");
        builder.HasIndex(memoria => new { memoria.Bairro, memoria.RemovidaEm });
    }
}

internal sealed class RegistroDePrivacidadeConfiguracao : IEntityTypeConfiguration<RegistroDePrivacidade>
{
    public void Configure(EntityTypeBuilder<RegistroDePrivacidade> builder)
    {
        builder.ToTable("RegistrosDePrivacidade");
        builder.HasKey(registro => registro.Id);

        builder.Property(registro => registro.Origem).HasConversion<string>().HasMaxLength(20);
        builder.Property(registro => registro.Resultado).HasConversion<string>().HasMaxLength(20);
        builder.Property(registro => registro.Bairro).HasMaxLength(MemoriaDoBairro.TamanhoMaximoBairro);
        builder.Property(registro => registro.VersaoDasRegras).HasMaxLength(20).IsRequired();

        // Histórico sem chave estrangeira para página, família e idoso: é um registro de auditoria
        // e continua valendo para o painel mesmo se a página for apagada.
        builder.HasMany(registro => registro.Achados)
            .WithOne()
            .HasForeignKey(achado => achado.RegistroId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(registro => registro.Achados).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(registro => registro.CriadoEm);
    }
}

internal sealed class AchadoRegistradoConfiguracao : IEntityTypeConfiguration<AchadoRegistrado>
{
    public void Configure(EntityTypeBuilder<AchadoRegistrado> builder)
    {
        builder.ToTable("AchadosDePrivacidade");
        builder.HasKey(achado => achado.Id);

        builder.Property(achado => achado.RegraId).HasMaxLength(40).IsRequired();
        builder.Property(achado => achado.Tipo).HasMaxLength(40).IsRequired();
        builder.Property(achado => achado.Rotulo).HasMaxLength(60).IsRequired();
        builder.Property(achado => achado.Nivel).HasMaxLength(10).IsRequired();
        builder.Property(achado => achado.Campo).HasMaxLength(20);
        builder.Property(achado => achado.TrechoMascarado).HasMaxLength(AchadoRegistrado.TamanhoMaximoTrecho).IsRequired();
    }
}
