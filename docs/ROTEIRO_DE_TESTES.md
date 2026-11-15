# Roteiro de testes da Me Conta API

Passo a passo para testar **todas as requisições** da API, inclusive as do porta-retrato Me Conta (IoT), com o resultado esperado de cada uma. As marcas 📸 indicam onde tirar as fotos usadas no [README](../README.md) (salve em `docs/evidencias/` com o nome indicado).

Todos os dados usados aqui são **fictícios**.

---

## 1. Preparação

### Ferramentas

| Ferramenta | Para quê |
|---|---|
| **Visual Studio 2022 (17.12 ou mais novo)** | Rodar o arquivo [`src/MeConta.Api/MeConta.Api.http`](../src/MeConta.Api/MeConta.Api.http): 88 chamadas numeradas, na mesma ordem deste roteiro (menos as de envio de áudio, 49 a 53). O token, o convite, o envio e o aviso passam sozinhos de uma chamada para a outra. |
| **Swagger UI** (`http://localhost:5080/swagger`) | Envio de áudio (multipart, passos 49 a 53) e as fotos. |
| **Terminal** | Subir a API e rodar os testes automatizados. |

### Subir a API

```powershell
cd me-conta-api
dotnet run --project src/MeConta.Api --launch-profile http
```

Na primeira vez, a API cria o banco `MeConta`, aplica as migrações (`InitialCreate` e `PortaRetrato`), grava as 5 categorias e as 25 perguntas e cria os dados de demonstração, inclusive o porta-retrato da Vó Lourdes.

### Começar do zero

Alguns passos só funcionam uma vez por banco (por exemplo, cadastrar o Marcos). Para repetir o roteiro inteiro, pare a API (`Ctrl+C`), apague o banco e suba de novo:

```powershell
dotnet ef database drop -p src/MeConta.Infrastructure -s src/MeConta.Api --force
dotnet run --project src/MeConta.Api --launch-profile http
```

### Dados de demonstração

| Quem | Login | Situação inicial |
|---|---|---|
| Bia (19, neta) | `bia@example.com` / `MeConta@2026` | Criou a **Família Souza** (id 1) |
| Cláudia (45, filha) | `claudia@example.com` / `MeConta@2026` | Entrou pelo convite `CASA26` |
| Marcos (24, morador do bairro) | criado no passo 16 | Ainda não é da família |
| Vó Lourdes (71) | — | Idosa **id 1**, WhatsApp `5511900000001`, já tem uma pergunta enviada (envio **id 1**) |
| Vô Antônio (78) | — | Idoso **id 2**, sem WhatsApp |
| Porta-retrato da Vó Lourdes | chave `demo-porta-retrato-vo-lourdes-2026` | Código `k7m2q9xd` (tópicos MQTT `meconta/k7m2q9xd/...`), pareado pela Bia |

Confira os IDs no passo 10. Se forem diferentes, ajuste as variáveis `@familiaSouza`, `@voLourdes` e `@voAntonio` no topo do arquivo `.http`.

### Como usar o token no Swagger

1. Faça o login (passo 4) e copie o `accessToken`.
2. Clique em **Authorize**, cole **só o token** (sem a palavra "Bearer") e confirme.

### Como testar as rotas do aparelho no Swagger

As rotas do grupo **Porta-retrato (aparelho)** não usam login. Em cada uma, preencha o campo `X-Chave-PortaRetrato` com `demo-porta-retrato-vo-lourdes-2026`. As do grupo **Porta-retrato** (pareamento e avisos) usam o token da Bia, como as demais.

### Como ler os erros

Todo erro volta no formato ProblemDetails. A mensagem para o app fica em `detail`:

```json
{ "status": 409, "title": "Conflito", "detail": "Cadastre o WhatsApp", "instance": "/api/v1/idosos/2/envios" }
```

---

## 2. Fotos da documentação

| Foto | O que fazer |
|---|---|
| 📸 `01-swagger.png` | Abra `http://localhost:5080/swagger`: título "Me Conta API", botão **Authorize** e os grupos de endpoints, inclusive "Porta-retrato (aparelho)" e "Porta-retrato". |
| 📸 `02-scalar.png` | Abra `http://localhost:5080/scalar` com um endpoint aberto (ex.: `POST /api/v1/idosos/{idosoId}/envios`). |

---

## 3. Roteiro

Marque a coluna ✓ conforme for testando.

### Bloco A · Rotas públicas

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 1 | `GET /health` | — | **200** `Healthy` | ☐ |
| 2 | `GET /api/v1/categorias` | sem token | **200** com 5 categorias (receitas, historia-da-familia, oficios, o-bairro, conselhos) | ☐ |
| 3 | `GET /api/v1/familias` | **sem** token | **401** | ☐ |

### Bloco B · Login

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 4 📸 `03-login.png` | `POST /api/v1/auth/login` | `{ "email": "bia@example.com", "password": "MeConta@2026" }` | **200** com `tokenType`, `accessToken`, `expiresIn`, `refreshToken` | ☐ |
| 5 | `POST /api/v1/auth/refresh` | `{ "refreshToken": "<do passo 4>" }` | **200** com token novo | ☐ |
| 6 | `POST /api/v1/auth/login` | senha `SenhaErrada@1` | **401** | ☐ |

No Swagger, deixe `useCookies` e `useSessionCookies` em branco. Depois do passo 4, clique em **Authorize** e cole o token.

### Bloco C · Perfil

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 7 | `GET /api/v1/membros/eu` | token da Bia | **200** `{ "id": 1, "nome": "Bia", ... }` | ☐ |
| 8 | `POST /api/v1/membros` | `{ "nome": "Bia" }` | **409** "O seu perfil já foi criado." | ☐ |

### Bloco D · Famílias (US01) e convite (US03)

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 9 | `GET /api/v1/familias` | token da Bia | **200** com a Família Souza, `meuPapel` = `Criador`, `quantidadeDeIdosos` = 2 | ☐ |
| 10 | `GET /api/v1/familias/1` | — | **200** com `membros` (Bia e Cláudia) e `idosos` (Vó Lourdes e Vô Antônio). **Confira os IDs aqui.** | ☐ |
| 11 | `POST /api/v1/familias` | `{ "nome": "Família Oliveira" }` | **201** com `meuPapel` = `Criador` | ☐ |
| 12 | `POST /api/v1/familias` | `{ "nome": "   " }` | **400** "Dê um nome para a família" | ☐ |
| 13 | `POST /api/v1/familias` | nome com 61 letras | **400** "Use até 60 caracteres" | ☐ |
| 14 | `GET /api/v1/familias/999` | — | **404** "Família não encontrada." | ☐ |
| 15 | `POST /api/v1/familias/1/convites` | sem corpo | **201** `{ "codigo": "K7M2QX", "expiraEm": "..." }` (48 horas depois). **Anote o código.** | ☐ |

### Bloco E · Um parente de fora entra na família

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 16 | `POST /api/v1/auth/register` | `{ "email": "marcos@example.com", "password": "MeConta@2026" }` | **200** sem corpo (repetir dá 400: e-mail já usado) | ☐ |
| 17 | `POST /api/v1/auth/register` | `{ "email": "fraca@example.com", "password": "123" }` | **400** (senha fraca) | ☐ |
| 18 | `POST /api/v1/auth/login` | Marcos | **200**. Troque o token no **Authorize** pelo do Marcos. | ☐ |
| 19 | `GET /api/v1/membros/eu` | token do Marcos | **403** "Complete o seu perfil antes de continuar (POST /api/v1/membros)." | ☐ |
| 20 | `POST /api/v1/membros` | `{ "nome": "Marcos" }` | **201** | ☐ |
| 21 | `GET /api/v1/familias/1` | token do Marcos | **403** "Só quem é da família pode ver e mudar estes dados." | ☐ |
| 22 | `GET /api/v1/familias/1/caderno` | token do Marcos | **403** (quem não é da família não vê o caderno) | ☐ |
| 23 | `POST /api/v1/convites/ZZZZZZ/aceitar` | token do Marcos | **400** "Código inválido ou expirado. Peça um novo." | ☐ |
| 24 | `POST /api/v1/convites/{codigo}/aceitar` | código do passo 15 | **200** com o Marcos na lista de `membros`, papel `Membro` | ☐ |
| 25 | `GET /api/v1/familias/1` | token do Marcos | **200** (agora ele é da família) | ☐ |

Volte o **Authorize** para o token da Bia antes do próximo bloco.

### Bloco F · Idosos (US02) e "Ensine sua avó" (US14)

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 26 | `POST /api/v1/familias/1/idosos` | `{ "nome": "Maria Souza", "apelido": "Vó Maria", "whatsApp": "(11) 90000-0002" }` | **201** com `whatsApp` = `5511900000002` | ☐ |
| 27 | `POST /api/v1/familias/1/idosos` | `whatsApp`: `"98765-4321"` | **400** "Confira o número do WhatsApp" | ☐ |
| 28 | `POST /api/v1/familias/1/idosos` | `whatsApp`: `"(11) 90000-0001"` (o da Vó Lourdes) | **409** "Esse número já está na família" | ☐ |
| 29 | `POST /api/v1/familias/1/idosos` | `apelido`: `""` | **400** "Informe como a família chama essa pessoa (ex.: Vó Lourdes)" | ☐ |
| 30 | `POST /api/v1/familias/1/idosos` | `{ "nome": "Benedito Souza", "apelido": "Vô Benedito", "whatsApp": null }` | **201** com `whatsApp` = `null` | ☐ |
| 31 | `GET /api/v1/familias/1/idosos` | — | **200** com 4 idosos | ☐ |
| 32 | `GET /api/v1/idosos/1` | — | **200** com os dados da Vó Lourdes | ☐ |
| 33 | `PUT /api/v1/idosos/1` | `{ "nome": "Lourdes Aparecida Souza", "apelido": "Vó Lourdes", "whatsApp": "(11) 90000-0001" }` | **200** com o nome novo (o próprio número não conta como repetido) | ☐ |
| 34 | `GET /api/v1/idosos/999` | — | **404** "Idoso não encontrado." | ☐ |
| 35 | `POST /api/v1/idosos/2/roteiro-concluido` | sem corpo | **200** com `respondeSozinho` = `true` | ☐ |

### Bloco G · Perguntas (US04, US05, US06)

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 36 | `GET /api/v1/idosos/1/perguntas?categoria=receitas` | — | **200** com 5 perguntas; a de id 1 vem com `jaPerguntada` = `true` | ☐ |
| 37 | `GET /api/v1/idosos/1/perguntas` | — | **200** com as 25 do catálogo | ☐ |
| 38 | `GET /api/v1/idosos/1/perguntas?categoria=xyz` | — | **404** "Categoria não encontrada." | ☐ |
| 39 | `GET /api/v1/idosos/1/pergunta-da-semana` | — | **200** com `semanaInicio` (segunda), `semanaFim` (domingo) e uma `pergunta` com `jaPerguntada` = `false` | ☐ |
| 40 | `POST /api/v1/familias/1/perguntas` | `{ "texto": "Vó, como foi o dia em que a senhora chegou em Pirituba?", "categoriaId": 4 }` | **201** com `tipo` = `DaFamilia` | ☐ |
| 41 📸 `07-erro-pix.png` | `POST /api/v1/familias/1/perguntas` | `{ "texto": "Vó, me manda um PIX de 50 reais?", "categoriaId": 5 }` | **400** "O Me Conta não envia pedidos de dinheiro ou senhas" | ☐ |
| 42 | `POST /api/v1/familias/1/perguntas` | `{ "texto": "Vó, qual é a senha do seu banco?", "categoriaId": 5 }` | **400** (mesma mensagem) | ☐ |
| 43 | `POST /api/v1/familias/1/perguntas` | `{ "texto": "Receita?", "categoriaId": 1 }` | **400** "Escreva uma pergunta um pouco maior" | ☐ |
| 44 | `POST /api/v1/familias/1/perguntas` | `{ "texto": "Vó, qual era a sua música preferida?", "categoriaId": 9 }` | **400** "Escolha uma categoria para a pergunta" | ☐ |

Depois do passo 40, a lista do passo 37 passa a trazer 26 perguntas: a da família vem primeiro.

### Bloco H · Envio pelo WhatsApp (US07)

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 45 📸 `04-envio-whatsapp.png` | `POST /api/v1/idosos/1/envios` | `{ "perguntaId": 16 }` | **201** com `mensagem` = "Vó Lourdes, me conta: Como era o bairro quando você chegou?", `linkWhatsApp` começando com `https://wa.me/5511900000001?text=` e `status` = `Enviada`. **Anote o `id`** (provavelmente 2). | ☐ |
| 46 📸 `08-erro-sem-whatsapp.png` | `POST /api/v1/idosos/2/envios` | `{ "perguntaId": 16 }` | **409** "Cadastre o WhatsApp" | ☐ |
| 47 | `POST /api/v1/idosos/1/envios` | `{ "perguntaId": 999 }` | **404** "Pergunta não encontrada." | ☐ |
| 48 | `GET /api/v1/idosos/1/envios?status=Enviada` | — | **200** com os 2 envios da Vó Lourdes, do mais recente para o mais antigo | ☐ |

### Bloco I · Guardar a resposta em áudio (US08, US10) · pelo Swagger

Use `POST /api/v1/envios/{envioId}/pagina` → **Try it out**, com o token da Bia no Authorize. Tenha à mão um áudio curto (`.mp3`, `.m4a`, `.ogg` ou `.opus`), uma foto (`.jpg` ou `.png`) e qualquer `.pdf`.

| Nº | `envioId` | Campos do formulário | Esperado | ✓ |
|---|---|---|---|---|
| 49 📸 `05-upload-audio.png` | o do passo 45 | `Audio` = o áudio · `Foto` = a foto · `Titulo` = `Pirituba quando a rua era de terra` · `DuracaoSegundos` = `45` · `ChaveIdempotencia` = `3fa85f64-5717-4562-b3fc-2c963f66afa6` | **201** com `titulo`, `idosoApelido` = `Vó Lourdes`, `temFoto` = `true`, `audioUrl` e `fotoUrl`. **Anote o `id`** (provavelmente 1). | ☐ |
| 50 | o do passo 45 | os mesmos campos, **mesma** `ChaveIdempotencia` | **200** com a **mesma** página, sem criar outra (modo offline, US10) | ☐ |
| 51 | o do passo 45 | os mesmos campos, `ChaveIdempotencia` = `7c9e6679-7425-40de-944b-e07fc1f90ae7` | **409** "Essa pergunta já foi respondida" | ☐ |
| 52 | `1` | `Audio` = o `.pdf` · `DuracaoSegundos` = `45` | **400** "Escolha um arquivo de áudio (mp3, m4a, ogg ou opus)" | ☐ |
| 53 | `1` | `Audio` = o áudio · `DuracaoSegundos` em branco | **400** (validação do campo `DuracaoSegundos`) | ☐ |

Teste opcional: no envio `1`, mande um áudio acima de 20 MB. Esperado: **413** "Áudio muito grande: o limite é 20 MB".

Se o `id` da página do passo 49 não for 1, ajuste `@paginaId` no arquivo `.http`.

### Bloco J · Caderno (US09, US11) e indicador (US12)

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 54 📸 `06-caderno.png` | `GET /api/v1/familias/1/caderno?pagina=1&tamanho=20` | — | **200** com `itens` (a página do passo 49), `total` = 1, `totalDePaginas` = 1 | ☐ |
| 55 | `GET /api/v1/familias/1/caderno?idosoId=1&categoria=o-bairro` | — | **200** com a mesma página (filtros por idoso e categoria) | ☐ |
| 56 | `GET /api/v1/familias/1/caderno?tamanho=100` | — | **400** "Use página a partir de 1 e tamanho de 1 a 50." | ☐ |
| 57 | `GET /api/v1/paginas/1` | — | **200** com a página | ☐ |
| 58 | `GET /api/v1/paginas/1/audio` | — | **200**: o áudio toca (no Swagger aparece um player ou link de download) | ☐ |
| 59 | `GET /api/v1/paginas/1/foto` | — | **200**: a foto aparece | ☐ |
| 60 📸 `09-indicador.png` | `GET /api/v1/indicadores/conversas-de-verdade` | — | **200** com `idososCadastrados` = 4, `idososQueResponderam` = 1, `percentualConversasDeVerdade` = 25, `minutosDeAudio` = 0,8, `idososQueRespondemSozinhos` = 1 | ☐ |
| 61 | `GET /api/v1/indicadores/conversas-de-verdade?data=2026-01-05` | — | **200** com `mensagem` = "Ainda não há dados" e `percentualConversasDeVerdade` = `null` (sem divisão por zero) | ☐ |
| 62 | `POST /api/v1/auth/login` | Cláudia | **200**. Troque o token no **Authorize** pelo da Cláudia. | ☐ |
| 63 | `DELETE /api/v1/paginas/1` | token da Cláudia | **403** "Só quem guardou a página pode apagá-la." | ☐ |
| 64 | `DELETE /api/v1/paginas/1` | token da **Bia** | **204** (página apagada do banco e do disco) | ☐ |
| 65 | `GET /api/v1/idosos/1/envios` | token da Bia | **200**: o envio do passo 45 voltou para `status` = `Enviada`, com `paginaId` = `null` | ☐ |
| 66 | `GET /api/v1/paginas/1` | token da Bia | **404** "Página não encontrada." | ☐ |

O valor do passo 60 considera os 4 idosos da Família Souza depois do bloco F e um áudio de 45 segundos. Com outra duração, `minutosDeAudio` muda.

### Bloco K · Porta-retrato Me Conta, IoT (US15)

O porta-retrato é a caixinha com ESP32 que acende quando chega pergunta e tem o botão "quero contar". No Wokwi, quem chama estas rotas é a ponte Node-RED; aqui elas são chamadas direto. Use o token da **Bia** nas rotas da família e a chave `demo-porta-retrato-vo-lourdes-2026` no campo `X-Chave-PortaRetrato` nas rotas do aparelho.

Neste ponto, a Vó Lourdes tem **duas perguntas esperando**: a do envio 1 (dos dados de demonstração) e a do passo 45, que voltou para `Enviada` no passo 64.

| Nº | Requisição | O que enviar | Esperado | ✓ |
|---|---|---|---|---|
| 67 | `GET /api/v1/idosos/1/porta-retrato` | token da Bia | **200** com `codigo` = `k7m2q9xd`, `topicoBase` = `meconta/k7m2q9xd` e `conectado` = `false` (a chave nunca aparece aqui) | ☐ |
| 68 | `GET /api/v1/idosos/2/porta-retrato` | token da Bia | **404** "Este idoso ainda não tem porta-retrato." | ☐ |
| 69 📸 `13-porta-retrato-estado.png` | `GET /api/v1/porta-retrato/estado` | chave do aparelho | **200** com `temPerguntaNova` = `true`, `perguntasEsperando` = 2 e a `pergunta` mais recente: a do passo 45 ("Como era o bairro quando você chegou?"), com `quemPerguntou` = `Bia`. **Anote o `envioId`.** | ☐ |
| 70 | `GET /api/v1/porta-retrato/estado` | campo da chave em branco | **401** "Chave do porta-retrato ausente ou inválida." | ☐ |
| 71 📸 `15-porta-retrato-chave-invalida.png` | `GET /api/v1/porta-retrato/estado` | chave `chave-que-nao-existe` | **401** (mesma mensagem) | ☐ |
| 72 📸 `14-porta-retrato-quero-contar.png` | `POST /api/v1/porta-retrato/quero-contar` | chave do aparelho · `{ "envioId": <do passo 69> }` | **201** com `apelido` = `Vó Lourdes`, a `pergunta`, `visto` = `false`. **Anote o `id` do aviso.** | ☐ |
| 73 | `POST /api/v1/porta-retrato/quero-contar` | o mesmo do passo 72 | **200** com o **mesmo** aviso (apertar de novo não duplica) | ☐ |
| 74 | `GET /api/v1/porta-retrato/estado` | chave do aparelho | **200** com `temPerguntaNova` = `true`, `perguntasEsperando` = 1: agora a tela mostra a pergunta do envio 1 ("Qual é o segredo do seu prato mais famoso?"), que ainda espera. **Anote o `envioId`.** | ☐ |
| 75 | `POST /api/v1/porta-retrato/quero-contar` | chave do aparelho · `{ "envioId": <do passo 74> }` | **201** | ☐ |
| 76 📸 `17-porta-retrato-moldura-apagada.png` | `GET /api/v1/porta-retrato/estado` | chave do aparelho | **200** com `temPerguntaNova` = `false`, `perguntasEsperando` = 0 e `pergunta` = `null`: a moldura apaga | ☐ |
| 77 | `POST /api/v1/porta-retrato/quero-contar` | chave do aparelho · `{ "envioId": 999 }` | **400** "Essa pergunta não é deste porta-retrato" | ☐ |
| 78 | `POST /api/v1/porta-retrato/quero-contar` | chave do aparelho · `{}` | **201** com `envioId` = `null` e `pergunta` = `null` ("quero contar uma coisa") | ☐ |
| 79 | `POST /api/v1/porta-retrato/quero-contar` | `{}` de novo, em menos de 2 minutos | **200** com o **mesmo** aviso do passo 78 | ☐ |
| 80 | `PUT /api/v1/porta-retrato/conexao` | chave do aparelho · `{ "online": true }` | **204** sem corpo | ☐ |
| 81 | `GET /api/v1/idosos/1/porta-retrato` | token da Bia | **200** com `conectado` = `true` e `ultimoContatoEm` preenchido. Repetindo depois de 90 segundos sem o passo 80, volta a `false`. | ☐ |
| 82 📸 `16-avisos-quero-contar.png` | `GET /api/v1/familias/1/avisos?somenteNaoVistos=true` | token da Bia | **200** com 3 avisos (passos 78, 75 e 72), do mais recente para o mais antigo | ☐ |
| 83 | `POST /api/v1/avisos/{avisoId}/visto` | `avisoId` do passo 72 | **200** com `visto` = `true` e `vistoEm` preenchido | ☐ |
| 84 | `GET /api/v1/familias/1/avisos?somenteNaoVistos=true` | token da Bia | **200** com 2 avisos (o do passo 72 saiu da lista) | ☐ |
| 85 | `GET /api/v1/familias/1/avisos?somenteNaoVistos=false` | token da Bia | **200** com os 3 avisos, o do passo 72 como visto | ☐ |
| 86 | `POST /api/v1/avisos/999/visto` | token da Bia | **404** "Aviso não encontrado." | ☐ |
| 87 | `POST /api/v1/idosos/1/envios` | `{ "perguntaId": 6 }` | **201** (a família manda uma pergunta nova) | ☐ |
| 88 | `GET /api/v1/porta-retrato/estado` | chave do aparelho | **200** com `temPerguntaNova` = `true` e a pergunta do passo 87: a moldura acende de novo | ☐ |

**Não use** `POST /api/v1/idosos/{idosoId}/porta-retrato` (parear de novo) nem `DELETE /api/v1/idosos/{idosoId}/porta-retrato` durante o roteiro. O primeiro gera uma chave nova, e a chave de demonstração para de funcionar; o segundo remove o porta-retrato. Para voltar ao porta-retrato de demonstração, comece do zero (seção 1).

---

## 4. Banco de dados e migrações

| Foto | O que fazer | ✓ |
|---|---|---|
| 📸 `10-banco.png` | No Visual Studio: **Exibir → Pesquisador de Objetos do SQL Server** → `(localdb)\MSSQLLocalDB` → **Bancos de Dados** → `MeConta` → **Tabelas**. Clique com o botão direito em `dbo.Perguntas` → **Exibir Dados** (aparecem as 25 do catálogo e a da família, com a coluna `Origem`). | ☐ |
| 📸 `11-migracoes.png` | Clique com o botão direito em `dbo.__EFMigrationsHistory` → **Exibir Dados** (linhas `..._InitialCreate` e `..._PortaRetrato`) e mostre a pasta `src/MeConta.Infrastructure/Persistencia/Migrations` no Gerenciador de Soluções. | ☐ |

Confira também que existem as tabelas `Familias`, `Participacoes`, `Convites`, `Idosos`, `Categorias`, `Perguntas`, `Envios`, `Paginas`, `PortaRetratos`, `AvisosQueroContar`, `Membros` e `AspNetUsers`, e que os áudios enviados foram para `src/MeConta.Api/storage/audios/`.

---

## 5. Testes automatizados

| Foto | O que fazer | Esperado | ✓ |
|---|---|---|---|
| 📸 `12-testes.png` | Pare a API (`Ctrl+C`) e rode `dotnet test` na pasta do projeto | `total: 151; falhou: 0` e nenhum aviso amarelo no build | ☐ |

---

## 6. Resumo da cobertura

| User story | Passos |
|---|---|
| US01 · Criar a família | 9 a 14 |
| US02 · Cadastrar o idoso | 26 a 34 |
| US03 · Entrar por convite | 15, 21 a 25 |
| US04 · Perguntas por categoria | 2, 36 a 38 |
| US05 · Pergunta da semana | 39 |
| US06 · Pergunta própria (anti-golpe) | 40 a 44 |
| US07 · Enviar pelo WhatsApp | 45 a 48 |
| US08 · Salvar a resposta em áudio | 49, 51 a 53 |
| US09 · Ver e ouvir o caderno | 22, 54 a 59 |
| US10 · Funcionar sem internet (idempotência) | 50 |
| US11 · Apagar uma página | 63 a 66 |
| US12 · Indicador "Conversas de verdade" | 60, 61 |
| US14 · Roteiro "Ensine sua avó" | 35 |
| US15 · Porta-retrato (aviso de pergunta nova e botão "quero contar") | 67 a 88 |
| Login, perfil e acesso | 3 a 8, 16 a 21, 70, 71 |
| Monitoramento | 1 |
