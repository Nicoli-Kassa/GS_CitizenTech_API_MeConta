using MeConta.Privacidade.Regras;

namespace MeConta.Privacidade.Resultados;

/// <summary>
/// Calcula o índice Risco Cidadão de um texto, de 0 a 100.
///
/// Cada achado tem uma chance de causar dano (peso do nível × confiança do achado).
/// O índice é a chance de pelo menos um deles causar dano: 100 × (1 − Π(1 − chance)).
/// Assim, dois dados médios pesam mais que um, mas o índice nunca passa de 100.
/// Depois somam-se os bônus das combinações perigosas e aplicam-se pisos:
/// qualquer dado alto deixa o índice em pelo menos 70; qualquer dado médio, em pelo menos 30.
/// </summary>
public static class CalculadoraDeRiscoCidadao
{
    public const int InicioDaFaixaMedia = 30;
    public const int InicioDaFaixaAlta = 70;

    private const double PesoAlto = 0.7;
    private const double PesoMedio = 0.35;
    private const double PesoBaixo = 0.1;

    public static int Calcular(IReadOnlyCollection<AchadoPii> achados, IEnumerable<CombinacaoEncontrada> combinacoes)
    {
        ArgumentNullException.ThrowIfNull(achados);
        ArgumentNullException.ThrowIfNull(combinacoes);

        if (achados.Count == 0)
        {
            return 0;
        }

        double chanceDeNenhumDano = 1.0;
        foreach (AchadoPii achado in achados)
        {
            chanceDeNenhumDano *= 1.0 - PesoDe(achado.Nivel) * achado.Confianca;
        }

        int indice = (int)Math.Round(100 * (1 - chanceDeNenhumDano), MidpointRounding.AwayFromZero);
        indice += combinacoes.Sum(combinacao => combinacao.Bonus);

        if (achados.Any(achado => achado.Nivel == NivelDeRisco.Alto))
        {
            indice = Math.Max(indice, InicioDaFaixaAlta);
        }
        else if (achados.Any(achado => achado.Nivel == NivelDeRisco.Medio))
        {
            indice = Math.Max(indice, InicioDaFaixaMedia);
        }

        return Math.Clamp(indice, 0, 100);
    }

    public static NivelDeRisco FaixaDe(int indice) => indice switch
    {
        >= InicioDaFaixaAlta => NivelDeRisco.Alto,
        >= InicioDaFaixaMedia => NivelDeRisco.Medio,
        _ => NivelDeRisco.Baixo,
    };

    private static double PesoDe(NivelDeRisco nivel) => nivel switch
    {
        NivelDeRisco.Alto => PesoAlto,
        NivelDeRisco.Medio => PesoMedio,
        _ => PesoBaixo,
    };
}
