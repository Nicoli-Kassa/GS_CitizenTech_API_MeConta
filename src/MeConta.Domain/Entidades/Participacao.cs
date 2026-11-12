using MeConta.Domain.Enums;

namespace MeConta.Domain.Entidades;

/// <summary>
/// Liga um membro a uma família (relação N:N). Uma neta pode participar
/// da família do lado da mãe e da família do lado do pai.
/// </summary>
public sealed class Participacao
{
    // Construtor usado pelo Entity Framework ao ler do banco.
    private Participacao()
    {
    }

    internal Participacao(int membroId, PapelNaFamilia papel, DateTime agora)
    {
        if (membroId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(membroId), "O membro deve estar salvo.");
        }

        MembroId = membroId;
        Papel = papel;
        EntrouEm = agora;
    }

    public int FamiliaId { get; private set; }

    public int MembroId { get; private set; }

    public PapelNaFamilia Papel { get; private set; }

    public DateTime EntrouEm { get; private set; }

    /// <summary>Navegação para listar os nomes dos membros.</summary>
    public Membro? Membro { get; private set; }
}
