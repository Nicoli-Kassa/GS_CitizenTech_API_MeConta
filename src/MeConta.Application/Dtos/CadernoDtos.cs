using MeConta.Domain.Entidades;

namespace MeConta.Application.Dtos;

/// <summary>
/// Arquivo recebido pela API, sem depender de tipos do ASP.NET.
/// </summary>
/// <param name="NomeArquivo">Nome original, usado para conferir a extensão.</param>
/// <param name="TamanhoBytes">Tamanho informado no envio.</param>
/// <param name="AbrirLeitura">Abre o conteúdo para leitura.</param>
public sealed record ArquivoRecebido(string NomeArquivo, long TamanhoBytes, Func<Stream> AbrirLeitura);

/// <summary>Resposta em áudio que vira página do caderno (US08 e US10).</summary>
/// <param name="Transcricao">O que a pessoa idosa disse no áudio, escrito pela família (opcional).</param>
public sealed record NovaResposta(
    string? Titulo,
    int DuracaoSegundos,
    Guid? ChaveIdempotencia,
    ArquivoRecebido Audio,
    ArquivoRecebido? Foto,
    string? Transcricao = null);

public sealed record PaginaResponse(
    int Id,
    int EnvioId,
    int IdosoId,
    string IdosoApelido,
    string Titulo,
    string Pergunta,
    string? Transcricao,
    int CategoriaId,
    int DuracaoSegundos,
    bool TemFoto,
    string AudioUrl,
    string? FotoUrl,
    int CriadaPorMembroId,
    Guid ChaveIdempotencia,
    DateTime CriadaEm)
{
    /// <summary>
    /// Monta a resposta a partir da página com o envio e o idoso carregados.
    /// </summary>
    public static PaginaResponse De(PaginaCaderno pagina)
    {
        EnvioPergunta envio = pagina.Envio
            ?? throw new InvalidOperationException("Carregue o envio junto com a página.");

        return new PaginaResponse(
            pagina.Id,
            envio.Id,
            envio.IdosoId,
            envio.Idoso?.Apelido ?? string.Empty,
            pagina.Titulo,
            envio.TextoPergunta,
            pagina.Transcricao,
            envio.CategoriaId,
            pagina.DuracaoSegundos,
            pagina.TemFoto,
            $"/api/v1/paginas/{pagina.Id}/audio",
            pagina.TemFoto ? $"/api/v1/paginas/{pagina.Id}/foto" : null,
            pagina.CriadaPorMembroId,
            pagina.ChaveIdempotencia,
            pagina.CriadaEm);
    }
}

/// <summary>Transcrição do áudio da página. Texto em branco apaga a transcrição.</summary>
public sealed record TranscricaoRequest(string? Texto);

/// <summary>Resultado de guardar uma resposta: <c>Criada</c> é falso quando a chave já existia.</summary>
public sealed record ResultadoGuardarResposta(PaginaResponse Pagina, bool Criada);

/// <summary>Filtros do caderno (US09).</summary>
/// <param name="IdosoId">Só as páginas deste idoso.</param>
/// <param name="Categoria">Slug da categoria. Ex.: "receitas".</param>
/// <param name="Pagina">Número da página da lista, começando em 1.</param>
/// <param name="Tamanho">Itens por página, de 1 a 50.</param>
public sealed record FiltroCaderno(int? IdosoId, string? Categoria, int Pagina = 1, int Tamanho = 20);

public sealed record ListaPaginada<T>(IReadOnlyList<T> Itens, int Pagina, int Tamanho, int Total)
{
    public int TotalDePaginas => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamanho);
}

/// <summary>Arquivo pronto para ser devolvido pela API.</summary>
public sealed record ArquivoParaDownload(Stream Conteudo, string ContentType);
