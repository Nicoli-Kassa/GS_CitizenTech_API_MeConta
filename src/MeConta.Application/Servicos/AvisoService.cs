using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;

namespace MeConta.Application.Servicos;

/// <summary>
/// Avisos "quero contar" do porta-retrato, vistos pela família no app (US15).
/// </summary>
public sealed class AvisoService(
    IControleDeAcesso acesso,
    IAvisoRepository avisos,
    IEnvioRepository envios,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : IAvisoService
{
    public async Task<IReadOnlyList<AvisoResponse>> ListarAsync(
        int familiaId,
        bool somenteNaoVistos,
        CancellationToken cancellationToken = default)
    {
        await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);
        return await avisos.ListarDaFamiliaAsync(familiaId, somenteNaoVistos, cancellationToken);
    }

    public async Task<AvisoResponse> MarcarVistoAsync(int avisoId, CancellationToken cancellationToken = default)
    {
        AvisoQueroContar aviso = await avisos.ObterPorIdAsync(avisoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Aviso não encontrado.");
        AcessoAoIdoso acessoAoIdoso = await acesso.GarantirAcessoAoIdosoAsync(aviso.IdosoId, cancellationToken);

        aviso.MarcarVisto(acessoAoIdoso.Membro.Id, relogio.GetUtcNow().UtcDateTime);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        string? pergunta = null;
        if (aviso.EnvioId is int envioId)
        {
            EnvioPergunta? envio = await envios.ObterPorIdAsync(envioId, cancellationToken);
            pergunta = envio?.TextoPergunta;
        }

        return AvisoResponse.De(aviso, acessoAoIdoso.Idoso.Apelido, pergunta);
    }
}
