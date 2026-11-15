using MeConta.Domain.Comum;
using MeConta.Domain.Excecoes;

namespace MeConta.Domain.Entidades;

/// <summary>
/// O idoso apertou o botão "quero contar" no porta-retrato (US15).
/// A família vê o aviso no app e liga ou visita para gravar a resposta.
/// </summary>
public sealed class AvisoQueroContar : Entidade
{
    public const string MensagemEnvioDeOutroIdoso = "Essa pergunta não é deste porta-retrato";

    // Construtor usado pelo Entity Framework ao ler do banco.
    private AvisoQueroContar()
    {
    }

    /// <param name="portaRetrato">Aparelho em que o botão foi apertado.</param>
    /// <param name="envio">
    /// Pergunta que estava na tela, ou <see langword="null"/> quando o idoso
    /// apertou o botão sem pergunta nova ("quero contar uma coisa").
    /// </param>
    /// <param name="agora">Hora do aviso (UTC).</param>
    /// <exception cref="RegraDeNegocioException">A pergunta foi enviada para outro idoso.</exception>
    public AvisoQueroContar(PortaRetrato portaRetrato, EnvioPergunta? envio, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(portaRetrato);

        if (envio is not null && envio.IdosoId != portaRetrato.IdosoId)
        {
            throw new RegraDeNegocioException(MensagemEnvioDeOutroIdoso);
        }

        IdosoId = portaRetrato.IdosoId;
        EnvioId = envio?.Id;
        CriadoEm = agora;
    }

    public int IdosoId { get; private set; }

    public int? EnvioId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    public DateTime? VistoEm { get; private set; }

    public int? VistoPorMembroId { get; private set; }

    public bool FoiVisto => VistoEm is not null;

    /// <summary>
    /// Alguém da família viu o aviso. Marcar de novo não muda quem viu primeiro.
    /// </summary>
    public void MarcarVisto(int membroId, DateTime agora)
    {
        if (FoiVisto)
        {
            return;
        }

        VistoEm = agora;
        VistoPorMembroId = membroId;
    }
}
