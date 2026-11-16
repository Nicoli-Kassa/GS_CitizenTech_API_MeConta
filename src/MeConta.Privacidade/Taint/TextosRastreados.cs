using MeConta.Privacidade.Resultados;

namespace MeConta.Privacidade.Taint;

/// <summary>
/// Texto que veio do usuário (título, transcrição, pergunta) e ainda não passou pelo motor de PII.
/// É a "fonte contaminada" da análise de taint: as saídas inseguras (mapa do bairro, log, IA,
/// porta-retrato) não aceitam este tipo, só <see cref="TextoVerificado"/>.
///
/// Se alguém tentar escrever este texto num log por engano ($"{texto}"), sai só um aviso,
/// porque <see cref="ToString"/> não devolve o conteúdo.
/// </summary>
public sealed class TextoDoUsuario
{
    public TextoDoUsuario(string valor)
    {
        ArgumentNullException.ThrowIfNull(valor);
        ValorBruto = valor;
    }

    /// <summary>
    /// Conteúdo original. Só pode ir para o banco da própria família.
    /// Qualquer outro uso é apontado pelas regras de taint do Semgrep.
    /// </summary>
    public string ValorBruto { get; }

    public override string ToString() => "[texto do usuário não verificado]";
}

/// <summary>
/// Texto que já passou pelo motor de PII. Só o motor cria este tipo.
/// Cada saída pega a versão que lhe cabe:
/// log e IA recebem <see cref="ParaRegistro"/> (dados trocados por avisos);
/// o mapa do bairro recebe <see cref="ConteudoParaPublicar"/>, que recusa o texto com risco alto
/// e exige a confirmação da família quando há risco médio.
/// </summary>
public sealed class TextoVerificado
{
    private readonly string _original;

    internal TextoVerificado(string original, ResultadoAnalisePii resultado)
    {
        _original = original;
        Resultado = resultado;
    }

    public ResultadoAnalisePii Resultado { get; }

    /// <summary>Versão segura do texto, com os dados pessoais trocados por avisos. Pode ir para log e IA.</summary>
    public string ParaRegistro => Resultado.TextoSeguro;

    public bool PodePublicar(bool familiaConfirmou) =>
        !Resultado.DeveBloquear && (!Resultado.ExigeConfirmacao || familiaConfirmou);

    /// <summary>
    /// Texto original, liberado para o mapa do bairro só se a análise permitir.
    /// </summary>
    /// <exception cref="PublicacaoBloqueadaException">Risco alto, ou risco médio sem confirmação.</exception>
    public string ConteudoParaPublicar(bool familiaConfirmou)
    {
        if (!PodePublicar(familiaConfirmou))
        {
            throw new PublicacaoBloqueadaException(Resultado);
        }

        return _original;
    }

    public override string ToString() => ParaRegistro;
}

/// <summary>
/// Um campo de texto do usuário, com o nome que ele terá nos achados (ex.: "Titulo", "Transcricao").
/// </summary>
public sealed record CampoDoUsuario(string Nome, TextoDoUsuario Texto);

/// <summary>
/// Vários campos do mesmo conteúdo que já passaram juntos pelo motor (ex.: título e transcrição).
/// A decisão de publicar vale para o conjunto: se um campo tem dado de risco alto, nenhum é liberado.
/// Só o motor cria este tipo.
/// </summary>
public sealed class CamposVerificados
{
    private readonly Dictionary<string, string> _originais;

    internal CamposVerificados(IReadOnlyDictionary<string, string> originais, AnaliseDeCampos analise)
    {
        _originais = new Dictionary<string, string>(originais, StringComparer.Ordinal);
        Analise = analise;
    }

    public AnaliseDeCampos Analise { get; }

    /// <summary>Versão segura do campo, com os dados trocados por avisos. Pode ir para log e IA.</summary>
    public string ParaRegistro(string campo) => Analise.TextoSeguroDe(campo);

    public bool PodePublicar(bool familiaConfirmou) =>
        !Analise.DeveBloquear && (!Analise.ExigeConfirmacao || familiaConfirmou);

    /// <summary>
    /// Texto original do campo, liberado para o mapa do bairro só se a análise do conjunto permitir.
    /// </summary>
    /// <exception cref="PublicacaoBloqueadaException">Risco alto em algum campo, ou risco médio sem confirmação.</exception>
    public string ConteudoParaPublicar(string campo, bool familiaConfirmou)
    {
        if (!_originais.TryGetValue(campo, out string? original))
        {
            throw new ArgumentException($"O campo \"{campo}\" não foi verificado.", nameof(campo));
        }

        if (!PodePublicar(familiaConfirmou))
        {
            throw new PublicacaoBloqueadaException(Analise);
        }

        return original;
    }
}

/// <summary>
/// A memória não pode ir para o mapa do bairro. Leva o resultado da análise (sem o dado em si)
/// para a API responder com os achados mascarados.
/// </summary>
public sealed class PublicacaoBloqueadaException(IResultadoDeRisco resultado)
    : Exception(resultado.DeveBloquear
        ? "A memória tem dados que podem expor a pessoa a golpes e não pode ser publicada."
        : "A memória tem dados pessoais. Confirme que a família quer publicar mesmo assim.")
{
    public IResultadoDeRisco Resultado { get; } = resultado;
}
