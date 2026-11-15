using MeConta.Domain.Entidades;

namespace MeConta.Application.Dtos;

/// <summary>
/// Porta-retrato recém-pareado. A <c>Chave</c> aparece só nesta resposta:
/// copie para a configuração da ponte (Node-RED), porque a API guarda apenas o hash.
/// </summary>
public sealed record PortaRetratoPareadoResponse(
    int Id,
    int IdosoId,
    string Codigo,
    string Chave,
    string TopicoBase,
    DateTime CriadoEm)
{
    public static PortaRetratoPareadoResponse De(PortaRetrato portaRetrato, string chave) => new(
        portaRetrato.Id,
        portaRetrato.IdosoId,
        portaRetrato.Codigo,
        chave,
        portaRetrato.TopicoBase,
        portaRetrato.CriadoEm);
}

/// <summary>Porta-retrato do idoso, sem a chave.</summary>
public sealed record PortaRetratoResponse(
    int Id,
    int IdosoId,
    string Codigo,
    string TopicoBase,
    bool Conectado,
    DateTime? UltimoContatoEm,
    DateTime CriadoEm)
{
    public static PortaRetratoResponse De(PortaRetrato portaRetrato, DateTime agora) => new(
        portaRetrato.Id,
        portaRetrato.IdosoId,
        portaRetrato.Codigo,
        portaRetrato.TopicoBase,
        portaRetrato.EstaConectado(agora),
        portaRetrato.UltimoContatoEm,
        portaRetrato.CriadoEm);
}

/// <summary>
/// O que o porta-retrato deve mostrar agora. Vai para um broker MQTT público,
/// então leva só o apelido, o primeiro nome de quem perguntou e o texto da pergunta.
/// </summary>
/// <param name="Codigo">Código público do aparelho.</param>
/// <param name="Apelido">Como a família chama o idoso. Ex.: "Vó Lourdes".</param>
/// <param name="TemPerguntaNova">Verdadeiro quando há pergunta esperando: a moldura acende.</param>
/// <param name="PerguntasEsperando">Quantas perguntas esperam o idoso (a tela mostra a mais recente).</param>
/// <param name="Pergunta">A pergunta que aparece na tela, ou nula.</param>
public sealed record EstadoPortaRetratoResponse(
    string Codigo,
    string Apelido,
    bool TemPerguntaNova,
    int PerguntasEsperando,
    PerguntaNoPortaRetrato? Pergunta);

/// <param name="EnvioId">Id do envio; volta no "quero contar".</param>
/// <param name="QuemPerguntou">Primeiro nome de quem enviou. Ex.: "Bia".</param>
/// <param name="Texto">Texto da pergunta.</param>
/// <param name="EnviadaEm">Data e hora do envio (UTC).</param>
public sealed record PerguntaNoPortaRetrato(int EnvioId, string QuemPerguntou, string Texto, DateTime EnviadaEm);

/// <summary>Botão "quero contar" apertado no aparelho.</summary>
/// <param name="EnvioId">Pergunta que estava na tela. Vazio quando não havia pergunta.</param>
public sealed record QueroContarRequest(int? EnvioId);

/// <summary>Estado de conexão do aparelho visto pela ponte no broker.</summary>
/// <param name="Online">Verdadeiro quando o aparelho está conectado ao broker.</param>
public sealed record ConexaoPortaRetratoRequest(bool Online);

/// <summary>Aviso "quero contar" para a família ver no app.</summary>
public sealed record AvisoResponse(
    int Id,
    int IdosoId,
    string Apelido,
    int? EnvioId,
    string? Pergunta,
    DateTime CriadoEm,
    bool Visto,
    DateTime? VistoEm)
{
    public static AvisoResponse De(AvisoQueroContar aviso, string apelido, string? pergunta) => new(
        aviso.Id,
        aviso.IdosoId,
        apelido,
        aviso.EnvioId,
        pergunta,
        aviso.CriadoEm,
        aviso.FoiVisto,
        aviso.VistoEm);
}

/// <summary>Resultado do "quero contar": o aviso e se ele foi criado agora ou já existia.</summary>
public sealed record ResultadoQueroContar(AvisoResponse Aviso, bool Criado);
