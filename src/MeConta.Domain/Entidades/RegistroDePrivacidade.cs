using MeConta.Domain.Comum;
using MeConta.Domain.Enums;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Histórico de cada vez que uma memória passou pelo filtro de dados pessoais (US18).
/// Alimenta o painel "Risco Cidadão". Não guarda nenhum dado pessoal em claro:
/// só o tipo do dado, o nível e o trecho já mascarado (ex.: "***.***.***-09").
/// </summary>
public sealed class RegistroDePrivacidade : Entidade
{
    private readonly List<AchadoRegistrado> _achados = [];

    // Construtor usado pelo Entity Framework ao ler do banco.
    private RegistroDePrivacidade()
    {
    }

    public RegistroDePrivacidade(
        int paginaId,
        int familiaId,
        int idosoId,
        int membroId,
        OrigemDaVerificacao origem,
        ResultadoDaVerificacao resultado,
        string? bairro,
        int indiceRiscoCidadao,
        bool analiseCompleta,
        string versaoDasRegras,
        IEnumerable<AchadoRegistrado> achados,
        DateTime agora)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(indiceRiscoCidadao);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indiceRiscoCidadao, 100);
        ArgumentNullException.ThrowIfNull(achados);

        PaginaId = paginaId;
        FamiliaId = familiaId;
        IdosoId = idosoId;
        MembroId = membroId;
        Origem = origem;
        Resultado = resultado;
        Bairro = string.IsNullOrWhiteSpace(bairro) ? null : bairro.Trim();
        IndiceRiscoCidadao = indiceRiscoCidadao;
        AnaliseCompleta = analiseCompleta;
        VersaoDasRegras = versaoDasRegras;
        CriadoEm = agora;
        _achados.AddRange(achados);
    }

    public int PaginaId { get; private set; }

    public int FamiliaId { get; private set; }

    public int IdosoId { get; private set; }

    public int MembroId { get; private set; }

    public OrigemDaVerificacao Origem { get; private set; }

    public ResultadoDaVerificacao Resultado { get; private set; }

    /// <summary>Bairro escolhido na tentativa de publicação; vazio na análise.</summary>
    public string? Bairro { get; private set; }

    public int IndiceRiscoCidadao { get; private set; }

    public bool AnaliseCompleta { get; private set; }

    public string VersaoDasRegras { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }

    public IReadOnlyCollection<AchadoRegistrado> Achados => _achados.AsReadOnly();
}

/// <summary>
/// Um dado pessoal encontrado numa verificação, já mascarado.
/// </summary>
public sealed class AchadoRegistrado : Entidade
{
    public const int TamanhoMaximoTrecho = 200;

    // Construtor usado pelo Entity Framework ao ler do banco.
    private AchadoRegistrado()
    {
    }

    public AchadoRegistrado(string regraId, string tipo, string rotulo, string nivel, string? campo, string trechoMascarado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regraId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipo);
        ArgumentException.ThrowIfNullOrWhiteSpace(rotulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nivel);
        ArgumentNullException.ThrowIfNull(trechoMascarado);

        RegraId = regraId;
        Tipo = tipo;
        Rotulo = rotulo;
        Nivel = nivel;
        Campo = campo;
        // O trecho já vem mascarado; só é cortado para caber na coluna.
        TrechoMascarado = trechoMascarado.Length > TamanhoMaximoTrecho
            ? trechoMascarado[..TamanhoMaximoTrecho]
            : trechoMascarado;
    }

    public int RegistroId { get; private set; }

    public string RegraId { get; private set; } = string.Empty;

    /// <summary>Tipo do dado (ex.: "Cpf", "Saude").</summary>
    public string Tipo { get; private set; } = string.Empty;

    /// <summary>Nome do dado em linguagem simples (ex.: "CPF", "Dado de saúde").</summary>
    public string Rotulo { get; private set; } = string.Empty;

    /// <summary>"Baixo", "Medio" ou "Alto".</summary>
    public string Nivel { get; private set; } = string.Empty;

    /// <summary>Em que campo estava ("Titulo" ou "Transcricao").</summary>
    public string? Campo { get; private set; }

    public string TrechoMascarado { get; private set; } = string.Empty;
}
