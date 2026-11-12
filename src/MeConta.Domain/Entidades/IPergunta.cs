using MeConta.Domain.Enums;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Contrato comum a qualquer pergunta que o Me Conta pode enviar.
/// </summary>
public interface IPergunta
{
    int Id { get; }

    string Texto { get; }

    int CategoriaId { get; }

    TipoPergunta Tipo { get; }

    /// <summary>
    /// Indica se a pergunta pode ser vista e enviada pela família informada.
    /// </summary>
    bool VisivelPara(int familiaId);
}
