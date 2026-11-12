namespace MeConta.Domain.Enums;

/// <summary>
/// Origem de uma pergunta.
/// </summary>
public enum TipoPergunta
{
    /// <summary>Pergunta do catálogo do Me Conta, visível para todas as famílias.</summary>
    Catalogo,

    /// <summary>Pergunta escrita por uma família, visível só para ela.</summary>
    DaFamilia,
}
