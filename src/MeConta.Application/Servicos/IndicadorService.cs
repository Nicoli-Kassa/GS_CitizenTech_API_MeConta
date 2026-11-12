using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Domain.Comum;
using MeConta.Domain.ValueObjects;

namespace MeConta.Application.Servicos;

/// <summary>
/// Indicador "Conversas de verdade" (US12): % de idosos que responderam
/// pelo menos uma pergunta na semana, de segunda a domingo.
/// </summary>
public sealed class IndicadorService(IIndicadorConsulta consulta, TimeProvider relogio) : IIndicadorService
{
    public const string MensagemSemDados = "Ainda não há dados";

    public async Task<IndicadorConversasResponse> ObterConversasDeVerdadeAsync(
        DateOnly? dataNaSemana,
        CancellationToken cancellationToken = default)
    {
        TimeZoneInfo fuso = FusoHorario.Brasilia;
        Semana semana = dataNaSemana is DateOnly data
            ? Semana.Contendo(data)
            : Semana.Contendo(relogio.GetUtcNow().UtcDateTime, fuso);

        DadosIndicador dados = await consulta.ObterAsync(
            semana.InicioUtc(fuso),
            semana.FimUtcExclusivo(fuso),
            cancellationToken);

        // Sem idosos cadastrados não há o que dividir: o painel mostra "Ainda não há dados".
        bool semDados = dados.IdososCadastrados == 0;
        double? percentual = semDados
            ? null
            : Math.Round(100.0 * dados.IdososQueResponderam / dados.IdososCadastrados, 1);

        return new IndicadorConversasResponse(
            semana.Inicio,
            semana.Fim,
            dados.IdososCadastrados,
            dados.IdososQueResponderam,
            percentual,
            Math.Round(dados.SegundosDeAudio / 60.0, 1),
            dados.IdososQueRespondemSozinhos,
            semDados ? MensagemSemDados : null);
    }
}
