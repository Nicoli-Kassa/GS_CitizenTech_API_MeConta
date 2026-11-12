using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Excecoes;

namespace MeConta.Application.Servicos;

/// <summary>
/// Perfil de quem usa o app. O cadastro de e-mail e senha fica no ASP.NET Identity;
/// aqui a pessoa informa o nome que aparece para a família.
/// </summary>
public sealed class MembroService(
    IUsuarioAtual usuarioAtual,
    IControleDeAcesso acesso,
    IMembroRepository membros,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IMembroService
{
    public async Task<MembroResponse> CriarPerfilAsync(
        CriarPerfilRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string usuarioId = usuarioAtual.UsuarioId
            ?? throw new AcessoNegadoException(ControleDeAcesso.MensagemSemLogin);

        if (await membros.ObterPorUsuarioIdAsync(usuarioId, cancellationToken) is not null)
        {
            throw new ConflitoException("O seu perfil já foi criado.");
        }

        Membro membro = new(usuarioId, request.Nome, relogio.GetUtcNow().UtcDateTime);
        await membros.AdicionarAsync(membro, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return MembroResponse.De(membro);
    }

    public async Task<MembroResponse> ObterPerfilAsync(CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        return MembroResponse.De(membro);
    }
}
