namespace MeConta.Application.Adapters;

/// <summary>
/// Gera códigos aleatórios de convite. Fica fora do domínio para que os testes usem um código fixo.
/// </summary>
public interface IGeradorDeCodigoConvite
{
    string Gerar();
}
