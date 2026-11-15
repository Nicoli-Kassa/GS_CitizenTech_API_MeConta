using System.Security.Cryptography;
using System.Text;
using MeConta.Domain.Comum;

namespace MeConta.Domain.Entidades;

/// <summary>
/// O porta-retrato Me Conta (US15): uma caixinha com ESP32 que acende quando chega pergunta
/// e tem um botão grande "quero contar". Cada idoso tem no máximo um porta-retrato.
/// O aparelho se identifica por uma chave secreta; o banco guarda só o hash dela.
/// </summary>
public sealed class PortaRetrato : Entidade
{
    public const int TamanhoCodigo = 8;

    /// <summary>
    /// Letras minúsculas e números do código público, que vai no nome dos tópicos MQTT.
    /// Ficam de fora l, o, 0 e 1, que se confundem na tela.
    /// </summary>
    public const string AlfabetoCodigo = "abcdefghijkmnpqrstuvwxyz23456789";

    /// <summary>Chaves menores que isso são fáceis de adivinhar.</summary>
    public const int TamanhoMinimoChave = 24;

    /// <summary>
    /// Sem notícia da ponte (Node-RED) por mais que isso, o porta-retrato aparece como desconectado,
    /// mesmo que o último aviso tenha sido "online".
    /// </summary>
    public static readonly TimeSpan TempoSemContatoParaDesconectar = TimeSpan.FromSeconds(90);

    // Construtor usado pelo Entity Framework ao ler do banco.
    private PortaRetrato()
    {
    }

    /// <summary>
    /// Liga um porta-retrato novo ao idoso. O código e a chave vêm de fora (gerador aleatório),
    /// para que o domínio continue previsível nos testes.
    /// </summary>
    public PortaRetrato(Idoso idoso, string codigo, string chave, int criadoPorMembroId, DateTime agora)
    {
        ArgumentNullException.ThrowIfNull(idoso);

        IdosoId = idoso.Id;
        DefinirCredenciais(codigo, chave);
        CriadoPorMembroId = criadoPorMembroId;
        CriadoEm = agora;
    }

    public int IdosoId { get; private set; }

    /// <summary>Código público do aparelho. Ex.: "k7m2q9xd". Não dá acesso a nada sozinho.</summary>
    public string Codigo { get; private set; } = string.Empty;

    /// <summary>SHA-256 da chave secreta, em hexadecimal. A chave em si não fica guardada.</summary>
    public string ChaveHash { get; private set; } = string.Empty;

    public int CriadoPorMembroId { get; private set; }

    public DateTime CriadoEm { get; private set; }

    /// <summary>Último estado de conexão informado pela ponte (Node-RED).</summary>
    public bool Online { get; private set; }

    public DateTime? UltimoContatoEm { get; private set; }

    public Idoso? Idoso { get; private set; }

    /// <summary>Prefixo dos tópicos MQTT deste aparelho. Ex.: "meconta/k7m2q9xd".</summary>
    public string TopicoBase => $"meconta/{Codigo}";

    /// <summary>
    /// Confere a chave enviada pelo aparelho comparando os hashes.
    /// </summary>
    public bool ConfereChave(string? chave) =>
        !string.IsNullOrWhiteSpace(chave)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(CalcularHash(chave)),
            Encoding.ASCII.GetBytes(ChaveHash));

    /// <summary>
    /// Pareia de novo: troca código e chave. A chave antiga deixa de funcionar na hora
    /// (ex.: aparelho perdido ou chave exposta).
    /// </summary>
    public void TrocarCredenciais(string codigo, string chave, int membroId, DateTime agora)
    {
        DefinirCredenciais(codigo, chave);
        CriadoPorMembroId = membroId;
        CriadoEm = agora;
        Online = false;
        UltimoContatoEm = null;
    }

    /// <summary>
    /// Guarda o estado de conexão que a ponte viu no broker (status online/offline do aparelho).
    /// </summary>
    public void RegistrarConexao(bool online, DateTime agora)
    {
        Online = online;
        UltimoContatoEm = agora;
    }

    /// <summary>
    /// Conectado = o último aviso foi "online" e ele chegou há pouco tempo.
    /// </summary>
    public bool EstaConectado(DateTime agora) =>
        Online
        && UltimoContatoEm is DateTime ultimo
        && agora - ultimo <= TempoSemContatoParaDesconectar;

    /// <summary>
    /// Hash da chave. A chave é longa e aleatória, então um SHA-256 simples basta
    /// (não é uma senha escolhida por uma pessoa).
    /// </summary>
    public static string CalcularHash(string chave) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave.Trim())));

    private void DefinirCredenciais(string codigo, string chave)
    {
        string codigoNormalizado = (codigo ?? string.Empty).Trim().ToLowerInvariant();
        if (codigoNormalizado.Length != TamanhoCodigo || codigoNormalizado.Any(letra => !AlfabetoCodigo.Contains(letra)))
        {
            throw new ArgumentException(
                $"O código deve ter {TamanhoCodigo} caracteres do alfabeto do porta-retrato.",
                nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(chave) || chave.Trim().Length < TamanhoMinimoChave)
        {
            throw new ArgumentException(
                $"A chave deve ter pelo menos {TamanhoMinimoChave} caracteres.",
                nameof(chave));
        }

        Codigo = codigoNormalizado;
        ChaveHash = CalcularHash(chave);
    }
}
