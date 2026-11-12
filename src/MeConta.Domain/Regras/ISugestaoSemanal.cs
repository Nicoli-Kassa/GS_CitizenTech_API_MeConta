using MeConta.Domain.Entidades;
using MeConta.Domain.ValueObjects;

namespace MeConta.Domain.Regras;

/// <summary>
/// Escolhe a pergunta da semana de cada idoso (US05).
/// </summary>
public interface ISugestaoSemanal
{
    /// <summary>
    /// Sugere uma pergunta do catálogo que ainda não foi enviada ao idoso.
    /// Retorna <see langword="null"/> quando todas já foram enviadas;
    /// o app então mostra o convite "Escreva uma pergunta sua".
    /// </summary>
    IPergunta? Sugerir(
        int idosoId,
        IEnumerable<IPergunta> perguntas,
        IReadOnlySet<int> idsJaEnviados,
        Semana semana);
}
