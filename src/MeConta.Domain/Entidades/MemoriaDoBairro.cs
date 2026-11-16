using MeConta.Domain.Comum;
using MeConta.Domain.Excecoes;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Uma página do caderno que a família levou para o mapa de memórias do bairro (US16),
/// com a autorização da pessoa idosa. Guarda uma cópia do que foi publicado, já conferido
/// pelo filtro de dados pessoais: título, apelido e, se houver, a transcrição.
/// Nunca guarda telefone nem nome completo (US16, US17).
/// </summary>
public sealed class MemoriaDoBairro : Entidade
{
    public const int TamanhoMaximoBairro = 60;
    public const string MensagemBairroVazio = "Escolha o bairro da memória";
    public const string MensagemBairroLongo = "Use até 60 caracteres no nome do bairro";
    public const string MensagemJaNoMapa = "Esta memória já está no mapa do bairro";

    // Construtor usado pelo Entity Framework ao ler do banco.
    private MemoriaDoBairro()
    {
    }

    /// <param name="pagina">Página com o envio e o idoso carregados.</param>
    /// <param name="bairro">Bairro do mapa. Sempre o bairro, nunca o endereço.</param>
    /// <param name="idosoAutorizou">A família marcou que a pessoa idosa autorizou a publicação (LGPD, art. 7º, I).</param>
    /// <param name="tituloVerificado">Título já liberado pelo filtro de dados pessoais.</param>
    /// <param name="textoVerificado">Transcrição já liberada pelo filtro, ou <see langword="null"/>.</param>
    /// <param name="indiceRiscoCidadao">Índice Risco Cidadão da memória no momento da publicação.</param>
    /// <param name="publicadaPorMembroId">Quem publicou.</param>
    /// <param name="agora">Hora da publicação (UTC).</param>
    /// <exception cref="RegraDeNegocioException">Sem autorização ou sem bairro.</exception>
    public MemoriaDoBairro(
        PaginaCaderno pagina,
        string? bairro,
        bool idosoAutorizou,
        string tituloVerificado,
        string? textoVerificado,
        int indiceRiscoCidadao,
        int publicadaPorMembroId,
        DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(pagina);
        EnvioPergunta envio = pagina.Envio
            ?? throw new ArgumentException("Carregue o envio junto com a página.", nameof(pagina));
        Idoso idoso = envio.Idoso
            ?? throw new ArgumentException("Carregue o idoso junto com o envio.", nameof(pagina));

        if (!idosoAutorizou)
        {
            throw new RegraDeNegocioException(MensagemSemAutorizacao(idoso.Apelido));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tituloVerificado);
        ArgumentOutOfRangeException.ThrowIfNegative(indiceRiscoCidadao);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(indiceRiscoCidadao, 100);

        Bairro = Texto.Obrigatorio(bairro, TamanhoMaximoBairro, MensagemBairroVazio, MensagemBairroLongo);
        PaginaId = pagina.Id;
        IdosoId = idoso.Id;
        FamiliaId = idoso.FamiliaId;
        CategoriaId = envio.CategoriaId;
        Apelido = idoso.Apelido;
        Titulo = tituloVerificado;
        Transcricao = string.IsNullOrWhiteSpace(textoVerificado) ? null : textoVerificado;
        IndiceRiscoCidadao = indiceRiscoCidadao;
        AutorizadaPeloIdoso = true;
        PublicadaPorMembroId = publicadaPorMembroId;
        PublicadaEm = agora;
    }

    public int PaginaId { get; private set; }

    public int IdosoId { get; private set; }

    public int FamiliaId { get; private set; }

    public int CategoriaId { get; private set; }

    public string Bairro { get; private set; } = string.Empty;

    /// <summary>Como a família chama a pessoa idosa (ex.: "Vó Lourdes"). O nome completo nunca vai para o mapa.</summary>
    public string Apelido { get; private set; } = string.Empty;

    public string Titulo { get; private set; } = string.Empty;

    /// <summary>Transcrição publicada, já conferida pelo filtro de dados pessoais.</summary>
    public string? Transcricao { get; private set; }

    public int IndiceRiscoCidadao { get; private set; }

    /// <summary>Registro do consentimento: a memória só existe se a pessoa idosa autorizou.</summary>
    public bool AutorizadaPeloIdoso { get; private set; }

    public int PublicadaPorMembroId { get; private set; }

    public DateTime PublicadaEm { get; private set; }

    public DateTime? RemovidaEm { get; private set; }

    public int? RemovidaPorMembroId { get; private set; }

    public PaginaCaderno? Pagina { get; private set; }

    public bool EstaNoMapa => RemovidaEm is null;

    public static string MensagemSemAutorizacao(string apelido) =>
        $"Marque que {apelido} autorizou a publicação no mapa do bairro";

    /// <summary>
    /// Tira a memória do mapa na hora (US16). Tirar de novo não muda quem tirou primeiro.
    /// </summary>
    public void TirarDoMapa(int membroId, DateTime agora)
    {
        if (!EstaNoMapa)
        {
            return;
        }

        RemovidaEm = agora;
        RemovidaPorMembroId = membroId;
    }
}
