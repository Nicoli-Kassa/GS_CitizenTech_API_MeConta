using MeConta.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MeConta.Infrastructure.Persistencia.Configuracoes;

internal sealed class PortaRetratoConfiguracao : IEntityTypeConfiguration<PortaRetrato>
{
    /// <summary>SHA-256 em hexadecimal: 64 caracteres.</summary>
    private const int TamanhoHash = 64;

    public void Configure(EntityTypeBuilder<PortaRetrato> builder)
    {
        builder.ToTable("PortaRetratos");
        builder.HasKey(portaRetrato => portaRetrato.Id);

        builder.Property(portaRetrato => portaRetrato.Codigo)
            .HasMaxLength(PortaRetrato.TamanhoCodigo)
            .IsFixedLength()
            .IsRequired();
        builder.Property(portaRetrato => portaRetrato.ChaveHash)
            .HasMaxLength(TamanhoHash)
            .IsFixedLength()
            .IsRequired();
        builder.Ignore(portaRetrato => portaRetrato.TopicoBase);

        // Um porta-retrato por idoso; código e chave não se repetem.
        builder.HasIndex(portaRetrato => portaRetrato.IdosoId).IsUnique();
        builder.HasIndex(portaRetrato => portaRetrato.Codigo).IsUnique();
        builder.HasIndex(portaRetrato => portaRetrato.ChaveHash).IsUnique();

        builder.HasOne(portaRetrato => portaRetrato.Idoso)
            .WithMany()
            .HasForeignKey(portaRetrato => portaRetrato.IdosoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(portaRetrato => portaRetrato.CriadoPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AvisoQueroContarConfiguracao : IEntityTypeConfiguration<AvisoQueroContar>
{
    public void Configure(EntityTypeBuilder<AvisoQueroContar> builder)
    {
        builder.ToTable("AvisosQueroContar");
        builder.HasKey(aviso => aviso.Id);
        builder.Ignore(aviso => aviso.FoiVisto);

        // O aviso fica ligado ao idoso (não ao aparelho): se o porta-retrato for trocado, o histórico continua.
        builder.HasOne<Idoso>()
            .WithMany()
            .HasForeignKey(aviso => aviso.IdosoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Sem cascata aqui: o SQL Server não aceita dois caminhos de exclusão
        // (Idoso → Envios → Avisos e Idoso → Avisos) chegando à mesma tabela.
        builder.HasOne<EnvioPergunta>()
            .WithMany()
            .HasForeignKey(aviso => aviso.EnvioId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Membro>()
            .WithMany()
            .HasForeignKey(aviso => aviso.VistoPorMembroId)
            .OnDelete(DeleteBehavior.Restrict);

        // Um aviso por pergunta; vários avisos sem pergunta são permitidos.
        builder.HasIndex(aviso => aviso.EnvioId)
            .IsUnique()
            .HasFilter("[EnvioId] IS NOT NULL");
        builder.HasIndex(aviso => new { aviso.IdosoId, aviso.CriadoEm });
    }
}
