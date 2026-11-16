using MeConta.Domain.Entidades;
using MeConta.Privacidade.Resultados;

namespace MeConta.Application.Dtos;

/// <summary>
/// Um dado pessoal encontrado, sem o valor: só o trecho mascarado e a explicação do risco.
/// </summary>
/// <param name="RegraId">Regra do motor que encontrou o dado (ex.: "PII-CPF-001").</param>
/// <param name="Tipo">Tipo do dado (ex.: "Cpf").</param>
/// <param name="Rotulo">Nome do dado em linguagem simples (ex.: "CPF").</param>
/// <param name="Nivel">"Alto" bloqueia; "Medio" pede confirmação.</param>
/// <param name="Campo">"Titulo" ou "Transcricao".</param>
/// <param name="TrechoMascarado">Ex.: "***.***.***-09".</param>
/// <param name="Explicacao">Por que esse dado é arriscado, em linguagem simples.</param>
/// <param name="Confianca">De 0 a 1: quanto o motor tem certeza de que é o dado.</param>
public sealed record AchadoResponse(
    string RegraId,
    string Tipo,
    string Rotulo,
    string Nivel,
    string? Campo,
    string TrechoMascarado,
    string Explicacao,
    double Confianca)
{
    public static AchadoResponse De(AchadoPii achado) =>
        new(
            achado.RegraId,
            achado.Tipo,
            achado.Rotulo,
            achado.Nivel.ToString(),
            achado.Campo,
            achado.TrechoMascarado,
            achado.Explicacao,
            Math.Round(achado.Confianca, 2));
}

/// <summary>Combinação de dados que aumentou o risco (ex.: endereço + "moro sozinha").</summary>
public sealed record CombinacaoResponse(string Id, int Bonus, string Explicacao);

/// <summary>Título e transcrição com cada dado pessoal trocado por um aviso ("[CPF removido]").</summary>
public sealed record VersaoSeguraResponse(string Titulo, string? Transcricao);

/// <summary>
/// Resultado do filtro de dados pessoais de uma página (US18).
/// </summary>
/// <param name="PaginaId">Página analisada.</param>
/// <param name="Resultado">Liberada, PrecisaConfirmar ou Bloqueada.</param>
/// <param name="IndiceRiscoCidadao">De 0 (nenhum dado pessoal) a 100.</param>
/// <param name="Faixa">Baixo (0 a 29), Medio (30 a 69) ou Alto (70 a 100).</param>
/// <param name="Bloqueada">Tem dado de risco alto: não pode ir para o mapa.</param>
/// <param name="ExigeConfirmacao">Tem dado de risco médio: a família precisa confirmar.</param>
/// <param name="AnaliseCompleta">Falso quando o texto não pôde ser analisado inteiro (aí a publicação é bloqueada).</param>
/// <param name="Achados">Dados encontrados, mascarados.</param>
/// <param name="Combinacoes">Combinações perigosas encontradas.</param>
/// <param name="VersaoSegura">Título e transcrição sem os dados pessoais.</param>
/// <param name="VersaoDasRegras">Versão do arquivo de regras usado.</param>
public sealed record AnalisePrivacidadeResponse(
    int PaginaId,
    string Resultado,
    int IndiceRiscoCidadao,
    string Faixa,
    bool Bloqueada,
    bool ExigeConfirmacao,
    bool AnaliseCompleta,
    IReadOnlyList<AchadoResponse> Achados,
    IReadOnlyList<CombinacaoResponse> Combinacoes,
    VersaoSeguraResponse VersaoSegura,
    string VersaoDasRegras)
{
    public static AnalisePrivacidadeResponse De(int paginaId, string resultado, AnaliseDeCampos analise, bool temTranscricao) =>
        new(
            paginaId,
            resultado,
            analise.IndiceRiscoCidadao,
            analise.Faixa.ToString(),
            analise.DeveBloquear,
            analise.ExigeConfirmacao,
            analise.AnaliseCompleta,
            analise.Achados.Select(AchadoResponse.De).ToList(),
            analise.Combinacoes.Select(combinacao => new CombinacaoResponse(combinacao.Id, combinacao.Bonus, combinacao.Explicacao)).ToList(),
            new VersaoSeguraResponse(
                analise.TextoSeguroDe(CamposDaPagina.Titulo),
                temTranscricao ? analise.TextoSeguroDe(CamposDaPagina.Transcricao) : null),
            analise.VersaoDasRegras);
}

/// <summary>Nomes dos campos da página que passam pelo filtro.</summary>
public static class CamposDaPagina
{
    public const string Titulo = "Titulo";
    public const string Transcricao = "Transcricao";
}

/// <summary>
/// Pedido para levar uma página do caderno ao mapa do bairro (US16).
/// </summary>
/// <param name="Bairro">Bairro do mapa (ex.: "Pirituba"). Nunca o endereço.</param>
/// <param name="IdosoAutorizou">Caixa "A Vó Lourdes autorizou". Sem ela, nada é publicado.</param>
/// <param name="ConfirmoDadosPessoais">
/// A família viu o alerta de dado pessoal de risco médio (saúde, telefone, endereço...) e quer publicar mesmo assim.
/// Não libera dados de risco alto (CPF, senha, cartão...): esses sempre bloqueiam.
/// </param>
public sealed record PublicarMemoriaRequest(string? Bairro, bool IdosoAutorizou, bool ConfirmoDadosPessoais = false);

/// <summary>
/// Uma memória do mapa do bairro, como qualquer morador vê (US17): título, apelido e áudio.
/// Nunca traz telefone, nome completo nem nome da família.
/// </summary>
public sealed record MemoriaDoBairroResponse(
    int Id,
    string Bairro,
    string Titulo,
    string Apelido,
    string? Transcricao,
    int CategoriaId,
    string AudioUrl,
    DateTime PublicadaEm)
{
    public static MemoriaDoBairroResponse De(MemoriaDoBairro memoria) =>
        new(
            memoria.Id,
            memoria.Bairro,
            memoria.Titulo,
            memoria.Apelido,
            memoria.Transcricao,
            memoria.CategoriaId,
            $"/api/v1/memorias/{memoria.Id}/audio",
            memoria.PublicadaEm);
}

/// <summary>Memória publicada e a análise que a liberou.</summary>
public sealed record PublicacaoResponse(MemoriaDoBairroResponse Memoria, AnalisePrivacidadeResponse Analise);

/// <summary>
/// Painel executivo "Risco Cidadão": como o filtro de dados pessoais está protegendo as pessoas idosas.
/// Só números agregados e trechos mascarados; nenhum título, nome ou família.
/// </summary>
/// <param name="GeradoEm">Hora do cálculo (UTC).</param>
/// <param name="PeriodoDias">Quantos dias para trás entram no cálculo.</param>
/// <param name="IndiceRiscoCidadao">Média do índice das memórias verificadas no período (0 a 100).</param>
/// <param name="Faixa">Faixa da média: Baixo, Medio ou Alto.</param>
/// <param name="Totais">Contagens do período.</param>
/// <param name="PorTipo">Dados pessoais encontrados, por tipo.</param>
/// <param name="PorDia">Evolução diária (horário de Brasília).</param>
/// <param name="PorBairro">Tentativas de publicação por bairro.</param>
/// <param name="UltimosAlertas">Dados encontrados mais recentes, mascarados.</param>
/// <param name="VersaoDasRegras">Versão do arquivo de regras em uso.</param>
public sealed record RiscoCidadaoResponse(
    DateTime GeradoEm,
    int PeriodoDias,
    int IndiceRiscoCidadao,
    string Faixa,
    TotaisRiscoCidadao Totais,
    IReadOnlyList<RiscoPorTipo> PorTipo,
    IReadOnlyList<RiscoPorDia> PorDia,
    IReadOnlyList<RiscoPorBairro> PorBairro,
    IReadOnlyList<AlertaRecente> UltimosAlertas,
    string VersaoDasRegras);

/// <param name="Verificacoes">Vezes que uma memória passou pelo filtro (análises e tentativas de publicação).</param>
/// <param name="Publicadas">Memórias que foram para o mapa.</param>
/// <param name="Bloqueadas">Tentativas barradas por dado de risco alto.</param>
/// <param name="PrecisaramConfirmar">Verificações com dado de risco médio.</param>
/// <param name="SemRisco">Verificações sem nenhum dado pessoal.</param>
/// <param name="DadosEncontrados">Dados pessoais encontrados no período.</param>
/// <param name="VazamentosEvitados">Dados de risco alto que estavam numa tentativa de publicação e não foram para o mapa.</param>
public sealed record TotaisRiscoCidadao(
    int Verificacoes,
    int Publicadas,
    int Bloqueadas,
    int PrecisaramConfirmar,
    int SemRisco,
    int DadosEncontrados,
    int VazamentosEvitados);

public sealed record RiscoPorTipo(string Tipo, string Rotulo, string Nivel, int Quantidade);

public sealed record RiscoPorDia(DateOnly Dia, int Verificacoes, int Bloqueadas, int Publicadas, int IndiceMedio);

public sealed record RiscoPorBairro(string Bairro, int Tentativas, int Publicadas, int Bloqueadas, int IndiceMedio);

public sealed record AlertaRecente(
    DateTime Quando,
    string Rotulo,
    string Nivel,
    string? Campo,
    string TrechoMascarado,
    string Resultado,
    string? Bairro);
