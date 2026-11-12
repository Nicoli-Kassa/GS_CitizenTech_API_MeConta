using MeConta.Domain.Comum;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Assunto das perguntas: receitas, história da família, ofícios, o bairro e conselhos.
/// As cinco categorias vêm do seed do banco.
/// </summary>
public sealed class Categoria : Entidade
{
    // Construtor usado pelo Entity Framework ao ler do banco.
    private Categoria()
    {
    }

    public Categoria(string slug, string nome)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("O identificador da categoria é obrigatório.", nameof(slug));
        }

        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(nome));
        }

        Slug = slug.Trim().ToLowerInvariant();
        Nome = nome.Trim();
    }

    /// <summary>Identificador usado na URL. Ex.: "receitas".</summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>Nome mostrado no app. Ex.: "Receitas".</summary>
    public string Nome { get; private set; } = string.Empty;
}
