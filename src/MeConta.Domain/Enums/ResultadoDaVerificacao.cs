namespace MeConta.Domain.Enums;

/// <summary>
/// O que aconteceu quando uma memória passou pelo filtro de dados pessoais (US18).
/// </summary>
public enum ResultadoDaVerificacao
{
    /// <summary>Nenhum dado pessoal: pode ir para o mapa do bairro.</summary>
    Liberada = 1,

    /// <summary>Dado pessoal de risco médio: a família precisa confirmar antes de publicar.</summary>
    PrecisaConfirmar = 2,

    /// <summary>Dado de risco alto (CPF, senha, cartão...) ou análise incompleta: não pode publicar.</summary>
    Bloqueada = 3,

    /// <summary>A memória foi para o mapa do bairro.</summary>
    Publicada = 4,
}

/// <summary>
/// Por que a memória passou pelo filtro.
/// </summary>
public enum OrigemDaVerificacao
{
    /// <summary>A família pediu só a análise, para ver o risco antes de publicar.</summary>
    Analise = 1,

    /// <summary>A família tentou publicar no mapa do bairro.</summary>
    Publicacao = 2,
}
