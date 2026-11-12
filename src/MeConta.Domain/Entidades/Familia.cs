using MeConta.Domain.Comum;
using MeConta.Domain.Enums;
using MeConta.Domain.Excecoes;
using MeConta.Domain.ValueObjects;

namespace MeConta.Domain.Entidades;

/// <summary>
/// A família reúne os parentes, os idosos e os convites.
/// É a "raiz" desse grupo de objetos: membros, idosos e convites só mudam por métodos dela,
/// o que garante as regras que envolvem mais de um item (ex.: telefone repetido).
/// </summary>
public sealed class Familia : Entidade
{
    public const int TamanhoMaximoNome = 60;

    public const string MensagemConviteInvalido = "Código inválido ou expirado. Peça um novo.";
    public const string MensagemTelefoneRepetido = "Esse número já está na família";

    private readonly List<Participacao> _participacoes = [];
    private readonly List<Idoso> _idosos = [];
    private readonly List<Convite> _convites = [];

    // Construtor usado pelo Entity Framework ao ler do banco.
    private Familia()
    {
    }

    /// <summary>
    /// Cria a família (US01). Quem cria já entra como membro, com o papel de criador.
    /// </summary>
    public Familia(string nome, int criadorMembroId, DateTime agora)
    {
        Nome = Texto.Obrigatorio(
            nome,
            TamanhoMaximoNome,
            "Dê um nome para a família",
            $"Use até {TamanhoMaximoNome} caracteres");
        CriadaEm = agora;
        _participacoes.Add(new Participacao(criadorMembroId, PapelNaFamilia.Criador, agora));
    }

    public string Nome { get; private set; } = string.Empty;

    public DateTime CriadaEm { get; private set; }

    public IReadOnlyCollection<Participacao> Participacoes => _participacoes.AsReadOnly();

    public IReadOnlyCollection<Idoso> Idosos => _idosos.AsReadOnly();

    public IReadOnlyCollection<Convite> Convites => _convites.AsReadOnly();

    /// <summary>
    /// Indica se o membro participa da família. É a base da regra "só a família vê o caderno".
    /// </summary>
    public bool EhMembro(int membroId) =>
        _participacoes.Any(participacao => participacao.MembroId == membroId);

    /// <summary>
    /// Gera um convite de 48 horas (US03). O código vem de fora (gerador aleatório),
    /// para que o domínio continue previsível nos testes.
    /// </summary>
    public Convite GerarConvite(string codigo, int membroId, DateTime agora)
    {
        Convite convite = new(codigo, membroId, agora);
        _convites.Add(convite);
        return convite;
    }

    /// <summary>
    /// Coloca o membro na família se o código existir e ainda estiver no prazo (US03).
    /// Usar o código de novo não duplica o membro.
    /// </summary>
    /// <exception cref="RegraDeNegocioException">Código inexistente ou vencido.</exception>
    public void AceitarConvite(string codigo, int membroId, DateTime agora)
    {
        string normalizado = Convite.Normalizar(codigo);
        Convite? convite = _convites.FirstOrDefault(item => item.Codigo == normalizado);

        if (convite is null || !convite.EstaValido(agora))
        {
            throw new RegraDeNegocioException(MensagemConviteInvalido);
        }

        if (EhMembro(membroId))
        {
            return;
        }

        _participacoes.Add(new Participacao(membroId, PapelNaFamilia.Membro, agora));
    }

    /// <summary>
    /// Cadastra o idoso (US02). O mesmo WhatsApp não pode aparecer duas vezes na família.
    /// </summary>
    /// <exception cref="ConflitoException">Número já cadastrado na família.</exception>
    public Idoso CadastrarIdoso(string nome, string apelido, Telefone? telefone, DateTime agora)
    {
        GarantirTelefoneLivre(telefone, idosoIgnorado: null);

        Idoso idoso = new(nome, apelido, telefone, agora);
        _idosos.Add(idoso);
        return idoso;
    }

    /// <summary>
    /// Atualiza os dados de um idoso desta família, mantendo a regra do telefone único.
    /// </summary>
    public void AtualizarIdoso(Idoso idoso, string nome, string apelido, Telefone? telefone)
    {
        ArgumentNullException.ThrowIfNull(idoso);

        if (!_idosos.Contains(idoso))
        {
            throw new InvalidOperationException("O idoso não pertence a esta família.");
        }

        GarantirTelefoneLivre(telefone, idosoIgnorado: idoso);
        idoso.DefinirDados(nome, apelido, telefone);
    }

    private void GarantirTelefoneLivre(Telefone? telefone, Idoso? idosoIgnorado)
    {
        if (telefone is null)
        {
            return;
        }

        bool repetido = _idosos.Any(idoso => idoso != idosoIgnorado && idoso.Telefone == telefone);
        if (repetido)
        {
            throw new ConflitoException(MensagemTelefoneRepetido);
        }
    }
}
