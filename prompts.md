# Registro de uso de IA

Este arquivo registra como usei inteligência artificial como ferramenta de apoio no backend do Me Conta, como pede o brief da Global Solution.

Defini o escopo, a arquitetura, as tecnologias e as regras de negócio a partir do backlog do projeto. Usei a IA como apoio para tirar dúvidas, acelerar a escrita de código e documentação e resolver problemas. Todo o conteúdo gerado com apoio da IA foi revisado, ajustado, compilado e testado por mim.

**Ferramenta:** Claude (Anthropic)
**Período:** 10 a 14/11/2026

---

## Resumo

| Etapa                    | Data          | Meu trabalho                                                                 | Apoio da IA                                                              |
| ------------------------ | ------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| 1. Planejamento          | 10/11         | Defini a arquitetura, o banco, o login, o escopo e quais user stories entram | Sugestões de organização do domínio e dos endpoints                      |
| 2. Domínio e testes      | 10/11         | Defini as regras a partir dos critérios de aceite, revisei e rodei os testes | Geração da primeira versão das classes e dos testes                      |
| 3. Banco, serviços e API | 12/11         | Gerei a migração, subi a API, testei os endpoints e conferi o banco          | Geração da primeira versão das camadas Application, Infrastructure e Api |
| 4. Documentação          | 12/11 e 14/11 | Revisei os textos, preenchi os dados, segui o roteiro e tirei os prints      | Estrutura do README e do roteiro de testes                               |
| 5. Problemas             | 14/11         | Investiguei os erros e apliquei as correções                                 | Explicação das causas                                                    |

---

## 1. Planejamento do backend · 10/11/2026

**O que eu precisava:** organizar uma Web API que atendesse às disciplinas C# Software Development e SOA e Web Services, a partir do backlog do Me Conta (US01 a US18).

**Como usei a IA:** enviei o brief, a Ficha da CitizenTech, o backlog e os critérios de avaliação, e pedi sugestões de entidades, estrutura de pastas e endpoints.

**Minhas decisões:**

- Uma única API para as duas disciplinas, num só repositório, organizada em quatro camadas (Domain, Application, Infrastructure, Api).
- SQL Server como banco, com LocalDB no desenvolvimento.
- ASP.NET Identity para o login, com token Bearer.
- xUnit nos testes, com dublês escritos à mão.
- Documentação automática em Swagger UI e Scalar.
- Mapa do bairro (US16, US17) e filtro de dados pessoais (US18) fora desta entrega, por prazo.

---

## 2. Domínio e testes unitários · 10/11/2026

**O que eu precisava:** transformar os critérios de aceite do backlog em regras do domínio, com testes.

**Como usei a IA:** pedi apoio para gerar a primeira versão das entidades, value objects, regras e testes unitários, seguindo as regras e as mensagens de erro do backlog.

**Resultado:**

- Entidades `Familia`, `Membro`, `Participacao`, `Convite`, `Idoso`, `Categoria`, `Pergunta` (abstrata) com `PerguntaCatalogo` e `PerguntaDaFamilia`, `EnvioPergunta` e `PaginaCaderno`.
- Value objects `Telefone`, `Semana` e `ArquivoDeMidia` (abstrato), com `ArquivoDeAudio` e `ArquivoDeFoto`.
- Regras com interface: `FiltroAntiGolpe` e `SugestaoSemanal`.
- Testes unitários dos critérios de aceite das US01 a US08, US11 e US14.

**Meu trabalho de validação:**

- Rodei `dotnet build` e `dotnet test`: 103 testes aprovados, nenhuma falha.
- Conferi se as mensagens de erro batiam com o backlog (ex.: "Dê um nome para a família", "Esse número já está na família").
- Estudei onde cada pilar de POO aparece no código: herança em `Pergunta` e `ArquivoDeMidia`, polimorfismo em `VisivelPara`, encapsulamento nos setters privados e nas coleções somente leitura.

---

## 3. Banco de dados, serviços e API · 12/11/2026

**O que eu precisava:** ligar o domínio ao banco SQL Server e expor os casos de uso como API REST documentada.

**Como usei a IA:** pedi apoio para gerar a primeira versão das camadas de aplicação, infraestrutura e API, seguindo a arquitetura definida na etapa 1.

**Resultado:**

- **Application:** serviços com interfaces (`FamiliaService`, `IdosoService`, `PerguntaService`, `EnvioService`, `CadernoService`, `IndicadorService`, `ControleDeAcesso`), DTOs e contratos de repositório e de adapters.
- **Infrastructure:** `AppDbContext` com Identity e configurações Fluent API (herança TPH, relação N:N, índices únicos). Também o catálogo de perguntas via `HasData`, os repositórios, as consultas LINQ e o armazenamento de arquivos.
- **Api:** controllers, login com `MapIdentityApi`, erros no formato ProblemDetails, OpenAPI com Swagger UI e Scalar, e a rota `/health`.

**Meu trabalho de validação:**

- Gerei a migração inicial com `dotnet ef migrations add InitialCreate` e subi a API, que criou o banco e aplicou a migração.
- Testei os endpoints pelo Swagger com a conta de demonstração, incluindo os casos de erro (400, 403, 404, 409).
- Conferi no banco as tabelas criadas e o catálogo de perguntas.

---

## 4. Documentação e roteiro de testes · 12/11 e 14/11/2026

**O que eu precisava:** documentar a solução e organizar os testes manuais e as evidências.

**Como usei a IA:** pedi apoio para estruturar o README (a partir da minha Ficha da CitizenTech e do backlog), um roteiro com todas as requisições e o arquivo `.http` para testar no Visual Studio.

**Meu trabalho:**

- Revisei os textos do `README.md`, preenchi meu RM e os links do repositório e do pitch.
- Segui o `docs/ROTEIRO_DE_TESTES.md` requisição por requisição e tirei os prints de `docs/evidencias/`.
- Usei somente dados fictícios nos testes (e-mails `@example.com` e telefones inventados).

---

## 5. Problemas resolvidos · 14/11/2026

| Problema                                            | Como a IA ajudou                                                                         | O que eu fiz                                                                   |
| --------------------------------------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| Swagger devolvia **401** depois do login            | Explicou que o token estava sendo enviado com aspas (`Bearer "CfDJ8..."`)                | Corrigi o token no **Authorize**, sem aspas, e a chamada passou a devolver 200 |
| Ver o banco e as migrações sem o Visual Studio      | Indicou a extensão MSSQL do VS Code e os comandos `dotnet ef migrations list` e `script` | Instalei a extensão e conectei ao banco                                        |
| Erro de conexão no VS Code ("server was not found") | Apontou que o nome do servidor estava no campo errado                                    | Corrigi para `(localdb)\MSSQLLocalDB` e conectei ao banco `MeConta`            |
| Revisão final da entrega                            | Sugeriu uma lista de checagem dos critérios de C# e SOA                                  | Usei a lista para revisar a entrega antes de enviar                            |

---

## Responsabilidade

Todo o código e a documentação foram revisados, executados e testados por mim antes da entrega. Nenhum dado pessoal real foi enviado à IA nem usado no projeto.
