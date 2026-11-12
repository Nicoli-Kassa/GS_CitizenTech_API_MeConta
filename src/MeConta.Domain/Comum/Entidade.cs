namespace MeConta.Domain.Comum;

/// <summary>
/// Base de todas as entidades com identidade própria.
/// O Id é gerado pelo banco de dados quando a entidade é salva.
/// </summary>
public abstract class Entidade
{
    public int Id { get; protected set; }
}
