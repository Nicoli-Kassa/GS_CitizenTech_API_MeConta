using MeConta.Application.Adapters;

namespace MeConta.Application.Servicos;

/// <summary>
/// Alertas escritos à mão para cada tipo de dado pessoal e para cada regra do Semgrep.
/// São a reserva da IA: valem quando não há chave do Gemini, quando o plano gratuito chega ao limite,
/// quando a IA demora ou quando a resposta dela não passa na conferência do motor.
/// </summary>
public static class ExplicacoesLocais
{
    /// <summary>Origem das falhas encontradas pelo motor de dados pessoais.</summary>
    public const string OrigemDadoPessoal = "Dado pessoal";

    /// <summary>Origem das falhas encontradas pelo Semgrep no código.</summary>
    public const string OrigemCodigo = "Código";

    /// <summary>Tipo usado para as combinações perigosas (ex.: endereço + rotina da casa).</summary>
    public const string TipoCombinacao = "Combinacao";

    private sealed record Texto(string Alerta, string RiscoReal, string OQueFazer);

    /// <summary>Por tipo de dado pessoal (o mesmo "tipo" das regras do motor).</summary>
    private static readonly Dictionary<string, Texto> PorTipo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cpf"] = new(
            "Tem um CPF no texto.",
            "Com o CPF, golpistas fingem ser do banco ou do INSS, abrem contas e pedem empréstimos no nome da pessoa.",
            "Apague o número antes de levar a memória ao mapa. Banco e INSS nunca pedem CPF por telefone ou WhatsApp."),
        ["Rg"] = new(
            "Tem um número de RG no texto.",
            "O RG ajuda a falsificar documentos e a confirmar a identidade da pessoa em golpes por telefone.",
            "Apague o número. Para contar a história, basta dizer que a pessoa levou o documento."),
        ["CartaoDeCredito"] = new(
            "Tem um número de cartão no texto.",
            "Com o número do cartão dá para fazer compras pela internet, e a cobrança cai na conta da pessoa.",
            "Apague o número. Se ele já foi enviado para alguém, peça ao banco para bloquear o cartão."),
        ["DadosBancarios"] = new(
            "Tem agência ou conta bancária no texto.",
            "Com agência e conta, golpistas fazem depósitos falsos, pedem devolução ou tentam entrar na conta.",
            "Apague agência e conta. Para contar a história, basta o nome do banco."),
        ["ChavePix"] = new(
            "Tem uma chave PIX no texto.",
            "Chave PIX publicada vira alvo do golpe do falso comprovante e de pedidos de devolução de um PIX que nunca existiu.",
            "Apague a chave. Antes de devolver qualquer PIX, confira no extrato do banco."),
        ["Senha"] = new(
            "Tem uma senha no texto.",
            "Senha publicada é acesso direto ao banco, ao celular ou ao aplicativo da pessoa.",
            "Apague a senha do texto. Depois, troque essa senha hoje mesmo: ninguém do bairro precisa saber."),
        ["CodigoDeVerificacao"] = new(
            "Tem um código de verificação no texto.",
            "É o código que golpistas pedem para clonar o WhatsApp e pedir dinheiro à família em nome da pessoa.",
            "Apague o código. Códigos que chegam por SMS ou WhatsApp nunca devem ser passados a ninguém."),
        ["Saude"] = new(
            "O texto fala da saúde da pessoa.",
            "Dado de saúde é sensível pela LGPD. Publicado, atrai golpes de falsos planos de saúde, remédios milagrosos e consultas falsas.",
            "Combine com a pessoa idosa se ela quer contar isso ao bairro. Dá para guardar a memória sem citar a doença ou o remédio."),
        ["Telefone"] = new(
            "Tem um número de telefone no texto.",
            "Com o telefone, golpistas ligam ou mandam mensagem fingindo ser um neto, o banco ou a farmácia.",
            "Apague o número. Quem quiser falar com a família pode usar o próprio Me Conta."),
        ["Email"] = new(
            "Tem um e-mail no texto.",
            "O e-mail publicado recebe mensagens falsas de bancos e lojas, com links que roubam senhas.",
            "Apague o e-mail antes de publicar."),
        ["Endereco"] = new(
            "O texto mostra onde a pessoa mora.",
            "Rua e número no mapa mostram a casa da pessoa e facilitam visitas de golpistas e furtos.",
            "Deixe só o nome do bairro. A memória continua bonita sem a rua e o número."),
        ["Cep"] = new(
            "Tem um CEP no texto.",
            "O CEP leva até a rua da pessoa.",
            "Apague o CEP. No mapa, basta o nome do bairro."),
        ["DataDeNascimento"] = new(
            "Tem a data de nascimento completa no texto.",
            "A data de nascimento confirma a identidade em bancos e órgãos públicos e ajuda golpistas a se passar pela pessoa.",
            "Deixe só a idade ou o ano, sem dia e mês."),
        ["Vulnerabilidade"] = new(
            "O texto conta a rotina da casa.",
            "Dizer que a pessoa mora sozinha, a que horas sai ou onde guarda dinheiro avisa quem quer aplicar golpe ou furtar.",
            "Tire do texto quando a pessoa fica sozinha, onde guarda dinheiro e como é a segurança da casa."),
        [TipoCombinacao] = new(
            "Dados que, juntos, ficam mais perigosos.",
            "Separados, cada dado já é um risco; juntos, dizem a um golpista quem procurar, onde e quando.",
            "Tire pelo menos um dos dois dados da combinação, de preferência o endereço ou a rotina."),
    };

    /// <summary>Por regra do Semgrep (id curto, sem o prefixo da pasta).</summary>
    private static readonly Dictionary<string, Texto> PorRegraDeCodigo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["meconta-pii-para-log"] = new(
            "O registro de erros do servidor podia guardar dados pessoais da família.",
            "Quem cuida do servidor, ou alguém que invada o log, leria CPF, telefone ou saúde da pessoa idosa.",
            "O log guarda só a rota e números de identificação. O texto da família nunca vai para lá."),
        ["meconta-pii-para-porta-retrato"] = new(
            "A pergunta e o apelido iam para o porta-retrato sem passar pelo filtro.",
            "A mensagem atravessa a internet e aparece na tela da sala: um telefone ou dado de saúde escrito na pergunta ficaria à vista.",
            "Tudo o que vai para o aparelho passa antes pelo filtro de dados pessoais."),
        ["meconta-pii-para-ia"] = new(
            "Texto da família podia ir para a IA sem passar pelo filtro.",
            "No plano gratuito, o provedor de IA pode guardar e usar o que recebe: um CPF enviado sairia do controle da família.",
            "A IA só recebe o tipo do dado e o texto já mascarado."),
        ["meconta-pii-para-mapa-do-bairro"] = new(
            "Uma memória podia chegar ao mapa do bairro sem passar pelo filtro.",
            "O mapa é público: qualquer morador leria o dado pessoal da pessoa idosa.",
            "Só o texto liberado pelo filtro vai para o mapa."),
        ["meconta-texto-bruto-fora-do-motor"] = new(
            "Uma parte do código lia o texto da família sem passar pelo filtro.",
            "Se esse texto for parar num log, na tela ou na IA, os dados pessoais vazam.",
            "Só o motor de dados pessoais lê o texto original; o resto do código usa a versão verificada."),
        ["meconta-chave-gemini-no-codigo"] = new(
            "A chave da IA estava escrita no código.",
            "Quem visse o repositório usaria a chave em nome do projeto, gastando o limite ou pedindo coisas indevidas.",
            "A chave fica guardada fora do código e, se vazar, é trocada na hora."),
        ["meconta-chave-no-appsettings"] = new(
            "Uma senha ou chave estava no arquivo de configuração.",
            "O arquivo vai para o GitHub: qualquer pessoa leria a senha.",
            "Chaves e senhas ficam guardadas fora do repositório."),
    };

    private static readonly Texto ReservaDadoPessoal = new(
        "O texto tem um dado pessoal.",
        "Dados pessoais publicados ajudam golpistas a se passar pela pessoa ou pela família.",
        "Apague o dado antes de levar a memória ao mapa.");

    private static readonly Texto ReservaCodigo = new(
        "O código tinha um caminho por onde dados pessoais podiam vazar.",
        "Dados da família podiam chegar a um lugar que não é seguro.",
        "Fazer o dado passar pelo filtro de dados pessoais antes de sair.");

    /// <summary>Tipos de dado pessoal que têm texto próprio.</summary>
    public static IReadOnlyCollection<string> TiposConhecidos => PorTipo.Keys;

    /// <summary>Regras do Semgrep que têm texto próprio.</summary>
    public static IReadOnlyCollection<string> RegrasConhecidas => PorRegraDeCodigo.Keys;

    /// <summary>Risco real de um tipo de dado pessoal, para a IA usar como referência.</summary>
    public static string RiscoDoTipo(string tipo) =>
        PorTipo.TryGetValue(tipo, out Texto? texto) ? texto.RiscoReal : ReservaDadoPessoal.RiscoReal;

    /// <summary>Alertas locais para todas as falhas do pedido, na mesma ordem.</summary>
    public static IReadOnlyList<AlertaTraduzido> Traduzir(PedidoDeTraducao pedido) =>
        pedido.Falhas.Select(Traduzir).ToList();

    /// <summary>Alerta local de uma falha.</summary>
    public static AlertaTraduzido Traduzir(FalhaParaTraduzir falha)
    {
        string tipo = TipoDaFalha(falha);
        Texto texto = falha.Origem == OrigemCodigo
            ? PorRegraDeCodigo.GetValueOrDefault(falha.Rotulo, ReservaCodigo)
            : PorTipo.GetValueOrDefault(tipo, ReservaDadoPessoal);

        // Cada combinação tem a própria explicação no catálogo de regras ("Endereço junto com a rotina...").
        string riscoReal = falha.Origem != OrigemCodigo && tipo == TipoCombinacao
            ? falha.DescricaoTecnica
            : texto.RiscoReal;

        return new AlertaTraduzido(falha.Id, texto.Alerta, riscoReal, texto.OQueFazer);
    }

    /// <summary>
    /// Nas falhas de dado pessoal, o id do pedido é o próprio tipo ("Cpf", "Saude", "Combinacao-1").
    /// </summary>
    private static string TipoDaFalha(FalhaParaTraduzir falha)
    {
        int hifen = falha.Id.IndexOf('-', StringComparison.Ordinal);
        return hifen > 0 ? falha.Id[..hifen] : falha.Id;
    }
}
