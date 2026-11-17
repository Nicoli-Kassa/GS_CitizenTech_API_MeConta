# Taint analysis do Me Conta

Todo texto que a família escreve no Me Conta (título, transcrição, pergunta, bairro, nome, apelido) pode conter um dado pessoal: um CPF ditado no áudio, um telefone, um remédio, o endereço de quem mora sozinha. A taint analysis rastreia esse texto desde a entrada até as **saídas inseguras** (logs, porta-retrato, IA, mapa do bairro) e garante que ele só chega lá depois de passar pelo [motor de dados pessoais](motor-pii.md).

```
FONTE (texto do usuário)  ──►  HIGIENIZADOR (motor de PII)  ──►  SAÍDA INSEGURA
título, transcrição,           Verificar / VerificarCampos       log do servidor
pergunta, bairro, nome,        Analisar → TextoSeguro            porta-retrato (broker MQTT público)
apelido, caminho da URL        ConteudoParaPublicar              IA (Gemini)
                                                                 mapa do bairro (público)
```

A proteção tem três camadas: o compilador, a análise estática e os testes.

## 1. Camada de tipos: o compilador não deixa pular o filtro

| Tipo | O que faz |
| --- | --- |
| [`TextoDoUsuario`](../../src/MeConta.Privacidade/Taint/TextosRastreados.cs) | Texto contaminado. O `ToString()` devolve só `[texto do usuário não verificado]`: se alguém o colocar num log por engano, o conteúdo não aparece. |
| [`TextoVerificado`](../../src/MeConta.Privacidade/Taint/TextosRastreados.cs) | Só o motor cria. `ParaRegistro` é a versão mascarada (para log e IA); `ConteudoParaPublicar` devolve o original só se a análise permitir e lança `PublicacaoBloqueadaException` se não. |
| [`CamposVerificados`](../../src/MeConta.Privacidade/Taint/TextosRastreados.cs) | O mesmo para vários campos juntos (título e transcrição): um CPF na transcrição bloqueia o título também. |

O construtor de `TextoVerificado` e de `CamposVerificados` é `internal`: fora do módulo `MeConta.Privacidade`, a única forma de obter um é chamar o motor.

## 2. Análise estática: Semgrep em modo taint

As regras ficam em [`seguranca/semgrep/`](../../seguranca/semgrep) e rodam a cada push no [GitHub Actions](../../.github/workflows/seguranca.yml).

| Regra | Saída protegida | Por que é insegura | Referência |
| --- | --- | --- | --- |
| `meconta-pii-para-log` | `ILogger`, `Console`, `File.Write*` | o log fica no servidor e é lido por quem cuida dele | CWE-532; LGPD art. 46 |
| `meconta-pii-para-porta-retrato` | respostas do serviço do aparelho e publicação MQTT | o porta-retrato recebe a pergunta por um broker MQTT público | CWE-319; LGPD art. 6º, III e VII |
| `meconta-pii-para-ia` | `HttpClient` e o explicador de risco | o plano gratuito do Gemini pode usar o que recebe | CWE-201; LGPD art. 33 |
| `meconta-pii-para-mapa-do-bairro` | `new MemoriaDoBairro(...)` (bairro, título, transcrição) | o mapa é aberto para qualquer pessoa | CWE-359; LGPD arts. 7º, I e 11 |
| `meconta-texto-bruto-fora-do-motor` | uso de `.ValorBruto` fora do módulo de privacidade | é o atalho para pular o higienizador | — |
| `meconta-chave-gemini-no-codigo` | chave do Gemini (`AIza…` ou `AQ.…`) em qualquer arquivo | segredo no GitHub | CWE-798 |
| `meconta-chave-no-appsettings` | chave ou senha preenchida no `appsettings*.json` | o arquivo vai para o GitHub | CWE-798 |

- **Fontes:** propriedades com texto do usuário (`Titulo`, `Transcricao`, `TextoPergunta`, `Bairro`, `Apelido`, `Nome`, `Email`, `Telefone`, `Mensagem`, `Texto`, `ValorBruto`) e partes da requisição HTTP (`Request.Path`, `Query`, `Form`, `Body`).
- **Higienizadores:** `Verificar`, `VerificarCampos`, `Analisar`, `ParaRegistro`, `TextoSeguro`, `ConteudoParaPublicar`, `Mascarador.Mascarar`; e `.Length`/`.Count`, que não carregam o conteúdo. No porta-retrato, também `FilaDoPortaRetrato.PrimeiroNome` (do nome de quem perguntou, vai só o primeiro nome).
- **As próprias regras são testadas.** Os arquivos [`me-conta-taint.cs`](../../seguranca/semgrep/me-conta-taint.cs) e [`me-conta-segredos.txt`](../../seguranca/semgrep/me-conta-segredos.txt) têm casos marcados com `ruleid` (a regra precisa apontar) e `ok` (não pode apontar): 16 vazamentos e 11 usos corretos. Resultado: [`7/7: All tests passed`](../../seguranca/relatorios/teste-das-regras.txt).

## 3. Auditoria do protótipo: o que a análise encontrou

A primeira rodada foi feita no código que já estava no GitHub (commit `07c2855`, com o motor e o mapa do bairro prontos). Ela encontrou **5 vazamentos reais em 2 arquivos**, que nenhum teste tinha apontado.

| # | Onde | O que vazava | Risco |
| --- | --- | --- | --- |
| 1 | [`TratadorDeExcecoes.cs`](../../src/MeConta.Api/Configuracao/TratadorDeExcecoes.cs), linha 36 | O log de erro gravava o **caminho da URL**. Em `/api/v1/bairros/{bairro}/memorias`, o caminho leva o texto digitado pela família. | Um endereço digitado no lugar do bairro iria parar no log do servidor. |
| 2 a 5 | [`AparelhoPortaRetratoService.cs`](../../src/MeConta.Application/Servicos/AparelhoPortaRetratoService.cs), linhas 54, 61, 104 e 111 | O texto da **pergunta escrita pela família** e o apelido iam crus para o porta-retrato, que os recebe por um **broker MQTT público** (`test.mosquitto.org`). | "Vó, me liga no (11) 98765-4321" ou "a senhora ainda toma o remédio da pressão?" ficaria legível para qualquer pessoa inscrita no tópico. |

Relatório completo: [`antes-semgrep.txt`](../../seguranca/relatorios/antes-semgrep.txt) (e em SARIF: [`antes-semgrep.sarif`](../../seguranca/relatorios/antes-semgrep.sarif)).

**Correções:**

1. O log de erro passou a registrar o **modelo da rota** (`api/v1/bairros/{bairro}/memorias`), e não o caminho pedido.
2. O serviço do aparelho passa a pergunta e o apelido pelo motor e manda só a versão segura. Exemplo testado em [`AparelhoPortaRetratoServiceTests`](../../tests/MeConta.UnitTests/Servicos/AparelhoPortaRetratoServiceTests.cs): "Vó, a senhora ainda toma remédio para pressão? Me liga no (11) 98765-4321" chega à telinha como "Vó, a senhora ainda toma [dado de saúde removido]? Me liga no [telefone removido]".

**Depois das correções: 0 achados** em 163 arquivos ([`depois-semgrep.txt`](../../seguranca/relatorios/depois-semgrep.txt)).

## 4. Em tempo de execução

| Saída | O que acontece com um CPF na transcrição |
| --- | --- |
| Mapa do bairro | Publicação recusada com **422**, mesmo com a confirmação da família (passo 94 do [roteiro](../ROTEIRO_DE_TESTES.md), print [`19-publicacao-bloqueada.png`](../evidencias/19-publicacao-bloqueada.png)). |
| Resposta da API | Só o trecho mascarado (`***.***.***-09`) e a versão segura ("[CPF removido]"). |
| Histórico do painel | Só tipo, nível e trecho mascarado ([`RegistroDePrivacidade`](../../src/MeConta.Domain/Entidades/RegistroDePrivacidade.cs)). |
| Log do servidor | Linha de auditoria só com ids e o trecho mascarado ([`AuditoriaEmLog`](../../src/MeConta.Api/Adapters/AuditoriaEmLog.cs)): `Privacidade: Publicacao da página 4 por membro 1 → Bloqueada, Risco Cidadão 70, dados: CPF ***.***.***-09`. |
| Porta-retrato | Pergunta com o dado trocado pelo aviso ("[CPF removido]"). |

Os testes [`PrivacidadeServiceTests`](../../tests/MeConta.UnitTests/Servicos/PrivacidadeServiceTests.cs) conferem que nenhum evento de auditoria e nenhum registro do histórico carregam título, transcrição ou dado em claro.

## 5. Segredos

- A chave do Gemini fica no **user-secrets** do Windows (`dotnet user-secrets set "Gemini:ChaveApi" ...`), fora do repositório; o `MeConta.Api.csproj` guarda só o `UserSecretsId`, que não é segredo.
- **TruffleHog** procura chaves e senhas em todo o histórico do git a cada push. Rodada de 16/11 nos 16 commits: **0 segredos** ([`trufflehog.txt`](../../seguranca/relatorios/trufflehog.txt)).
- As regras `meconta-chave-*` conhecem os dois formatos de chave do Gemini e o `appsettings`.

## 6. Pipeline (DevSecOps)

[`.github/workflows/seguranca.yml`](../../.github/workflows/seguranca.yml) roda a cada push e pull request:

| Etapa | Ferramenta | Se falhar |
| --- | --- | --- |
| Testar as regras | `semgrep --test seguranca/semgrep` | pipeline vermelho |
| Regras do Me Conta | `semgrep scan --config seguranca/semgrep --error` | pipeline vermelho |
| Regras públicas de C# | `semgrep scan --config p/csharp` | só relatório (artefato `relatorios-semgrep`) |
| Segredos no histórico | TruffleHog (`--fail`) | pipeline vermelho |
| Build e testes | `dotnet test` | pipeline vermelho |

Para quem desenvolve no Linux, no macOS ou no WSL, o [`.pre-commit-config.yaml`](../../.pre-commit-config.yaml) roda o Semgrep e o TruffleHog antes de cada commit (`pip install pre-commit` e depois `pre-commit install`).

### Ver o pipeline barrando um vazamento

1. Crie um ramo: `git switch -c demo/vazamento`.
2. Em [`AparelhoPortaRetratoService.cs`](../../src/MeConta.Application/Servicos/AparelhoPortaRetratoService.cs), troque `motor.Verificar(new TextoDoUsuario(envio.TextoPergunta)).ParaRegistro,` por `envio.TextoPergunta,` (dentro de `ObterEstadoAsync`).
3. `git commit -am "Demonstração: pergunta sem filtro para o porta-retrato"` e `git push -u origin demo/vazamento`.
4. Na aba **Actions** do GitHub, o job **Taint analysis** fica vermelho apontando a linha, e o job **Build e testes** também falha no teste da pergunta com telefone.
5. Volte para o `main` (`git switch main`) e apague o ramo (`git push origin --delete demo/vazamento`).

## Rodar no computador

Com Docker:

```bash
docker run --rm -v "$PWD:/src" semgrep/semgrep semgrep --test seguranca/semgrep
docker run --rm -v "$PWD:/src" semgrep/semgrep semgrep scan --config seguranca/semgrep --error
docker run --rm -v "$PWD:/repo" trufflesecurity/trufflehog git file:///repo --results=verified,unknown --fail
```

Ou no WSL/Linux, com `pip install semgrep`, os mesmos comandos sem o `docker run`.

## Limitações

- No Semgrep Community Edition o rastreamento é **dentro de cada método**. Um texto que passa de um método para outro só é seguido pela camada de tipos e pelos testes; por isso as três camadas existem juntas.
- As fontes são reconhecidas pelo **nome da propriedade** (`Titulo`, `Bairro`...). Uma propriedade nova com outro nome precisa entrar na lista da regra.
- O analisador de C# do Semgrep ainda não entende o cabeçalho dos construtores primários (C# 12): as linhas `public sealed class X(IDependencia d)` ficam sem análise (cerca de 0,4% do código). O corpo das classes é analisado normalmente.
- As regras públicas `p/csharp` ficam só como relatório, para o pipeline não depender de regras que mudam fora do projeto.
