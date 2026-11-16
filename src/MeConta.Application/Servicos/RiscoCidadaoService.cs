using MeConta.Application.Abstracoes;
using MeConta.Application.Dtos;
using MeConta.Domain.Comum;
using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Resultados;

namespace MeConta.Application.Servicos;

/// <summary>
/// Painel executivo "Risco Cidadão": junta o histórico do filtro de dados pessoais
/// em números que mostram quanto a pessoa idosa está protegida.
/// Trabalha só com o que o histórico guarda (tipo do dado, nível, trecho mascarado),
/// então nenhum dado pessoal sai daqui.
/// </summary>
public sealed class RiscoCidadaoService(
    IRegistroDePrivacidadeRepository registros,
    IMotorPii motor,
    TimeProvider relogio) : IRiscoCidadaoService
{
    public const int PeriodoPadraoDias = 14;
    public const int PeriodoMaximoDias = 90;
    public const int QuantidadeDeAlertas = 10;
    private const string NivelAlto = "Alto";

    public async Task<RiscoCidadaoResponse> ObterAsync(int dias = PeriodoPadraoDias, CancellationToken cancellationToken = default)
    {
        if (dias is < 1 or > PeriodoMaximoDias)
        {
            throw new RegraDeNegocioException($"Escolha um período de 1 a {PeriodoMaximoDias} dias.");
        }

        TimeZoneInfo fuso = FusoHorario.Brasilia;
        DateTime agora = relogio.GetUtcNow().UtcDateTime;
        DateOnly hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(agora, fuso));
        DateOnly primeiroDia = hoje.AddDays(-(dias - 1));
        DateTime desdeUtc = TimeZoneInfo.ConvertTimeToUtc(primeiroDia.ToDateTime(TimeOnly.MinValue), fuso);

        IReadOnlyList<RegistroDePrivacidade> lista = await registros.ListarDesdeAsync(desdeUtc, cancellationToken);

        int indice = MediaDoIndice(lista);
        return new RiscoCidadaoResponse(
            agora,
            dias,
            indice,
            CalculadoraDeRiscoCidadao.FaixaDe(indice).ToString(),
            CalcularTotais(lista),
            AgruparPorTipo(lista),
            AgruparPorDia(lista, primeiroDia, hoje, fuso),
            AgruparPorBairro(lista),
            UltimosAlertas(lista),
            motor.VersaoDasRegras);
    }

    private static TotaisRiscoCidadao CalcularTotais(IReadOnlyList<RegistroDePrivacidade> lista)
    {
        // Dados de risco alto que estavam numa tentativa de publicação e ficaram fora do mapa.
        int vazamentosEvitados = lista
            .Where(registro => registro.Origem == OrigemDaVerificacao.Publicacao
                && registro.Resultado == ResultadoDaVerificacao.Bloqueada)
            .SelectMany(registro => registro.Achados)
            .Count(achado => achado.Nivel == NivelAlto);

        return new TotaisRiscoCidadao(
            lista.Count,
            lista.Count(registro => registro.Resultado == ResultadoDaVerificacao.Publicada),
            lista.Count(registro => registro.Resultado == ResultadoDaVerificacao.Bloqueada),
            lista.Count(registro => registro.Resultado == ResultadoDaVerificacao.PrecisaConfirmar),
            lista.Count(registro => registro.Achados.Count == 0),
            lista.Sum(registro => registro.Achados.Count),
            vazamentosEvitados);
    }

    private static List<RiscoPorTipo> AgruparPorTipo(IReadOnlyList<RegistroDePrivacidade> lista)
    {
        // LINQ em sintaxe de consulta: cada achado de cada verificação, agrupado pelo tipo do dado.
        IEnumerable<RiscoPorTipo> consulta =
            from registro in lista
            from achado in registro.Achados
            group achado by new { achado.Tipo, achado.Rotulo, achado.Nivel } into grupo
            orderby grupo.Count() descending, grupo.Key.Rotulo
            select new RiscoPorTipo(grupo.Key.Tipo, grupo.Key.Rotulo, grupo.Key.Nivel, grupo.Count());

        return consulta.ToList();
    }

    /// <summary>Um item por dia do período, mesmo sem verificações, para o gráfico não ter buracos.</summary>
    private static List<RiscoPorDia> AgruparPorDia(
        IReadOnlyList<RegistroDePrivacidade> lista,
        DateOnly primeiroDia,
        DateOnly ultimoDia,
        TimeZoneInfo fuso)
    {
        ILookup<DateOnly, RegistroDePrivacidade> porDia = lista.ToLookup(
            registro => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(registro.CriadoEm, fuso)));

        List<RiscoPorDia> dias = [];
        for (DateOnly dia = primeiroDia; dia <= ultimoDia; dia = dia.AddDays(1))
        {
            List<RegistroDePrivacidade> doDia = porDia[dia].ToList();
            dias.Add(new RiscoPorDia(
                dia,
                doDia.Count,
                doDia.Count(registro => registro.Resultado == ResultadoDaVerificacao.Bloqueada),
                doDia.Count(registro => registro.Resultado == ResultadoDaVerificacao.Publicada),
                MediaDoIndice(doDia)));
        }

        return dias;
    }

    private static List<RiscoPorBairro> AgruparPorBairro(IReadOnlyList<RegistroDePrivacidade> lista) =>
        lista
            .Where(registro => registro.Origem == OrigemDaVerificacao.Publicacao && registro.Bairro is not null)
            .GroupBy(registro => registro.Bairro!, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new RiscoPorBairro(
                grupo.First().Bairro!,
                grupo.Count(),
                grupo.Count(registro => registro.Resultado == ResultadoDaVerificacao.Publicada),
                grupo.Count(registro => registro.Resultado == ResultadoDaVerificacao.Bloqueada),
                MediaDoIndice(grupo.ToList())))
            .OrderByDescending(bairro => bairro.Tentativas)
            .ThenBy(bairro => bairro.Bairro)
            .ToList();

    private static List<AlertaRecente> UltimosAlertas(IReadOnlyList<RegistroDePrivacidade> lista) =>
        lista
            .OrderByDescending(registro => registro.CriadoEm)
            .ThenByDescending(registro => registro.Id)
            .SelectMany(registro => registro.Achados.Select(achado => new AlertaRecente(
                registro.CriadoEm,
                achado.Rotulo,
                achado.Nivel,
                achado.Campo,
                achado.TrechoMascarado,
                registro.Resultado.ToString(),
                registro.Bairro)))
            .Take(QuantidadeDeAlertas)
            .ToList();

    private static int MediaDoIndice(IReadOnlyCollection<RegistroDePrivacidade> lista) =>
        lista.Count == 0
            ? 0
            : (int)Math.Round(lista.Average(registro => registro.IndiceRiscoCidadao), MidpointRounding.AwayFromZero);
}
