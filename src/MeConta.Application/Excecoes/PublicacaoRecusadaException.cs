using MeConta.Application.Dtos;

namespace MeConta.Application.Excecoes;

/// <summary>
/// O filtro de dados pessoais não liberou a memória para o mapa do bairro (US18).
/// A API transforma esta exceção em resposta 422 (Unprocessable Content) com a análise completa:
/// os dados encontrados (mascarados), o índice Risco Cidadão e a versão segura do texto.
/// </summary>
public sealed class PublicacaoRecusadaException : Exception
{
    public PublicacaoRecusadaException(AnalisePrivacidadeResponse analise)
        : base(analise.Bloqueada
            ? "A memória tem dados que podem expor a pessoa idosa a golpes e não pode ir para o mapa do bairro."
            : "A memória tem dados pessoais. Confira o alerta e confirme que a família quer publicar mesmo assim.")
    {
        Analise = analise;
    }

    public AnalisePrivacidadeResponse Analise { get; }
}
