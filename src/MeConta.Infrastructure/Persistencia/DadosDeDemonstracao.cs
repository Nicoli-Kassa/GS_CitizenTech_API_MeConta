using MeConta.Domain.Entidades;
using MeConta.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// Dados fictícios para demonstrar a API, baseados nas personas do projeto.
/// Nenhum dado é real: e-mails usam o domínio reservado example.com e o telefone é inventado.
/// Só roda se o banco ainda não tiver membros.
/// </summary>
public static class DadosDeDemonstracao
{
    public const string Senha = "MeConta@2026";
    public const string EmailBia = "bia@example.com";
    public const string EmailClaudia = "claudia@example.com";
    public const string CodigoConvite = "CASA26";

    public static async Task CriarAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        AppDbContext db = services.GetRequiredService<AppDbContext>();
        if (await db.Membros.AnyAsync(cancellationToken))
        {
            return;
        }

        UserManager<IdentityUser> usuarios = services.GetRequiredService<UserManager<IdentityUser>>();
        DateTime agora = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        // Bia (19, neta) cria a família; Cláudia (45, filha) entra pelo convite.
        Membro bia = await CriarMembroAsync(usuarios, db, EmailBia, "Bia", agora);
        Membro claudia = await CriarMembroAsync(usuarios, db, EmailClaudia, "Cláudia", agora);
        await db.SaveChangesAsync(cancellationToken);

        Familia familia = new("Família Souza", bia.Id, agora);
        familia.GerarConvite(CodigoConvite, bia.Id, agora);
        familia.AceitarConvite(CodigoConvite, claudia.Id, agora);

        // Dona Lourdes (71) usa WhatsApp; Seu Antônio (78) ainda não usa o celular.
        Idoso lourdes = familia.CadastrarIdoso("Lourdes Souza", "Vó Lourdes", new Telefone("(11) 90000-0001"), agora);
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
