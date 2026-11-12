using MeConta.Domain.Entidades;
using MeConta.Domain.Enums;

namespace MeConta.Application.Dtos;

public sealed record CategoriaResponse(int Id, string Slug, string Nome)
{
    public static CategoriaResponse De(Categoria categoria) => new(categoria.Id, categoria.Slug, categoria.Nome);
}

/// <summary>Pergunta escrita pela família (US06).</summary>
/// <param name="Texto">De 10 a 200 caracteres. Ex.: "Vó, como a senhora conheceu o vô?"</param>
/// <param name="CategoriaId">Id da categoria (1 a 5).</param>
public sealed record CriarPerguntaRequest(string Texto, int CategoriaId);

public sealed record PerguntaResponse(int Id, string Texto, int CategoriaId, TipoPergunta Tipo, bool JaPerguntada)
{
    public static PerguntaResponse De(IPergunta pergunta, bool jaPerguntada) =>
        new(pergunta.Id, pergunta.Texto, pergunta.CategoriaId, pergunta.Tipo, jaPerguntada);
}

/// <summary>Sugestão da semana para um idoso (US05).</summary>
public sealed record PerguntaDaSemanaResponse(
    int IdosoId,
    DateOnly SemanaInicio,
    DateOnly SemanaFim,
    PerguntaResponse Pergunta);
