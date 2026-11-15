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
/// é criado à parte, para aparecer também em bancos que já tinham os dados de demonstração.
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

    private const string NomeFamilia = "Família Souza";
    private const string ApelidoLourdes = "Vó Lourdes";

    public static async Task CriarAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        AppDbContext db = services.GetRequiredService<AppDbContext>();
        if (!await db.Membros.AnyAsync(cancellationToken))
        {
            await CriarFamiliaAsync(services, db, cancellationToken);
        }

        await CriarPortaRetratoAsync(services, db, cancellationToken);
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
