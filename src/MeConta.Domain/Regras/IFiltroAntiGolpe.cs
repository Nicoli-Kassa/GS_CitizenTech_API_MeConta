namespace MeConta.Domain.Regras;

/// <summary>
/// Reconhece textos que pedem dinheiro, PIX, senha ou código de verificação.
/// Protege o idoso de golpes: o Me Conta nunca envia esse tipo de pedido (RNF09).
/// </summary>
public interface IFiltroAntiGolpe
{
    /// <summary>
    /// Retorna <see langword="true"/> quando o texto parece um pedido de dinheiro ou de dados de acesso.
    /// </summary>
    bool EhSuspeito(string texto);
}
