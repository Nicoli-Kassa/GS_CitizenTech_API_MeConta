using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Domain.Regras;

/// <summary>
/// Decide o que o porta-retrato mostra: as perguntas que ainda esperam o idoso.
/// </summary>
public static class FilaDoPortaRetrato
{
    /// <summary>
    /// Perguntas enviadas que ainda não viraram página do caderno e para as quais
    /// o idoso ainda não apertou "quero contar", da mais recente para a mais antiga.
    /// A primeira da lista é a que aparece na tela.
    /// </summary>
    /// <param name="enviosDoIdoso">Envios do idoso (qualquer status).</param>
    /// <param name="enviosComAviso">Ids dos envios que já receberam "quero contar".</param>
    public static IReadOnlyList<EnvioPergunta> PerguntasEsperando(
        IEnumerable<EnvioPergunta> enviosDoIdoso,
        IReadOnlySet<int> enviosComAviso)
    {
        ArgumentNullException.ThrowIfNull(enviosDoIdoso);
        ArgumentNullException.ThrowIfNull(enviosComAviso);

        return enviosDoIdoso
            .Where(envio => envio.Status == StatusEnvio.Enviada && !enviosComAviso.Contains(envio.Id))
            .OrderByDescending(envio => envio.EnviadoEm)
            .ThenByDescending(envio => envio.Id)
            .ToList();
    }

    /// <summary>
    /// Primeiro nome de quem perguntou. Só ele vai para o aparelho, que fala por um broker MQTT público.
    /// </summary>
    public static string PrimeiroNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return "Sua família";
        }

        return nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }
}
