<#
.SYNOPSIS
  Simula famílias usando o filtro de dados pessoais, para o painel Risco Cidadão mudar ao vivo (vídeo da entrega).

.DESCRIPTION
  Faz login como Bia, pega páginas do caderno da Família Souza e, a cada rodada, troca a transcrição de uma
  página por uma memória fictícia, pede a análise do filtro e tenta publicar no mapa de um bairro.
  Memórias com dado de risco alto são barradas (422); com risco médio, metade das vezes a família confirma.
  O que foi publicado sai do mapa logo depois, e no fim as transcrições originais voltam.
  A cada três rodadas, mostra a explicação da IA para a família.

  Todos os dados são fictícios. O CPF 123.456.789-09 e o cartão 4111 1111 1111 1111 são números de teste.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File seguranca\demo\simular-atividade.ps1
  powershell -ExecutionPolicy Bypass -File seguranca\demo\simular-atividade.ps1 -Rodadas 20 -Pausa 3
#>
param(
    [string]$Api = "http://localhost:5080",
    [string]$Email = "bia@example.com",
    [string]$Senha = "MeConta@2026",
    [int]$FamiliaId = 1,
    [int]$Rodadas = 12,
    [int]$Pausa = 4
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http

# Memórias fictícias e o que o filtro deve fazer com cada uma.
$memorias = @(
    @{ Texto = "Quando comecei na fábrica, o crachá tinha meu CPF 123.456.789-09 escrito embaixo da foto."; Espera = "CPF" },
    @{ Texto = "Aos domingos a família inteira almoçava debaixo do pé de jabuticaba."; Espera = "nada" },
    @{ Texto = "Depois dos setenta passei a tomar remédio de pressão, mas nunca deixei de ir ao baile."; Espera = "saúde" },
    @{ Texto = "Quem quiser a receita do bolo pode ligar no (11) 98765-4321 que eu explico."; Espera = "telefone" },
    @{ Texto = "Meu neto anotou num papel que a senha do banco é 4821, e eu guardei na carteira."; Espera = "senha" },
    @{ Texto = "A primeira vez que vi o mar foi em Santos, de trem, com a minha irmã."; Espera = "nada" },
    @{ Texto = "A casa da Rua das Flores, 123 tinha um portão azul. Hoje moro sozinha e de tarde fico sozinha em casa."; Espera = "endereço e rotina" },
    @{ Texto = "Comprei a primeira televisão a prazo, no cartão 4111 1111 1111 1111, em dez vezes."; Espera = "cartão" },
    @{ Texto = "Mandei as fotos da festa junina para o e-mail vo.lourdes@example.com."; Espera = "e-mail" },
    @{ Texto = "Um moço ligou e pediu o código que chegou por SMS, 482913. Quase passei!"; Espera = "código" },
    @{ Texto = "Eu dançava forró no clube do bairro todo sábado, com o vestido que a minha mãe costurou."; Espera = "nada" }
)
$bairros = @("Pirituba", "Perus", "Freguesia do Ó", "Lapa", "Jaraguá")

$http = [System.Net.Http.HttpClient]::new()
$http.BaseAddress = [Uri]$Api

# Envia JSON em UTF-8 (acentos chegam certos também no Windows PowerShell 5.1).
function Chamar([string]$metodo, [string]$caminho, $corpo = $null) {
    $pedido = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($metodo), $caminho)
    if ($null -ne $corpo) {
        $json = $corpo | ConvertTo-Json -Depth 5 -Compress
        $pedido.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, "application/json")
    }
    $resposta = $http.SendAsync($pedido).GetAwaiter().GetResult()
    $texto = $resposta.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $dados = $null
    if ($texto) { try { $dados = $texto | ConvertFrom-Json } catch { $dados = $null } }
    [pscustomobject]@{ Status = [int]$resposta.StatusCode; Dados = $dados }
}

function Escrever([string]$texto, [string]$cor = "Gray") { Write-Host $texto -ForegroundColor $cor }

# ---- Login ----
$login = Chamar "POST" "/api/v1/auth/login" @{ email = $Email; password = $Senha }
if ($login.Status -ne 200) { throw "Login falhou (HTTP $($login.Status)). A API está rodando em $Api?" }
$http.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $login.Dados.accessToken)
Escrever "Login como $Email." "Green"

# ---- Páginas do caderno ----
$caderno = Chamar "GET" "/api/v1/familias/$FamiliaId/caderno?tamanho=50"
if ($caderno.Status -ne 200 -or $caderno.Dados.itens.Count -eq 0) {
    throw "Nenhuma página no caderno da família $FamiliaId. Suba a API com --Banco:PaginasDoFiltroDePrivacidade=true (bloco L do roteiro)."
}
$paginas = @($caderno.Dados.itens | Select-Object -First 4)
$originais = @{}
foreach ($p in $paginas) { $originais[$p.id] = $p.transcricao }
$ocupadas = @{}
Escrever "Usando as páginas $(($paginas | ForEach-Object { $_.id }) -join ', '). Painel: $Api/risco-cidadao/" "Green"
Escrever ""

try {
    for ($rodada = 1; $rodada -le $Rodadas; $rodada++) {
        $livres = @($paginas | Where-Object { -not $ocupadas.ContainsKey($_.id) })
        if ($livres.Count -eq 0) { Escrever "Todas as páginas já estão no mapa. Fim." "Yellow"; break }

        $pagina = $livres[($rodada - 1) % $livres.Count]
        $memoria = $memorias[($rodada - 1) % $memorias.Count]
        $bairro = $bairros[($rodada - 1) % $bairros.Count]
        $confirmar = ($rodada % 2 -eq 0)

        Escrever ("[{0}/{1}] Página {2} · {3} · esperado: {4}" -f $rodada, $Rodadas, $pagina.id, $bairro, $memoria.Espera) "Cyan"

        [void](Chamar "PUT" "/api/v1/paginas/$($pagina.id)/transcricao" @{ texto = $memoria.Texto })

        $analise = Chamar "POST" "/api/v1/paginas/$($pagina.id)/analise-privacidade"
        $dadosEncontrados = ($analise.Dados.achados | ForEach-Object { "$($_.rotulo) $($_.trechoMascarado)" }) -join "; "
        if (-not $dadosEncontrados) { $dadosEncontrados = "nenhum" }
        Escrever ("      filtro: {0}, índice {1} · dados: {2}" -f $analise.Dados.resultado, $analise.Dados.indiceRiscoCidadao, $dadosEncontrados)

        $publicacao = Chamar "POST" "/api/v1/paginas/$($pagina.id)/publicacao" @{ bairro = $bairro; idosoAutorizou = $true; confirmoDadosPessoais = $confirmar }
        switch ($publicacao.Status) {
            201 {
                Escrever "      publicada no mapa de $bairro$(if ($confirmar) { ' (família confirmou)' })" "Green"
                [void](Chamar "DELETE" "/api/v1/paginas/$($pagina.id)/publicacao")
            }
            422 { Escrever "      barrada: $($publicacao.Dados.analise.resultado)" "Red" }
            409 { Escrever "      já estava no mapa: página deixada de lado" "Yellow"; $ocupadas[$pagina.id] = $true }
            default { Escrever "      publicação respondeu HTTP $($publicacao.Status)" "Yellow" }
        }

        if ($rodada % 3 -eq 0 -and $analise.Dados.achados.Count -gt 0) {
            $explicacao = Chamar "GET" "/api/v1/paginas/$($pagina.id)/explicacao-de-risco"
            if ($explicacao.Status -eq 200 -and $explicacao.Dados.alertas.Count -gt 0) {
                $alerta = $explicacao.Dados.alertas[0]
                Escrever "      IA ($($explicacao.Dados.fonte)): $($alerta.alerta)" "Magenta"
                Escrever "         risco real: $($alerta.riscoReal)" "Magenta"
            }
        }

        Start-Sleep -Seconds $Pausa
    }
}
finally {
    # Devolve as transcrições originais das páginas usadas.
    foreach ($p in $paginas) {
        [void](Chamar "PUT" "/api/v1/paginas/$($p.id)/transcricao" @{ texto = $originais[$p.id] })
    }
    Escrever ""
    Escrever "Transcrições originais devolvidas. Veja o painel: $Api/risco-cidadao/" "Green"
    $http.Dispose()
}
