using MeConta.Domain.Comum;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Código para um parente entrar na família (US03). Vale por 48 horas
/// e pode ser usado por mais de uma pessoa nesse período.
/// </summary>
public sealed class Convite : Entidade
{
    public const int TamanhoCodigo = 6;

    /// <summary>
    /// Letras e números aceitos no código. Ficam de fora 0, O, 1 e I,
    /// que se confundem quando alguém lê o código em voz alta.
    /// </summary>
    public const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static readonly TimeSpan Validade = TimeSpan.FromHours(48);

    // Construtor usado pelo Entity Framework ao ler do banco.
    private Convite()
    {
    }

    internal Convite(string codigo, int criadoPorMembroId, DateTime agora)
    {
        string normalizado = Normalizar(codigo);
        if (normalizado.Length != TamanhoCodigo || normalizado.Any(letra => !Alfabeto.Contains(letra)))
        {
            throw new ArgumentException(
                $"O código deve ter {TamanhoCodigo} caracteres do alfabeto do convite.",
                nameof(codigo));
        }

        Codigo = normalizado;
        CriadoPorMembroId = criadoPorMembroId;
        CriadoEm = agora;
        ExpiraEm = agora.Add(Validade);
    }

    public int FamiliaId { get; private set; }

    public string Codigo { get; private set; } = string.Empty;

    public int CriadoPorMembroId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime ExpiraEm { get; private set; }

    /// <summary>
    /// O convite vale até o instante exato em que completa 48 horas.
    /// </summary>
    public bool EstaValido(DateTime agora) => agora < ExpiraEm;

    /// <summary>
    /// Deixa o código do jeito que é guardado: sem espaços e em maiúsculas.
    /// Assim "  ab3k9z " e "AB3K9Z" são o mesmo código.
    /// </summary>
    public static string Normalizar(string? codigo) =>
        (codigo ?? string.Empty).Trim().ToUpperInvariant();
}
