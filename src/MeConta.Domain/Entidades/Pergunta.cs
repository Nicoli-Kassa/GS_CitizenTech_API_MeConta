using MeConta.Domain.Comum;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Implementa o que vale para toda pergunta: texto de 10 a 200 caracteres e uma categoria.
/// Cada tipo concreto decide quem pode ver a pergunta.
/// No banco, as duas subclasses ficam numa única tabela, separadas por uma coluna discriminadora (TPH).
/// </summary>
public abstract class Pergunta : Entidade, IPergunta
{
    public const int TamanhoMinimo = 10;
    public const int TamanhoMaximo = 200;

    // Construtor usado pelo Entity Framework ao ler do banco.
    protected Pergunta()
    {
    }

    protected Pergunta(string texto, int categoriaId)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < TamanhoMinimo)
        {
            throw new RegraDeNegocioException("Escreva uma pergunta um pouco maior");
        }

        if (texto.Trim().Length > TamanhoMaximo)
        {
            throw new RegraDeNegocioException($"Use até {TamanhoMaximo} caracteres na pergunta");
        }

        if (categoriaId <= 0)
        {
            throw new RegraDeNegocioException("Escolha uma categoria para a pergunta");
        }

        Texto = texto.Trim();
        CategoriaId = categoriaId;
    }

    public string Texto { get; private set; } = string.Empty;

    public int CategoriaId { get; private set; }

    public Categoria? Categoria { get; private set; }

    public abstract TipoPergunta Tipo { get; }

    /// <inheritdoc />
    public abstract bool VisivelPara(int familiaId);
}
