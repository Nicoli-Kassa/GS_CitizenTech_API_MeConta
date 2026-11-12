namespace MeConta.Infrastructure.Persistencia.Configuracoes;

/// <summary>
/// Categorias e perguntas que entram no banco pela migração (HasData).
/// As perguntas usam "você" para servir tanto para avôs quanto para avós.
/// </summary>
internal static class CatalogoInicial
{
    public const int Receitas = 1;
    public const int HistoriaDaFamilia = 2;
    public const int Oficios = 3;
    public const int OBairro = 4;
    public const int Conselhos = 5;

    public static readonly object[] Categorias =
    [
        new { Id = Receitas, Slug = "receitas", Nome = "Receitas" },
        new { Id = HistoriaDaFamilia, Slug = "historia-da-familia", Nome = "História da família" },
        new { Id = Oficios, Slug = "oficios", Nome = "Ofícios" },
        new { Id = OBairro, Slug = "o-bairro", Nome = "O bairro" },
        new { Id = Conselhos, Slug = "conselhos", Nome = "Conselhos" },
    ];

    public static readonly object[] Perguntas =
    [
        new { Id = 1, CategoriaId = Receitas, Texto = "Qual é o segredo do seu prato mais famoso?" },
        new { Id = 2, CategoriaId = Receitas, Texto = "Que receita você aprendeu com a sua mãe ou a sua avó?" },
        new { Id = 3, CategoriaId = Receitas, Texto = "O que não podia faltar na mesa nas festas de fim de ano?" },
        new { Id = 4, CategoriaId = Receitas, Texto = "Qual doce você fazia quando alguém da família fazia aniversário?" },
        new { Id = 5, CategoriaId = Receitas, Texto = "Qual comida te lembra a sua infância?" },

        new { Id = 6, CategoriaId = HistoriaDaFamilia, Texto = "Como você conheceu o amor da sua vida?" },
        new { Id = 7, CategoriaId = HistoriaDaFamilia, Texto = "Como eram os seus pais quando você era criança?" },
        new { Id = 8, CategoriaId = HistoriaDaFamilia, Texto = "De onde veio a nossa família antes de chegar aqui?" },
        new { Id = 9, CategoriaId = HistoriaDaFamilia, Texto = "Qual é a história mais engraçada que aconteceu na nossa família?" },
        new { Id = 10, CategoriaId = HistoriaDaFamilia, Texto = "Como foi o dia em que você saiu da casa dos seus pais?" },

        new { Id = 11, CategoriaId = Oficios, Texto = "Qual foi o seu primeiro trabalho e o que você fazia lá?" },
        new { Id = 12, CategoriaId = Oficios, Texto = "Que ofício você sabe fazer que quase ninguém mais faz?" },
        new { Id = 13, CategoriaId = Oficios, Texto = "Quem te ensinou a profissão que você teve?" },
        new { Id = 14, CategoriaId = Oficios, Texto = "Qual foi o dia mais marcante no seu trabalho?" },
        new { Id = 15, CategoriaId = Oficios, Texto = "Que ferramenta ou máquina você usava todo dia no trabalho?" },

        new { Id = 16, CategoriaId = OBairro, Texto = "Como era o bairro quando você chegou?" },
        new { Id = 17, CategoriaId = OBairro, Texto = "Qual loja ou padaria do bairro já não existe mais?" },
        new { Id = 18, CategoriaId = OBairro, Texto = "Como eram as festas juninas na sua rua?" },
        new { Id = 19, CategoriaId = OBairro, Texto = "Onde as crianças brincavam no bairro quando você era jovem?" },
        new { Id = 20, CategoriaId = OBairro, Texto = "Qual era o nome antigo de alguma rua daqui?" },

        new { Id = 21, CategoriaId = Conselhos, Texto = "Qual foi o melhor conselho que você já recebeu?" },
        new { Id = 22, CategoriaId = Conselhos, Texto = "O que você gostaria de ter sabido aos 20 anos?" },
        new { Id = 23, CategoriaId = Conselhos, Texto = "Que conselho você daria para quem está começando a trabalhar?" },
        new { Id = 24, CategoriaId = Conselhos, Texto = "Como você fazia para economizar quando as coisas estavam difíceis?" },
        new { Id = 25, CategoriaId = Conselhos, Texto = "O que é mais importante para manter uma família unida?" },
    ];
}
