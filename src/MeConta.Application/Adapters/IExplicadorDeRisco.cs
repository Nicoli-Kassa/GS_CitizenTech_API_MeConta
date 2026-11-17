namespace MeConta.Application.Adapters;

/// <summary>
/// IA que traduz uma falha técnica em um alerta que qualquer pessoa entende.
/// A implementação da API usa o Gemini (camada Infrastructure).
///
/// Quem chama é sempre o <see cref="Servicos.TradutorDeRisco"/>, que antes confere com o motor de dados
/// pessoais tudo o que vai sair e, depois, tudo o que a IA respondeu.
/// A IA é uma saída insegura: no plano gratuito, o que é enviado pode ser usado pelo provedor.
/// </summary>
public interface IExplicadorDeRisco
{
    /// <summary>Nome mostrado no painel. Ex.: "Gemini".</summary>
    string Nome { get; }

    /// <summary>Falso quando não há chave configurada: aí a tradução fica com as explicações locais.</summary>
    bool Disponivel { get; }

    /// <summary>Pede à IA um alerta para cada falha do pedido.</summary>
    /// <exception cref="Exception">Qualquer falha (rede, limite do plano, resposta fora do formato) vira exceção.</exception>
    Task<TraducaoDaIa> ExplicarAsync(PedidoDeTraducao pedido, CancellationToken cancellationToken);
}

/// <summary>Para quem o alerta é escrito.</summary>
public enum PublicoDoAlerta
{
    /// <summary>A família da pessoa idosa, na hora de levar uma memória ao mapa do bairro.</summary>
    Familia,

    /// <summary>Quem acompanha o painel executivo Risco Cidadão.</summary>
    Gestor,
}

/// <summary>
/// Uma falha técnica para traduzir. Nunca leva o dado em si: só o tipo, o nível e, no máximo,
/// o trecho mascarado ("***.***.***-09") ou o arquivo e a linha do código.
/// </summary>
/// <param name="Id">Identificador da falha no pedido (a IA devolve o mesmo).</param>
/// <param name="Origem">"Dado pessoal" (achado do motor) ou "Código" (achado do Semgrep).</param>
/// <param name="Rotulo">Nome da falha. Ex.: "CPF" ou "meconta-pii-para-log".</param>
/// <param name="Nivel">"Alto" ou "Medio".</param>
/// <param name="DescricaoTecnica">O que a ferramenta disse, em linguagem técnica.</param>
/// <param name="Exemplo">Trecho mascarado, ou arquivo e linhas do código.</param>
/// <param name="Referencia">CWE ou artigo da LGPD, quando houver.</param>
public sealed record FalhaParaTraduzir(
    string Id,
    string Origem,
    string Rotulo,
    string Nivel,
    string DescricaoTecnica,
    string? Exemplo = null,
    string? Referencia = null);

/// <summary>Pedido de tradução: as falhas e, quando é uma memória, o texto já mascarado.</summary>
/// <param name="Publico">Família ou gestor.</param>
/// <param name="Falhas">Falhas a traduzir.</param>
/// <param name="TextoMascarado">
/// Título e transcrição com cada dado pessoal trocado por um aviso ("[CPF removido]").
/// A IA usa para sugerir uma versão da memória que pode ir para o mapa.
/// </param>
public sealed record PedidoDeTraducao(
    PublicoDoAlerta Publico,
    IReadOnlyList<FalhaParaTraduzir> Falhas,
    string? TextoMascarado = null);

/// <summary>Um alerta em linguagem simples.</summary>
/// <param name="Id">Mesmo id da falha do pedido.</param>
/// <param name="Alerta">Uma frase curta que diz o problema.</param>
/// <param name="RiscoReal">O que pode acontecer com a pessoa na vida real.</param>
/// <param name="OQueFazer">Uma ação simples para resolver.</param>
public sealed record AlertaTraduzido(string Id, string Alerta, string RiscoReal, string OQueFazer);

/// <summary>Resposta da IA, antes da conferência pelo motor.</summary>
/// <param name="Alertas">Um alerta por falha.</param>
/// <param name="VersaoSugerida">A memória reescrita sem os dados pessoais (só quando o pedido tem texto).</param>
/// <param name="Modelo">Modelo que respondeu. Ex.: "gemini-3.5-flash".</param>
public sealed record TraducaoDaIa(
    IReadOnlyList<AlertaTraduzido> Alertas,
    string? VersaoSugerida,
    string? Modelo = null);
