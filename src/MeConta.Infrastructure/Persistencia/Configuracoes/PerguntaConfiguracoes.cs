using MeConta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

internal sealed class CategoriaConfiguracao : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");
        builder.HasKey(categoria => categoria.Id);

        builder.Property(categoria => categoria.Slug).HasMaxLength(40).IsRequired();
        builder.Property(categoria => categoria.Nome).HasMaxLength(60).IsRequired();
        builder.HasIndex(categoria => categoria.Slug).IsUnique();

        // As cinco categorias entram pela migração.
        builder.HasData(CatalogoInicial.Categorias);
    }
}

/// <summary>
/// As duas subclasses de <see cref="Pergunta"/> ficam na mesma tabela (TPH),
/// separadas pela coluna "Origem".
/// </summary>
internal sealed class PerguntaConfiguracao : IEntityTypeConfiguration<Pergunta>
{
    public void Configure(EntityTypeBuilder<Pergunta> builder)
    {
        builder.ToTable("Perguntas");
        builder.HasKey(pergunta => pergunta.Id);

        builder.HasDiscriminator<string>("Origem")
            .HasValue<PerguntaCatalogo>("Catalogo")
            .HasValue<PerguntaDaFamilia>("Familia");
        builder.Property<string>("Origem").HasMaxLength(20);

        builder.Property(pergunta => pergunta.Texto).HasMaxLength(Pergunta.TamanhoMaximo).IsRequired();
        builder.Ignore(pergunta => pergunta.Tipo);

        builder.HasOne(pergunta => pergunta.Categoria)
            .WithMany()
            .HasForeignKey(pergunta => pergunta.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PerguntaCatalogoConfiguracao : IEntityTypeConfiguration<PerguntaCatalogo>
{
    public void Configure(EntityTypeBuilder<PerguntaCatalogo> builder)
    {
        // As 25 perguntas do catálogo entram pela migração.
        builder.HasData(CatalogoInicial.Perguntas);
    }
}

internal sealed class PerguntaDaFamiliaConfiguracao : IEntityTypeConfiguration<PerguntaDaFamilia>
{
    public void Configure(EntityTypeBuilder<PerguntaDaFamilia> builder)
    {
        builder.HasOne<Familia>()
            .WithMany()
            .HasForeignKey(pergunta => pergunta.FamiliaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(pergunta => pergunta.CriadaPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
