namespace MeConta.Application.Dtos;

/// <summary>Um alerta em linguagem simples, já conferido pelo motor de dados pessoais.</summary>
/// <param name="Rotulo">O que foi encontrado. Ex.: "CPF".</param>
/// <param name="Nivel">"Alto" bloqueia; "Medio" pede confirmação.</param>
/// <param name="Trechos">Onde apareceu, mascarado. Ex.: "***.***.***-09".</param>
/// <param name="Alerta">Uma frase curta que diz o problema.</param>
/// <param name="RiscoReal">O que pode acontecer com a pessoa idosa na vida real.</param>
/// <param name="OQueFazer">Uma ação simples para resolver.</param>
public sealed record AlertaDeRiscoResponse(
    string Rotulo,
    string Nivel,
    IReadOnlyList<string> Trechos,
    string Alerta,
    string RiscoReal,
    string OQueFazer);

/// <summary>
/// Explicação, para a família, dos dados pessoais de uma página do caderno (missão 03).
/// </summary>
/// <param name="PaginaId">Página explicada.</param>
/// <param name="Resultado">Liberada, PrecisaConfirmar ou Bloqueada.</param>
/// <param name="IndiceRiscoCidadao">De 0 a 100.</param>
/// <param name="Faixa">Baixo, Medio ou Alto.</param>
/// <param name="Resumo">Uma frase que resume a situação da memória.</param>
/// <param name="Alertas">Um alerta por tipo de dado encontrado (e um por combinação perigosa).</param>
/// <param name="VersaoSugerida">A memória reescrita sem os dados pessoais, pronta para revisar e salvar.</param>
/// <param name="Fonte">Quem escreveu os alertas: "Gemini (modelo)", "Local" (texto da equipe) ou "Motor" (nada a explicar).</param>
/// <param name="Observacao">Por que a reserva local foi usada, quando foi.</param>
public sealed record ExplicacaoDeRiscoResponse(
    int PaginaId,
    string Resultado,
    int IndiceRiscoCidadao,
    string Faixa,
    string Resumo,
    IReadOnlyList<AlertaDeRiscoResponse> Alertas,
    string? VersaoSugerida,
    string Fonte,
    string? Observacao);

/// <summary>Alerta do painel executivo para um tipo de dado encontrado no período.</summary>
/// <param name="Tipo">Tipo do dado. Ex.: "Cpf".</param>
/// <param name="Rotulo">Nome do dado. Ex.: "CPF".</param>
/// <param name="Nivel">"Alto" ou "Medio".</param>
/// <param name="Quantidade">Vezes que apareceu no período.</param>
/// <param name="Alerta">Uma frase curta que diz o problema.</param>
/// <param name="RiscoReal">O que pode acontecer com a pessoa idosa.</param>
/// <param name="OQueFazer">O que fazer.</param>
public sealed record AlertaDoPainelResponse(
    string Tipo,
    string Rotulo,
    string Nivel,
    int Quantidade,
    string Alerta,
    string RiscoReal,
    string OQueFazer);

/// <summary>Alertas do painel Risco Cidadão, traduzidos pela IA.</summary>
public sealed record AlertasDoPainelResponse(
    DateTime GeradoEm,
    int PeriodoDias,
    IReadOnlyList<AlertaDoPainelResponse> Alertas,
    string Fonte,
    string? Observacao,
    EstatisticasDaIaResponse Ia);

/// <summary>Uma falha do código encontrada pelo Semgrep, com a tradução para o cidadão.</summary>
/// <param name="Regra">Regra do Semgrep. Ex.: "meconta-pii-para-log".</param>
/// <param name="Cwe">Classificação da falha. Ex.: "CWE-532: Insertion of Sensitive Information into Log File".</param>
/// <param name="Arquivo">Arquivo onde estava.</param>
/// <param name="Linhas">Linhas do arquivo.</param>
/// <param name="Situacao">"Corrigida" (não aparece mais na auditoria de depois) ou "Aberta".</param>
/// <param name="DescricaoTecnica">O que o Semgrep disse.</param>
/// <param name="Alerta">A falha em linguagem simples.</param>
/// <param name="RiscoReal">O que podia acontecer com a pessoa idosa.</param>
/// <param name="OQueFazer">O que foi feito, ou o que fazer.</param>
public sealed record FalhaDeCodigoResponse(
    string Regra,
    string? Cwe,
    string Arquivo,
    IReadOnlyList<int> Linhas,
    string Situacao,
    string DescricaoTecnica,
    string Alerta,
    string RiscoReal,
    string OQueFazer);

/// <summary>
/// Auditoria do código (taint analysis com Semgrep) traduzida para o cidadão.
/// </summary>
/// <param name="Ferramenta">Ex.: "Semgrep OSS 1.185.0".</param>
/// <param name="AchadosAntes">Achados na auditoria antes das correções.</param>
/// <param name="AchadosDepois">Achados na auditoria depois das correções.</param>
/// <param name="Corrigidas">Falhas que não aparecem mais.</param>
/// <param name="Abertas">Falhas que continuam.</param>
/// <param name="Falhas">Cada falha, agrupada por regra e arquivo.</param>
/// <param name="Fonte">Quem escreveu os alertas.</param>
/// <param name="Observacao">Por que a reserva local foi usada, quando foi.</param>
/// <param name="Ia">Como a IA está se saindo.</param>
public sealed record AuditoriaDeCodigoResponse(
    string Ferramenta,
    int AchadosAntes,
    int AchadosDepois,
    int Corrigidas,
    int Abertas,
    IReadOnlyList<FalhaDeCodigoResponse> Falhas,
    string Fonte,
    string? Observacao,
    EstatisticasDaIaResponse Ia);

/// <summary>
/// Como a IA está se saindo desde que a API subiu. Mostra que nada passa sem a conferência do motor.
/// </summary>
/// <param name="Nome">"Gemini".</param>
/// <param name="Disponivel">Tem chave configurada.</param>
/// <param name="UltimoModelo">Último modelo que respondeu.</param>
/// <param name="EmPausa">A IA falhou há menos de um minuto e está descansando.</param>
/// <param name="Chamadas">Pedidos feitos à IA.</param>
/// <param name="RespostasAproveitadas">Respostas que passaram, inteiras ou em parte, na conferência do motor.</param>
/// <param name="FrasesDescartadas">Frases da IA trocadas pelo texto local porque traziam dado pessoal ou vieram vazias.</param>
/// <param name="EnviosBarrados">Pedidos que nem saíram, porque ainda tinham dado pessoal.</param>
/// <param name="UsosDaReserva">Vezes que valeram só as explicações locais.</param>
/// <param name="RespostasDoCache">Vezes que a resposta já estava guardada e a IA nem foi chamada.</param>
public sealed record EstatisticasDaIaResponse(
    string Nome,
    bool Disponivel,
    string? UltimoModelo,
    bool EmPausa,
    int Chamadas,
    int RespostasAproveitadas,
    int FrasesDescartadas,
    int EnviosBarrados,
    int UsosDaReserva,
    int RespostasDoCache);
