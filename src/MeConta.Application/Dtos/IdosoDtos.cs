using MeConta.Domain.Entidades;

namespace MeConta.Application.Dtos;

/// <summary>Dados do idoso (US02). Nada de CPF, endereço, data de nascimento ou saúde.</summary>
/// <param name="Nome">Nome. Ex.: "Lourdes Souza".</param>
/// <param name="Apelido">Como a família chama. Ex.: "Vó Lourdes".</param>
/// <param name="WhatsApp">Celular com DDD. Pode ficar vazio para quem ainda não usa o celular.</param>
public sealed record IdosoRequest(string Nome, string Apelido, string? WhatsApp);

public sealed record IdosoResponse(
    int Id,
    int FamiliaId,
    string Nome,
    string Apelido,
    string? WhatsApp,
    bool RespondeSozinho,
    DateTime CadastradoEm)
{
    public static IdosoResponse De(Idoso idoso) => new(
        idoso.Id,
        idoso.FamiliaId,
        idoso.Nome,
        idoso.Apelido,
        idoso.Telefone?.Valor,
        idoso.RespondeSozinho,
        idoso.CadastradoEm);
}
