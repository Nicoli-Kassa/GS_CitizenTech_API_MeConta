using MeConta.Domain.ValueObjects;

namespace MeConta.Application.Adapters;

/// <summary>
/// Onde ficam os áudios e as fotos. Hoje é uma pasta no disco;
/// trocar por um serviço de nuvem exige só outra implementação desta interface.
/// </summary>
public interface IArmazenamentoDeArquivos
{
    /// <summary>Salva o arquivo e devolve o caminho relativo gerado. Ex.: "audios/3f2a...opus".</summary>
    Task<string> SalvarAsync(Stream conteudo, ArquivoDeMidia arquivo, CancellationToken cancellationToken = default);

    /// <summary>Abre o arquivo para leitura, ou devolve <see langword="null"/> se ele não existir.</summary>
    Task<Stream?> AbrirAsync(string caminho, CancellationToken cancellationToken = default);

    Task RemoverAsync(string caminho, CancellationToken cancellationToken = default);
}
