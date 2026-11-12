namespace MeConta.Application.Dtos;

/// <summary>
/// Indicador de impacto da Ficha da CitizenTech (US12): conversas de verdade por semana.
/// </summary>
/// <param name="SemanaInicio">Segunda-feira da semana.</param>
/// <param name="SemanaFim">Domingo da semana.</param>
/// <param name="IdososCadastrados">Idosos cadastrados até o fim da semana.</param>
/// <param name="IdososQueResponderam">Idosos com pelo menos uma página guardada na semana.</param>
/// <param name="PercentualConversasDeVerdade">Responderam ÷ cadastrados × 100, ou nulo quando não há idosos.</param>
/// <param name="MinutosDeAudio">Soma da duração das páginas guardadas na semana.</param>
/// <param name="IdososQueRespondemSozinhos">Idosos que concluíram o roteiro "Ensine sua avó".</param>
/// <param name="Mensagem">"Ainda não há dados" quando não há idosos cadastrados.</param>
public sealed record IndicadorConversasResponse(
    DateOnly SemanaInicio,
    DateOnly SemanaFim,
    int IdososCadastrados,
    int IdososQueResponderam,
    double? PercentualConversasDeVerdade,
    double MinutosDeAudio,
    int IdososQueRespondemSozinhos,
    string? Mensagem);
