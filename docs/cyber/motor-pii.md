# Motor de parsing PII do Me Conta

Antes de uma memória sair do caderno da família e ir para o mapa do bairro, o Me Conta procura nela dados que podem expor a pessoa idosa a golpes. O módulo `src/MeConta.Privacidade` faz essa busca. Ele não depende de nenhum outro projeto nem de pacote externo.

## Como funciona

```
texto da família
   │
   ▼
1. Normalização ──── minúsculas, sem acentos, sem caracteres invisíveis,
   │                 dígitos de largura cheia viram dígitos comuns,
   │                 números falados viram dígitos ("um dois três" → 123)
   ▼
2. Regras (regras-pii.json) ──── expressão regular + validador + palavras de contexto
   │
   ▼
3. Sobreposições ──── quando duas regras pegam o mesmo trecho, fica a mais grave
   │
   ▼
4. Resultado ──── achados mascarados, versão segura do texto, índice Risco Cidadão,
                  decisão: publicar, pedir confirmação ou bloquear
```

- **Normalização com mapa de posições.** Cada caractere do texto normalizado sabe de onde veio no original. Por isso um CPF falado ("um dois três...") é encontrado no texto normalizado e removido no texto original, frase inteira.
- **Números falados.** As respostas chegam em áudio e a transcrição escreve os números por extenso. O motor entende dígitos soltos ("nove meia meia sete"), grupos ("cento e vinte e três") e palavras entre os grupos ("ponto", "traço"). Só converte sequências com pelo menos 3 dígitos, para não mexer em "dois filhos".
- **Validadores.** CPF (dígito verificador), cartão (algoritmo de Luhn) e telefone (DDD existente, celular começando com 9). Um número de pedido com 11 dígitos não vira CPF.
- **Contexto.** Palavras perto do trecho ("meu CPF é", "liga no") aumentam a confiança. Algumas regras só valem com contexto: o RG e o CEP são números muito comuns sem ele. Um CPF com dígito errado continua bloqueado se vier com a palavra "CPF", porque ainda é uma tentativa de publicar o documento.
- **Falha fechada.** Se o texto passa do limite de tamanho, ou se uma expressão demora mais de 250 ms (texto feito para travar o motor), o resultado é "análise incompleta" e a publicação é bloqueada.
- **Nada de dado em claro na saída.** O resultado guarda só posição e trecho mascarado (`***.***.***-09`). A versão segura troca cada dado por um aviso (`[CPF removido]`), e só ela pode ir para log ou para a IA.

## Regras

As regras ficam em `src/MeConta.Privacidade/Regras/regras-pii.json`, dentro da DLL. Para ensinar o motor a reconhecer um dado novo, basta acrescentar uma regra no arquivo, sem mexer no código. O teste `CarregarDeJson_RegraNovaNoArquivo_MotorPassaAEncontrarODadoSemMudarCodigo` mostra isso com uma regra de placa de carro. Uma regra com erro (expressão inválida, nível ou validador que não existe, campo vazio) impede o carregamento, com uma mensagem que diz qual regra corrigir.

| Regra | Dado | Nível | Como confirma |
|---|---|---|---|
| `PII-CPF-001` | CPF | Alto (bloqueia) | validador `cpf`; contexto aumenta a confiança |
| `PII-RG-001` | RG | Alto (bloqueia) | só com contexto (rg, identidade, registro geral) |
| `PII-CARTAO-001` | Número de cartão | Alto (bloqueia) | validador `cartao`; contexto aumenta a confiança |
| `PII-BANCO-001` | Agência bancária | Alto (bloqueia) | contexto aumenta a confiança |
| `PII-BANCO-002` | Conta bancária | Alto (bloqueia) | "conta corrente/poupança" + número |
| `PII-BANCO-003` | Conta bancária | Alto (bloqueia) | só com contexto (banco, agência, depósito...) |
| `PII-PIX-001` | Chave PIX | Alto (bloqueia) | "pix" + e-mail, telefone ou chave aleatória |
| `PII-SENHA-001` | Senha | Alto (bloqueia) | "senha é ..." ou "senha" + número |
| `PII-CODIGO-001` | Código de verificação | Alto (bloqueia) | "código de verificação/do WhatsApp" + número |
| `PII-SAUDE-001` | Dado de saúde (doença) | Médio (pede confirmação) | dicionário com radicais |
| `PII-SAUDE-002` | Dado de saúde (remédio, tratamento) | Médio (pede confirmação) | dicionário com radicais |
| `PII-TEL-001` | Telefone | Médio (pede confirmação) | validador `telefone`; contexto aumenta a confiança |
| `PII-EMAIL-001` | E-mail | Médio (pede confirmação) | também o e-mail falado ("arroba", "ponto") |
| `PII-END-001` | Endereço | Médio (pede confirmação) | tipo de via + nome + número |
| `PII-CEP-001` | CEP | Médio (pede confirmação) | só com contexto (cep) |
| `PII-NASC-001` | Data de nascimento | Médio (pede confirmação) | "nasci/aniversário" + data completa |
| `PII-ROTINA-001` | Rotina da casa | Médio (pede confirmação) | "moro sozinha", "guardo dinheiro em casa", "não tem grade" |

A regra de rotina não procura um dado pessoal clássico, mas uma informação que, publicada, ajuda golpistas e assaltantes. Ela existe por causa de quem o Me Conta protege.

### Armadilhas que o motor evita

| Texto | Por que não é alerta |
|---|---|
| "nasceu em 1950 na rua 12" | ano e número de rua não são CPF |
| "a panela de pressão fica 25 minutos" | "pressão" só conta como saúde em "pressão alta/baixa" ou "remédio para pressão" |
| "não tem remédio, minha filha" | expressão, não medicamento |
| "o chá de boldo era o remédio caseiro" | remédio caseiro fica de fora |
| "me conta 1950 foi um ano difícil" | "conta" + número só vira conta bancária com contexto de banco |
| "sou de câncer, signo de água" | signo, não doença |
| "oficina na Rua Voluntários da Pátria por quase 40 anos" | "40 anos" não é número da casa |

## Índice Risco Cidadão

Cada achado tem uma chance de causar dano: peso do nível (alto 0,7; médio 0,35) vezes a confiança do achado. O índice é a chance de pelo menos um deles causar dano:

```
índice = 100 × (1 − Π (1 − peso × confiança))  +  bônus das combinações
```

Combinações perigosas somam pontos: endereço + rotina da casa (+25), telefone + rotina (+15), endereço + telefone (+10). Qualquer dado alto deixa o índice em pelo menos 70, e qualquer dado médio, em pelo menos 30. O máximo é 100.

| Faixa | Índice | O que acontece |
|---|---|---|
| Baixo | 0 a 29 | pode publicar |
| Médio | 30 a 69 | a família precisa confirmar |
| Alto | 70 a 100 | se houver dado de risco alto, a publicação é bloqueada |

## Avaliação

Dois conjuntos de memórias simuladas, com o gabarito dos dados que cada uma contém (`tests/MeConta.UnitTests/Privacidade/`). Os números são de teste ou inventados.

- **Corpus de desenvolvimento** (`corpus-memorias.json`): 64 memórias, 38 com dado pessoal e 26 sem, incluindo as armadilhas acima. As regras foram escritas olhando para ele.
- **Conjunto de validação** (`corpus-validacao.json`): 30 memórias escritas depois das regras, com frases diferentes, para medir o motor em textos que ele não tinha visto.

Na primeira rodada, o conjunto de validação mostrou erros que o corpus de desenvolvimento escondia:

| | Precisão | Recall | Bloqueio certo |
|---|---:|---:|---:|
| Validação, primeira rodada | 90% | 86% | 28 de 30 |
| Validação, depois dos ajustes | 100% | 100% | 30 de 30 |
| Desenvolvimento, depois dos ajustes | 100% | 100% | 64 de 64 |

Erros da primeira rodada e o ajuste feito:

| Memória | Erro | Ajuste |
|---|---|---|
| V04 "Rua Voluntários da Pátria por quase 40 anos" | endereço a mais | palavras como "por", "quase", "desde" não fazem parte do nome da rua; número seguido de "anos" não é número da casa |
| V21 "A chave pix da costura é o e-mail ..." | chave PIX não encontrada (só e-mail) | aceitar até 40 caracteres entre "pix" e a chave |
| V23 "o código do WhatsApp por mensagem: 845 221" | código não encontrado | aceitar "do WhatsApp" e o código em dois grupos de 3 dígitos |
| V26 "Avenida Mutinga, número 3500" | endereço não encontrado | aceitar vírgula antes de "número" |

Depois dos ajustes, o conjunto de validação também passou a ser usado no ajuste, então os 100% dele não medem mais textos inéditos. O número honesto para textos novos é o da primeira rodada (90% de precisão e 86% de recall). Os dois conjuntos viram testes automáticos, para que um ajuste futuro não quebre o que já funciona.

Para gerar as tabelas de novo:

```bash
dotnet run --project tools/MeConta.Privacidade.Avaliacao
```

## Limitações

- O nome completo de uma pessoa não é detectado: nomes são parecidos demais com palavras comuns e com o próprio conteúdo das memórias ("a Dona Lourdes da padaria").
- Datas e anos falados por extenso ("mil novecentos e cinquenta") não são convertidos em dígitos.
- O dicionário de saúde cobre as doenças e os remédios mais comuns entre idosos, não todos.
- "Senha é ..." pega a palavra seguinte, mesmo quando não é uma senha de verdade ("a senha da festa era cantar"). Para o mapa do bairro, preferimos errar para o lado do alerta.

## Versões das regras

| Versão | O que mudou |
| --- | --- |
| 1.0.0 | 17 regras e 3 combinações. |
| 1.0.1 | Regra `PII-SENHA-001`: fronteira de palavra antes de "e", "era", "fica" e "foi". Antes, "senha no te**xto**" virava "senha **e** xto" e "as senhas **fica**m" virava senha. Achado pelos testes da [IA na remediação](remediacao-ia.md); os dois conjuntos de memórias simuladas deram o mesmo resultado. |
