// Casos de teste das regras de taint (semgrep --test seguranca/semgrep).
// "ruleid" marca a linha que a regra PRECISA apontar; "ok" marca a linha que ela NÃO pode apontar.
// Este arquivo não é compilado: fica fora de qualquer projeto da solução.

public sealed class CasosDeLog
{
    private readonly ILogger<CasosDeLog> logger;
    private readonly IMotorPii motor;

    public void Vazamentos(PaginaCaderno pagina, PublicarMemoriaRequest request, HttpContext httpContext)
    {
        // ruleid: meconta-pii-para-log
        logger.LogInformation("Transcrição recebida: {Texto}", pagina.Transcricao);

        string titulo = pagina.Titulo;
        // ruleid: meconta-pii-para-log
        logger.LogWarning($"Publicando {titulo}");

        // ruleid: meconta-pii-para-log
        Console.WriteLine("Bairro pedido: " + request.Bairro);

        // ruleid: meconta-pii-para-log
        logger.LogError("Erro em {Caminho}", httpContext.Request.Path);

        // ruleid: meconta-pii-para-log
        File.AppendAllText("auditoria.txt", pagina.Transcricao);
    }

    public void Correto(PaginaCaderno pagina, HttpContext httpContext)
    {
        // ok: meconta-pii-para-log
        logger.LogInformation("Página {Id} recebida", pagina.Id);

        TextoVerificado verificado = motor.Verificar(new TextoDoUsuario(pagina.Transcricao));
        // ok: meconta-pii-para-log
        logger.LogInformation("Transcrição segura: {Texto}", verificado.ParaRegistro);

        string seguro = motor.Analisar(pagina.Titulo).TextoSeguro;
        // ok: meconta-pii-para-log
        logger.LogInformation("Título seguro: {Titulo}", seguro);

        // ok: meconta-pii-para-log
        logger.LogInformation("Transcrição com {Tamanho} caracteres", pagina.Transcricao.Length);

        string rota = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "desconhecida";
        // ok: meconta-pii-para-log
        logger.LogError("Erro na rota {Rota}", rota);
    }
}

public sealed class AparelhoPortaRetratoCasos
{
    private readonly IMotorPii motor;

    public EstadoPortaRetratoResponse Vazamento(EnvioPergunta envio, Idoso idoso)
    {
        PerguntaNoPortaRetrato naTela = new(envio.Id, "Bia", envio.TextoPergunta, envio.EnviadoEm);
        // ruleid: meconta-pii-para-porta-retrato
        return new EstadoPortaRetratoResponse("k7m2q9xd", "Vó Lourdes", true, 1, naTela);
    }

    public PerguntaNoPortaRetrato VazamentoComNew(EnvioPergunta envio)
    {
        // ruleid: meconta-pii-para-porta-retrato
        return new PerguntaNoPortaRetrato(envio.Id, "Bia", envio.TextoPergunta, envio.EnviadoEm);
    }

    public AvisoResponse VazamentoNoAviso(AvisoQueroContar aviso, Idoso idoso, EnvioPergunta envio)
    {
        // ruleid: meconta-pii-para-porta-retrato
        return AvisoResponse.De(aviso, idoso.Apelido, envio.TextoPergunta);
    }

    public PerguntaNoPortaRetrato Correto(EnvioPergunta envio)
    {
        string textoSeguro = motor.Verificar(new TextoDoUsuario(envio.TextoPergunta)).ParaRegistro;
        // ok: meconta-pii-para-porta-retrato
        return new PerguntaNoPortaRetrato(envio.Id, "Bia", textoSeguro, envio.EnviadoEm);
    }
}

public sealed class CasosDeIa
{
    private readonly HttpClient http;
    private readonly IMotorPii motor;

    public async Task Vazamento(PaginaCaderno pagina)
    {
        // ruleid: meconta-pii-para-ia
        await http.PostAsJsonAsync("https://generativelanguage.googleapis.com/v1beta/models", new { texto = pagina.Transcricao });

        string prompt = "Explique o risco deste texto: " + pagina.Titulo;
        // ruleid: meconta-pii-para-ia
        StringContent corpo = new StringContent(prompt);
    }

    public async Task Correto(PaginaCaderno pagina, AchadoPii achado)
    {
        string seguro = motor.Analisar(pagina.Transcricao).TextoSeguro;
        // ok: meconta-pii-para-ia
        await http.PostAsJsonAsync("https://generativelanguage.googleapis.com/v1beta/models", new { texto = seguro });

        // ok: meconta-pii-para-ia
        await http.PostAsJsonAsync("https://generativelanguage.googleapis.com/v1beta/models", new { trecho = achado.TrechoMascarado });
    }
}

public sealed class CasosDoMapa
{
    private readonly IMotorPii motor;

    public MemoriaDoBairro Vazamento(PaginaCaderno pagina, PublicarMemoriaRequest request, Membro membro)
    {
        // ruleid: meconta-pii-para-mapa-do-bairro
        return new MemoriaDoBairro(pagina, "Pirituba", true, pagina.Titulo, null, 0, membro.Id, DateTime.UtcNow);
    }

    public MemoriaDoBairro VazamentoDoBairro(PaginaCaderno pagina, PublicarMemoriaRequest request, Membro membro, string titulo)
    {
        // ruleid: meconta-pii-para-mapa-do-bairro
        return new MemoriaDoBairro(pagina, request.Bairro, true, titulo, null, 0, membro.Id, DateTime.UtcNow);
    }

    public MemoriaDoBairro Correto(PaginaCaderno pagina, Membro membro, CamposVerificados verificados)
    {
        string titulo = verificados.ConteudoParaPublicar("Titulo", false);
        string? transcricao = verificados.ConteudoParaPublicar("Transcricao", false);
        // ok: meconta-pii-para-mapa-do-bairro
        return new MemoriaDoBairro(pagina, "Pirituba", true, titulo, transcricao, 0, membro.Id, DateTime.UtcNow);
    }
}

public sealed class CasosDeTextoBruto
{
    public string Desvio(TextoDoUsuario texto)
    {
        // ruleid: meconta-texto-bruto-fora-do-motor
        return texto.ValorBruto;
    }
}
