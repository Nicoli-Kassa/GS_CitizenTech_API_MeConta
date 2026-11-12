using MeConta.Domain.Entidades;

namespace MeConta.Application.Dtos;

/// <summary>Dados para criar o perfil de quem acabou de se cadastrar no login.</summary>
/// <param name="Nome">Como a pessoa quer ser chamada no app. Ex.: "Bia".</param>
public sealed record CriarPerfilRequest(string Nome);

public sealed record MembroResponse(int Id, string Nome, DateTime CriadoEm)
{
    public static MembroResponse De(Membro membro) => new(membro.Id, membro.Nome, membro.CriadoEm);
}
