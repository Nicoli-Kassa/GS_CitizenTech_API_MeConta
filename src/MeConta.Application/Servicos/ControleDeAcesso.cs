using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;

namespace MeConta.Application.Servicos;

/// <summary>
/// Concentra a regra "só a família vê e mexe nos dados da família" (US09, RNF03).
/// </summary>
public sealed class ControleDeAcesso(
    IUsuarioAtual usuarioAtual,
    IMembroRepository membros,
    IFamiliaRepository familias,
    IIdosoRepository idosos) : IControleDeAcesso
{
    public const string MensagemSemLogin = "Faça login para continuar.";
    public const string MensagemPerfilIncompleto = "Complete o seu perfil antes de continuar (POST /api/v1/membros).";
    public const string MensagemForaDaFamilia = "Só quem é da família pode ver e mudar estes dados.";

    public async Task<Membro> ObterMembroAtualAsync(CancellationToken cancellationToken = default)
    {
        string usuarioId = usuarioAtual.UsuarioId
            ?? throw new AcessoNegadoException(MensagemSemLogin);

        Membro? membro = await membros.ObterPorUsuarioIdAsync(usuarioId, cancellationToken);
        return membro ?? throw new AcessoNegadoException(MensagemPerfilIncompleto);
    }

    public async Task<Membro> GarantirAcessoAFamiliaAsync(int familiaId, CancellationToken cancellationToken = default)
    {
        Membro membro = await ObterMembroAtualAsync(cancellationToken);

        if (!await familias.ExisteAsync(familiaId, cancellationToken))
        {
            throw new RecursoNaoEncontradoException("Família não encontrada.");
        }

        if (!await familias.EhMembroAsync(familiaId, membro.Id, cancellationToken))
        {
            throw new AcessoNegadoException(MensagemForaDaFamilia);
        }

        return membro;
    }

    public async Task<AcessoAoIdoso> GarantirAcessoAoIdosoAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        Idoso idoso = await idosos.ObterPorIdAsync(idosoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Idoso não encontrado.");

        Membro membro = await GarantirAcessoAFamiliaAsync(idoso.FamiliaId, cancellationToken);
        return new AcessoAoIdoso(membro, idoso);
    }
}
