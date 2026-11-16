namespace MeConta.Infrastructure.Persistencia;

/// <summary>
/// Som curto (1,5 s, um tom de 440 Hz em Ogg/Opus) usado como áudio das páginas de demonstração.
/// Não é a voz de ninguém: serve só para o botão "Ouvir" funcionar nos testes.
/// </summary>
internal static class AudioDeDemonstracao
{
    public const string NomeArquivo = "demonstracao.ogg";

    public const int DuracaoSegundos = 2;

    private const string Base64 =
        "T2dnUwACAAAAAAAAAAC7N/HnAAAAAF3FpZgBE09wdXNIZWFkAQE4AYC7AAAAAABPZ2dTAAAAAAAAAAAAALs38ecBAAAADF9Q6gE+" +
        "T3B1c1RhZ3MNAAAATGF2ZjYwLjE2LjEwMAEAAAAdAAAAZW5jb2Rlcj1MYXZjNjAuMzEuMTAyIGxpYm9wdXNPZ2dTAACAuwAAAAAA" +
        "ALs38ecCAAAAHIQHuTIwJyIjIyMjJCUhISEhIB8jHiAeICAhIR8iISAgICAgIiAgHiEhHyAgJCEfHyIhIR8hH0iAsYmRQQrvAAD9" +
        "D492VLhF8yDX5k0eDrw+ZZERif5c0k5k8M0eoXKfNgjuzkn860iJAn4JURiCA6+Bi7M5Rxm4YqiLxF6PzpFISRuPnUJfDz+mU3sK" +
        "dkiHjp7SEU6PeKKX/yS8PjJlcKgNT7K6kPfEW9ouFmBGVTBIh46e0hFHV1CrSz2BPhjmTuamVlg0iyiQ3PwW5mp+GfZa9EiHjp7S" +
        "EUQx2110AWVIPPD2DR/ABqByYVrXghVfOGgJ06pQSIeOntIRRDtALPzvXq/vWb4fc8Y2xP8VjMT6QoO7kNZt+5xIh46e0hFHkYZ0" +
        "chysRLl4o5zLx0tvK6wjfc0D+ryIM9cYgEiHjp7SEUQ9QMfXIcGWEPSZTHFg+5X4cDmxfJ+ebufcY2Zbz0iHjp7SEU5CMzHmgrJu" +
        "jx9DIyTOGPY7E3hsXZZqz18PJQyGW6BIh46e0hFEG418eGc31cqj9WmVrauZ6PD/IvMdNwzIazBIh46e0hFEOz7IVLgn9YTHcfH4" +
        "JDQ4N0ViGJ0CtljidpBIh46e0hFK7KJk3ij6BfmLqRhHB5medCiYmOC78GJGL7BIJ7VPZOHiNZrCGGvSienRir0V3vAW7zKTDb1m" +
        "LCfkpi1IJ7VPZOHzBHCLT3nJd/fg1TyYQD6/c92/PM/5XaUGBEgntU9k4dNWysskYzJs4703V2cUmlBUJu0BugGmckBIJ7VPZOHT" +
        "W9FaVkBQdiB9aPPkrWoyDTUhdxt0lKMjU6zwwEgntU9k4fLLLs62fxenK7TRG/pf0f46+jBpyWtBsEgntU9k4eIpnCPTco/xCqVx" +
        "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" +
        "iOLTZOgKcA==";

    public static byte[] Bytes => Convert.FromBase64String(Base64);
}
