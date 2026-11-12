# Registro de uso de IA

Este arquivo registra como usei inteligência artificial no backend do Me Conta, como pede o brief da Global Solution.
A IA foi usada para aprender e revisar. Revisei, executei e testei todo o código gerado, e sei explicar cada parte.

**Ferramenta:** Claude (Anthropic)

---

## 10/11/2026 · Planejamento do backend

**Objetivo:** definir o modelo de domínio, a estrutura de pastas e os endpoints da Web API que atende C# Software Development e SOA e Web Services.

**Prompt (resumo):** enviei o brief da GS, a Ficha da CitizenTech, o backlog do Azure Boards (US01 a US18) e as rubricas de C# e SOA, e pedi uma proposta de entidades, relações, pastas e endpoints.

**Resultado:** proposta com 4 projetos (Domain, Application, Infrastructure, Api) mais testes, e a lista de endpoints ligada às user stories.

**Minhas decisões:** uma única API para as duas matérias; SQL Server como banco.

---

## 10/11/2026 · Alinhamento com os exemplos da aula

**Prompt (resumo):** enviei dois exemplos de código do professor de C# (`ProblemaOriginal`, Aula 06, e `BancoLabGabarito`) e pedi para ajustar a proposta ao padrão da aula.

**Resultado:** troquei PostgreSQL por SQL Server, autenticação própria por ASP.NET Identity (`MapIdentityApi`) e bibliotecas de teste por xUnit com fakes escritos à mão. O domínio passou a seguir o modelo interface → classe abstrata → classes `sealed` do `BancoLabGabarito`.

---

## 10/11/2026 · Parte 1: domínio e testes de domínio

**Prompt (resumo):** pedi as entidades, value objects e regras de negócio do domínio, com testes unitários em xUnit.

**Resultado gerado:**
- Entidades `Familia`, `Membro`, `Participacao`, `Convite`, `Idoso`, `Categoria`, `Pergunta` (abstrata) com `PerguntaCatalogo` e `PerguntaDaFamilia`, `EnvioPergunta` e `PaginaCaderno`.
- Value objects `Telefone`, `Semana`, `ArquivoDeMidia` (abstrato) com `ArquivoDeAudio` e `ArquivoDeFoto`.
- Regras `FiltroAntiGolpe` e `SugestaoSemanal`, cada uma com interface.
- Testes unitários dos critérios de aceite das US01 a US08, US11 e US14.

**O que revisei ou mudei:** _(preencher)_
