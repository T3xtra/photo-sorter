using System.Threading;
using System.Threading.Tasks;

namespace PhotoSorter.Core.Services.Abstractions;

/// <summary>
/// Decodes an image file into PNG-encoded bytes that any Avalonia <c>Bitmap</c> can construct
/// from, regardless of the source format or the current platform's native codec support.
/// Returning plain bytes (not an Avalonia type) keeps Core UI-framework-free: constructing an
/// Avalonia Bitmap requires its platform render interface to be initialized, which only exists
/// once the Avalonia application has started - not in Core or in unit tests.
/// </summary>
public interface IImageDecoder
{
    Task<byte[]> DecodeToPngAsync(string filePath, CancellationToken cancellationToken = default);
}
