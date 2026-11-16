using MeConta.Privacidade.Regras;

namespace MeConta.Privacidade.Resultados;

/// <summary>
/// Um dado pessoal encontrado. Nunca guarda o dado em si: só a posição e o trecho mascarado.
/// Por isso um achado pode ir para log, painel ou IA sem vazar nada.
/// <see cref="Campo"/> diz em que campo ele estava, quando a análise é de vários campos (título, transcrição).
/// </summary>
public sealed record AchadoPii(
    string RegraId,
    string Tipo,
    string Rotulo,
    NivelDeRisco Nivel,
    int Inicio,
    int Tamanho,
    string TrechoMascarado,
    double Confianca,
    bool TemContexto,
    string Explicacao,
    string? Campo = null);

/// <summary>
/// Combinação de dados que aumentou o risco (ex.: endereço + "moro sozinha").
/// </summary>
public sealed record CombinacaoEncontrada(string Id, int Bonus, string Explicacao);

/// <summary>
/// O que todo resultado de análise informa: os achados, o índice Risco Cidadão e a decisão.
/// </summary>
public interface IResultadoDeRisco
{
    IReadOnlyList<AchadoPii> Achados { get; }

    IReadOnlyList<CombinacaoEncontrada> Combinacoes { get; }

    int IndiceRiscoCidadao { get; }

    NivelDeRisco Faixa { get; }

    bool AnaliseCompleta { get; }

    string VersaoDasRegras { get; }

    /// <summary>Algum dado de risco alto, ou análise incompleta: não pode publicar.</summary>
    bool DeveBloquear { get; }

    /// <summary>Algum dado de risco médio: a família precisa confirmar antes de publicar.</summary>
    bool ExigeConfirmacao { get; }
}

/// <summary>
/// Resultado da análise de um texto pelo motor.
/// </summary>
public sealed class ResultadoAnalisePii : IResultadoDeRisco
{
    internal ResultadoAnalisePii(
        IReadOnlyList<AchadoPii> achados,
        IReadOnlyList<CombinacaoEncontrada> combinacoes,
        int indiceRiscoCidadao,
        string textoSeguro,
        bool analiseCompleta,
        string versaoDasRegras)
    {
        Achados = achados;
        Combinacoes = combinacoes;
        IndiceRiscoCidadao = indiceRiscoCidadao;
        TextoSeguro = textoSeguro;
        AnaliseCompleta = analiseCompleta;
        VersaoDasRegras = versaoDasRegras;
    }

    public IReadOnlyList<AchadoPii> Achados { get; }

    public IReadOnlyList<CombinacaoEncontrada> Combinacoes { get; }

    /// <summary>Risco Cidadão do texto, de 0 (nenhum dado pessoal) a 100.</summary>
    public int IndiceRiscoCidadao { get; }

    /// <summary>Faixa do índice: até 29 Baixo, de 30 a 69 Médio, a partir de 70 Alto.</summary>
    public NivelDeRisco Faixa => CalculadoraDeRiscoCidadao.FaixaDe(IndiceRiscoCidadao);

    /// <summary>
    /// Texto com cada dado pessoal trocado por um aviso ("[CPF removido]").
    /// É a única versão do texto que pode ir para log ou para a IA.
    /// </summary>
    public string TextoSeguro { get; }

    /// <summary>
    /// Falso quando a análise não terminou (texto grande demais ou expressão que passou do tempo).
    /// Nesse caso o motor bloqueia, em vez de deixar passar sem conferir.
    /// </summary>
    public bool AnaliseCompleta { get; }

    public string VersaoDasRegras { get; }

    /// <summary>Algum dado de risco alto, ou análise incompleta: não pode publicar.</summary>
    public bool DeveBloquear => !AnaliseCompleta || Achados.Any(achado => achado.Nivel == NivelDeRisco.Alto);

    /// <summary>Algum dado de risco médio: a família precisa confirmar antes de publicar.</summary>
    public bool ExigeConfirmacao => Achados.Any(achado => achado.Nivel == NivelDeRisco.Medio);

    public bool TemDadoPessoal => Achados.Count > 0;
}
