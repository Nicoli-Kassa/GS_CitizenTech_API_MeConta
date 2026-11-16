/*
  Porta-retrato Me Conta (ESP32)

  Uma caixinha para a casa do idoso: a moldura de LEDs acende quando a família manda
  uma pergunta, a telinha mostra quem perguntou e o quê, e o botão grande "quero contar"
  avisa a família que ele quer responder. Assim quem ainda não domina o celular também
  participa das conversas.

  Como conversa com o resto do Me Conta:
    ESP32 <-> MQTT (broker público) <-> Node-RED <-> HTTP <-> API Me Conta <-> app da família

  Tópicos (meconta/<código do aparelho>/...):
    pergunta      (recebe, retida)  o que mostrar: pergunta nova ou nada
    quero-contar  (envia)           o botão foi apertado
    status        (envia, retida)   online / offline ("last will")
    ambiente      (envia)           luz do ambiente e modo noite

  Privacidade: pelo broker passam só o apelido, o primeiro nome de quem perguntou e o
  texto da pergunta. A luz do ambiente é usada só aqui dentro (brilho e modo noite).
*/

#include <WiFi.h>
#include <Wire.h>
#include <time.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <Adafruit_NeoPixel.h>

// ---------------------------------------------------------------------------
// Configuração
// ---------------------------------------------------------------------------

const char* WIFI_REDE = "Wokwi-GUEST";
const char* WIFI_SENHA = "";
const int WIFI_CANAL = 6;  // o canal fixo deixa a conexão no Wokwi mais rápida

const char* MQTT_SERVIDOR = "test.mosquitto.org";
const uint16_t MQTT_PORTA = 1883;

// Código público do porta-retrato da Vó Lourdes (o mesmo da API). Não dá acesso a nada sozinho.
const char* CODIGO_APARELHO = "k7m2q9xd";

const char* FUSO_HORARIO = "<-03>3";  // horário de Brasília, sem horário de verão

// Pinos
const int PINO_ANEL = 4;     // anel de LEDs (DIN)
const int PINO_BOTAO = 13;   // botão "quero contar" (outro lado no GND)
const int PINO_BUZZER = 25;  // buzzer (+)
const int PINO_LUZ = 34;     // sensor de luz (AO)
const int PINO_TESTE_REDE = 27;  // chave para simular a queda do Wi-Fi (teste)
// Telinha OLED no I2C padrão: SDA = 21, SCL = 22

// Tempos (ms)
const unsigned long INTERVALO_RECONEXAO_WIFI = 30000;  // tenta reconectar a cada 30 s
const unsigned long INTERVALO_RECONEXAO_MQTT = 5000;
const unsigned long TEMPO_LIMITE_WIFI = 25000;
const unsigned long INTERVALO_LOG_WIFI = 3000;
const unsigned long INTERVALO_AMBIENTE = 10000;  // também serve de sinal de vida para o broker
const unsigned long INTERVALO_LEITURA_LUZ = 1000;
const unsigned long TEMPO_AGRADECIMENTO = 6000;
const unsigned long INTERVALO_ROLAGEM = 2500;
const unsigned long INTERVALO_ANIMACAO = 40;  // 25 quadros por segundo bastam e aliviam a simulação
const unsigned long INTERVALO_TELA = 250;
const unsigned long ANTIRREPIQUE_BOTAO = 40;

// Luz do ambiente (lux)
const float LUX_ENTRA_NOITE = 10.0;  // abaixo disso: modo noite
const float LUX_SAI_NOITE = 20.0;    // acima disso: sai do modo noite (folga para não ficar piscando)
const uint8_t BRILHO_NOITE = 60;
const uint8_t BRILHO_MINIMO = 150;
const uint8_t BRILHO_MAXIMO = 255;

// ---------------------------------------------------------------------------
// Peças
// ---------------------------------------------------------------------------

const int LEDS_DO_ANEL = 24;
const int LARGURA_TELA = 128;
const int ALTURA_TELA = 64;
const int CARACTERES_POR_LINHA = 21;  // fonte padrão: 6 px por letra
const int LINHAS_DA_PERGUNTA = 5;

Adafruit_NeoPixel anel(LEDS_DO_ANEL, PINO_ANEL, NEO_GRB + NEO_KHZ800);
Adafruit_SSD1306 tela(LARGURA_TELA, ALTURA_TELA, &Wire, -1);
WiFiClient clienteWifi;
PubSubClient mqtt(clienteWifi);

String topicoPergunta;
String topicoQueroContar;
String topicoStatus;
String topicoAmbiente;

// ---------------------------------------------------------------------------
// Estado
// ---------------------------------------------------------------------------

enum class Tela { Conectando, SemRede, Ocioso, PerguntaNova, Agradecimento };

struct Pergunta {
  bool existe = false;
  long envioId = 0;
  String quemPerguntou;
  String texto;
  int esperando = 0;
};

// Uma nota do buzzer (frequência 0 = pausa).
// Os tipos ficam aqui em cima porque o Arduino gera as declarações das funções
// antes da primeira função do arquivo, e elas precisam conhecer estes tipos.
struct Nota {
  unsigned int frequencia;
  unsigned long duracao;
};

Tela telaAtual = Tela::Conectando;
Pergunta perguntaAtual;
String apelido = "Me Conta";

// Último envio avisado: até a ponte atualizar a pergunta, ele não acende de novo.
long envioAvisado = -1;
String quemVaiLigar;

// Aviso feito sem conexão: é mandado assim que a conexão voltar.
bool avisoPendente = false;
long envioDoAvisoPendente = 0;  // 0 = "quero contar" sem pergunta

bool simulandoQuedaDaRede = false;
bool modoNoite = false;
float luxAtual = 0;
uint8_t brilhoAtual = BRILHO_MAXIMO;

unsigned long inicioDaTela = 0;
unsigned long ultimaTentativaWifi = 0;
unsigned long ultimaTentativaMqtt = 0;
unsigned long ultimoAmbiente = 0;
unsigned long ultimaLeituraLuz = 0;
unsigned long ultimaAnimacao = 0;
unsigned long ultimaTela = 0;
unsigned long ultimaRolagem = 0;
int primeiraLinhaVisivel = 0;
uint8_t ultimaImagemEnviada[LARGURA_TELA * ALTURA_TELA / 8];
bool imagemJaEnviada = false;
bool wifiConectando = false;
int tentativasWifi = 0;
unsigned long ultimoLogWifi = 0;

// Funções usadas antes de aparecerem no arquivo.
void publicarAmbiente();
void aoApertarBotao();

// ---------------------------------------------------------------------------
// Texto: UTF-8 -> fonte da telinha (página de código 437)
// ---------------------------------------------------------------------------

// A fonte padrão da telinha tem algumas letras acentuadas (á, é, ç...). As que ela
// não tem (ã, õ e maiúsculas acentuadas) viram a letra sem acento.
char latin1ParaTela(uint8_t c) {
  switch (c) {
    case 0xE1: return (char)0xA0;  // á
    case 0xE9: return (char)0x82;  // é
    case 0xED: return (char)0xA1;  // í
    case 0xF3: return (char)0xA2;  // ó
    case 0xFA: return (char)0xA3;  // ú
    case 0xE2: return (char)0x83;  // â
    case 0xEA: return (char)0x88;  // ê
    case 0xF4: return (char)0x93;  // ô
    case 0xE0: return (char)0x85;  // à
    case 0xE7: return (char)0x87;  // ç
    case 0xFC: return (char)0x81;  // ü
    case 0xF1: return (char)0xA4;  // ñ
    case 0xC7: return (char)0x80;  // Ç
    case 0xC9: return (char)0x90;  // É
    case 0xAA: return (char)0xA6;  // ª
    case 0xBA: return (char)0xA7;  // º
    case 0xE3: return 'a';  // ã
    case 0xF5: return 'o';  // õ
    case 0xC0: case 0xC1: case 0xC2: case 0xC3: return 'A';
    case 0xC8: case 0xCA: return 'E';
    case 0xCC: case 0xCD: return 'I';
    case 0xD2: case 0xD3: case 0xD4: case 0xD5: return 'O';
    case 0xD9: case 0xDA: return 'U';
    default: return '?';
  }
}

String paraTela(const String& utf8) {
  String saida;
  saida.reserve(utf8.length());
  for (unsigned int i = 0; i < utf8.length(); i++) {
    uint8_t c = (uint8_t)utf8[i];
    if (c < 0x80) {
      saida += (char)c;
    } else if ((c == 0xC2 || c == 0xC3) && i + 1 < utf8.length()) {
      uint8_t latin1 = (uint8_t)utf8[i + 1] + (c == 0xC3 ? 0x40 : 0x00);
      saida += latin1ParaTela(latin1);
      i++;
    } else if ((c & 0xF0) == 0xE0 && i + 2 < utf8.length()) {
      // Travessões e aspas curvas viram os sinais simples; o resto é ignorado.
      uint8_t b1 = (uint8_t)utf8[i + 1], b2 = (uint8_t)utf8[i + 2];
      if (c == 0xE2 && b1 == 0x80) {
        if (b2 == 0x93 || b2 == 0x94) saida += '-';
        else if (b2 >= 0x98 && b2 <= 0x9B) saida += '\'';
        else if (b2 >= 0x9C && b2 <= 0x9F) saida += '"';
        else if (b2 == 0xA6) saida += "...";
      }
      i += 2;
    } else if ((c & 0xF8) == 0xF0) {
      i += 3;  // emojis: a fonte não tem, então ficam de fora
    }
  }
  return saida;
}

// Quebra o texto em linhas de até "largura" caracteres, sem cortar palavras.
int quebrarEmLinhas(const String& texto, String linhas[], int maxLinhas, int largura) {
  int total = 0;
  String atual;
  int i = 0;
  int n = texto.length();
  while (i < n && total < maxLinhas) {
    while (i < n && texto[i] == ' ') i++;
    int fim = i;
    while (fim < n && texto[fim] != ' ') fim++;
    String palavra = texto.substring(i, fim);
    i = fim;
    if (palavra.length() == 0) break;

    while ((int)palavra.length() > largura) {  // palavra maior que a linha: corta
      if (atual.length() > 0) {
        linhas[total++] = atual;
        atual = "";
        if (total >= maxLinhas) return total;
      }
      linhas[total++] = palavra.substring(0, largura);
      palavra = palavra.substring(largura);
      if (total >= maxLinhas) return total;
    }

    if (atual.length() == 0) {
      atual = palavra;
    } else if ((int)(atual.length() + 1 + palavra.length()) <= largura) {
      atual += " " + palavra;
    } else {
      linhas[total++] = atual;
      atual = palavra;
    }
  }
  if (atual.length() > 0 && total < maxLinhas) linhas[total++] = atual;
  return total;
}

void escreverCentralizado(const String& texto, int y, int tamanho) {
  tela.setTextSize(tamanho);
  int largura = texto.length() * 6 * tamanho;
  tela.setCursor(max(0, (LARGURA_TELA - largura) / 2), y);
  tela.print(texto);
}

// ---------------------------------------------------------------------------
// Ícones (8 x 8)
// ---------------------------------------------------------------------------

const uint8_t ICONE_CORACAO[] PROGMEM = {0x00, 0x66, 0xFF, 0xFF, 0xFF, 0x7E, 0x3C, 0x18};
const uint8_t ICONE_LUA[] PROGMEM = {0x1C, 0x38, 0x70, 0x70, 0x70, 0x78, 0x3E, 0x1C};
const uint8_t ICONE_SEM_REDE[] PROGMEM = {0x81, 0x42, 0x24, 0x18, 0x18, 0x24, 0x42, 0x81};

// ---------------------------------------------------------------------------
// Som
// ---------------------------------------------------------------------------

const Nota MELODIA_PERGUNTA[] = {{784, 120}, {0, 40}, {1047, 220}};  // sol, dó: "chegou pergunta"
const Nota MELODIA_AVISO[] = {{659, 100}, {0, 30}, {784, 100}, {0, 30}, {1047, 180}};

const Nota* melodiaTocando = nullptr;
int notasDaMelodia = 0;
int notaAtual = 0;
unsigned long fimDaNota = 0;

void tocar(const Nota* melodia, int quantidade) {
  if (modoNoite) {
    Serial.println("[som] modo noite: sem bipe");
    return;
  }
  melodiaTocando = melodia;
  notasDaMelodia = quantidade;
  notaAtual = -1;
  fimDaNota = 0;
}

void atualizarSom(unsigned long agora) {
  if (melodiaTocando == nullptr || agora < fimDaNota) return;
  notaAtual++;
  if (notaAtual >= notasDaMelodia) {
    noTone(PINO_BUZZER);
    melodiaTocando = nullptr;
    return;
  }
  const Nota& nota = melodiaTocando[notaAtual];
  if (nota.frequencia > 0) tone(PINO_BUZZER, nota.frequencia, nota.duracao);
  else noTone(PINO_BUZZER);
  fimDaNota = agora + nota.duracao;
}

// ---------------------------------------------------------------------------
// Luz do ambiente e modo noite
// ---------------------------------------------------------------------------

// Converte a tensão do módulo do sensor em lux (com mais luz, a tensão cai).
float lerLux() {
  float volts = analogReadMilliVolts(PINO_LUZ) / 1000.0;
  if (volts >= 3.29) return 0.1;
  if (volts <= 0.01) return 100000.0;
  const float GAMA = 0.7;
  const float RL10 = 50;  // resistência do sensor (kΩ) com 10 lux
  float resistencia = 2000 * volts / (1 - volts / 3.3);
  return pow(RL10 * 1e3 * pow(10, GAMA) / resistencia, 1 / GAMA);
}

void atualizarLuz(unsigned long agora) {
  if (agora - ultimaLeituraLuz < INTERVALO_LEITURA_LUZ && ultimaLeituraLuz != 0) return;
  ultimaLeituraLuz = agora;
  luxAtual = lerLux();

  bool antes = modoNoite;
  if (!modoNoite && luxAtual < LUX_ENTRA_NOITE) modoNoite = true;
  else if (modoNoite && luxAtual > LUX_SAI_NOITE) modoNoite = false;

  if (modoNoite) {
    brilhoAtual = BRILHO_NOITE;
  } else {
    // Escala logarítmica: 20 lux (penumbra) -> mínimo; 300 lux (sala acesa) ou mais -> máximo.
    float fator = (log10(max(luxAtual, 20.0f)) - log10(20.0)) / (log10(300.0) - log10(20.0));
    fator = constrain(fator, 0.0f, 1.0f);
    brilhoAtual = BRILHO_MINIMO + (uint8_t)(fator * (BRILHO_MAXIMO - BRILHO_MINIMO));
  }

  if (antes != modoNoite) {
    tela.dim(modoNoite);
    Serial.printf("[luz] %.1f lux: %s\n", luxAtual, modoNoite ? "modo noite ligado" : "modo noite desligado");
    publicarAmbiente();
  }
}

// ---------------------------------------------------------------------------
// MQTT
// ---------------------------------------------------------------------------

void publicarAmbiente() {
  if (!mqtt.connected()) return;
  JsonDocument doc;
  doc["lux"] = round(luxAtual * 10) / 10.0;
  doc["modoNoite"] = modoNoite;
  doc["brilho"] = brilhoAtual;
  char json[96];
  serializeJson(doc, json);
  mqtt.publish(topicoAmbiente.c_str(), json);
  ultimoAmbiente = millis();
}

bool publicarQueroContar(long envioId) {
  if (!mqtt.connected()) return false;
  JsonDocument doc;
  if (envioId > 0) doc["envioId"] = envioId;
  else doc["envioId"] = nullptr;
  char json[64];
  serializeJson(doc, json);
  bool ok = mqtt.publish(topicoQueroContar.c_str(), json);
  Serial.printf("[mqtt] quero-contar %s: %s\n", json, ok ? "enviado" : "falhou");
  return ok;
}

void mudarTela(Tela nova) {
  if (telaAtual != nova) {
    telaAtual = nova;
    inicioDaTela = millis();
    primeiraLinhaVisivel = 0;
    ultimaRolagem = millis();
  }
}

// Depois de conectar ou de agradecer: mostra a pergunta, se houver, ou a tela de descanso.
void voltarAoNormal() {
  if (perguntaAtual.existe && perguntaAtual.envioId != envioAvisado) mudarTela(Tela::PerguntaNova);
  else mudarTela(Tela::Ocioso);
}

void aoReceberMensagem(char* topico, uint8_t* conteudo, unsigned int tamanho) {
  if (topicoPergunta != topico || tamanho == 0) return;

  JsonDocument doc;
  DeserializationError erro = deserializeJson(doc, conteudo, tamanho);
  if (erro) {
    Serial.printf("[mqtt] pergunta com JSON inválido: %s\n", erro.c_str());
    return;
  }

  const char* novoApelido = doc["apelido"] | "";
  if (strlen(novoApelido) > 0) apelido = novoApelido;

  bool temPergunta = doc["temPerguntaNova"] | false;
  long envioId = doc["envioId"] | 0L;

  // A ponte ainda não viu o aviso: não acende de novo a pergunta que já foi avisada.
  if (temPergunta && envioId == envioAvisado) temPergunta = false;

  if (!temPergunta) {
    Serial.println("[mqtt] nenhuma pergunta esperando");
    perguntaAtual = Pergunta();
    if (telaAtual == Tela::PerguntaNova) mudarTela(Tela::Ocioso);
    return;
  }

  bool nova = !perguntaAtual.existe || perguntaAtual.envioId != envioId;
  perguntaAtual.existe = true;
  perguntaAtual.envioId = envioId;
  perguntaAtual.quemPerguntou = doc["quemPerguntou"] | "Sua família";
  perguntaAtual.texto = doc["pergunta"] | "";
  perguntaAtual.esperando = doc["perguntasEsperando"] | 1;

  Serial.printf("[mqtt] pergunta %ld de %s: %s (%d esperando)\n", envioId,
                perguntaAtual.quemPerguntou.c_str(), perguntaAtual.texto.c_str(), perguntaAtual.esperando);

  if (telaAtual == Tela::Ocioso || telaAtual == Tela::PerguntaNova) {
    if (nova) {
      primeiraLinhaVisivel = 0;
      tocar(MELODIA_PERGUNTA, sizeof(MELODIA_PERGUNTA) / sizeof(Nota));
    }
    mudarTela(Tela::PerguntaNova);
  }
}

void conectarMqtt(unsigned long agora) {
  if (agora - ultimaTentativaMqtt < INTERVALO_RECONEXAO_MQTT && ultimaTentativaMqtt != 0) return;
  ultimaTentativaMqtt = agora;

  String id = String("meconta-") + CODIGO_APARELHO + "-" + String((uint32_t)random(0x10000), HEX);
  Serial.printf("[mqtt] conectando a %s como %s...\n", MQTT_SERVIDOR, id.c_str());

  // "Last will": se o aparelho sumir sem avisar, o broker publica "offline" por ele.
  if (mqtt.connect(id.c_str(), topicoStatus.c_str(), 1, true, "offline")) {
    Serial.println("[mqtt] conectado");
    mqtt.publish(topicoStatus.c_str(), "online", true);
    mqtt.subscribe(topicoPergunta.c_str(), 1);
    publicarAmbiente();

    if (avisoPendente && publicarQueroContar(envioDoAvisoPendente)) {
      Serial.println("[mqtt] aviso feito sem conexão enviado agora");
      avisoPendente = false;
    }
    voltarAoNormal();
  } else {
    Serial.printf("[mqtt] falhou (código %d), tentando de novo em %lu s\n", mqtt.state(),
                  INTERVALO_RECONEXAO_MQTT / 1000);
  }
}

// ---------------------------------------------------------------------------
// Rede
// ---------------------------------------------------------------------------

const char* nomeDoStatusWifi(int status) {
  switch (status) {
    case WL_IDLE_STATUS: return "ligado à rede, esperando IP";  // no ESP32 3.x: associou, sem DHCP ainda
    case WL_NO_SSID_AVAIL: return "rede não encontrada";
    case WL_CONNECTED: return "conectado";
    case WL_CONNECT_FAILED: return "falhou";
    case WL_CONNECTION_LOST: return "conexão perdida";
    case WL_DISCONNECTED: return "desconectado";
    default: return "outro";
  }
}

void tentarWifi(unsigned long agora) {
  ultimaTentativaWifi = agora;
  ultimoLogWifi = agora;
  if (simulandoQuedaDaRede) {
    Serial.println("[wifi] tentando reconectar... rede indisponível (queda simulada)");
    return;
  }
  tentativasWifi++;
  Serial.printf("[wifi] conectando a %s (tentativa %d)...\n", WIFI_REDE, tentativasWifi);
  // Só desconecta se a tentativa anterior ficou pela metade; na primeira vez não precisa.
  if (tentativasWifi > 1) WiFi.disconnect();
  WiFi.begin(WIFI_REDE, WIFI_SENHA, WIFI_CANAL);
  wifiConectando = true;
}

void atualizarRede(unsigned long agora) {
  bool wifiOk = !simulandoQuedaDaRede && WiFi.status() == WL_CONNECTED;

  if (!wifiOk) {
    if (wifiConectando && agora - ultimoLogWifi >= INTERVALO_LOG_WIFI) {
      ultimoLogWifi = agora;
      int status = WiFi.status();
      Serial.printf("[wifi] %lu s: %s (%d)\n", (agora - ultimaTentativaWifi) / 1000, nomeDoStatusWifi(status), status);
    }
    if (wifiConectando && agora - ultimaTentativaWifi > TEMPO_LIMITE_WIFI) {
      wifiConectando = false;
      Serial.printf("[wifi] sem resposta; nova tentativa em %lu s\n", INTERVALO_RECONEXAO_WIFI / 1000);
    }
    if (!wifiConectando && telaAtual != Tela::Agradecimento) mudarTela(Tela::SemRede);
    if (!wifiConectando && agora - ultimaTentativaWifi >= INTERVALO_RECONEXAO_WIFI) tentarWifi(agora);
    return;
  }

  if (wifiConectando) {
    wifiConectando = false;
    Serial.printf("[wifi] conectado, IP %s\n", WiFi.localIP().toString().c_str());
    configTzTime(FUSO_HORARIO, "pool.ntp.org", "time.google.com");
    ultimaTentativaMqtt = 0;
  }

  if (!mqtt.connected()) {
    if (telaAtual != Tela::Agradecimento && telaAtual != Tela::Conectando) mudarTela(Tela::SemRede);
    conectarMqtt(agora);
    return;
  }

  mqtt.loop();
  if (agora - ultimoAmbiente >= INTERVALO_AMBIENTE) publicarAmbiente();
}

// Chave de teste: cada vez que muda de posição, liga ou desliga a queda simulada do Wi-Fi.
int ultimaPosicaoDaChave = -1;

void verificarChaveDeTeste(unsigned long agora) {
  int posicao = digitalRead(PINO_TESTE_REDE);
  if (ultimaPosicaoDaChave == -1) {
    ultimaPosicaoDaChave = posicao;
    return;
  }
  if (posicao == ultimaPosicaoDaChave) return;
  ultimaPosicaoDaChave = posicao;
  simulandoQuedaDaRede = !simulandoQuedaDaRede;

  if (simulandoQuedaDaRede) {
    // Sem desconectar do broker "com educação": o broker percebe a queda e publica o "offline".
    Serial.println("[teste] queda do Wi-Fi simulada");
    WiFi.disconnect();
    wifiConectando = false;
    ultimaTentativaWifi = agora;
    mudarTela(Tela::SemRede);
  } else {
    Serial.println("[teste] Wi-Fi de volta; reconectando");
    ultimaTentativaWifi = agora - INTERVALO_RECONEXAO_WIFI;  // tenta já
  }
}

// ---------------------------------------------------------------------------
// Botão "quero contar"
// ---------------------------------------------------------------------------

int leituraAnteriorBotao = HIGH;
int estadoBotao = HIGH;
unsigned long mudancaBotao = 0;

void verificarBotao(unsigned long agora) {
  int leitura = digitalRead(PINO_BOTAO);
  if (leitura != leituraAnteriorBotao) {
    mudancaBotao = agora;
    leituraAnteriorBotao = leitura;
  }
  if (agora - mudancaBotao < ANTIRREPIQUE_BOTAO || leitura == estadoBotao) return;
  estadoBotao = leitura;
  if (estadoBotao == LOW) aoApertarBotao();
}

void aoApertarBotao() {
  if (telaAtual == Tela::Agradecimento || telaAtual == Tela::Conectando) return;

  // A pergunta que estava esperando vai junto, mesmo se a tela estiver mostrando "sem conexão".
  long envioId = (perguntaAtual.existe && perguntaAtual.envioId != envioAvisado) ? perguntaAtual.envioId : 0;
  Serial.printf("[botao] quero contar (envio %ld)\n", envioId);

  quemVaiLigar = envioId > 0 ? perguntaAtual.quemPerguntou : String("");
  if (envioId > 0) envioAvisado = envioId;

  if (!publicarQueroContar(envioId)) {
    avisoPendente = true;
    envioDoAvisoPendente = envioId;
    Serial.println("[botao] sem conexão: o aviso sai quando a conexão voltar");
  }

  tocar(MELODIA_AVISO, sizeof(MELODIA_AVISO) / sizeof(Nota));
  mudarTela(Tela::Agradecimento);
}

// ---------------------------------------------------------------------------
// Moldura de LEDs
// ---------------------------------------------------------------------------

uint32_t corComBrilho(uint8_t r, uint8_t g, uint8_t b, float nivel) {
  float f = constrain(nivel, 0.0f, 1.0f) * brilhoAtual / 255.0;
  return anel.Color(r * f, g * f, b * f);
}

void pintarAnel(uint32_t cor) {
  for (int i = 0; i < LEDS_DO_ANEL; i++) anel.setPixelColor(i, cor);
}

void atualizarAnel(unsigned long agora) {
  if (agora - ultimaAnimacao < INTERVALO_ANIMACAO) return;
  ultimaAnimacao = agora;
  unsigned long t = agora - inicioDaTela;

  switch (telaAtual) {
    case Tela::Conectando: {
      // Um ponto azul girando, com rastro.
      anel.clear();
      int cabeca = (t / 60) % LEDS_DO_ANEL;
      for (int k = 0; k < 5; k++) {
        int i = (cabeca - k + LEDS_DO_ANEL) % LEDS_DO_ANEL;
        anel.setPixelColor(i, corComBrilho(40, 90, 255, 1.0 - k * 0.2));
      }
      break;
    }
    case Tela::SemRede: {
      bool aceso = (t / 500) % 2 == 0;
      pintarAnel(aceso ? corComBrilho(255, 0, 0, 1.0) : 0);
      break;
    }
    case Tela::Ocioso:
      anel.clear();
      break;
    case Tela::PerguntaNova: {
      // Âmbar "respirando": chama a atenção sem assustar.
      float nivel = 0.55 + 0.45 * (0.5 - 0.5 * cos(2 * PI * (t % 3000) / 3000.0));
      pintarAnel(corComBrilho(255, 150, 0, nivel));
      break;
    }
    case Tela::Agradecimento: {
      float nivel;
      if (t < 800) nivel = ((t / 200) % 2 == 0) ? 1.0 : 0.0;  // pisca duas vezes
      else nivel = 1.0 - (float)(t - 800) / (TEMPO_AGRADECIMENTO - 800);  // e apaga devagar
      pintarAnel(corComBrilho(0, 255, 90, nivel));
      break;
    }
  }
  anel.show();
}

// ---------------------------------------------------------------------------
// Telinha
// ---------------------------------------------------------------------------

void desenharConectando() {
  escreverCentralizado("Me Conta", 14, 2);
  escreverCentralizado("conectando...", 40, 1);
}

void desenharSemRede(unsigned long agora) {
  tela.drawBitmap(4, 4, ICONE_SEM_REDE, 8, 8, SSD1306_WHITE);
  tela.setTextSize(1);
  tela.setCursor(18, 4);
  tela.print(simulandoQuedaDaRede || WiFi.status() != WL_CONNECTED ? "Sem Wi-Fi" : "Sem servidor");

  escreverCentralizado(paraTela("Sem conexão agora."), 22, 1);
  if (WiFi.status() != WL_CONNECTED || simulandoQuedaDaRede) {
    long falta = ((long)INTERVALO_RECONEXAO_WIFI - (long)(agora - ultimaTentativaWifi)) / 1000;
    if (wifiConectando) escreverCentralizado("Tentando conectar...", 34, 1);
    else escreverCentralizado("Tento de novo em " + String(max(falta, 0L)) + "s", 34, 1);
  } else {
    escreverCentralizado("Tentando conectar...", 34, 1);
  }
  if (avisoPendente) escreverCentralizado(paraTela("Seu aviso está guardado"), 50, 1);
}

void desenharOcioso() {
  struct tm agora;
  bool temHora = getLocalTime(&agora, 0) && agora.tm_year > 120;

  if (temHora) {
    char hora[6];
    strftime(hora, sizeof(hora), "%H:%M", &agora);
    escreverCentralizado(hora, 6, 3);
    const char* dias[] = {"dom", "seg", "ter", "qua", "qui", "sex", "sab"};
    char data[24];
    snprintf(data, sizeof(data), "%s, %02d/%02d", dias[agora.tm_wday], agora.tm_mday, agora.tm_mon + 1);
    escreverCentralizado(data, 32, 1);
  } else {
    escreverCentralizado("Me Conta", 10, 2);
  }

  tela.drawBitmap(4, 54, modoNoite ? ICONE_LUA : ICONE_CORACAO, 8, 8, SSD1306_WHITE);
  tela.setTextSize(1);
  tela.setCursor(16, 54);
  tela.print(paraTela(apelido));
  tela.setCursor(16, 44);
  tela.print("Nada novo por aqui");
}

void desenharPergunta(unsigned long agora) {
  // Faixa de cima: quem perguntou.
  tela.fillRect(0, 0, LARGURA_TELA, 10, SSD1306_WHITE);
  tela.setTextColor(SSD1306_BLACK);
  tela.setTextSize(1);
  tela.setCursor(2, 1);
  String cabecalho = paraTela(perguntaAtual.quemPerguntou) + " perguntou:";
  unsigned int espaco = perguntaAtual.esperando > 1 ? 17 : 21;  // deixa lugar para o "+1"
  if (cabecalho.length() > espaco) cabecalho = cabecalho.substring(0, espaco);
  tela.print(cabecalho);
  if (perguntaAtual.esperando > 1) {
    String mais = "+" + String(perguntaAtual.esperando - 1);
    tela.setCursor(LARGURA_TELA - 2 - mais.length() * 6, 1);
    tela.print(mais);
  }
  tela.setTextColor(SSD1306_WHITE);

  // A pergunta, em até 5 linhas; se for maior, rola devagar.
  String linhas[12];
  int total = quebrarEmLinhas(paraTela(perguntaAtual.texto), linhas, 12, CARACTERES_POR_LINHA);
  if (total > LINHAS_DA_PERGUNTA && agora - ultimaRolagem >= INTERVALO_ROLAGEM) {
    ultimaRolagem = agora;
    primeiraLinhaVisivel = (primeiraLinhaVisivel + 1) % (total - LINHAS_DA_PERGUNTA + 1);
  }
  for (int i = 0; i < LINHAS_DA_PERGUNTA && primeiraLinhaVisivel + i < total; i++) {
    tela.setCursor(1, 13 + i * 8);
    tela.print(linhas[primeiraLinhaVisivel + i]);
  }

  // Rodapé piscando devagar.
  if ((agora / 700) % 3 != 0) {
    tela.drawLine(0, 54, LARGURA_TELA - 1, 54, SSD1306_WHITE);
    escreverCentralizado("Aperte: quero contar", 56, 1);
  }
}

void desenharAgradecimento() {
  escreverCentralizado("Que bom!", 4, 2);
  String mensagem;
  if (quemVaiLigar.length() > 0) mensagem = quemVaiLigar + " já sabe! Logo vão te ligar para ouvir a sua história.";
  else mensagem = "Avisamos a família que você quer contar uma história.";
  if (avisoPendente) mensagem = "Sem internet agora. O aviso sai assim que ela voltar.";

  String linhas[4];
  int total = quebrarEmLinhas(paraTela(mensagem), linhas, 4, CARACTERES_POR_LINHA);
  tela.setTextSize(1);
  for (int i = 0; i < total; i++) {
    tela.setCursor(1, 24 + i * 10);
    tela.print(linhas[i]);
  }
}

void atualizarTela(unsigned long agora) {
  if (agora - ultimaTela < INTERVALO_TELA) return;
  ultimaTela = agora;

  tela.clearDisplay();
  tela.setTextColor(SSD1306_WHITE);
  tela.setTextSize(1);
  switch (telaAtual) {
    case Tela::Conectando: desenharConectando(); break;
    case Tela::SemRede: desenharSemRede(agora); break;
    case Tela::Ocioso: desenharOcioso(); break;
    case Tela::PerguntaNova: desenharPergunta(agora); break;
    case Tela::Agradecimento: desenharAgradecimento(); break;
  }

  // Só manda para a telinha se a imagem mudou: o envio pelo I2C é o que mais pesa.
  const uint8_t* imagem = tela.getBuffer();
  if (imagemJaEnviada && memcmp(imagem, ultimaImagemEnviada, sizeof(ultimaImagemEnviada)) == 0) return;
  memcpy(ultimaImagemEnviada, imagem, sizeof(ultimaImagemEnviada));
  imagemJaEnviada = true;
  tela.display();
}

// ---------------------------------------------------------------------------
// Início e laço principal
// ---------------------------------------------------------------------------

void setup() {
  Serial.begin(115200);
  Serial.println();
  Serial.println("Porta-retrato Me Conta");

  pinMode(PINO_BOTAO, INPUT_PULLUP);
  pinMode(PINO_TESTE_REDE, INPUT_PULLUP);
  pinMode(PINO_BUZZER, OUTPUT);

  anel.begin();
  anel.clear();
  anel.show();

  if (!tela.begin(SSD1306_SWITCHCAPVCC, 0x3C)) {
    Serial.println("[tela] não encontrada");
  }
  tela.cp437(true);
  tela.setTextWrap(false);

  String base = String("meconta/") + CODIGO_APARELHO;
  topicoPergunta = base + "/pergunta";
  topicoQueroContar = base + "/quero-contar";
  topicoStatus = base + "/status";
  topicoAmbiente = base + "/ambiente";
  Serial.printf("Tópicos: %s/...\n", base.c_str());

  mqtt.setServer(MQTT_SERVIDOR, MQTT_PORTA);
  mqtt.setCallback(aoReceberMensagem);
  mqtt.setBufferSize(1024);  // a pergunta pode ter até 200 caracteres
  // O Wokwi pode rodar mais devagar que o tempo real (ex.: 44%). Com 30 s de "keep alive"
  // e o ambiente publicado a cada 10 s, o broker não derruba a conexão por engano;
  // numa queda de verdade, ele percebe em até ~45 s e publica o "offline".
  mqtt.setKeepAlive(30);
  mqtt.setSocketTimeout(5);

  WiFi.mode(WIFI_STA);
  mudarTela(Tela::Conectando);
  tentarWifi(millis());
}

void loop() {
  unsigned long agora = millis();

  verificarChaveDeTeste(agora);
  atualizarRede(agora);
  atualizarLuz(agora);
  verificarBotao(agora);

  if (telaAtual == Tela::Agradecimento && agora - inicioDaTela >= TEMPO_AGRADECIMENTO) {
    if (mqtt.connected()) voltarAoNormal();
    else mudarTela(Tela::SemRede);
  }

  atualizarAnel(agora);
  atualizarTela(agora);
  atualizarSom(agora);
  delay(2);
}
