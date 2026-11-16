using MeConta.Privacidade.Motor;
using MeConta.Privacidade.Regras;
using MeConta.Privacidade.Resultados;

namespace MeConta.UnitTests.Privacidade;

/// <summary>
/// Testes do motor com memórias fictícias. Os CPFs, cartões e telefones são números de teste.
/// </summary>
public sealed class MotorPiiTests
{
    private readonly MotorPii _motor = MotorPii.CriarPadrao();

    // ---------- Casos de teste do backlog (CT41, CT42, CT43) ----------

    [Theory]
    [InlineData("meu CPF é 123.456.789-09")]
    [InlineData("meu CPF é 12345678909")]
    public void Analisar_CpfComOuSemPontuacao_BloqueiaEMostraOTrechoMascarado(string texto)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        AchadoPii achado = Assert.Single(resultado.Achados);
        Assert.Equal("Cpf", achado.Tipo);
        Assert.Equal(NivelDeRisco.Alto, achado.Nivel);
        Assert.True(resultado.DeveBloquear);
        Assert.EndsWith("09", achado.TrechoMascarado);
        Assert.DoesNotContain("456", achado.TrechoMascarado);
        Assert.Equal("meu CPF é [CPF removido]", resultado.TextoSeguro);
    }

    [Fact]
    public void Analisar_AnoENumeroDeRua_NaoTrataComoCpf()
    {
        ResultadoAnalisePii resultado = _motor.Analisar("nasceu em 1950 na rua 12");

        Assert.Empty(resultado.Achados);
        Assert.False(resultado.DeveBloquear);
        Assert.Equal(0, resultado.IndiceRiscoCidadao);
    }

    [Fact]
    public void Analisar_RemedioParaPressao_GeraAlertaDeSaudeComNivelMedio()
    {
        ResultadoAnalisePii resultado = _motor.Analisar("tomo remédio para pressão todo dia");

        AchadoPii achado = Assert.Single(resultado.Achados);
        Assert.Equal("Saude", achado.Tipo);
        Assert.Equal(NivelDeRisco.Medio, achado.Nivel);
        Assert.False(resultado.DeveBloquear);
        Assert.True(resultado.ExigeConfirmacao);
        Assert.Equal(NivelDeRisco.Medio, resultado.Faixa);
        Assert.Equal("tomo [dado de saúde removido] todo dia", resultado.TextoSeguro);
    }

    // ---------- Robustez: formas diferentes de escrever o mesmo dado ----------

    [Theory]
    [InlineData("Meu cpf é um dois três, quatro cinco seis, sete oito nove, traço zero nove")]
    [InlineData("cento e vinte e três quatrocentos e cinquenta e seis setecentos e oitenta e nove zero nove")]
    [InlineData("CPF 123 456 789 09")]
    [InlineData("CPF １２３.４５６.７８９-０９")]
    [InlineData("meu c​pf é 123.​456.789-09")]
    [InlineData("MEU CPF É 123.456.789/09")]
    public void Analisar_CpfFaladoComEspacosOuEscondido_Encontra(string texto)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Contains(resultado.Achados, achado => achado.Tipo == "Cpf");
        Assert.True(resultado.DeveBloquear);
    }

    [Fact]
    public void Analisar_CpfComDigitoErradoMasComContexto_AindaBloqueia()
    {
        // Um dígito trocado continua sendo uma tentativa de publicar o CPF.
        ResultadoAnalisePii resultado = _motor.Analisar("Meu CPF é 937.865.797-42, se não me engano.");

        AchadoPii achado = Assert.Single(resultado.Achados);
        Assert.Equal("Cpf", achado.Tipo);
        Assert.True(achado.Confianca < 0.9);
        Assert.True(resultado.DeveBloquear);
    }

    [Fact]
    public void Analisar_OnzeDigitosSemContextoEDigitoInvalido_NaoTrataComoCpf()
    {
        ResultadoAnalisePii resultado = _motor.Analisar("O pedido 12345678900 chegou ontem.");

        Assert.DoesNotContain(resultado.Achados, achado => achado.Tipo == "Cpf");
    }

    [Theory]
    [InlineData("o número do cartão é 4111 1111 1111 1111", "CartaoDeCredito")]
    [InlineData("A senha do banco é 4521, não conta pra ninguém", "Senha")]
    [InlineData("O código de verificação que chegou foi 482913", "CodigoDeVerificacao")]
    [InlineData("manda no pix: lourdes.souza@exemplo.com", "ChavePix")]
    [InlineData("A aposentadoria cai na agência 1234-5", "DadosBancarios")]
    [InlineData("conta corrente 98765-4", "DadosBancarios")]
    [InlineData("Meu RG é 12.345.678-9", "Rg")]
    public void Analisar_DadoDeRiscoAlto_Bloqueia(string texto, string tipo)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Contains(resultado.Achados, achado => achado.Tipo == tipo && achado.Nivel == NivelDeRisco.Alto);
        Assert.True(resultado.DeveBloquear);
        Assert.True(resultado.IndiceRiscoCidadao >= CalculadoraDeRiscoCidadao.InicioDaFaixaAlta);
    }

    [Theory]
    [InlineData("Liga pra mim no (11) 98765-4321", "Telefone")]
    [InlineData("Meu zap é onze nove oito sete seis cinco quatro três dois um.", "Telefone")]
    [InlineData("meu email é maria.souza@exemplo.com", "Email")]
    [InlineData("o email dela é tereza ponto lima arroba exemplo ponto com", "Email")]
    [InlineData("Eu moro na Rua das Flores, 123", "Endereco")]
    [InlineData("meu cep é 02998-000", "Cep")]
    [InlineData("nasci em 12/03/1948 em Minas", "DataDeNascimento")]
    [InlineData("Tenho diabetes desde os 60", "Saude")]
    [InlineData("Moro sozinha desde que seu avô se foi", "Vulnerabilidade")]
    [InlineData("Eu guardo dinheiro debaixo do colchão", "Vulnerabilidade")]
    public void Analisar_DadoDeRiscoMedio_PedeConfirmacaoSemBloquear(string texto, string tipo)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Contains(resultado.Achados, achado => achado.Tipo == tipo && achado.Nivel == NivelDeRisco.Medio);
        Assert.False(resultado.DeveBloquear);
        Assert.True(resultado.ExigeConfirmacao);
    }

    // ---------- Falsos positivos que o motor precisa evitar ----------

    [Theory]
    [InlineData("Feijão bom é na panela de pressão: 25 minutos depois que começar a chiar.")]
    [InlineData("Não tem remédio, minha filha: o que é da gente ninguém tira.")]
    [InlineData("Me conta 1950 foi um ano difícil, mas a gente era feliz com pouco.")]
    [InlineData("Sou de câncer, por isso choro em casamento.")]
    [InlineData("O chá de boldo era o remédio caseiro pra tudo.")]
    [InlineData("A receita leva duas xícaras de farinha e 250 gramas de manteiga.")]
    [InlineData("O ônibus 8700 passava de hora em hora.")]
    [InlineData("A conta da venda era anotada na caderneta.")]
    [InlineData("Comprei a primeira televisão em 12 prestações.")]
    [InlineData("ela me ensinou: cento e vinte gramas de açúcar")]
    [InlineData("Quando faltava luz, a gente contava história até a meia-noite.")]
    public void Analisar_MemoriaSemDadoPessoal_NaoGeraAlerta(string texto)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Empty(resultado.Achados);
        Assert.Equal(texto, resultado.TextoSeguro);
        Assert.Equal(NivelDeRisco.Baixo, resultado.Faixa);
    }

    // ---------- Combinações, sobreposições e saída segura ----------

    [Fact]
    public void Analisar_EnderecoEMoraSozinha_SomaBonusDaCombinacao()
    {
        ResultadoAnalisePii resultado = _motor.Analisar("Moro sozinha na Rua das Palmeiras, 45.");

        CombinacaoEncontrada combinacao = Assert.Single(resultado.Combinacoes);
        Assert.Equal("COMB-ENDERECO-ROTINA", combinacao.Id);
        Assert.Equal(NivelDeRisco.Alto, resultado.Faixa);
        Assert.False(resultado.DeveBloquear);
    }

    [Fact]
    public void Analisar_ChavePixQueEhTelefone_FicaSoComOAchadoMaisGrave()
    {
        ResultadoAnalisePii resultado = _motor.Analisar("minha chave pix é 11 98877-6655");

        AchadoPii achado = Assert.Single(resultado.Achados);
        Assert.Equal("ChavePix", achado.Tipo);
    }

    [Fact]
    public void Analisar_VariosDados_TextoSeguroEAchadosNaoTrazemNenhumValorOriginal()
    {
        string texto = "Meu CPF é 628.194.821-12, liga no (11) 97654-3210 ou escreve pra lourdes@exemplo.com";

        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Equal(3, resultado.Achados.Count);
        foreach (string valor in new[] { "628.194.821", "97654", "lourdes@" })
        {
            Assert.DoesNotContain(valor, resultado.TextoSeguro);
            Assert.DoesNotContain(resultado.Achados, achado => achado.TrechoMascarado.Contains(valor, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Analisar_Achados_ApontamParaOTrechoCertoDoTextoOriginal()
    {
        string texto = "Anotado: CPF 526.018.159-06 e telefone (11) 98765-4321.";

        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        string trechos = string.Join(" | ", resultado.Achados.Select(achado => texto.Substring(achado.Inicio, achado.Tamanho)));
        Assert.Equal("526.018.159-06 | (11) 98765-4321", trechos);
    }

    // ---------- Falha fechada ----------

    [Fact]
    public void Analisar_TextoMaiorQueOLimite_BloqueiaSemAnalisar()
    {
        MotorPii motor = new(CatalogoDeRegras.CarregarPadrao(), new OpcoesDoMotorPii { TamanhoMaximoDoTexto = 20 });

        ResultadoAnalisePii resultado = motor.Analisar("Uma memória comprida demais para o limite.");

        Assert.False(resultado.AnaliseCompleta);
        Assert.True(resultado.DeveBloquear);
        Assert.Equal(100, resultado.IndiceRiscoCidadao);
        Assert.Equal(MotorPii.AvisoTextoNaoVerificado, resultado.TextoSeguro);
    }

    [Fact]
    public void Analisar_ExpressaoQuePassaDoTempo_BloqueiaEmVezDeLiberar()
    {
        // Regra de propósito lenta: (a+)+ com um texto que não fecha trava a expressão.
        const string json = """
        {
          "versao": "teste",
          "regras": [{
            "id": "LENTA", "tipo": "Teste", "rotulo": "Teste", "nivel": "Medio",
            "padrao": "(a+)+b", "mascara": "Total",
            "substituicao": "[removido]", "explicacao": "teste"
          }]
        }
        """;
        MotorPii motor = new(CatalogoDeRegras.CarregarDeJson(json, TimeSpan.FromMilliseconds(20)));

        ResultadoAnalisePii resultado = motor.Analisar(new string('a', 5000) + "!");

        Assert.False(resultado.AnaliseCompleta);
        Assert.True(resultado.DeveBloquear);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Analisar_TextoVazio_SemAchadosESemBloqueio(string texto)
    {
        ResultadoAnalisePii resultado = _motor.Analisar(texto);

        Assert.Empty(resultado.Achados);
        Assert.False(resultado.DeveBloquear);
        Assert.Equal(0, resultado.IndiceRiscoCidadao);
    }
}
