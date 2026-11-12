using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.Regras;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Pergunta escrita pela própria família (US06). Só essa família vê e envia.
/// Passa pelo filtro anti-golpe: o Me Conta nunca envia pedidos de dinheiro, PIX, senha ou código.
/// </summary>
public sealed class PerguntaDaFamilia : Pergunta
{
    public const string MensagemPedidoSuspeito = "O Me Conta não envia pedidos de dinheiro ou senhas";

    // Construtor usado pelo Entity Framework ao ler do banco.
    private PerguntaDaFamilia()
    {
    }

    public PerguntaDaFamilia(
        string texto,
        int categoriaId,
        int familiaId,
        int criadaPorMembroId,
        DateTime agora,
        IFiltroAntiGolpe filtro)
        : base(texto, categoriaId)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (familiaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(familiaId), "A família deve estar salva.");
        }

        if (filtro.EhSuspeito(Texto))
        {
            throw new RegraDeNegocioException(MensagemPedidoSuspeito);
        }

        FamiliaId = familiaId;
        CriadaPorMembroId = criadaPorMembroId;
        CriadaEm = agora;
    }

    public int FamiliaId { get; private set; }

    public int CriadaPorMembroId { get; private set; }

    public DateTime CriadaEm { get; private set; }

    public override TipoPergunta Tipo => TipoPergunta.DaFamilia;

    public override bool VisivelPara(int familiaId) => FamiliaId == familiaId;
}
