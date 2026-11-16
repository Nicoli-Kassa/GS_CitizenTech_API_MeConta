using MeConta.Privacidade.Regras;

namespace MeConta.Privacidade.Resultados;

/// <summary>
/// Resultado de um campo dentro de uma análise de vários campos.
/// </summary>
public sealed record ResultadoDoCampo(string Nome, ResultadoAnalisePii Resultado);

/// <summary>
/// Análise de vários campos do mesmo conteúdo (ex.: título e transcrição de uma memória).
/// Cada campo é analisado separado, mas a decisão vale para o conjunto: um CPF na transcrição
/// impede a publicação do título também, e as combinações perigosas são procuradas entre os campos
/// (endereço no título + "moro sozinha" na transcrição).
/// </summary>
public sealed class AnaliseDeCampos : IResultadoDeRisco
{
    internal AnaliseDeCampos(
        IReadOnlyList<ResultadoDoCampo> campos,
        IReadOnlyList<AchadoPii> achados,
        IReadOnlyList<CombinacaoEncontrada> combinacoes,
        int indiceRiscoCidadao,
        string versaoDasRegras)
    {
        Campos = campos;
        Achados = achados;
        Combinacoes = combinacoes;
        IndiceRiscoCidadao = indiceRiscoCidadao;
        VersaoDasRegras = versaoDasRegras;
    }

    public IReadOnlyList<ResultadoDoCampo> Campos { get; }

    /// <summary>Achados de todos os campos, cada um com o nome do campo.</summary>
    public IReadOnlyList<AchadoPii> Achados { get; }

    public IReadOnlyList<CombinacaoEncontrada> Combinacoes { get; }

    public int IndiceRiscoCidadao { get; }

    public NivelDeRisco Faixa => CalculadoraDeRiscoCidadao.FaixaDe(IndiceRiscoCidadao);

    public bool AnaliseCompleta => Campos.All(campo => campo.Resultado.AnaliseCompleta);

    public string VersaoDasRegras { get; }

    public bool DeveBloquear => !AnaliseCompleta || Achados.Any(achado => achado.Nivel == NivelDeRisco.Alto);

    public bool ExigeConfirmacao => Achados.Any(achado => achado.Nivel == NivelDeRisco.Medio);

    /// <summary>Versão segura de um campo, com os dados trocados por avisos.</summary>
    public string TextoSeguroDe(string campo) => ResultadoDe(campo).TextoSeguro;

    public ResultadoAnalisePii ResultadoDe(string campo) =>
        Campos.FirstOrDefault(item => item.Nome == campo)?.Resultado
        ?? throw new ArgumentException($"O campo \"{campo}\" não foi analisado.", nameof(campo));
}
