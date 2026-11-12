using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Dtos;

/// <summary>Dados para criar uma família (US01).</summary>
/// <param name="Nome">Nome da família, até 60 caracteres. Ex.: "Família Souza".</param>
public sealed record CriarFamiliaRequest(string Nome);

public sealed record FamiliaResumoResponse(
    int Id,
    string Nome,
    PapelNaFamilia MeuPapel,
    int QuantidadeDeIdosos,
    DateTime CriadaEm);

public sealed record MembroDaFamiliaResponse(int MembroId, string Nome, PapelNaFamilia Papel, DateTime EntrouEm);

public sealed record FamiliaDetalheResponse(
    int Id,
    string Nome,
    DateTime CriadaEm,
    IReadOnlyList<MembroDaFamiliaResponse> Membros,
    IReadOnlyList<IdosoResponse> Idosos)
{
    public static FamiliaDetalheResponse De(Familia familia) => new(
        familia.Id,
        familia.Nome,
        familia.CriadaEm,
        familia.Participacoes
            .OrderBy(participacao => participacao.EntrouEm)
            .Select(participacao => new MembroDaFamiliaResponse(
                participacao.MembroId,
                participacao.Membro?.Nome ?? string.Empty,
                participacao.Papel,
                participacao.EntrouEm))
            .ToList(),
        familia.Idosos
            .OrderBy(idoso => idoso.Apelido)
            .Select(IdosoResponse.De)
            .ToList());
}

/// <summary>Código para um parente entrar na família (US03).</summary>
public sealed record ConviteResponse(string Codigo, DateTime ExpiraEm);
