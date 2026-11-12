using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Servicos;

/// <summary>
/// Envio da pergunta pelo WhatsApp com um toque (US07).
/// A API registra o envio e devolve o link wa.me; quem abre o WhatsApp é o app.
/// </summary>
public sealed class EnvioService(
    IControleDeAcesso acesso,
    IPerguntaRepository perguntas,
    IEnvioRepository envios,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IEnvioService
{
    public async Task<EnvioResponse> EnviarAsync(
        int idosoId,
        EnviarPerguntaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);

        Pergunta pergunta = await perguntas.ObterPorIdAsync(request.PerguntaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Pergunta não encontrada.");

        EnvioPergunta envio = new(
            acessoAoIdoso.Idoso,
            pergunta,
            acessoAoIdoso.Membro.Id,
            relogio.GetUtcNow().UtcDateTime);
        await envios.AdicionarAsync(envio, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return EnvioResponse.De(envio);
    }

    public async Task<IReadOnlyList<EnvioResponse>> ListarAsync(
        int idosoId,
        StatusEnvio? status,
        CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAoIdosoAsync(idosoId, cancellationToken);
        IReadOnlyList<EnvioPergunta> lista = await envios.ListarDoIdosoAsync(idosoId, status, cancellationToken);
        return lista.Select(EnvioResponse.De).ToList();
    }
}
