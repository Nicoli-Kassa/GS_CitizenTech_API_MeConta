using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;

namespace MeConta.Application.Servicos;

/// <summary>
/// Caderno da família: guardar a resposta em áudio (US08), funcionar sem internet (US10),
/// ver e ouvir (US09), apagar (US11) e guardar a transcrição que o filtro de dados pessoais confere (US18).
/// </summary>
public sealed class CadernoService(
    IControleDeAcesso acesso,
    ICategoriaRepository categorias,
    IEnvioRepository envios,
    IPaginaRepository paginas,
    IArmazenamentoDeArquivos armazenamento,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio) : ICadernoService
{
    public const int TamanhoMaximoDaLista = 50;

    public async Task<ResultadoGuardarResposta> GuardarRespostaAsync(
        int envioId,
        NovaResposta resposta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resposta);
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);

        EnvioPergunta envio = await envios.ObterComIdosoAsync(envioId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Pergunta enviada não encontrada.");
        await acesso.GarantirAcessoAFamiliaAsync(envio.Idoso!.FamiliaId, cancellationToken);

        // US10: o celular reenviou uma página que já chegou? Devolve a mesma, sem duplicar.
        if (resposta.ChaveIdempotencia is Guid chave)
        {
            PaginaCaderno? existente = await paginas.ObterPorChaveAsync(chave, cancellationToken);
            if (existente is not null)
            {
                if (existente.EnvioId != envio.Id)
                {
                    throw new ConflitoException("Essa chave já foi usada em outra página.");
                }

                return new ResultadoGuardarResposta(PaginaResponse.De(existente), Criada: false);
            }
        }

        // Confere antes de gravar arquivos, para não deixar arquivo solto no disco.
        if (envio.Status == StatusEnvio.Respondida)
        {
            throw new ConflitoException(EnvioPergunta.MensagemJaRespondida);
        }

        ArquivoDeAudio audio = new(resposta.Audio.NomeArquivo, resposta.Audio.TamanhoBytes);
        ArquivoDeFoto? foto = resposta.Foto is null
            ? null
            : new ArquivoDeFoto(resposta.Foto.NomeArquivo, resposta.Foto.TamanhoBytes);

        List<string> arquivosSalvos = [];
        try
        {
            string audioCaminho = await SalvarAsync(resposta.Audio, audio, cancellationToken);
            arquivosSalvos.Add(audioCaminho);

            string? fotoCaminho = null;
            if (resposta.Foto is not null && foto is not null)
            {
                fotoCaminho = await SalvarAsync(resposta.Foto, foto, cancellationToken);
                arquivosSalvos.Add(fotoCaminho);
            }

            PaginaCaderno pagina = envio.Responder(
                resposta.Titulo,
                audioCaminho,
                resposta.DuracaoSegundos,
                fotoCaminho,
                membro.Id,
                resposta.ChaveIdempotencia ?? Guid.NewGuid(),
                relogio.GetUtcNow().UtcDateTime);
            pagina.DefinirTranscricao(resposta.Transcricao);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            return new ResultadoGuardarResposta(PaginaResponse.De(pagina), Criada: true);
        }
        catch
        {
            // Se a página não foi gravada, os arquivos também não devem ficar.
            foreach (string caminho in arquivosSalvos)
            {
                await armazenamento.RemoverAsync(caminho, CancellationToken.None);
            }

            throw;
        }
    }

    public async Task<ListaPaginada<PaginaResponse>> ListarAsync(
        int familiaId,
        FiltroCaderno filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        await acesso.GarantirAcessoAFamiliaAsync(familiaId, cancellationToken);

        if (filtro.Pagina < 1 || filtro.Tamanho < 1 || filtro.Tamanho > TamanhoMaximoDaLista)
        {
            throw new RegraDeNegocioException($"Use página a partir de 1 e tamanho de 1 a {TamanhoMaximoDaLista}.");
        }

        int? categoriaId = null;
        if (!string.IsNullOrWhiteSpace(filtro.Categoria))
        {
            Categoria categoria = await categorias.ObterPorSlugAsync(filtro.Categoria, cancellationToken)
                ?? throw new RecursoNaoEncontradoException("Categoria não encontrada.");
            categoriaId = categoria.Id;
        }

        (IReadOnlyList<PaginaCaderno> itens, int total) = await paginas.ListarDaFamiliaAsync(
            familiaId,
            filtro.IdosoId,
            categoriaId,
            filtro.Pagina,
            filtro.Tamanho,
            cancellationToken);

        return new ListaPaginada<PaginaResponse>(
            itens.Select(PaginaResponse.De).ToList(),
            filtro.Pagina,
            filtro.Tamanho,
            total);
    }

    public async Task<PaginaResponse> ObterAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);
        return PaginaResponse.De(pagina);
    }

    public async Task<ArquivoParaDownload> AbrirAudioAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);
        Stream conteudo = await AbrirArquivoAsync(pagina.AudioCaminho, cancellationToken);
        return new ArquivoParaDownload(conteudo, ArquivoDeAudio.ObterContentType(pagina.AudioCaminho));
    }

    public async Task<ArquivoParaDownload> AbrirFotoAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);
        string caminho = pagina.FotoCaminho
            ?? throw new RecursoNaoEncontradoException("Esta página não tem foto.");

        Stream conteudo = await AbrirArquivoAsync(caminho, cancellationToken);
        return new ArquivoParaDownload(conteudo, ArquivoDeFoto.ObterContentType(caminho));
    }

    public async Task ApagarAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);

        if (!pagina.PodeSerApagadaPor(membro.Id))
        {
            throw new AcessoNegadoException("Só quem guardou a página pode apagá-la.");
        }

        string audioCaminho = pagina.AudioCaminho;
        string? fotoCaminho = pagina.FotoCaminho;

        pagina.Envio!.ReabrirAposExclusao();
        paginas.Remover(pagina);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        // Os arquivos saem depois do banco: se o banco falhar, nada some do disco.
        await armazenamento.RemoverAsync(audioCaminho, cancellationToken);
        if (fotoCaminho is not null)
        {
            await armazenamento.RemoverAsync(fotoCaminho, cancellationToken);
        }
    }

    public async Task<PaginaResponse> DefinirTranscricaoAsync(
        int paginaId,
        TranscricaoRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);

        pagina.DefinirTranscricao(request.Texto);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return PaginaResponse.De(pagina);
    }

    private async Task<string> SalvarAsync(
        ArquivoRecebido recebido,
        ArquivoDeMidia midia,
        CancellationToken cancellationToken)
    {
        await using Stream conteudo = recebido.AbrirLeitura();
        return await armazenamento.SalvarAsync(conteudo, midia, cancellationToken);
    }

    private async Task<PaginaCaderno> ObterComAcessoAsync(int paginaId, CancellationToken cancellationToken)
    {
        PaginaCaderno pagina = await paginas.ObterComEnvioAsync(paginaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Página não encontrada.");

        await acesso.GarantirAcessoAFamiliaAsync(pagina.Envio!.Idoso!.FamiliaId, cancellationToken);
        return pagina;
    }

    private async Task<Stream> AbrirArquivoAsync(string caminho, CancellationToken cancellationToken) =>
        await armazenamento.AbrirAsync(caminho, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Arquivo não encontrado.");
}
