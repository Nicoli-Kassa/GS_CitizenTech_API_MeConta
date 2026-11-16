namespace MeConta.Privacidade.Regras;

/// <summary>
/// Gravidade de um achado e faixa do índice Risco Cidadão.
/// </summary>
public enum NivelDeRisco
{
    /// <summary>Pouco risco: a memória pode ser publicada.</summary>
    Baixo = 1,

    /// <summary>Dado pessoal que expõe a pessoa: a família precisa confirmar antes de publicar.</summary>
    Medio = 2,

    /// <summary>Dado que permite golpe ou fraude: a publicação é bloqueada.</summary>
    Alto = 3,
}

/// <summary>
/// Como o trecho encontrado aparece nos alertas e no painel.
/// </summary>
public enum EstiloDeMascara
{
    /// <summary>Esconde letras e dígitos, deixa os dois últimos dígitos: "***.***.***-09".</summary>
    Digitos = 1,

    /// <summary>Deixa a primeira letra de cada palavra: "r****** p*** p******".</summary>
    Palavras = 2,

    /// <summary>Esconde tudo, menos espaços e pontuação: "****".</summary>
    Total = 3,
}
