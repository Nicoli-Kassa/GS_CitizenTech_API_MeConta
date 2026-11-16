using MeConta.Application.Adapters;
using MeConta.Domain.Entidades;
using MeConta.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// Dados fictícios para demonstrar a API, baseados nas personas do projeto.
/// Nenhum dado é real: e-mails usam o domínio reservado example.com e o telefone é inventado.
/// A família só é criada se o banco ainda não tiver membros; o porta-retrato da Vó Lourdes
/// e as páginas para o filtro de dados pessoais são criados à parte, para aparecerem também
/// em bancos que já tinham os dados de demonstração.
/// </summary>
public static class DadosDeDemonstracao
{
    public const string Senha = "MeConta@2026";
    public const string EmailBia = "bia@example.com";
    public const string EmailClaudia = "claudia@example.com";
    public const string CodigoConvite = "CASA26";

    /// <summary>Código público do porta-retrato da Vó Lourdes (vai nos tópicos MQTT).</summary>
    public const string CodigoPortaRetrato = "k7m2q9xd";

    /// <summary>
    /// Chave fixa do porta-retrato de demonstração, usada no fluxo do Node-RED.
    /// Fora da demonstração, a chave é sorteada no pareamento e mostrada uma vez só.
    /// </summary>
    public const string ChavePortaRetrato = "demo-porta-retrato-vo-lourdes-2026";

    /// <summary>
    /// Páginas de demonstração para o filtro de dados pessoais (US18), com transcrições fictícias.
    /// Uma de cada tipo: sem dado pessoal, com CPF, com dado de saúde e com endereço + rotina da casa.
    /// O CPF é um número de teste (123.456.789-09), não pertence a ninguém.
    /// </summary>
    private static readonly (Guid Chave, string Titulo, string Transcricao)[] PaginasParaOFiltro =
    [
        (new Guid("6d1f0a3e-5b1c-4c43-9a51-000000000001"),
            "O bolo de domingo",
            "Três ovos caipiras, uma xícara de açúcar e manteiga da venda do seu Joaquim. A panela de pressão fica de fora: bolo é no forno."),
        (new Guid("6d1f0a3e-5b1c-4c43-9a51-000000000002"),
            "A fábrica de tecidos",
            "Entrei na fábrica em 1962. Pediram meu CPF, 123.456.789-09, e a carteira de trabalho. Eu tinha dezesseis anos."),
        (new Guid("6d1f0a3e-5b1c-4c43-9a51-000000000003"),
            "Os chás da vó",
            "Hoje tomo remédio para pressão, mas o chá de erva-cidreira continua todo fim de tarde."),
        (new Guid("6d1f0a3e-5b1c-4c43-9a51-000000000004"),
            "A casa da Rua das Flores",
            "Morei na Rua das Flores, 123 a vida inteira. Hoje moro sozinha e fico sozinha a tarde toda, mas a vizinha passa aqui."),
    ];

    private const string NomeFamilia = "Família Souza";
    private const string ApelidoLourdes = "Vó Lourdes";

    /// <param name="services">Serviços da API.</param>
    /// <param name="paginasDoFiltro">
    /// Cria também as quatro páginas para o filtro de dados pessoais (configuração "Banco:PaginasDoFiltroDePrivacidade").
    /// Fica desligado por padrão para não mudar os números do roteiro de testes dos blocos A a K.
    /// </param>
    /// <param name="cancellationToken">Cancelamento.</param>
    public static async Task CriarAsync(
        IServiceProvider services,
        bool paginasDoFiltro = false,
        CancellationToken cancellationToken = default)
    {
        AppDbContext db = services.GetRequiredService<AppDbContext>();
        if (!await db.Membros.AnyAsync(cancellationToken))
        {
            await CriarFamiliaAsync(services, db, cancellationToken);
        }

        await CriarPortaRetratoAsync(services, db, cancellationToken);
        if (paginasDoFiltro)
        {
            await CriarPaginasParaOFiltroAsync(services, db, cancellationToken);
        }
    }

    private static async Task CriarFamiliaAsync(
        IServiceProvider services,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        UserManager<IdentityUser> usuarios = services.GetRequiredService<UserManager<IdentityUser>>();
        DateTime agora = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        // Bia (19, neta) cria a família; Cláudia (45, filha) entra pelo convite.
        Membro bia = await CriarMembroAsync(usuarios, db, EmailBia, "Bia", agora);
        Membro claudia = await CriarMembroAsync(usuarios, db, EmailClaudia, "Cláudia", agora);
        await db.SaveChangesAsync(cancellationToken);

        Familia familia = new(NomeFamilia, bia.Id, agora);
        familia.GerarConvite(CodigoConvite, bia.Id, agora);
        familia.AceitarConvite(CodigoConvite, claudia.Id, agora);

        // Dona Lourdes (71) usa WhatsApp; Seu Antônio (78) ainda não usa o celular.
        Idoso lourdes = familia.CadastrarIdoso("Lourdes Souza", ApelidoLourdes, new Telefone("(11) 90000-0001"), agora);
        familia.CadastrarIdoso("Antônio Souza", "Vô Antônio", null, agora);

        db.Familias.Add(familia);
        await db.SaveChangesAsync(cancellationToken);

        // Uma pergunta já enviada, aguardando a resposta em áudio.
        PerguntaCatalogo primeira = await db.Set<PerguntaCatalogo>()
            .OrderBy(pergunta => pergunta.Id)
            .FirstAsync(cancellationToken);
        db.Envios.Add(new EnvioPergunta(lourdes, primeira, bia.Id, agora));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Porta-retrato da Vó Lourdes (US15), pareado pela Bia, com código e chave fixos.
    /// Só cria se ainda não houver nenhum porta-retrato e a Vó Lourdes da Família Souza existir.
    /// </summary>
    private static async Task CriarPortaRetratoAsync(
        IServiceProvider services,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (await db.PortaRetratos.AnyAsync(cancellationToken))
        {
            return;
        }

        Idoso? lourdes = await (
                from idoso in db.Idosos
                join familia in db.Familias on idoso.FamiliaId equals familia.Id
                where familia.Nome == NomeFamilia && idoso.Apelido == ApelidoLourdes
                select idoso)
            .FirstOrDefaultAsync(cancellationToken);
        Membro? bia = await db.Membros.FirstOrDefaultAsync(membro => membro.Nome == "Bia", cancellationToken);

        if (lourdes is null || bia is null)
        {
            return;
        }

        DateTime agora = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        db.PortaRetratos.Add(new PortaRetrato(lourdes, CodigoPortaRetrato, ChavePortaRetrato, bia.Id, agora));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Quatro páginas respondidas da Vó Lourdes, com transcrição, para testar o filtro de dados pessoais
    /// e o mapa do bairro sem precisar gravar áudio. Só cria se ainda não existirem (chaves fixas).
    /// </summary>
    private static async Task CriarPaginasParaOFiltroAsync(
        IServiceProvider services,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        Guid[] chaves = PaginasParaOFiltro.Select(pagina => pagina.Chave).ToArray();
        if (await db.Paginas.AnyAsync(pagina => chaves.Contains(pagina.ChaveIdempotencia), cancellationToken))
        {
            return;
        }

        Idoso? lourdes = await (
                from idoso in db.Idosos
                join familia in db.Familias on idoso.FamiliaId equals familia.Id
                where familia.Nome == NomeFamilia && idoso.Apelido == ApelidoLourdes
                select idoso)
            .FirstOrDefaultAsync(cancellationToken);
        Membro? bia = await db.Membros.FirstOrDefaultAsync(membro => membro.Nome == "Bia", cancellationToken);

        // Uma pergunta diferente do catálogo para cada página (a primeira já foi usada no envio inicial).
        List<PerguntaCatalogo> perguntas = await db.Set<PerguntaCatalogo>()
            .OrderBy(pergunta => pergunta.Id)
            .Skip(1)
            .Take(PaginasParaOFiltro.Length)
            .ToListAsync(cancellationToken);

        if (lourdes is null || bia is null || perguntas.Count < PaginasParaOFiltro.Length)
        {
            return;
        }

        IArmazenamentoDeArquivos armazenamento = services.GetRequiredService<IArmazenamentoDeArquivos>();
        DateTime agora = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        byte[] audio = AudioDeDemonstracao.Bytes;

        for (int i = 0; i < PaginasParaOFiltro.Length; i++)
        {
            (Guid chave, string titulo, string transcricao) = PaginasParaOFiltro[i];

            EnvioPergunta envio = new(lourdes, perguntas[i], bia.Id, agora);
            db.Envios.Add(envio);

            await using MemoryStream conteudo = new(audio);
            string caminho = await armazenamento.SalvarAsync(
                conteudo,
                new ArquivoDeAudio(AudioDeDemonstracao.NomeArquivo, audio.Length),
                cancellationToken);

            PaginaCaderno pagina = envio.Responder(
                titulo,
                caminho,
                AudioDeDemonstracao.DuracaoSegundos,
                fotoCaminho: null,
                bia.Id,
                chave,
                agora);
            pagina.DefinirTranscricao(transcricao);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Membro> CriarMembroAsync(
        UserManager<IdentityUser> usuarios,
        AppDbContext db,
        string email,
        string nome,
        DateTime agora)
    {
        IdentityUser usuario = new() { UserName = email, Email = email, EmailConfirmed = true };
        IdentityResult resultado = await usuarios.CreateAsync(usuario, Senha);
        if (!resultado.Succeeded)
        {
            string erros = string.Join("; ", resultado.Errors.Select(erro => erro.Description));
            throw new InvalidOperationException($"Não foi possível criar o usuário de demonstração: {erros}");
        }

        Membro membro = new(usuario.Id, nome, agora);
        db.Membros.Add(membro);
        return membro;
    }
}
