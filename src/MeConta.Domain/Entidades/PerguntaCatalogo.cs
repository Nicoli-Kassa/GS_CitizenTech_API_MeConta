using MeConta.Domain.Enums;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Pergunta do catálogo do Me Conta (US04). Qualquer família pode usar.
/// </summary>
public sealed class PerguntaCatalogo : Pergunta
{
    // Construtor usado pelo Entity Framework ao ler do banco.
    private PerguntaCatalogo()
    {
    }

    public PerguntaCatalogo(string texto, int categoriaId)
        : base(texto, categoriaId)
    {
    }

    public override TipoPergunta Tipo => TipoPergunta.Catalogo;

    public override bool VisivelPara(int familiaId) => true;
}
