using MeConta.Application.Adapters;
using MeConta.Domain.ValueObjects;

namespace MeConta.Infrastructure.Adapters;

/// <summary>
/// Guarda áudios e fotos numa pasta do servidor (ex.: storage/audios, storage/fotos).
/// O nome do arquivo é sempre um Guid gerado aqui: o nome enviado pelo celular nunca vira caminho.
/// </summary>
public sealed class ArmazenamentoEmDiscoAdapter : IArmazenamentoDeArquivos
{
    private const int TamanhoDoBuffer = 81920;

    private readonly string _pastaBase;

    public ArmazenamentoEmDiscoAdapter(string pastaBase)
    {
        if (string.IsNullOrWhiteSpace(pastaBase))
        {
            throw new ArgumentException("Informe a pasta de armazenamento.", nameof(pastaBase));
        }

        _pastaBase = Path.GetFullPath(pastaBase);
        Directory.CreateDirectory(_pastaBase);
    }

    public async Task<string> SalvarAsync(
        Stream conteudo,
        ArquivoDeMidia arquivo,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        ArgumentNullException.ThrowIfNull(arquivo);

        string relativo = $"{arquivo.Pasta}/{Guid.NewGuid():N}{arquivo.Extensao}";
        string completo = CaminhoCompleto(relativo);
        Directory.CreateDirectory(Path.GetDirectoryName(completo)!);

        await using FileStream destino = new(
            completo,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            TamanhoDoBuffer,
            useAsync: true);
        await conteudo.CopyToAsync(destino, cancellationToken);

        return relativo;
    }

    public Task<Stream?> AbrirAsync(string caminho, CancellationToken cancellationToken = default)
    {
        string completo = CaminhoCompleto(caminho);
        if (!File.Exists(completo))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream leitura = new FileStream(
            completo,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            TamanhoDoBuffer,
            useAsync: true);
        return Task.FromResult<Stream?>(leitura);
    }

    public Task RemoverAsync(string caminho, CancellationToken cancellationToken = default)
    {
        try
        {
            string completo = CaminhoCompleto(caminho);
            if (File.Exists(completo))
            {
                File.Delete(completo);
            }
        }
        catch (IOException)
        {
            // Arquivo em uso: fica para trás, mas a operação principal não falha por isso.
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Junta o caminho relativo à pasta base e recusa qualquer caminho que saia dela (ex.: "../").
    /// </summary>
    private string CaminhoCompleto(string relativo)
    {
        string completo = Path.GetFullPath(Path.Combine(_pastaBase, relativo));
        string pastaComSeparador = _pastaBase.EndsWith(Path.DirectorySeparatorChar)
            ? _pastaBase
            : _pastaBase + Path.DirectorySeparatorChar;

        if (!completo.StartsWith(pastaComSeparador, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho fora da pasta de armazenamento.");
        }

        return completo;
    }
}
