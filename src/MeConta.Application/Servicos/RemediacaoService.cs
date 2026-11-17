using MeConta.Application.Abstracoes;
using MeConta.Application.Adapters;
using MeConta.Application.Auditoria;
using MeConta.Application.Dtos;
using MeConta.Application.Excecoes;
using MeConta.Domain.Entidades;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;
using MeConta.Privacidade.Taint;

namespace MeConta.Application.Servicos;

/// <summary>
/// IA na remediação: traduz o que o motor de dados pessoais e o Semgrep encontraram
/// em alertas de risco real, para a família e para o painel executivo.
///
/// Só sai daqui o que o motor já mascarou: tipo do dado, nível, trecho com asteriscos,
/// texto com "[CPF removido]" no lugar do número, ou arquivo e linha do código.
/// </summary>
public sealed class RemediacaoService(
    IControleDeAcesso acesso,
    IPaginaRepository paginas,
    IMotorPii motor,
    ITradutorDeRisco tradutor,
    IRiscoCidadaoService riscoCidadao,
    IRelatoriosDeAuditoria relatorios,
    TimeProvider relogio) : IRemediacaoService
{
    public const string MensagemSemRelatorio =
        "Relatório da auditoria não encontrado. Rode o Semgrep como em docs/cyber/taint-analysis.md.";

    public const string SituacaoCorrigida = "Corrigida";
    public const string SituacaoAberta = "Aberta";

    private const int TrechosPorAlerta = 3;

    public async Task<ExplicacaoDeRiscoResponse> ExplicarPaginaAsync(int paginaId, CancellationToken cancellationToken = default)
    {
        PaginaCaderno pagina = await paginas.ObterComEnvioAsync(paginaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Página não encontrada.");
        await acesso.GarantirAcessoAFamiliaAsync(pagina.Envio!.Idoso!.FamiliaId, cancellationToken);

        // Título e transcrição entram no motor como texto do usuário; daqui para frente, só a análise.
        CamposVerificados verificados = motor.VerificarCampos(
        [
            new CampoDoUsuario(CamposDaPagina.Titulo, new TextoDoUsuario(pagina.Titulo)),
            new CampoDoUsuario(CamposDaPagina.Transcricao, new TextoDoUsuario(pagina.Transcricao ?? string.Empty)),
        ]);
        AnaliseDeCampos analise = verificados.Analise;
        string resultado = analise.DeveBloquear ? "Bloqueada" : analise.ExigeConfirmacao ? "PrecisaConfirmar" : "Liberada";

        if (!analise.AnaliseCompleta || analise.Achados.Count == 0)
        {
            return new ExplicacaoDeRiscoResponse(
                pagina.Id,
                resultado,
                analise.IndiceRiscoCidadao,
                analise.Faixa.ToString(),
                Resumo(analise),
                [],
                null,
                TradutorDeRisco.FonteMotor,
                null);
        }

        List<IGrouping<string, AchadoPii>> porTipo = analise.Achados
            .GroupBy(achado => achado.Tipo)
            .OrderByDescending(grupo => grupo.Any(achado => achado.Nivel == NivelDeRisco.Alto))
            .ThenBy(grupo => grupo.Min(achado => achado.Inicio))
            .ToList();

        List<FalhaParaTraduzir> falhas = porTipo.Select(FalhaDoTipo).ToList();
        falhas.AddRange(analise.Combinacoes.Select((combinacao, indice) => new FalhaParaTraduzir(
            $"{ExplicacoesLocais.TipoCombinacao}-{indice + 1}",
            ExplicacoesLocais.OrigemDadoPessoal,
            "Combinação perigosa",
            "Alto",
            combinacao.Explicacao,
            Referencia: $"+{combinacao.Bonus} pontos no índice Risco Cidadão")));

        // A IA reescreve a transcrição (a parte que a família edita); sem transcrição, o título.
        string textoMascarado = pagina.Transcricao is null
            ? analise.TextoSeguroDe(CamposDaPagina.Titulo)
            : analise.TextoSeguroDe(CamposDaPagina.Transcricao);

        ResultadoDaTraducao traducao = await tradutor.TraduzirAsync(
            new PedidoDeTraducao(PublicoDoAlerta.Familia, falhas, textoMascarado),
            cancellationToken);

        List<AlertaDeRiscoResponse> alertas = [];
        for (int i = 0; i < falhas.Count; i++)
        {
            FalhaParaTraduzir falha = falhas[i];
            AlertaTraduzido texto = traducao.Alertas[i];
            IReadOnlyList<string> trechos = i < porTipo.Count
                ? porTipo[i].Select(achado => achado.TrechoMascarado).Distinct().Take(TrechosPorAlerta).ToList()
                : [];
            alertas.Add(new AlertaDeRiscoResponse(falha.Rotulo, falha.Nivel, trechos, texto.Alerta, texto.RiscoReal, texto.OQueFazer));
        }

        return new ExplicacaoDeRiscoResponse(
            pagina.Id,
            resultado,
            analise.IndiceRiscoCidadao,
            analise.Faixa.ToString(),
            Resumo(analise),
            alertas,
            traducao.VersaoSugerida,
            traducao.Fonte,
            traducao.Observacao);
    }

    public async Task<AlertasDoPainelResponse> AlertasDoPainelAsync(
        int dias = RiscoCidadaoService.PeriodoPadraoDias,
        CancellationToken cancellationToken = default)
    {
        RiscoCidadaoResponse painel = await riscoCidadao.ObterAsync(dias, cancellationToken);

        // O pedido não leva quantidades: assim a tradução de cada tipo é feita uma vez e reaproveitada.
        List<FalhaParaTraduzir> falhas = painel.PorTipo
            .Select((tipo, indice) => new FalhaParaTraduzir(
                $"{tipo.Tipo}-{indice + 1}",
                ExplicacoesLocais.OrigemDadoPessoal,
                tipo.Rotulo,
                tipo.Nivel,
                ExplicacoesLocais.RiscoDoTipo(tipo.Tipo)))
            .ToList();

        ResultadoDaTraducao traducao = await tradutor.TraduzirAsync(
            new PedidoDeTraducao(PublicoDoAlerta.Gestor, falhas),
            cancellationToken);

        List<AlertaDoPainelResponse> alertas = painel.PorTipo
            .Select((tipo, indice) => new AlertaDoPainelResponse(
                tipo.Tipo,
                tipo.Rotulo,
                tipo.Nivel,
                tipo.Quantidade,
                traducao.Alertas[indice].Alerta,
                traducao.Alertas[indice].RiscoReal,
                traducao.Alertas[indice].OQueFazer))
            .ToList();

        return new AlertasDoPainelResponse(
            relogio.GetUtcNow().UtcDateTime,
            painel.PeriodoDias,
            alertas,
            traducao.Fonte,
            traducao.Observacao,
            tradutor.Estatisticas());
    }

    public async Task<AuditoriaDeCodigoResponse> AuditoriaAsync(CancellationToken cancellationToken = default)
    {
        RelatoriosSarif arquivos = await relatorios.CarregarAsync(cancellationToken);
        if (arquivos.Antes is null && arquivos.Depois is null)
        {
            throw new RecursoNaoEncontradoException(MensagemSemRelatorio);
        }

        RelatorioDeCodigo? antes = arquivos.Antes is null ? null : LeitorDeSarif.Ler(arquivos.Antes);
        RelatorioDeCodigo? depois = arquivos.Depois is null ? null : LeitorDeSarif.Ler(arquivos.Depois);
        IReadOnlyList<AchadoDeCodigo> achadosAntes = antes?.Achados ?? [];
        IReadOnlyList<AchadoDeCodigo> achadosDepois = depois?.Achados ?? [];

        // Uma falha por regra e arquivo: os quatro pontos do mesmo serviço viram um alerta só.
        HashSet<(string Regra, string Arquivo)> abertas = achadosDepois
            .Select(achado => (achado.Regra, achado.Arquivo))
            .ToHashSet();
        List<IGrouping<(string Regra, string Arquivo), AchadoDeCodigo>> grupos = achadosAntes
            .Concat(achadosDepois.Where(achado => !achadosAntes.Any(a => a.Regra == achado.Regra && a.Arquivo == achado.Arquivo)))
            .GroupBy(achado => (achado.Regra, achado.Arquivo))
            .ToList();

        List<FalhaParaTraduzir> falhas = grupos
            .Select((grupo, indice) =>
            {
                AchadoDeCodigo primeiro = grupo.First();
                string situacao = abertas.Contains(grupo.Key)
                    ? "Situação: continua aberta."
                    : "Situação: corrigida, não aparece mais na auditoria seguinte.";
                return new FalhaParaTraduzir(
                    $"Codigo-{indice + 1}",
                    ExplicacoesLocais.OrigemCodigo,
                    primeiro.Regra,
                    "Alto",
                    $"{primeiro.Descricao} {situacao}",
                    $"{grupo.Key.Arquivo}, linhas {string.Join(", ", grupo.Select(achado => achado.Linha).Distinct().Order())}",
                    primeiro.Cwe);
            })
            .ToList();

        ResultadoDaTraducao traducao = await tradutor.TraduzirAsync(
            new PedidoDeTraducao(PublicoDoAlerta.Gestor, falhas),
            cancellationToken);

        List<FalhaDeCodigoResponse> respostas = grupos
            .Select((grupo, indice) => new FalhaDeCodigoResponse(
                grupo.Key.Regra,
                grupo.First().Cwe,
                grupo.Key.Arquivo,
                grupo.Select(achado => achado.Linha).Distinct().Order().ToList(),
                abertas.Contains(grupo.Key) ? SituacaoAberta : SituacaoCorrigida,
                grupo.First().Descricao,
                traducao.Alertas[indice].Alerta,
                traducao.Alertas[indice].RiscoReal,
                traducao.Alertas[indice].OQueFazer))
            .ToList();

        return new AuditoriaDeCodigoResponse(
            (depois ?? antes)!.Ferramenta,
            achadosAntes.Count,
            achadosDepois.Count,
            respostas.Count(falha => falha.Situacao == SituacaoCorrigida),
            respostas.Count(falha => falha.Situacao == SituacaoAberta),
            respostas,
            traducao.Fonte,
            traducao.Observacao,
            tradutor.Estatisticas());
    }

    private static FalhaParaTraduzir FalhaDoTipo(IGrouping<string, AchadoPii> grupo)
    {
        AchadoPii primeiro = grupo.First();
        bool alto = grupo.Any(achado => achado.Nivel == NivelDeRisco.Alto);
        string regras = string.Join(", ", grupo.Select(achado => achado.RegraId).Distinct());

        return new FalhaParaTraduzir(
            grupo.Key,
            ExplicacoesLocais.OrigemDadoPessoal,
            primeiro.Rotulo,
            alto ? nameof(NivelDeRisco.Alto) : nameof(NivelDeRisco.Medio),
            $"Regra {regras}: {primeiro.Explicacao}",
            string.Join(", ", grupo.Select(achado => achado.TrechoMascarado).Distinct().Take(TrechosPorAlerta)),
            grupo.Key == "Saude" ? "LGPD, art. 11 (dado pessoal sensível)" : null);
    }

    private static string Resumo(AnaliseDeCampos analise)
    {
        if (!analise.AnaliseCompleta)
        {
            return "O texto é grande demais para conferir inteiro. Por segurança, esta memória não vai para o mapa do bairro.";
        }

        int altos = analise.Achados.Count(achado => achado.Nivel == NivelDeRisco.Alto);
        if (altos > 0)
        {
            return altos == 1
                ? "Esta memória não pode ir para o mapa do bairro: ela tem um dado que pode levar a golpes."
                : $"Esta memória não pode ir para o mapa do bairro: ela tem {altos} dados que podem levar a golpes.";
        }

        return analise.ExigeConfirmacao
            ? "Esta memória tem dado pessoal. Ela só vai para o mapa do bairro se a família confirmar."
            : "Nenhum dado pessoal encontrado: esta memória pode ir para o mapa do bairro.";
    }
}
