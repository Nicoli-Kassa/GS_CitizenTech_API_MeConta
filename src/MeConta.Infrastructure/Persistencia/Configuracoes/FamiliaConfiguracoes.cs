using MeConta.Domain.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

internal sealed class MembroConfiguracao : IEntityTypeConfiguration<Membro>
{
    public void Configure(EntityTypeBuilder<Membro> builder)
    {
        builder.ToTable("Membros");
        builder.HasKey(membro => membro.Id);

        builder.Property(membro => membro.Nome).HasMaxLength(Membro.TamanhoMaximoNome).IsRequired();
        builder.Property(membro => membro.UsuarioId).HasMaxLength(450).IsRequired();

        // Cada usuário do login tem no máximo um perfil.
        builder.HasIndex(membro => membro.UsuarioId).IsUnique();
        builder.HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<Membro>(membro => membro.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FamiliaConfiguracao : IEntityTypeConfiguration<Familia>
{
    public void Configure(EntityTypeBuilder<Familia> builder)
    {
        builder.ToTable("Familias");
        builder.HasKey(familia => familia.Id);

        builder.Property(familia => familia.Nome).HasMaxLength(Familia.TamanhoMaximoNome).IsRequired();

        // As coleções são só de leitura por fora; o EF usa os campos privados (_participacoes etc.).
        builder.HasMany(familia => familia.Participacoes)
            .WithOne()
            .HasForeignKey(participacao => participacao.FamiliaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(familia => familia.Idosos)
            .WithOne()
            .HasForeignKey(idoso => idoso.FamiliaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(familia => familia.Convites)
            .WithOne()
            .HasForeignKey(convite => convite.FamiliaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(familia => familia.Participacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(familia => familia.Idosos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(familia => familia.Convites).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ParticipacaoConfiguracao : IEntityTypeConfiguration<Participacao>
{
    public void Configure(EntityTypeBuilder<Participacao> builder)
    {
        builder.ToTable("Participacoes");

        // Chave composta: a mesma pessoa não entra duas vezes na mesma família.
        builder.HasKey(participacao => new { participacao.FamiliaId, participacao.MembroId });

        builder.Property(participacao => participacao.Papel).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(participacao => participacao.Membro)
            .WithMany()
            .HasForeignKey(participacao => participacao.MembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConviteConfiguracao : IEntityTypeConfiguration<Convite>
{
    public void Configure(EntityTypeBuilder<Convite> builder)
    {
        builder.ToTable("Convites");
        builder.HasKey(convite => convite.Id);

        builder.Property(convite => convite.Codigo)
            .HasMaxLength(Convite.TamanhoCodigo)
            .IsFixedLength()
            .IsRequired();
        builder.HasIndex(convite => convite.Codigo).IsUnique();

        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(convite => convite.CriadoPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IdosoConfiguracao : IEntityTypeConfiguration<Idoso>
{
    public void Configure(EntityTypeBuilder<Idoso> builder)
    {
        builder.ToTable("Idosos");
        builder.HasKey(idoso => idoso.Id);

        builder.Property(idoso => idoso.Nome).HasMaxLength(Idoso.TamanhoMaximoNome).IsRequired();
        builder.Property(idoso => idoso.Apelido).HasMaxLength(Idoso.TamanhoMaximoApelido).IsRequired();
        builder.Property(idoso => idoso.Telefone)
            .HasConversion<ConversorTelefone>()
            .HasMaxLength(ConversorTelefone.TamanhoMaximo);
        builder.Ignore(idoso => idoso.TemWhatsApp);

        // O mesmo WhatsApp não se repete na família; vários idosos sem WhatsApp são permitidos.
        builder.HasIndex(idoso => new { idoso.FamiliaId, idoso.Telefone })
            .IsUnique()
            .HasFilter("[Telefone] IS NOT NULL");
    }
}
