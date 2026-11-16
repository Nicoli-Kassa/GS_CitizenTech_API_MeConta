namespace MeConta.Privacidade.Normalizacao;

/// <summary>
/// Um pedaço do texto original, por posição e tamanho.
/// </summary>
public readonly record struct Trecho(int Inicio, int Tamanho)
{
    public int Fim => Inicio + Tamanho;

    public bool SeSobrepoe(Trecho outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}

/// <summary>
/// Texto preparado para a busca de dados pessoais: minúsculas, sem acentos, sem caracteres invisíveis
/// e com números falados ("um dois três") trocados por dígitos.
/// Guarda, para cada caractere, de onde ele veio no texto original. Assim um achado no texto
/// normalizado aponta para o trecho certo do original, para mascarar e remover.
/// </summary>
public sealed class TextoNormalizado
{
    private readonly int[] _inicioNoOriginal;
    private readonly int[] _fimNoOriginal;

    internal TextoNormalizado(string original, string valor, int[] inicioNoOriginal, int[] fimNoOriginal)
    {
        if (valor.Length != inicioNoOriginal.Length || valor.Length != fimNoOriginal.Length)
        {
            throw new ArgumentException("O mapa de posições precisa ter um item por caractere.");
        }

        Original = original;
        Valor = valor;
        _inicioNoOriginal = inicioNoOriginal;
        _fimNoOriginal = fimNoOriginal;
    }

    /// <summary>Texto como a família escreveu.</summary>
    public string Original { get; }

    /// <summary>Texto normalizado, onde as regras procuram.</summary>
    public string Valor { get; }

    /// <summary>
    /// Converte um trecho do texto normalizado no trecho correspondente do texto original.
    /// </summary>
    public Trecho ParaOriginal(int inicio, int tamanho)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inicio);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanho, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(inicio + tamanho, Valor.Length);

        int inicioOriginal = _inicioNoOriginal[inicio];
        int fimOriginal = _fimNoOriginal[inicio + tamanho - 1];
        return new Trecho(inicioOriginal, fimOriginal - inicioOriginal);
    }
}
