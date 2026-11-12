using MeConta.Domain.Comum;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Pessoa que usa o app (neta, filha, neto). O login fica no ASP.NET Identity;
/// esta entidade guarda só o que o Me Conta precisa: o nome e o vínculo com o usuário do login.
/// </summary>
public sealed class Membro : Entidade
{
    public const int TamanhoMaximoNome = 100;

    // Construtor usado pelo Entity Framework ao ler do banco.
    private Membro()
    {
    }

    public Membro(string usuarioId, string nome, DateTime agora)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            throw new ArgumentException("O usuário do login é obrigatório.", nameof(usuarioId));
        }

        UsuarioId = usuarioId;
        Nome = Texto.Obrigatorio(
            nome,
            TamanhoMaximoNome,
            "Informe o seu nome",
            $"Use até {TamanhoMaximoNome} caracteres no nome");
        CriadoEm = agora;
    }

    /// <summary>Id do usuário no ASP.NET Identity.</summary>
    public string UsuarioId { get; private set; } = string.Empty;

    public string Nome { get; private set; } = string.Empty;

    public DateTime CriadoEm { get; private set; }
}
