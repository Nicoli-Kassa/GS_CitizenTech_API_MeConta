namespace MeConta.Domain.Enums;

/// <summary>
/// Situação de uma pergunta enviada a um idoso.
/// </summary>
public enum StatusEnvio
{
    /// <summary>A pergunta saiu pelo WhatsApp e aguarda a resposta em áudio.</summary>
    Enviada,

    /// <summary>A resposta foi guardada como página do caderno.</summary>
    Respondida,
}
