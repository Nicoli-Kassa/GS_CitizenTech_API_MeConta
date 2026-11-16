# Registro de uso de IA

Este arquivo registra como usei inteligência artificial como ferramenta de apoio no backend do Me Conta, como pede o brief da Global Solution.

Defini o escopo, a arquitetura, as tecnologias e as regras de negócio a partir do backlog do projeto. Usei a IA como apoio para tirar dúvidas, acelerar a escrita de código e documentação e resolver problemas. Todo o conteúdo gerado com apoio da IA foi revisado, ajustado, compilado e testado por mim.

**Ferramenta:** Claude (Anthropic)
**Período:** 10 a 15/11/2026

---

## Resumo

| Etapa                    | Data          | Meu trabalho                                                                 | Apoio da IA                                                              |
| ------------------------ | ------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| 1. Planejamento          | 10/11         | Defini a arquitetura, o banco, o login, o escopo e quais user stories entram | Sugestões de organização do domínio e dos endpoints                      |
| 2. Domínio e testes      | 10/11         | Defini as regras a partir dos critérios de aceite, revisei e rodei os testes | Geração da primeira versão das classes e dos testes                      |
| 3. Banco, serviços e API | 12/11         | Gerei a migração, subi a API, testei os endpoints e conferi o banco          | Geração da primeira versão das camadas Application, Infrastructure e Api |
| 4. Documentação          | 12/11 e 14/11 | Revisei os textos, preenchi os dados, segui o roteiro e tirei os prints      | Estrutura do README e do roteiro de testes                               |
| 5. Problemas             | 14/11         | Investiguei os erros e apliquei as correções                                 | Explicação das causas                                                    |
| 6. Porta-retrato (IoT)   | 15/11         | Defini o papel do aparelho, gerei a migração, testei as rotas e tirei os prints | Primeira versão das entidades, serviços, rotas, testes e roteiro         |
| 7. Dependência vulnerável | 15/11        | Atualizei o pacote e confirmei o build sem avisos                            | Explicação do aviso e da versão corrigida                                |
| 8. Motor de PII (Cybersecurity) | 15/11   | Defini o que conta como dado de risco e o que bloqueia, revisei as regras e rodei os testes e a avaliação | Primeira versão do módulo, das regras, dos testes e dos conjuntos de memórias simuladas |

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

## 6. Porta-retrato Me Conta (IoT) · 15/11/2026

**O que eu precisava:** ligar à API o porta-retrato da solução (US15), uma caixinha com ESP32 que acende quando chega pergunta e tem um botão "quero contar", conversando com a API por meio de uma ponte Node-RED.

**Como usei a IA:** expliquei como o aparelho funciona e pedi apoio para gerar a primeira versão das entidades, dos serviços, das rotas, dos testes e da atualização do roteiro de testes.

**Minhas decisões:**

- O aparelho não usa o login da família: ele se identifica por uma chave própria, enviada no cabeçalho `X-Chave-PortaRetrato`, e a API guarda só o hash dela.
- A chave só dá acesso ao estado do próprio idoso e ao botão "quero contar". Para o aparelho vai só o apelido, o primeiro nome de quem perguntou e o texto da pergunta.
- O aviso "quero contar" fica ligado ao idoso, e a família vê os avisos no app.

**Resultado:**

- Entidades `PortaRetrato` e `AvisoQueroContar`, a regra `FilaDoPortaRetrato` e os serviços `PortaRetratoService`, `AparelhoPortaRetratoService` e `AvisoService`.
- Rotas `/api/v1/porta-retrato/estado`, `/quero-contar` e `/conexao` para o aparelho, e as rotas de pareamento e de avisos para a família.
- Testes unitários dos serviços com repositórios em memória: o total passou de 103 para 151.
- Bloco K do roteiro de testes (passos 67 a 88) e o arquivo `.http` com todas as chamadas.

**Meu trabalho de validação:**

- Gerei a migração com `dotnet ef migrations add PortaRetrato` e rodei `dotnet build` e `dotnet test`: 151 testes aprovados.
- Testei as rotas pelo Swagger: moldura acesa, "quero contar", moldura apagada, chave inválida (401) e avisos para a família, e tirei os prints de `docs/evidencias/`.

---

## 7. Dependência vulnerável · 15/11/2026

| Problema | Como a IA ajudou | O que eu fiz |
| --- | --- | --- |
| O build mostrava os avisos NU1902 e NU1903 no pacote `System.Security.Cryptography.Pkcs` 9.0.11, usado indiretamente pelo projeto | Explicou que a versão tinha falhas de negação de serviço publicadas e qual versão já trazia a correção | Adicionei `System.Security.Cryptography.Pkcs` 10.0.13 na Infrastructure e confirmei o build sem avisos |

---

## 8. Motor de dados pessoais (Cybersecurity) · 15/11/2026

**O que eu precisava:** um motor que procure dados pessoais numa memória antes de ela sair do caderno da família e ir para o mapa do bairro (US18), com regras que possam ser acrescentadas sem mexer no código e um índice "Risco Cidadão" para o painel.

**Como usei a IA:** expliquei o problema, os dados que preocupam (CPF, saúde, senhas e o que expõe a rotina de uma pessoa idosa) e o fato de as respostas chegarem em áudio. Pedi apoio para gerar a primeira versão do módulo, das regras, dos testes e de dois conjuntos de memórias simuladas para medir os acertos.

**Minhas decisões:**

- O módulo fica dentro da solução da API (`src/MeConta.Privacidade`), sem pacote externo, para rodar onde o dado entra.
- As regras ficam num arquivo JSON: dado novo = regra nova no arquivo.
- Três níveis: risco alto bloqueia a publicação (CPF, RG, cartão, dados bancários, PIX, senha, código de verificação); risco médio pede confirmação da família (saúde, telefone, e-mail, endereço, CEP, data de nascimento, rotina da casa).
- Na dúvida, bloquear: texto grande demais ou análise que passa do tempo não é liberado.
- O resultado nunca guarda o dado em claro, só o trecho mascarado.
- A avaliação mostra também os erros da primeira rodada no conjunto de validação, e não só o resultado depois dos ajustes.

**Resultado:**

- Projeto `MeConta.Privacidade`: normalização (acentos, caracteres invisíveis, números falados), 17 regras em `regras-pii.json`, validadores de CPF, cartão e telefone, combinações de risco, índice Risco Cidadão e os tipos `TextoDoUsuario` e `TextoVerificado`.
- 117 testes novos em `tests/MeConta.UnitTests/Privacidade`, incluindo os casos CT41 a CT43 do backlog.
- Conjuntos de 64 e 30 memórias simuladas com gabarito, e a ferramenta `tools/MeConta.Privacidade.Avaliacao`, que gera a tabela de precisão e recall.
- Documentação em `docs/cyber/motor-pii.md`.

**Meu trabalho de validação:**

- Rodei `dotnet build` e `dotnet test`: 268 testes aprovados.
- Rodei a ferramenta de avaliação e conferi as tabelas do `docs/cyber/motor-pii.md`.
- Testei frases do dia a dia que não podem gerar alerta, como "panela de pressão" e "me conta 1950".

---

## Responsabilidade

Todo o código e a documentação foram revisados, executados e testados por mim antes da entrega. Nenhum dado pessoal real foi enviado à IA nem usado no projeto.
