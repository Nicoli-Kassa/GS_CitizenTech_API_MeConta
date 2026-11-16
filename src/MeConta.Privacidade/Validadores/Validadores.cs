namespace MeConta.Privacidade.Validadores;

/// <summary>
/// Confere se um trecho encontrado por uma regra é mesmo o dado que parece ser
/// (por exemplo, se os 11 dígitos têm o dígito verificador de um CPF).
/// Diminui os falsos positivos: nem todo número de 11 dígitos é um CPF.
/// </summary>
public interface IValidadorPii
{
    /// <summary>Nome usado no campo "validador" do arquivo de regras.</summary>
    string Nome { get; }

    bool EhValido(string trecho);
}

/// <summary>
/// Base para validadores de documentos numéricos. O passo a passo é o mesmo para todos
/// (tirar a pontuação, conferir o tamanho, descartar números repetidos); cada subclasse
/// implementa só a conta final.
/// </summary>
public abstract class ValidadorDeDigitos : IValidadorPii
{
    public abstract string Nome { get; }

    /// <summary>Quantidades de dígitos aceitas.</summary>
    protected abstract IReadOnlySet<int> TamanhosAceitos { get; }

    public bool EhValido(string trecho)
    {
        ArgumentNullException.ThrowIfNull(trecho);

        string digitos = new(trecho.Where(char.IsAsciiDigit).ToArray());

        if (!TamanhosAceitos.Contains(digitos.Length))
        {
            return false;
        }

        // "111.111.111-11" passa na conta do CPF, mas não é um documento de verdade.
        if (digitos.Distinct().Count() == 1)
        {
            return false;
        }

        return DigitosSaoValidos(digitos);
    }

    protected abstract bool DigitosSaoValidos(string digitos);

    protected static int Digito(char letra) => letra - '0';
}

/// <summary>
/// CPF: 11 dígitos, com os dois últimos calculados a partir dos nove primeiros (módulo 11).
/// </summary>
public sealed class ValidadorCpf : ValidadorDeDigitos
{
    private static readonly IReadOnlySet<int> Tamanhos = new HashSet<int> { 11 };

    public override string Nome => "cpf";

    protected override IReadOnlySet<int> TamanhosAceitos => Tamanhos;

    protected override bool DigitosSaoValidos(string digitos) =>
        CalcularDigitoVerificador(digitos, 9) == Digito(digitos[9])
        && CalcularDigitoVerificador(digitos, 10) == Digito(digitos[10]);

    private static int CalcularDigitoVerificador(string digitos, int quantidade)
    {
        int soma = 0;
        for (int i = 0; i < quantidade; i++)
        {
            soma += Digito(digitos[i]) * (quantidade + 1 - i);
        }

        int resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}

/// <summary>
/// Cartão de crédito ou débito: 13 a 19 dígitos que passam no algoritmo de Luhn.
/// </summary>
public sealed class ValidadorCartao : ValidadorDeDigitos
{
    private static readonly IReadOnlySet<int> Tamanhos = Enumerable.Range(13, 7).ToHashSet();

    public override string Nome => "cartao";

    protected override IReadOnlySet<int> TamanhosAceitos => Tamanhos;

    protected override bool DigitosSaoValidos(string digitos)
    {
        int soma = 0;
        bool dobrar = false;

        // Da direita para a esquerda, dobra um dígito sim, um não.
        for (int i = digitos.Length - 1; i >= 0; i--)
        {
            int valor = Digito(digitos[i]);
            if (dobrar)
            {
                valor *= 2;
                if (valor > 9)
                {
                    valor -= 9;
                }
            }

            soma += valor;
            dobrar = !dobrar;
        }

        return soma % 10 == 0;
    }
}

/// <summary>
/// Telefone brasileiro: DDD que existe + 8 dígitos (fixo, começando de 2 a 5)
/// ou 9 dígitos (celular, começando com 9). Aceita o 55 do país na frente.
/// </summary>
public sealed class ValidadorTelefone : ValidadorDeDigitos
{
    private static readonly IReadOnlySet<int> Tamanhos = new HashSet<int> { 10, 11, 12, 13 };

    /// <summary>DDDs em uso no Brasil.</summary>
    private static readonly IReadOnlySet<int> Ddds = new HashSet<int>
    {
        11, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 24, 27, 28, 31, 32, 33, 34, 35, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48, 49, 51, 53, 54, 55, 61, 62, 63, 64, 65, 66, 67, 68, 69,
        71, 73, 74, 75, 77, 79, 81, 82, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93, 94, 95, 96, 97, 98, 99,
    };

    public override string Nome => "telefone";

    protected override IReadOnlySet<int> TamanhosAceitos => Tamanhos;

    protected override bool DigitosSaoValidos(string digitos)
    {
        string semPais = digitos.Length >= 12
            ? (digitos.StartsWith("55", StringComparison.Ordinal) ? digitos[2..] : string.Empty)
            : digitos;

        if (semPais.Length is not (10 or 11))
        {
            return false;
        }

        int ddd = int.Parse(semPais[..2], System.Globalization.CultureInfo.InvariantCulture);
        if (!Ddds.Contains(ddd))
        {
            return false;
        }

        char primeiro = semPais[2];
        return semPais.Length == 11 ? primeiro == '9' : primeiro is >= '2' and <= '5';
    }
}

/// <summary>
/// Validadores disponíveis para o arquivo de regras, pelo nome.
/// </summary>
public sealed class RegistroDeValidadores
{
    private readonly Dictionary<string, IValidadorPii> _validadores;

    public RegistroDeValidadores(IEnumerable<IValidadorPii> validadores)
    {
        ArgumentNullException.ThrowIfNull(validadores);
        _validadores = validadores.ToDictionary(validador => validador.Nome, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>CPF, cartão (Luhn) e telefone.</summary>
    public static RegistroDeValidadores Padrao() =>
        new([new ValidadorCpf(), new ValidadorCartao(), new ValidadorTelefone()]);

    public bool TentarObter(string nome, out IValidadorPii validador) =>
        _validadores.TryGetValue(nome, out validador!);
}
