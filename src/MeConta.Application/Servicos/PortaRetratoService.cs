using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;

namespace MeConta.Application.Servicos;

/// <summary>
/// Pareamento do porta-retrato com o idoso, feito por alguém da família (US15).
/// </summary>
public sealed class PortaRetratoService(
    IControleDeAcesso acesso,
    IPortaRetratoRepository portaRetratos,
    IGeradorDeCredenciais geradorDeCredenciais,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IPortaRetratoService
{
    public const string MensagemSemPortaRetrato = "Este idoso ainda não tem porta-retrato.";

    /// <summary>Tentativas de sortear um código que ainda não está em uso.</summary>
    private const int TentativasDeCodigo = 5;

    private DateTime Agora => relogio.GetUtcNow().UtcDateTime;

    public async Task<PortaRetratoPareadoResponse> ParearAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);

        string codigo = await SortearCodigoLivreAsync(cancellationToken);
        string chave = geradorDeCredenciais.GerarChave();

        PortaRetrato? portaRetrato = await portaRetratos.ObterDoIdosoAsync(idosoId, cancellationToken);
        if (portaRetrato is null)
        {
            portaRetrato = new PortaRetrato(acessoAoIdoso.Idoso, codigo, chave, acessoAoIdoso.Membro.Id, Agora);
            await portaRetratos.AdicionarAsync(portaRetrato, cancellationToken);
        }
        else
        {
            // Parear de novo troca as credenciais: a chave antiga para de funcionar na hora.
            portaRetrato.TrocarCredenciais(codigo, chave, acessoAoIdoso.Membro.Id, Agora);
        }

        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return PortaRetratoPareadoResponse.De(portaRetrato, chave);
    }

    public async Task<PortaRetratoResponse> ObterAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);
        PortaRetrato portaRetrato = await ObterDoIdosoAsync(idosoId, cancellationToken);
        return PortaRetratoResponse.De(portaRetrato, Agora);
    }

    public async Task DesparearAsync(int idosoId, CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);
        PortaRetrato portaRetrato = await ObterDoIdosoAsync(idosoId, cancellationToken);

        portaRetratos.Remover(portaRetrato);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<PortaRetrato> ObterDoIdosoAsync(int idosoId, CancellationToken cancellationToken) =>
        await portaRetratos.ObterDoIdosoAsync(idosoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException(MensagemSemPortaRetrato);

    private async Task<string> SortearCodigoLivreAsync(CancellationToken cancellationToken)
    {
        for (int tentativa = 0; tentativa < TentativasDeCodigo; tentativa++)
        {
            string codigo = geradorDeCredenciais.GerarCodigo();
            if (!await portaRetratos.CodigoEmUsoAsync(codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException("Não foi possível sortear um código livre para o porta-retrato.");
    }
}
