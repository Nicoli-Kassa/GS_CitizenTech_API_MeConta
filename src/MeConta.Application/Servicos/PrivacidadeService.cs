using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Resultados;
using MeConta.Privacidade.Taint;

namespace MeConta.Application.Servicos;

/// <summary>
/// Filtro de dados pessoais antes do mapa do bairro (US18) e o próprio mapa (US16, US17).
///
/// Fluxo dos dados (taint): o título e a transcrição da página são texto do usuário
/// (<see cref="TextoDoUsuario"/>). Só o motor de PII os transforma em <see cref="CamposVerificados"/>,
/// e só de lá sai o texto que vai para o mapa, que é uma saída pública.
/// O histórico e o log de auditoria guardam apenas o tipo do dado e o trecho mascarado.
/// </summary>
public sealed class PrivacidadeService(
    IControleDeAcesso acesso,
    IPaginaRepository paginas,
    IMemoriaDoBairroRepository memorias,
    IRegistroDePrivacidadeRepository registros,
    IArmazenamentoDeArquivos armazenamento,
    IMotorPii motor,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IRegistroDeAuditoria auditoria,
    TimeProvider relogio) : IPrivacidadeService
{
    public const string MensagemMemoriaForaDoMapa = "Memória não encontrada no mapa do bairro.";
    public const string MensagemPaginaForaDoMapa = "Esta memória não está no mapa do bairro.";
    public const string MensagemBairroComDadoPessoal = "Use só o nome do bairro, sem rua, número ou outro dado pessoal";

    public async Task<AnalisePrivacidadeResponse> AnalisarPaginaAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);

        CamposVerificados verificados = Verificar(pagina);
        ResultadoDaVerificacao resultado = DecidirAnalise(verificados.Analise);

        await RegistrarAsync(pagina, membro, OrigemDaVerificacao.Analise, resultado, bairro: null, verificados.Analise, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return CriarResposta(pagina, resultado, verificados.Analise);
    }

    public async Task<PublicacaoResponse> PublicarAsync(
        int paginaId,
        PublicarMemoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);
        Idoso idoso = pagina.Envio!.Idoso!;

        // Consentimento e bairro vêm antes do filtro: sem eles não há tentativa de publicação.
        if (!request.IdosoAutorizou)
        {
            throw new RegraDeNegocioException(MemoriaDoBairro.MensagemSemAutorizacao(idoso.Apelido));
        }

        if (string.IsNullOrWhiteSpace(request.Bairro))
        {
            throw new RegraDeNegocioException(MemoriaDoBairro.MensagemBairroVazio);
        }

        // O bairro também é texto do usuário e vai para a saída pública: passa pelo motor.
        TextoVerificado bairroVerificado = motor.Verificar(new TextoDoUsuario(request.Bairro.Trim()));
        if (bairroVerificado.Resultado.TemDadoPessoal || !bairroVerificado.PodePublicar(familiaConfirmou: false))
        {
            throw new RegraDeNegocioException(MensagemBairroComDadoPessoal);
        }

        string bairro = bairroVerificado.ConteudoParaPublicar(familiaConfirmou: false);

        if (await memorias.ObterNoMapaDaPaginaAsync(pagina.Id, cancellationToken) is not null)
        {
            throw new ConflitoException(MemoriaDoBairro.MensagemJaNoMapa);
        }

        CamposVerificados verificados = Verificar(pagina);
        AnaliseDeCampos analise = verificados.Analise;
        bool confirmou = request.ConfirmoDadosPessoais;

        if (!verificados.PodePublicar(confirmou))
        {
            ResultadoDaVerificacao recusa = analise.DeveBloquear
                ? ResultadoDaVerificacao.Bloqueada
                : ResultadoDaVerificacao.PrecisaConfirmar;

            // A tentativa barrada também entra no histórico: é o "vazamento evitado" do painel.
            await RegistrarAsync(pagina, membro, OrigemDaVerificacao.Publicacao, recusa, bairro, analise, cancellationToken);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

            throw new PublicacaoRecusadaException(CriarResposta(pagina, recusa, analise));
        }

        // Só o texto liberado pelo motor chega à saída pública.
        string titulo = verificados.ConteudoParaPublicar(CamposDaPagina.Titulo, confirmou);
        string? transcricao = pagina.Transcricao is null
            ? null
            : verificados.ConteudoParaPublicar(CamposDaPagina.Transcricao, confirmou);

        MemoriaDoBairro memoria = new(
            pagina,
            bairro,
            request.IdosoAutorizou,
            titulo,
            transcricao,
            analise.IndiceRiscoCidadao,
            membro.Id,
            relogio.GetUtcNow().UtcDateTime);
        await memorias.AdicionarAsync(memoria, cancellationToken);

        await RegistrarAsync(pagina, membro, OrigemDaVerificacao.Publicacao, ResultadoDaVerificacao.Publicada, memoria.Bairro, analise, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new PublicacaoResponse(
            MemoriaDoBairroResponse.De(memoria),
            CriarResposta(pagina, ResultadoDaVerificacao.Publicada, analise));
    }

    public async Task TirarDoMapaAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        Membro membro = await acesso.ObterMembroAtualAsync(cancellationToken);
        PaginaCaderno pagina = await ObterComAcessoAsync(paginaId, cancellationToken);

        MemoriaDoBairro memoria = await memorias.ObterNoMapaDaPaginaAsync(pagina.Id, cancellationToken)
            ?? throw new RecursoNaoEncontradoException(MensagemPaginaForaDoMapa);

        memoria.TirarDoMapa(membro.Id, relogio.GetUtcNow().UtcDateTime);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoriaDoBairroResponse>> ListarDoBairroAsync(
        string bairro,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bairro))
        {
            throw new RegraDeNegocioException(MemoriaDoBairro.MensagemBairroVazio);
        }

        IReadOnlyList<MemoriaDoBairro> lista = await memorias.ListarNoMapaDoBairroAsync(bairro.Trim(), cancellationToken);
        return lista.Select(MemoriaDoBairroResponse.De).ToList();
    }

    public async Task<ArquivoParaDownload> AbrirAudioDaMemoriaAsync(int memoriaId, CancellationToken cancellationToken = default)
    {
        MemoriaDoBairro memoria = await memorias.ObterNoMapaComPaginaAsync(memoriaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException(MensagemMemoriaForaDoMapa);

        string caminho = memoria.Pagina!.AudioCaminho;
        Stream conteudo = await armazenamento.AbrirAsync(caminho, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Arquivo não encontrado.");

        return new ArquivoParaDownload(conteudo, ArquivoDeAudio.ObterContentType(caminho));
    }

    /// <summary>Título e transcrição entram no motor como texto do usuário (fonte contaminada).</summary>
    private CamposVerificados Verificar(PaginaCaderno pagina) =>
        motor.VerificarCampos(
        [
            new CampoDoUsuario(CamposDaPagina.Titulo, new TextoDoUsuario(pagina.Titulo)),
            new CampoDoUsuario(CamposDaPagina.Transcricao, new TextoDoUsuario(pagina.Transcricao ?? string.Empty)),
        ]);

    private static ResultadoDaVerificacao DecidirAnalise(AnaliseDeCampos analise) =>
        analise.DeveBloquear ? ResultadoDaVerificacao.Bloqueada
        : analise.ExigeConfirmacao ? ResultadoDaVerificacao.PrecisaConfirmar
        : ResultadoDaVerificacao.Liberada;

    private static AnalisePrivacidadeResponse CriarResposta(
        PaginaCaderno pagina,
        ResultadoDaVerificacao resultado,
        AnaliseDeCampos analise) =>
        AnalisePrivacidadeResponse.De(pagina.Id, resultado.ToString(), analise, pagina.Transcricao is not null);

    private async Task RegistrarAsync(
        PaginaCaderno pagina,
        Membro membro,
        OrigemDaVerificacao origem,
        ResultadoDaVerificacao resultado,
        string? bairro,
        AnaliseDeCampos analise,
        CancellationToken cancellationToken)
    {
        IEnumerable<AchadoRegistrado> achados = analise.Achados.Select(achado => new AchadoRegistrado(
            achado.RegraId,
            achado.Tipo,
            achado.Rotulo,
            achado.Nivel.ToString(),
            achado.Campo,
            achado.TrechoMascarado));

        Idoso idoso = pagina.Envio!.Idoso!;
        RegistroDePrivacidade registro = new(
            pagina.Id,
            idoso.FamiliaId,
            idoso.Id,
            membro.Id,
            origem,
            resultado,
            bairro,
            analise.IndiceRiscoCidadao,
            analise.AnaliseCompleta,
            analise.VersaoDasRegras,
            achados,
            relogio.GetUtcNow().UtcDateTime);

        await registros.AdicionarAsync(registro, cancellationToken);

        // Log de auditoria no servidor: ids, resultado e dados só mascarados ("CPF ***.***.***-09").
        auditoria.Registrar(new EventoDePrivacidade(
            origem.ToString(),
            pagina.Id,
            membro.Id,
            resultado.ToString(),
            analise.IndiceRiscoCidadao,
            analise.Achados.Select(achado => $"{achado.Rotulo} {achado.TrechoMascarado}").ToList()));
    }

    private async Task<PaginaCaderno> ObterComAcessoAsync(int paginaId, CancellationToken cancellationToken)
    {
        PaginaCaderno pagina = await paginas.ObterComEnvioAsync(paginaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Página não encontrada.");

        await acesso.GarantirAcessoAFamiliaAsync(pagina.Envio!.Idoso!.FamiliaId, cancellationToken);
        return pagina;
    }
}
