// Painel executivo Risco Cidadão.
// Lê só as rotas abertas do painel (números agregados e trechos já mascarados).
// Todo texto vindo da API entra na página com textContent, nunca como HTML.
"use strict";

(() => {
  const API = "/api/v1/risco-cidadao";
  const INTERVALO_PAINEL = 5000;
  const INTERVALO_ALERTAS = 15000;
  const INTERVALO_AUDITORIA = 60000;
  const SVG = "http://www.w3.org/2000/svg";

  const numero = new Intl.NumberFormat("pt-BR");
  const hora = new Intl.DateTimeFormat("pt-BR", { hour: "2-digit", minute: "2-digit", second: "2-digit", timeZone: "America/Sao_Paulo" });
  const dataHora = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", hour: "2-digit", minute: "2-digit", timeZone: "America/Sao_Paulo" });

  const NOMES_DE_FAIXA = { Baixo: "Baixo: pouco risco", Medio: "Médio: atenção", Alto: "Alto: risco de golpe" };
  const NOMES_DE_NIVEL = { Alto: "Alto", Medio: "Médio" };
  const NOMES_DE_CAMPO = { Titulo: "Título", Transcricao: "Transcrição" };
  const NOMES_DE_RESULTADO = { Liberada: "Liberada", PrecisaConfirmar: "Precisa confirmar", Bloqueada: "Barrada", Publicada: "Publicada" };

  // Mesmas três séries, com a mesma cor, nos dois gráficos empilhados.
  const SERIES_DE_RESULTADO = [
    { nome: "Publicadas no mapa", classe: "1" },
    { nome: "Barradas pelo filtro", classe: "2" },
    { nome: "Outras verificações", classe: "3" },
  ];
  const SERIES_DE_NIVEL = [
    { nome: "Risco alto: bloqueia", classe: "2", nivel: "Alto" },
    { nome: "Risco médio: pede confirmação", classe: "1", nivel: "Medio" },
  ];

  const estado = { dias: 14, ocupado: false, assinaturaDosTipos: null };
  const $ = (id) => document.getElementById(id);
  const dica = $("dica");

  // ---------------- Utilidades ----------------

  function svg(tag, atributos = {}, pai = null) {
    const elemento = document.createElementNS(SVG, tag);
    for (const [nome, valor] of Object.entries(atributos)) elemento.setAttribute(nome, String(valor));
    if (pai) pai.appendChild(elemento);
    return elemento;
  }

  function html(tag, classe = "", texto = null, pai = null) {
    const elemento = document.createElement(tag);
    if (classe) elemento.className = classe;
    if (texto !== null && texto !== undefined) elemento.textContent = String(texto);
    if (pai) pai.appendChild(elemento);
    return elemento;
  }

  function limpar(elemento) {
    while (elemento.firstChild) elemento.removeChild(elemento.firstChild);
  }

  function diaCurto(iso) {
    const [, mes, dia] = iso.split("-");
    return `${dia}/${mes}`;
  }

  // Escala de números inteiros com no máximo 5 intervalos "redondos" (1, 2, 5, 10, 20, 25, 50...).
  function escalaInteira(valor) {
    const passos = [1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000, 10000];
    const passo = passos.find((p) => Math.ceil(Math.max(1, valor) / p) <= 5) || Math.ceil(valor / 5);
    const topo = passo * Math.max(1, Math.ceil(valor / passo));
    const marcas = [];
    for (let v = 0; v <= topo; v += passo) marcas.push(v);
    return { topo, marcas };
  }

  async function buscar(caminho) {
    const resposta = await fetch(caminho, { cache: "no-store", headers: { Accept: "application/json" } });
    if (resposta.status === 429) throw new Error("Muitos pedidos: o painel espera um pouco.");
    if (!resposta.ok) {
      let detalhe = `HTTP ${resposta.status}`;
      try { detalhe = (await resposta.json()).detail || detalhe; } catch { /* corpo sem JSON */ }
      throw new Error(detalhe);
    }
    return resposta.json();
  }

  // ---------------- Dica (tooltip) ----------------

  function mostrarDica(evento, titulo, linhas) {
    limpar(dica);
    html("div", "dica-titulo", titulo, dica);
    for (const linha of linhas) {
      const item = html("div", "dica-linha", null, dica);
      if (linha.classe) html("span", `dica-traco marca-${linha.classe}`, null, item);
      html("strong", "", linha.valor, item);
      html("span", "", linha.nome, item);
    }
    dica.hidden = false;
    posicionarDica(evento);
  }

  function posicionarDica(evento) {
    let x = evento.clientX;
    let y = evento.clientY;
    if (x === undefined || (x === 0 && y === 0)) {
      const caixa = evento.target.getBoundingClientRect();
      x = caixa.left + caixa.width / 2;
      y = caixa.top;
    }
    const largura = dica.offsetWidth;
    const altura = dica.offsetHeight;
    const esquerda = Math.min(window.innerWidth - largura - 8, x + 14);
    const topo = y - altura - 12 < 8 ? y + 16 : y - altura - 12;
    dica.style.left = `${Math.max(8, esquerda)}px`;
    dica.style.top = `${topo}px`;
  }

  function esconderDica() {
    dica.hidden = true;
  }

  function ligarDica(alvo, titulo, linhas) {
    alvo.setAttribute("tabindex", "0");
    alvo.setAttribute("aria-label", `${titulo}: ${linhas.map((l) => `${l.nome} ${l.valor}`).join(", ")}`);
    alvo.addEventListener("pointerenter", (e) => mostrarDica(e, titulo, linhas));
    alvo.addEventListener("pointermove", posicionarDica);
    alvo.addEventListener("pointerleave", esconderDica);
    alvo.addEventListener("focus", (e) => mostrarDica(e, titulo, linhas));
    alvo.addEventListener("blur", esconderDica);
  }

  // ---------------- Legenda e tabela ----------------

  function legenda(figura, series) {
    const lista = figura.querySelector(".legenda");
    if (!lista) return;
    limpar(lista);
    for (const serie of series) {
      const item = html("li", "", null, lista);
      const chave = html("span", "chave", null, item);
      chave.dataset.serie = serie.classe;
      html("span", "", serie.nome, item);
    }
  }

  function tabela(figura, cabecalho, linhas) {
    const caixa = figura.querySelector(".tabela");
    limpar(caixa);
    const t = html("table", "tabela-dados", null, caixa);
    const linhaTopo = html("tr", "", null, html("thead", "", null, t));
    for (const coluna of cabecalho) html("th", "", coluna, linhaTopo).setAttribute("scope", "col");
    const corpo = html("tbody", "", null, t);
    for (const linha of linhas) {
      const tr = html("tr", "", null, corpo);
      for (const celula of linha) html("td", "", celula, tr);
    }
  }

  function vazio(area, texto) {
    limpar(area);
    html("p", "vazio", texto, area);
  }

  // ---------------- Gráficos ----------------

  // Colunas empilhadas (verificações por dia). Cada coluna é o alvo da dica, com as três séries.
  function colunasEmpilhadas(figura, categorias, series, rotuloDoTotal) {
    const area = figura.querySelector(".area");
    limpar(area);
    const largura = 600;
    const altura = 250;
    const m = { esquerda: 34, direita: 8, topo: 18, baixo: 26 };
    const totais = categorias.map((_, i) => series.reduce((soma, s) => soma + s.valores[i], 0));
    const escala = escalaInteira(Math.max(...totais));
    const maximo = escala.topo;
    const banda = (largura - m.esquerda - m.direita) / categorias.length;
    const espessura = Math.min(24, Math.max(4, banda * 0.62));
    const y = (v) => altura - m.baixo - (v / maximo) * (altura - m.topo - m.baixo);

    const grafico = svg("svg", { viewBox: `0 0 ${largura} ${altura}`, role: "img",
      "aria-label": `${rotuloDoTotal} por dia. Maior valor: ${Math.max(...totais)}.` }, area);

    for (const marca of escala.marcas) {
      svg("line", { class: marca === 0 ? "linha-base" : "linha-grade", x1: m.esquerda, x2: largura - m.direita, y1: y(marca), y2: y(marca) }, grafico);
      svg("text", { class: "eixo-texto", x: m.esquerda - 6, y: y(marca) + 4, "text-anchor": "end" }, grafico).textContent = numero.format(marca);
    }

    const pulo = Math.ceil(categorias.length / 8);
    const indiceDoMaior = totais.indexOf(Math.max(...totais));
    categorias.forEach((categoria, i) => {
      const centro = m.esquerda + banda * i + banda / 2;
      if (i % pulo === 0 || i === categorias.length - 1) {
        svg("text", { class: "eixo-texto", x: centro, y: altura - 8, "text-anchor": "middle" }, grafico).textContent = categoria.rotulo;
      }

      const coluna = svg("g", { class: "coluna" }, grafico);
      svg("rect", { class: "alvo", x: centro - banda / 2, y: m.topo, width: banda, height: altura - m.topo - m.baixo }, coluna);

      let base = 0;
      const visiveis = series.map((s, k) => ({ s, k, v: s.valores[i] })).filter((p) => p.v > 0);
      visiveis.forEach((parte, ordem) => {
        const y0 = y(base);
        const y1 = y(base + parte.v);
        const ultima = ordem === visiveis.length - 1;
        const fundo = ordem === 0 ? y0 : y0 - 2; // 2px de superfície entre segmentos
        desenharBarraVertical(coluna, centro - espessura / 2, y1, espessura, Math.max(1, fundo - y1), ultima, `barra-seg marca-${parte.s.classe}`);
        base += parte.v;
      });

      if (i === indiceDoMaior && totais[i] > 0) {
        svg("text", { class: "valor-texto", x: centro, y: y(totais[i]) - 6, "text-anchor": "middle" }, grafico).textContent = numero.format(totais[i]);
      }

      ligarDica(coluna, categoria.titulo, [
        ...series.map((s) => ({ nome: s.nome, valor: numero.format(s.valores[i]), classe: s.classe })),
        { nome: rotuloDoTotal, valor: numero.format(totais[i]) },
      ]);
    });
  }

  // Barra vertical com a ponta de dados arredondada (4px) e a base reta.
  function desenharBarraVertical(pai, x, y, largura, altura, arredondar, classe) {
    const r = arredondar ? Math.min(4, largura / 2, altura) : 0;
    const d = `M${x},${y + altura} V${y + r} Q${x},${y} ${x + r},${y} H${x + largura - r} Q${x + largura},${y} ${x + largura},${y + r} V${y + altura} Z`;
    return svg("path", { d, class: classe }, pai);
  }

  // Barra horizontal com a ponta direita arredondada e a base reta.
  function desenharBarraHorizontal(pai, x, y, comprimento, espessura, arredondar, classe) {
    const r = arredondar ? Math.min(4, espessura / 2, comprimento) : 0;
    const d = `M${x},${y} H${x + comprimento - r} Q${x + comprimento},${y} ${x + comprimento},${y + r} V${y + espessura - r} Q${x + comprimento},${y + espessura} ${x + comprimento - r},${y + espessura} H${x} Z`;
    return svg("path", { d, class: classe }, pai);
  }

  // Linha do índice médio por dia, de 0 a 100, com as faixas de 30 e 70 marcadas e mira que acha o dia.
  function linhaDoIndice(figura, dias) {
    const area = figura.querySelector(".area");
    limpar(area);
    const largura = 600;
    const altura = 230;
    const m = { esquerda: 34, direita: 34, topo: 14, baixo: 26 };
    const passo = dias.length > 1 ? (largura - m.esquerda - m.direita) / (dias.length - 1) : 0;
    const x = (i) => (dias.length > 1 ? m.esquerda + passo * i : largura / 2);
    const y = (v) => altura - m.baixo - (v / 100) * (altura - m.topo - m.baixo);
    const comDados = dias.map((d, i) => ({ ...d, i })).filter((d) => d.verificacoes > 0);

    const grafico = svg("svg", { viewBox: `0 0 ${largura} ${altura}`, role: "img",
      "aria-label": comDados.length
        ? `Índice médio por dia. Último dia com verificação: ${comDados[comDados.length - 1].indiceMedio}.`
        : "Índice médio por dia: ainda sem verificações no período." }, area);

    for (const marca of [0, 30, 70, 100]) {
      svg("line", { class: marca === 0 ? "linha-base" : "linha-grade", x1: m.esquerda, x2: largura - m.direita, y1: y(marca), y2: y(marca) }, grafico);
      svg("text", { class: "eixo-texto", x: m.esquerda - 6, y: y(marca) + 4, "text-anchor": "end" }, grafico).textContent = String(marca);
    }
    svg("text", { class: "eixo-texto", x: largura - m.direita + 4, y: y(70) + 4 }, grafico).textContent = "alto";
    svg("text", { class: "eixo-texto", x: largura - m.direita + 4, y: y(30) + 4 }, grafico).textContent = "médio";

    const pulo = Math.ceil(dias.length / 8);
    dias.forEach((d, i) => {
      if (i % pulo === 0 || i === dias.length - 1) {
        svg("text", { class: "eixo-texto", x: x(i), y: altura - 8, "text-anchor": "middle" }, grafico).textContent = diaCurto(d.dia);
      }
    });

    // Trechos contínuos: dia sem verificação interrompe a linha (não é índice zero).
    const trechos = [];
    let atual = [];
    dias.forEach((d, i) => {
      if (d.verificacoes > 0) atual.push({ x: x(i), y: y(d.indiceMedio) });
      else if (atual.length) { trechos.push(atual); atual = []; }
    });
    if (atual.length) trechos.push(atual);

    for (const trecho of trechos) {
      if (trecho.length > 1) {
        const caminho = trecho.map((p, k) => `${k ? "L" : "M"}${p.x},${p.y}`).join(" ");
        svg("path", { class: "serie-area", d: `${caminho} L${trecho[trecho.length - 1].x},${y(0)} L${trecho[0].x},${y(0)} Z` }, grafico);
        svg("path", { class: "serie-linha", d: caminho }, grafico);
      } else {
        svg("circle", { class: "serie-ponto", cx: trecho[0].x, cy: trecho[0].y, r: 4 }, grafico);
      }
    }

    if (comDados.length) {
      const ultimo = comDados[comDados.length - 1];
      svg("circle", { class: "serie-ponto", cx: x(ultimo.i), cy: y(ultimo.indiceMedio), r: 4.5 }, grafico);
      svg("text", { class: "valor-texto", x: x(ultimo.i), y: y(ultimo.indiceMedio) - 10, "text-anchor": "middle" }, grafico).textContent = String(ultimo.indiceMedio);
    }

    const mira = svg("line", { class: "mira", y1: m.topo, y2: altura - m.baixo, visibility: "hidden" }, grafico);
    const ponto = svg("circle", { class: "serie-ponto", r: 4.5, visibility: "hidden" }, grafico);
    const superficie = svg("rect", { class: "alvo", x: m.esquerda - 10, y: m.topo, width: largura - m.esquerda - m.direita + 20, height: altura - m.topo - m.baixo }, grafico);

    const mover = (evento) => {
      const caixa = grafico.getBoundingClientRect();
      const px = ((evento.clientX - caixa.left) / caixa.width) * largura;
      const i = Math.max(0, Math.min(dias.length - 1, Math.round(passo ? (px - m.esquerda) / passo : 0)));
      const d = dias[i];
      mira.setAttribute("x1", x(i));
      mira.setAttribute("x2", x(i));
      mira.setAttribute("visibility", "visible");
      if (d.verificacoes > 0) {
        ponto.setAttribute("cx", x(i));
        ponto.setAttribute("cy", y(d.indiceMedio));
        ponto.setAttribute("visibility", "visible");
      } else {
        ponto.setAttribute("visibility", "hidden");
      }
      mostrarDica(evento, diaCurto(d.dia), d.verificacoes > 0
        ? [{ nome: "índice médio", valor: String(d.indiceMedio), classe: "1" }, { nome: "verificações", valor: numero.format(d.verificacoes) }]
        : [{ nome: "sem verificação neste dia", valor: "–" }]);
    };
    superficie.addEventListener("pointermove", mover);
    superficie.addEventListener("pointerleave", () => {
      mira.setAttribute("visibility", "hidden");
      ponto.setAttribute("visibility", "hidden");
      esconderDica();
    });
  }

  // Barras horizontais empilhadas (uma linha por categoria). Usada para tipos de dado e para bairros.
  function barrasHorizontais(figura, linhas, series, rotuloDoTotal) {
    const area = figura.querySelector(".area");
    limpar(area);
    if (!linhas.length) {
      vazio(area, "Nada no período.");
      return;
    }
    const largura = 600;
    const alturaDaLinha = 30;
    const espessura = 16;
    const m = { esquerda: 168, direita: 40, topo: 4, baixo: 4 };
    const altura = m.topo + m.baixo + alturaDaLinha * linhas.length;
    const maximo = Math.max(1, ...linhas.map((l) => l.valores.reduce((a, b) => a + b, 0)));
    const escala = (v) => (v / maximo) * (largura - m.esquerda - m.direita);

    const grafico = svg("svg", { viewBox: `0 0 ${largura} ${altura}`, role: "img",
      "aria-label": `${rotuloDoTotal}: ${linhas.map((l) => `${l.rotulo} ${l.valores.reduce((a, b) => a + b, 0)}`).join(", ")}.` }, area);
    svg("line", { class: "linha-base", x1: m.esquerda, x2: m.esquerda, y1: 0, y2: altura }, grafico);

    linhas.forEach((linha, i) => {
      const topo = m.topo + alturaDaLinha * i;
      const g = svg("g", { class: "coluna" }, grafico);
      svg("rect", { class: "alvo", x: 0, y: topo, width: largura, height: alturaDaLinha }, g);
      svg("text", { class: "rotulo-texto", x: m.esquerda - 10, y: topo + alturaDaLinha / 2 + 4, "text-anchor": "end" }, g).textContent = linha.rotulo;

      let x = m.esquerda;
      const partes = linha.valores.map((v, k) => ({ v, s: series[k] })).filter((p) => p.v > 0);
      partes.forEach((parte, ordem) => {
        const ultima = ordem === partes.length - 1;
        const comprimento = Math.max(1, escala(parte.v) - (ultima ? 0 : 2));
        desenharBarraHorizontal(g, x, topo + (alturaDaLinha - espessura) / 2, comprimento, espessura, ultima, `barra-seg marca-${parte.s.classe}`);
        x += escala(parte.v);
      });

      const total = linha.valores.reduce((a, b) => a + b, 0);
      svg("text", { class: "valor-texto", x: x + 6, y: topo + alturaDaLinha / 2 + 4 }, g).textContent = numero.format(total);

      ligarDica(g, linha.rotulo, [
        ...series.map((s, k) => ({ nome: s.nome, valor: numero.format(linha.valores[k]), classe: s.classe })).filter((l) => l.valor !== "0"),
        { nome: rotuloDoTotal, valor: numero.format(total) },
      ]);
    });
  }

  // ---------------- Blocos do painel ----------------

  function desenharIndicador(dados) {
    $("indice").textContent = String(dados.indiceRiscoCidadao);
    const faixa = $("faixa");
    faixa.dataset.faixa = dados.faixa;
    faixa.textContent = NOMES_DE_FAIXA[dados.faixa] || dados.faixa;
    const valor = $("medidor-valor");
    valor.dataset.faixa = dados.faixa;
    valor.style.width = `${Math.max(0, Math.min(100, dados.indiceRiscoCidadao))}%`;
    $("medidor").setAttribute("aria-label", `Índice ${dados.indiceRiscoCidadao} de 100, faixa ${NOMES_DE_NIVEL[dados.faixa] || dados.faixa}`);
    $("versao").textContent = dados.versaoDasRegras;

    const t = dados.totais;
    $("t-verificacoes").textContent = numero.format(t.verificacoes);
    $("t-evitados").textContent = numero.format(t.vazamentosEvitados);
    $("t-bloqueadas").textContent = numero.format(t.bloqueadas);
    $("t-publicadas").textContent = numero.format(t.publicadas);
    $("t-dados").textContent = numero.format(t.dadosEncontrados);
    $("t-semrisco").textContent = numero.format(t.semRisco);
  }

  function desenharDias(dados) {
    const figura = $("g-dias");
    const dias = dados.porDia;
    const series = SERIES_DE_RESULTADO.map((s, k) => ({
      ...s,
      valores: dias.map((d) => [d.publicadas, d.bloqueadas, Math.max(0, d.verificacoes - d.publicadas - d.bloqueadas)][k]),
    }));
    legenda(figura, series);
    if (!dias.some((d) => d.verificacoes > 0)) {
      vazio(figura.querySelector(".area"), "Nenhuma memória verificada no período.");
    } else {
      colunasEmpilhadas(figura, dias.map((d) => ({ rotulo: diaCurto(d.dia), titulo: diaCurto(d.dia) })), series, "verificações");
    }
    tabela(figura, ["Dia", "Verificações", "Publicadas", "Barradas", "Índice médio"],
      dias.map((d) => [diaCurto(d.dia), d.verificacoes, d.publicadas, d.bloqueadas, d.verificacoes ? d.indiceMedio : "–"]));
  }

  function desenharIndicePorDia(dados) {
    const figura = $("g-indice");
    linhaDoIndice(figura, dados.porDia);
    tabela(figura, ["Dia", "Índice médio", "Verificações"],
      dados.porDia.map((d) => [diaCurto(d.dia), d.verificacoes ? d.indiceMedio : "–", d.verificacoes]));
  }

  function desenharTipos(dados) {
    const figura = $("g-tipos");
    legenda(figura, SERIES_DE_NIVEL);
    const linhas = dados.porTipo.map((t) => ({
      rotulo: t.rotulo,
      valores: SERIES_DE_NIVEL.map((s) => (s.nivel === t.nivel ? t.quantidade : 0)),
    }));
    barrasHorizontais(figura, linhas, SERIES_DE_NIVEL, "vezes");
    tabela(figura, ["Dado", "Nível", "Vezes"], dados.porTipo.map((t) => [t.rotulo, NOMES_DE_NIVEL[t.nivel] || t.nivel, t.quantidade]));
  }

  function desenharBairros(dados) {
    const figura = $("g-bairros");
    legenda(figura, SERIES_DE_RESULTADO);
    const linhas = dados.porBairro.map((b) => ({
      rotulo: b.bairro,
      valores: [b.publicadas, b.bloqueadas, Math.max(0, b.tentativas - b.publicadas - b.bloqueadas)],
    }));
    barrasHorizontais(figura, linhas, SERIES_DE_RESULTADO, "tentativas");
    tabela(figura, ["Bairro", "Tentativas", "Publicadas", "Barradas", "Índice médio"],
      dados.porBairro.map((b) => [b.bairro, b.tentativas, b.publicadas, b.bloqueadas, b.indiceMedio]));
  }

  function desenharUltimos(dados) {
    const corpo = $("ultimos").querySelector("tbody");
    limpar(corpo);
    if (!dados.ultimosAlertas.length) {
      const celula = html("td", "", "Nenhum dado pessoal encontrado no período.", html("tr", "", null, corpo));
      celula.colSpan = 7;
      return;
    }
    for (const a of dados.ultimosAlertas) {
      const tr = html("tr", "", null, corpo);
      html("td", "", dataHora.format(new Date(a.quando.endsWith("Z") ? a.quando : `${a.quando}Z`)), tr);
      html("td", "", a.rotulo, tr);
      const nivel = html("td", "", null, tr);
      const etiqueta = html("span", "etiqueta", NOMES_DE_NIVEL[a.nivel] || a.nivel, nivel);
      etiqueta.dataset.nivel = a.nivel;
      html("td", "mascara", a.trechoMascarado, tr);
      html("td", "", NOMES_DE_CAMPO[a.campo] || a.campo || "–", tr);
      html("td", "", NOMES_DE_RESULTADO[a.resultado] || a.resultado, tr);
      html("td", "", a.bairro || "–", tr);
    }
  }

  function desenharNumerosDaIa(ia) {
    const lista = $("ia-numeros");
    limpar(lista);
    const itens = [
      ["IA", ia.disponivel ? `${ia.nome}${ia.ultimoModelo ? ` (${ia.ultimoModelo})` : ""}${ia.emPausa ? ", em pausa" : ""}` : "sem chave: textos da equipe"],
      ["Pedidos à IA", ia.chamadas],
      ["Respostas aproveitadas", ia.respostasAproveitadas],
      ["Frases descartadas pelo filtro", ia.frasesDescartadas],
      ["Envios barrados", ia.enviosBarrados],
      ["Respostas reaproveitadas", ia.respostasDoCache],
    ];
    for (const [rotulo, valor] of itens) {
      const div = html("div", "", null, lista);
      html("dt", "", rotulo, div);
      html("dd", "", typeof valor === "number" ? numero.format(valor) : valor, div);
    }
  }

  function desenharAlertas(dados) {
    $("fonte-alertas").textContent = dados.fonte;
    const observacao = $("obs-alertas");
    observacao.hidden = !dados.observacao;
    observacao.textContent = dados.observacao || "";

    const caixa = $("alertas");
    limpar(caixa);
    if (!dados.alertas.length) {
      html("p", "vazio", "Nenhum dado pessoal encontrado no período: nada a explicar.", caixa);
    }
    for (const a of dados.alertas) {
      const cartao = html("article", "alerta", null, caixa);
      const topo = html("div", "alerta-topo", null, cartao);
      html("span", "alerta-tipo", a.rotulo, topo);
      const etiqueta = html("span", "etiqueta", `${NOMES_DE_NIVEL[a.nivel] || a.nivel} · ${numero.format(a.quantidade)}×`, topo);
      etiqueta.dataset.nivel = a.nivel;
      html("p", "alerta-frase", a.alerta, cartao);
      const dl = html("dl", "", null, cartao);
      html("dt", "", "Risco real", dl);
      html("dd", "", a.riscoReal, dl);
      html("dt", "", "O que fazer", dl);
      html("dd", "", a.oQueFazer, dl);
    }
    desenharNumerosDaIa(dados.ia);
  }

  function desenharAuditoria(dados) {
    $("fonte-auditoria").textContent = dados.fonte;
    const resumo = $("auditoria-resumo");
    limpar(resumo);
    const bloco = (valor, rotulo) => {
      const div = html("div", "", null, resumo);
      html("div", "auditoria-numero", numero.format(valor), div);
      html("div", "auditoria-rotulo", rotulo, div);
    };
    bloco(dados.achadosAntes, "achados antes das correções");
    html("span", "seta", "→", resumo).setAttribute("aria-hidden", "true");
    bloco(dados.achadosDepois, "achados depois");
    bloco(dados.corrigidas, "falhas corrigidas");
    bloco(dados.abertas, "falhas abertas");
    html("p", "auditoria-rotulo", dados.ferramenta, resumo);

    const caixa = $("falhas");
    limpar(caixa);
    for (const f of dados.falhas) {
      const cartao = html("article", "alerta", null, caixa);
      const topo = html("div", "alerta-topo", null, cartao);
      html("span", "alerta-tipo", f.regra.replace(/^meconta-/, ""), topo);
      const etiqueta = html("span", "etiqueta", f.situacao, topo);
      etiqueta.dataset.situacao = f.situacao;
      html("p", "alerta-frase", f.alerta, cartao);
      const dl = html("dl", "", null, cartao);
      html("dt", "", "Risco real", dl);
      html("dd", "", f.riscoReal, dl);
      html("dt", "", f.situacao === "Corrigida" ? "O que foi feito" : "O que fazer", dl);
      html("dd", "", f.oQueFazer, dl);
      const tecnico = html("p", "tecnico", null, cartao);
      html("code", "", `${f.arquivo}: ${f.linhas.join(", ")}`, tecnico);
      if (f.cwe) tecnico.appendChild(document.createTextNode(` · ${f.cwe}`));
    }
  }

  // ---------------- Atualização ----------------

  function situacao(texto, estadoDaConexao) {
    $("situacao").textContent = texto;
    $("situacao").parentElement.dataset.estado = estadoDaConexao;
  }

  async function atualizarPainel() {
    if (estado.ocupado) return;
    estado.ocupado = true;
    document.body.classList.add("carregando");
    try {
      const dados = await buscar(`${API}?dias=${estado.dias}`);
      desenharIndicador(dados);
      desenharDias(dados);
      desenharIndicePorDia(dados);
      desenharTipos(dados);
      desenharBairros(dados);
      desenharUltimos(dados);
      document.querySelector(".viz-root").setAttribute("aria-busy", "false");
      situacao(`Ao vivo · ${hora.format(new Date())}`, "ok");

      // Novo tipo de dado no período: pede os alertas na hora, sem esperar o próximo ciclo.
      const assinatura = dados.porTipo.map((t) => t.tipo + t.nivel).join("|");
      if (assinatura !== estado.assinaturaDosTipos) {
        estado.assinaturaDosTipos = assinatura;
        atualizarAlertas();
      }
    } catch (erro) {
      situacao(`Sem conexão: ${erro.message}`, "erro");
    } finally {
      estado.ocupado = false;
      document.body.classList.remove("carregando");
    }
  }

  async function atualizarAlertas() {
    try {
      desenharAlertas(await buscar(`${API}/alertas?dias=${estado.dias}`));
    } catch (erro) {
      $("fonte-alertas").textContent = "indisponível";
      const observacao = $("obs-alertas");
      observacao.hidden = false;
      observacao.textContent = erro.message;
    }
  }

  async function atualizarAuditoria() {
    try {
      desenharAuditoria(await buscar(`${API}/auditoria`));
    } catch (erro) {
      const resumo = $("auditoria-resumo");
      limpar(resumo);
      html("p", "observacao", erro.message, resumo);
    }
  }

  function ligarControles() {
    for (const botao of document.querySelectorAll(".periodo")) {
      botao.addEventListener("click", () => {
        estado.dias = Number(botao.dataset.dias);
        for (const outro of document.querySelectorAll(".periodo")) outro.setAttribute("aria-pressed", String(outro === botao));
        estado.assinaturaDosTipos = null;
        atualizarPainel();
      });
    }
    for (const botao of document.querySelectorAll(".ver-tabela")) {
      botao.addEventListener("click", () => {
        const figura = botao.closest("figure");
        const mostrar = botao.getAttribute("aria-pressed") !== "true";
        botao.setAttribute("aria-pressed", String(mostrar));
        botao.textContent = mostrar ? "Ver gráfico" : "Ver tabela";
        figura.querySelector(".tabela").hidden = !mostrar;
        figura.querySelector(".area").hidden = mostrar;
        const lista = figura.querySelector(".legenda");
        if (lista) lista.hidden = mostrar;
      });
    }
  }

  function repetir(funcao, intervalo) {
    setInterval(() => {
      if (!document.hidden) funcao();
    }, intervalo);
  }

  ligarControles();
  atualizarPainel();
  atualizarAuditoria();
  repetir(atualizarPainel, INTERVALO_PAINEL);
  repetir(atualizarAlertas, INTERVALO_ALERTAS);
  repetir(atualizarAuditoria, INTERVALO_AUDITORIA);
})();
