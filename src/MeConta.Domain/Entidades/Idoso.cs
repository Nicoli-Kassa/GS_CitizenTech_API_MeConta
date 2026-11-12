using MeConta.Domain.Comum;
using MeConta.Domain.ValueObjects;

namespace MeConta.Domain.Entidades;

/// <summary>
/// A pessoa idosa que recebe as perguntas e responde por áudio no WhatsApp.
/// Guarda só o mínimo (LGPD, art. 6º, III): nome, apelido e WhatsApp.
/// Nada de CPF, endereço, data de nascimento ou dados de saúde.
/// </summary>
public sealed class Idoso : Entidade
{
    public const int TamanhoMaximoNome = 100;
    public const int TamanhoMaximoApelido = 40;

    // Construtor usado pelo Entity Framework ao ler do banco.
    private Idoso()
    {
    }

    /// <summary>
    /// Só a <see cref="Familia"/> cadastra idosos, porque é ela que garante
    /// que o mesmo WhatsApp não apareça duas vezes.
    /// </summary>
    internal Idoso(string nome, string apelido, Telefone? telefone, DateTime agora)
    {
        DefinirDados(nome, apelido, telefone);
        CadastradoEm = agora;
    }

    public int FamiliaId { get; private set; }

    public string Nome { get; private set; } = string.Empty;

    /// <summary>Como a família chama o idoso. Ex.: "Vó Lourdes".</summary>
    public string Apelido { get; private set; } = string.Empty;

    /// <summary>
    /// WhatsApp do idoso. Pode ficar vazio para quem ainda não usa o celular
    /// (degrau "Conectar"); nesse caso, não dá para enviar perguntas.
    /// </summary>
    public Telefone? Telefone { get; private set; }

    /// <summary>
    /// Fica verdadeiro quando o neto conclui o roteiro "Ensine sua avó" (US14).
    /// </summary>
    public bool RespondeSozinho { get; private set; }

    public DateTime CadastradoEm { get; private set; }

    public bool TemWhatsApp => Telefone is not null;

    /// <summary>
    /// Registra que o idoso concluiu o roteiro e já responde sem ajuda.
    /// </summary>
    public void MarcarRespondeSozinho() => RespondeSozinho = true;

    internal void DefinirDados(string nome, string apelido, Telefone? telefone)
    {
        Nome = Texto.Obrigatorio(
            nome,
            TamanhoMaximoNome,
            "Informe o nome",
            $"Use até {TamanhoMaximoNome} caracteres no nome");
        Apelido = Texto.Obrigatorio(
            apelido,
            TamanhoMaximoApelido,
            "Informe como a família chama essa pessoa (ex.: Vó Lourdes)",
            $"Use até {TamanhoMaximoApelido} caracteres no apelido");
        Telefone = telefone;
    }
}
