using System.Text.RegularExpressions;
using MeConta.Privacidade.Normalizacao;
using MeConta.Privacidade.Validadores;

namespace MeConta.Privacidade.Regras;

/// <summary>
/// Regra pronta para uso: expressão compilada, validador e palavras de contexto.
/// Criada a partir de uma <see cref="DefinicaoDeRegra"/> pelo <see cref="CatalogoDeRegras"/>.
/// </summary>
public sealed class RegraPii
{
    /// <summary>Quantos caracteres antes e depois do trecho procurar as palavras de contexto.</summary>
    private const int JanelaAntes = 40;
    private const int JanelaDepois = 15;

    private const double ConfiancaValidadoComContexto = 0.99;
    private const double ConfiancaValidado = 0.9;
    private const double ConfiancaSoComContexto = 0.9;
    private const double ConfiancaSemValidacaoComContexto = 0.75;
    private const double ConfiancaSemContexto = 0.75;

    private readonly Regex _padrao;
    private readonly Regex? _contexto;
    private readonly IValidadorPii? _validador;

    internal RegraPii(
        DefinicaoDeRegra definicao,
        NivelDeRisco nivel,
        EstiloDeMascara mascara,
        Regex padrao,
        Regex? contexto,
        IValidadorPii? validador)
    {
        Id = definicao.Id;
        Tipo = definicao.Tipo;
        Rotulo = definicao.Rotulo;
        Nivel = nivel;
        Mascara = mascara;
        Substituicao = definicao.Substituicao;
        Explicacao = definicao.Explicacao;
        ExigeContexto = definicao.ExigeContexto;
        AceitaSemValidacaoComContexto = definicao.AceitaSemValidacaoComContexto;
        _padrao = padrao;
        _contexto = contexto;
        _validador = validador;
    }

    public string Id { get; }

    public string Tipo { get; }

    public string Rotulo { get; }

    public NivelDeRisco Nivel { get; }

    public EstiloDeMascara Mascara { get; }

    public string Substituicao { get; }

    public string Explicacao { get; }

    public bool ExigeContexto { get; }

    public bool AceitaSemValidacaoComContexto { get; }

    public string? Validador => _validador?.Nome;

    /// <summary>
    /// Procura a regra no texto. Pode lançar <see cref="RegexMatchTimeoutException"/> se o texto
    /// for montado para travar a expressão; o motor trata isso bloqueando a publicação.
    /// </summary>
    internal IEnumerable<Candidato> Encontrar(TextoNormalizado texto)
    {
        foreach (Match encontrado in _padrao.Matches(texto.Valor))
        {
            Group valor = encontrado.Groups["valor"];
            Group alvo = valor.Success ? valor : encontrado.Groups[0];

            if (alvo.Length == 0)
            {
                continue;
            }

            bool temContexto = TemContexto(texto.Valor, encontrado.Index, encontrado.Index + encontrado.Length);
            double? confianca = CalcularConfianca(alvo.Value, temContexto);

            if (confianca is null)
            {
                continue;
            }

            Trecho trechoOriginal = texto.ParaOriginal(alvo.Index, alvo.Length);
            yield return new Candidato(this, trechoOriginal, confianca.Value, temContexto);
        }
    }

    private double? CalcularConfianca(string trecho, bool temContexto)
    {
        if (_validador is not null)
        {
            if (_validador.EhValido(trecho))
            {
                return temContexto ? ConfiancaValidadoComContexto : ConfiancaValidado;
            }

            return AceitaSemValidacaoComContexto && temContexto ? ConfiancaSemValidacaoComContexto : null;
        }

        if (ExigeContexto && !temContexto)
        {
            return null;
        }

        return temContexto ? ConfiancaSoComContexto : ConfiancaSemContexto;
    }

    private bool TemContexto(string texto, int inicio, int fim)
    {
        if (_contexto is null)
        {
            return false;
        }

        int inicioDaJanela = Math.Max(0, inicio - JanelaAntes);
        int fimDaJanela = Math.Min(texto.Length, fim + JanelaDepois);
        return _contexto.IsMatch(texto[inicioDaJanela..fimDaJanela]);
    }
}

/// <summary>
/// Trecho que uma regra encontrou, antes de o motor resolver as sobreposições.
/// </summary>
internal sealed record Candidato(RegraPii Regra, Trecho Trecho, double Confianca, bool TemContexto);
