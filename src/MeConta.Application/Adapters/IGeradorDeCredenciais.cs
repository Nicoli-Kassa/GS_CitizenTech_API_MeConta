namespace MeConta.Application.Adapters;

/// <summary>
/// Gera o código público e a chave secreta do porta-retrato.
/// Fica fora do domínio para que os testes usem valores fixos.
/// </summary>
public interface IGeradorDeCredenciais
{
    /// <summary>Código de 8 caracteres do alfabeto do porta-retrato. Ex.: "k7m2q9xd".</summary>
    string GerarCodigo();

    /// <summary>Chave secreta longa e aleatória, mostrada uma única vez.</summary>
    string GerarChave();
}
