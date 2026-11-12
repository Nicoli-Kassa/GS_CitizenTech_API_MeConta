using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.ValueObjects;

namespace MeConta.Domain.Regras;

/// <summary>
/// Escolha determinística, sem precisar guardar nada no banco:
/// a mesma semana e o mesmo idoso dão sempre a mesma pergunta,
/// e na semana seguinte a posição avança uma casa.
/// </summary>
public sealed class SugestaoSemanal : ISugestaoSemanal
{
    /// <summary>
    /// Espalha os idosos pelo catálogo, para que avós diferentes
    /// não recebam todos a mesma pergunta na mesma semana.
    /// </summary>
    private const int Espalhamento = 7;

    /// <inheritdoc />
    public IPergunta? Sugerir(
        int idosoId,
        IEnumerable<IPergunta> perguntas,
        IReadOnlySet<int> idsJaEnviados,
        Semana semana)
    {
        ArgumentNullException.ThrowIfNull(perguntas);
        ArgumentNullException.ThrowIfNull(idsJaEnviados);

        // Só perguntas do catálogo que esse idoso ainda não recebeu, sempre na mesma ordem.
        IPergunta[] candidatas = perguntas
            .Where(pergunta => pergunta.Tipo == TipoPergunta.Catalogo)
            .Where(pergunta => !idsJaEnviados.Contains(pergunta.Id))
            .OrderBy(pergunta => pergunta.Id)
            .ToArray();

        if (candidatas.Length == 0)
        {
            return null;
        }

        long posicao = (long)semana.Numero + ((long)idosoId * Espalhamento);

        // Resto da divisão sempre positivo, mesmo se a posição for negativa.
        int indice = (int)(((posicao % candidatas.Length) + candidatas.Length) % candidatas.Length);
        return candidatas[indice];
    }
}
