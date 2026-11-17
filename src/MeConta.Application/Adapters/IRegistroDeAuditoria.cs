namespace MeConta.Application.Adapters;

/// <summary>
/// Log de auditoria do filtro de dados pessoais. A implementação da API escreve no log do servidor.
/// O evento só carrega ids, números e trechos já mascarados: nenhum texto da família passa por aqui.
/// </summary>
public interface IRegistroDeAuditoria
{
    void Registrar(EventoDePrivacidade evento);
}

/// <summary>
/// O que o log registra de cada verificação.
/// </summary>
/// <param name="Acao">"Analise" ou "Publicacao".</param>
/// <param name="PaginaId">Página verificada.</param>
/// <param name="MembroId">Quem pediu.</param>
/// <param name="Resultado">Liberada, PrecisaConfirmar, Bloqueada ou Publicada.</param>
/// <param name="IndiceRiscoCidadao">De 0 a 100.</param>
/// <param name="Dados">Dados encontrados, só com rótulo e trecho mascarado. Ex.: "CPF ***.***.***-09".</param>
public sealed record EventoDePrivacidade(
    string Acao,
    int PaginaId,
    int MembroId,
    string Resultado,
    int IndiceRiscoCidadao,
    IReadOnlyList<string> Dados);
