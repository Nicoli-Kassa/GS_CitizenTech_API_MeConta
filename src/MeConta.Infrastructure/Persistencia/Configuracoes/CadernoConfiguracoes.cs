using MeConta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

internal sealed class EnvioPerguntaConfiguracao : IEntityTypeConfiguration<EnvioPergunta>
{
    public void Configure(EntityTypeBuilder<EnvioPergunta> builder)
    {
        builder.ToTable("Envios");
        builder.HasKey(envio => envio.Id);

        builder.Property(envio => envio.TextoPergunta).HasMaxLength(Pergunta.TamanhoMaximo).IsRequired();
        builder.Property(envio => envio.Mensagem).HasMaxLength(300).IsRequired();
        builder.Property(envio => envio.TelefoneDestino)
            .HasConversion<ConversorTelefone>()
            .HasMaxLength(ConversorTelefone.TamanhoMaximo)
            .IsRequired();
        builder.Property(envio => envio.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(envio => envio.Idoso)
            .WithMany()
            .HasForeignKey(envio => envio.IdosoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Pergunta>()
            .WithMany()
            .HasForeignKey(envio => envio.PerguntaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Categoria>()
            .WithMany()
            .HasForeignKey(envio => envio.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(envio => envio.EnviadoPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);

        // Cada envio tem no máximo uma página (1:0..1).
        builder.HasOne(envio => envio.Pagina)
            .WithOne(pagina => pagina.Envio)
            .HasForeignKey<PaginaCaderno>(pagina => pagina.EnvioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(envio => new { envio.IdosoId, envio.PerguntaId });
    }
}

internal sealed class PaginaCadernoConfiguracao : IEntityTypeConfiguration<PaginaCaderno>
{
    public void Configure(EntityTypeBuilder<PaginaCaderno> builder)
    {
        builder.ToTable("Paginas");
        builder.HasKey(pagina => pagina.Id);

        builder.Property(pagina => pagina.Titulo).HasMaxLength(PaginaCaderno.TamanhoMaximoTitulo).IsRequired();
        builder.Property(pagina => pagina.AudioCaminho).HasMaxLength(260).IsRequired();
        builder.Property(pagina => pagina.FotoCaminho).HasMaxLength(260);
        builder.Ignore(pagina => pagina.TemFoto);

        // US10: a mesma chave enviada duas vezes pelo celular não cria duas páginas.
        builder.HasIndex(pagina => pagina.ChaveIdempotencia).IsUnique();
        builder.HasIndex(pagina => pagina.CriadaEm);

        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(pagina => pagina.CriadaPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
