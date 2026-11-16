# O que cada matéria faz nesta API

O Me Conta é uma solução só, dividida em partes. Esta API é o centro: o app, o porta-retrato e o servidor Linux conversam com ela, e o módulo de privacidade roda dentro dela. Este documento mostra, matéria por matéria, **qual é a parte dela no Me Conta** e **onde, dentro deste repositório, cada item de entrega é atendido**.

[← Voltar ao README](../README.md)

## Visão geral

| Matéria | Papel no Me Conta | Onde está | Situação |
| --- | --- | --- | --- |
| [C# Software Development](#c-software-development) | Web API em .NET 10 com domínio orientado a objetos, EF Core e LINQ | Este repositório (`src/`, `tests/`) | ✅ Pronta |
| [SOA e Web Services](#soa-e-web-services) | Serviços REST que o app e o porta-retrato consomem, com banco, migrações e documentação automática | Este repositório (`src/`, `tests/`, `docs/`) | ✅ Pronta |
| [Physical Computing (IoT)](#physical-computing-iot) | Porta-retrato com ESP32: acende quando chega pergunta e tem o botão "quero contar" | Rotas do aparelho nesta API; circuito no Wokwi; ponte Node-RED | 🟡 API pronta; circuito e Node-RED em andamento |
| [Cybersecurity](#cybersecurity) | Filtro de dados pessoais antes de publicar no mapa do bairro, taint analysis e painel "Risco Cidadão" | Módulo `MeConta.Privacidade` dentro desta API | 🟡 Motor de PII pronto; integração, taint, IA e painel em andamento |
| [Operating Systems](#operating-systems) | VM Linux que roda esta API, com monitoramento e logs de auditoria | Scripts Bash (pasta própria); esta API fornece o `/health` e a configuração por variável de ambiente | 🔜 A fazer |
| [Testing, Compliance & QA](#testing-compliance--qa) | Backlog com critérios de aceite e casos de teste no Azure DevOps | Azure DevOps; nesta API, os casos que rodam pelo Swagger | ✅ Pronta |
| [Mobile](#mobile) | App Expo que a neta usa: perguntar, enviar pelo WhatsApp e montar o caderno | Repositório do app; consome esta API | 🟡 Em andamento |
| [IA & ML](#ia--ml) | Modelo com dados reais da PNAD TIC que aponta quais idosos têm mais chance de estar desconectados | [Repositório próprio](https://github.com/Nicoli-Kassa/GS_CitizenTech_IA-ML_MeConta) | ✅ Pronta (fora desta API) |

---

## C# Software Development

**A parte dela no Me Conta:** toda a Web API em C# / .NET 10. É onde ficam as regras que fazem o Me Conta funcionar: só a família vê a família, o WhatsApp não se repete, a pergunta da semana nunca repete, o filtro anti-golpe, a resposta que vira página do caderno.

| Critério | Onde é atendido |
| --- | --- |
| **Programação orientada a objetos** | Abstração: [`IPergunta`](../src/MeConta.Domain/Entidades/IPergunta.cs), [`Pergunta`](../src/MeConta.Domain/Entidades/Pergunta.cs) e [`ArquivoDeMidia`](../src/MeConta.Domain/ValueObjects/ArquivoDeMidia.cs) abstratas. Herança: [`Entidade`](../src/MeConta.Domain/Comum/Entidade.cs) → todas as entidades; `Pergunta` → [`PerguntaCatalogo`](../src/MeConta.Domain/Entidades/PerguntaCatalogo.cs) / [`PerguntaDaFamilia`](../src/MeConta.Domain/Entidades/PerguntaDaFamilia.cs); `ArquivoDeMidia` → [`ArquivoDeAudio`](../src/MeConta.Domain/ValueObjects/ArquivoDeAudio.cs) / [`ArquivoDeFoto`](../src/MeConta.Domain/ValueObjects/ArquivoDeFoto.cs). Polimorfismo: `VisivelPara(familiaId)` e o template method dos arquivos. Encapsulamento: setters privados e coleções somente leitura em [`Familia`](../src/MeConta.Domain/Entidades/Familia.cs) e [`EnvioPergunta`](../src/MeConta.Domain/Entidades/EnvioPergunta.cs). Value objects: [`Telefone`](../src/MeConta.Domain/ValueObjects/Telefone.cs), [`Semana`](../src/MeConta.Domain/ValueObjects/Semana.cs). Detalhes no README, seção [Modelo de domínio e POO](../README.md#modelo-de-domínio-e-poo). |
| **Web API** | 10 controllers em [`src/MeConta.Api/Controllers`](../src/MeConta.Api/Controllers), todos em `/api/v1`, com códigos HTTP corretos (201, 204, 400, 401, 403, 404, 409, 413). Login com ASP.NET Identity e token Bearer ([`Program.cs`](../src/MeConta.Api/Program.cs)). Erros no padrão ProblemDetails ([`TratadorDeExcecoes`](../src/MeConta.Api/Configuracao/TratadorDeExcecoes.cs)). Tabela completa no README, seção [Endpoints](../README.md#endpoints). |
| **EF Core + LINQ** | [`AppDbContext`](../src/MeConta.Infrastructure/Persistencia/AppDbContext.cs) com Fluent API em [`Configuracoes/`](../src/MeConta.Infrastructure/Persistencia/Configuracoes), herança TPH nas perguntas e conversores de valor. LINQ em sintaxe de consulta (`from ... join ... select`) e de métodos (`Where`, `Include`, `Skip/Take`, `SumAsync`) em [`Repositorios.cs`](../src/MeConta.Infrastructure/Repositorios/Repositorios.cs). |
| **async/await** | Todo o acesso ao banco e aos arquivos é assíncrono, com `CancellationToken` do controller até o EF Core: controllers → [serviços](../src/MeConta.Application/Servicos) → [repositórios](../src/MeConta.Infrastructure/Repositorios) → [armazenamento em disco](../src/MeConta.Infrastructure/Adapters/ArmazenamentoEmDiscoAdapter.cs). |
| **Qualidade do código** | Arquitetura em 4 projetos com a regra de dependência garantida pelo compilador ([`MeConta.slnx`](../MeConta.slnx)); interfaces e injeção de dependência ([`InjecaoDeDependencia.cs`](../src/MeConta.Infrastructure/InjecaoDeDependencia.cs), [`RegistroDaAplicacao.cs`](../src/MeConta.Api/Configuracao/RegistroDaAplicacao.cs)); `nullable` ligado e avisos tratados ([`Directory.Build.props`](../Directory.Build.props)); classes `sealed`, comentários XML; relógio injetável (`TimeProvider`) para testar datas; 151 testes. |
| **README, Swagger/Scalar, evidências** | [README](../README.md) com Ficha, como executar e integrantes; `/swagger` e `/scalar`; prints em [`docs/evidencias`](evidencias). |

## SOA e Web Services

**A parte dela no Me Conta:** os serviços que o app mobile e o porta-retrato consomem. A API expõe contratos REST versionados (`/api/v1`), documentados em OpenAPI, guarda tudo no SQL Server e é a mesma para todos os clientes.

| Critério | Onde é atendido |
| --- | --- |
| **Domínio** | Projeto [`MeConta.Domain`](../src/MeConta.Domain), sem nenhum pacote externo: entidades, value objects, regras ([`FiltroAntiGolpe`](../src/MeConta.Domain/Regras/FiltroAntiGolpe.cs), [`SugestaoSemanal`](../src/MeConta.Domain/Regras/SugestaoSemanal.cs), [`FilaDoPortaRetrato`](../src/MeConta.Domain/Regras/FilaDoPortaRetrato.cs)) e exceções de negócio que viram 400, 409 e 413. Casos de uso em [`MeConta.Application/Servicos`](../src/MeConta.Application/Servicos), com DTOs em [`Dtos/`](../src/MeConta.Application/Dtos) para não expor as entidades. Tabela de regras no README, seção [Regras de negócio](../README.md#regras-de-negócio). |
| **Banco de dados** | SQL Server com EF Core 10 (LocalDB no desenvolvimento), 11 tabelas do Me Conta mais as do Identity, índices únicos (WhatsApp na família, chave de idempotência, código do porta-retrato). Diagrama no README, seção [Banco de dados e migrações](../README.md#banco-de-dados-e-migrações). Print: [`10-banco.png`](evidencias/10-banco.png). |
| **Migrações** | [`InitialCreate`](../src/MeConta.Infrastructure/Persistencia/Migrations/20261112234120_InitialCreate.cs) (modelo e catálogo de 25 perguntas via `HasData`) e [`PortaRetrato`](../src/MeConta.Infrastructure/Persistencia/Migrations/20261115195031_PortaRetrato.cs) (incremental), aplicadas ao subir a API ([`DbInicializador`](../src/MeConta.Infrastructure/Persistencia/DbInicializador.cs)). Print: [`11-migracoes.png`](evidencias/11-migracoes.png). |
| **Documentação automática** | OpenAPI em `/openapi/v1.json`, exibido no Swagger UI (`/swagger`) e no Scalar (`/scalar`), com título, descrição e esquema Bearer ([`DocumentacaoOpenApi`](../src/MeConta.Api/Configuracao/DocumentacaoOpenApi.cs)). Prints: [`01-swagger.png`](evidencias/01-swagger.png), [`02-scalar.png`](evidencias/02-scalar.png). |
| **Testes automatizados** | 151 testes xUnit em [`tests/MeConta.UnitTests`](../tests/MeConta.UnitTests): domínio, regras e serviços, com dublês escritos à mão. Print: [`12-testes.png`](evidencias/12-testes.png). Além deles, o [roteiro de testes](ROTEIRO_DE_TESTES.md) e o arquivo [`MeConta.Api.http`](../src/MeConta.Api/MeConta.Api.http) cobrem as 88 chamadas da API. |
| **GitHub, README, evidências** | Este repositório; [README](../README.md) com execução e integrantes; prints em [`docs/evidencias`](evidencias). |

## Physical Computing (IoT)

**A parte dela no Me Conta:** o porta-retrato Me Conta, para o idoso que ainda não usa bem o celular (degrau "Conectar", US15). Um anel de LEDs acende quando a família manda uma pergunta, a telinha mostra quem perguntou e o botão grande "quero contar" avisa a família no app.

```
App ──HTTP──> Me Conta API <──HTTP── Node-RED <──MQTT──> broker <──MQTT──> ESP32 (Wokwi)
```

| O que é pedido | Onde é atendido |
| --- | --- |
| **Circuito integrado à solução, sem projeto paralelo** | O aparelho usa os mesmos dados da família: pareamento ao idoso ([`PortaRetratoController`](../src/MeConta.Api/Controllers/PortaRetratoController.cs)), rotas do aparelho com chave própria ([`AparelhoPortaRetratoController`](../src/MeConta.Api/Controllers/AparelhoPortaRetratoController.cs): `estado`, `quero-contar`, `conexao`) e avisos que a família vê no app ([`AvisosController`](../src/MeConta.Api/Controllers/AvisosController.cs)). |
| **Regras do aparelho** | [`PortaRetrato`](../src/MeConta.Domain/Entidades/PortaRetrato.cs), [`AvisoQueroContar`](../src/MeConta.Domain/Entidades/AvisoQueroContar.cs), [`FilaDoPortaRetrato`](../src/MeConta.Domain/Regras/FilaDoPortaRetrato.cs) e [`AparelhoPortaRetratoService`](../src/MeConta.Application/Servicos/AparelhoPortaRetratoService.cs): só o hash da chave é guardado, "quero contar" não duplica, conectado = sinal de vida nos últimos 90 s. |
| **Banco** | Migração [`PortaRetrato`](../src/MeConta.Infrastructure/Persistencia/Migrations/20261115195031_PortaRetrato.cs) (tabelas `PortaRetratos` e `AvisosQueroContar`) e porta-retrato de demonstração da Vó Lourdes (código `k7m2q9xd`). |
| **Testes e evidências** | [`PortaRetratoTests`](../tests/MeConta.UnitTests/Domain/PortaRetratoTests.cs), [`FilaDoPortaRetratoTests`](../tests/MeConta.UnitTests/Regras/FilaDoPortaRetratoTests.cs), [`AparelhoPortaRetratoServiceTests`](../tests/MeConta.UnitTests/Servicos/AparelhoPortaRetratoServiceTests.cs), [`PortaRetratoServiceTests`](../tests/MeConta.UnitTests/Servicos/PortaRetratoServiceTests.cs); chamadas 67 a 88 do [`MeConta.Api.http`](../src/MeConta.Api/MeConta.Api.http) (Bloco K do [roteiro](ROTEIRO_DE_TESTES.md)); prints [13](evidencias/13-porta-retrato-estado.png) a [17](evidencias/17-porta-retrato-moldura-apagada.png). |
| **Circuito no Wokwi, ponte Node-RED, vídeo e integrantes** | Em andamento: firmware e `flows.json` vão para a pasta `iot/` deste repositório, junto com o link do Wokwi e o `integrantes.txt`. |

## Cybersecurity

**A parte dela no Me Conta:** proteger o idoso antes de uma memória sair da família e ir para o mapa do bairro (degrau "Expandir", US18). Um motor de regras procura dados pessoais (CPF, saúde, senhas, endereço, rotina da casa), o taint analysis garante que texto não verificado não chega a saídas inseguras, e um LLM explica o risco em linguagem simples no painel "Risco Cidadão".

| O que é pedido | Onde é atendido |
| --- | --- |
| **Mapeamento de PII (motor de regras)** | Pronto: projeto [`src/MeConta.Privacidade`](../src/MeConta.Privacidade) com 17 regras em [`regras-pii.json`](../src/MeConta.Privacidade/Regras/regras-pii.json), validadores de CPF, cartão e telefone, normalização de texto falado e índice Risco Cidadão; [testes](../tests/MeConta.UnitTests/Privacidade) com dois conjuntos de memórias simuladas; [como funciona e avaliação](cyber/motor-pii.md). Ligação com as rotas de publicação em andamento. |
| **Taint analysis** | Em integração: tipos que separam texto do usuário de texto verificado nas saídas (mapa do bairro, logs, LLM, porta-retrato, mensagens de erro), regras Semgrep em `seguranca/semgrep/` e verificação no GitHub Actions. |
| **Tradução via IA** | Em integração: explicações do risco geradas pelo Gemini, recebendo só texto já mascarado, com explicações locais quando não há chave. |
| **Dashboard "Risco Cidadão"** | Em integração: rota `GET /api/v1/risco-cidadao` e painel servido pela própria API. |
| **Proteções que a API já tem** | Dados mínimos do idoso (sem CPF, endereço ou saúde, [`IdosoDtos`](../src/MeConta.Application/Dtos/IdosoDtos.cs)); [`FiltroAntiGolpe`](../src/MeConta.Domain/Regras/FiltroAntiGolpe.cs) contra pedidos de PIX, senha e código; acesso só da família ([`ControleDeAcesso`](../src/MeConta.Application/Servicos/ControleDeAcesso.cs)); chave do porta-retrato sorteada com gerador criptográfico ([`GeradorDeCredenciais`](../src/MeConta.Infrastructure/Adapters/GeradorDeCredenciais.cs)) e guardada só como hash SHA-256 ([`PortaRetrato`](../src/MeConta.Domain/Entidades/PortaRetrato.cs)); erros sem detalhe interno ([`TratadorDeExcecoes`](../src/MeConta.Api/Configuracao/TratadorDeExcecoes.cs)); nome de arquivo nunca vira caminho ([`ArmazenamentoEmDiscoAdapter`](../src/MeConta.Infrastructure/Adapters/ArmazenamentoEmDiscoAdapter.cs)); dependência vulnerável `System.Security.Cryptography.Pkcs` atualizada para 10.0.13 ([`MeConta.Infrastructure.csproj`](../src/MeConta.Infrastructure/MeConta.Infrastructure.csproj)). |

## Operating Systems

**A parte dela no Me Conta:** o servidor Linux onde a API roda, provisionado por script Bash, com monitoramento de CPU, disco e memória e logs de auditoria.

| O que é pedido | O que esta API oferece |
| --- | --- |
| **Provisionamento da aplicação web** | A API sobe com um comando (`dotnet run` ou `dotnet publish`), escuta em qualquer interface pelo perfil `rede-local` ([`launchSettings.json`](../src/MeConta.Api/Properties/launchSettings.json)) e lê o banco da variável `ConnectionStrings__DefaultConnection`, sem editar arquivo. As migrações são aplicadas sozinhas na primeira subida. |
| **Monitoramento e teste de falha** | `GET /health` responde 200 com a API e o banco no ar e 503 quando o banco cai ([`Program.cs`](../src/MeConta.Api/Program.cs)), o que o serviço de monitoramento pode consultar. |
| **Logs de auditoria** | Erros inesperados já são registrados com método e caminho ([`TratadorDeExcecoes`](../src/MeConta.Api/Configuracao/TratadorDeExcecoes.cs)). |
| **VM, rede, DNS, monitoramento como serviço** | A fazer: scripts Bash em pasta própria. |

## Testing, Compliance & QA

**A parte dela no Me Conta:** garantir que o que foi combinado é o que foi feito. O backlog (3 épicos, 11 features, 18 user stories) e os 43 casos de teste estão no [Azure DevOps](https://dev.azure.com/GSCitizenTech/MeConta).

| O que é pedido | Ligação com esta API |
| --- | --- |
| **Features com descrição e critérios de aceite** | Cada critério de aceite virou uma regra no domínio, com a mesma mensagem de erro (README, seção [Regras de negócio](../README.md#regras-de-negócio)). A coluna "User story" da tabela de [Endpoints](../README.md#endpoints) liga cada rota à US do backlog. |
| **Casos de teste positivos e negativos** | Os casos que rodam na API (por exemplo CT04, CT25 e CT29, pelo Swagger) seguem o [roteiro de testes](ROTEIRO_DE_TESTES.md); os limites dos casos (60 e 61 caracteres, 20 MB, convite de 48 h) também estão nos testes automatizados. |
| **Compliance** | LGPD (dados mínimos, só a família vê, quem criou apaga) no README, seção [Privacidade e segurança](../README.md#privacidade-e-segurança). |

## Mobile

**A parte dela no Me Conta:** o app que a neta usa. O caderno da família é a primeira tela; a partir dele, ela pergunta, envia pelo WhatsApp e guarda a resposta.

| O que é pedido | Ligação com esta API |
| --- | --- |
| **Consumo de dados** | O app consome estas rotas (login, famílias, perguntas, envios, caderno, avisos) e usa um mock com as mesmas regras e mensagens quando a API não está configurada. |
| **Rodar no celular** | Perfil `rede-local` ([`launchSettings.json`](../src/MeConta.Api/Properties/launchSettings.json)) e CORS liberado ([`Program.cs`](../src/MeConta.Api/Program.cs)). |
| **Modo offline** | A API aceita a `ChaveIdempotencia` da página: o que o app guardou sem internet não duplica ao sincronizar ([`CadernoService`](../src/MeConta.Application/Servicos/CadernoService.cs)). |

## IA & ML

**A parte dela no Me Conta:** responder com dados reais da PNAD Contínua TIC 2025 quais idosos têm mais chance de estar desconectados, ou seja, onde o degrau "Conectar" é mais necessário. Não faz parte desta API; está no [repositório de IA & ML](https://github.com/Nicoli-Kassa/GS_CitizenTech_IA-ML_MeConta). Os números que ela gerou (9,1 milhões de idosos sem internet; 4,9 milhões moram com alguém que usa) aparecem no README, seção [O problema](../README.md#o-problema).
