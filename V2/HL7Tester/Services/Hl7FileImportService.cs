using System.Text;
using Microsoft.Maui.Controls;
#if WINDOWS
using Windows.Storage;
using WinRT;
#endif
#if MACCATALYST
using Foundation;
#endif

namespace HL7Tester.Services;

public sealed record Hl7FileImportResult(
    bool Success,
    string? Content,
    string? FileName,
    string? ErrorMessage,
    bool MultipleFilesDropped = false)
{
    public static Hl7FileImportResult Ok(string content, string fileName, bool multipleFilesDropped)
        => new(true, content, fileName, null, multipleFilesDropped);

    public static Hl7FileImportResult Fail(string errorMessage, bool multipleFilesDropped = false)
        => new(false, null, null, errorMessage, multipleFilesDropped);
}

public static class Hl7FileImportService
{
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".hl7",
        ".h7",
        ".txt",
        ".msg",
        ".dat",
        ".edi",
        ".log"
    };

    public static async Task<Hl7FileImportResult> ImportDroppedContentAsync(DropEventArgs dropEventArgs)
    {
        ArgumentNullException.ThrowIfNull(dropEventArgs);

        var fileImportResult = await TryImportPlatformFilesAsync(dropEventArgs).ConfigureAwait(false);
        if (fileImportResult != null)
        {
            return fileImportResult;
        }

        var text = await dropEventArgs.Data.GetTextAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(text))
        {
            text = NormalizeText(text);

            if (File.Exists(text))
            {
                return await ImportFilePathAsync(text, multipleFilesDropped: false).ConfigureAwait(false);
            }

            return Hl7FileImportResult.Ok(text, "Dropped text", multipleFilesDropped: false);
        }

        return Hl7FileImportResult.Fail("No supported text file or HL7 text content was dropped.");
    }

    private static async Task<Hl7FileImportResult> ImportFilePathAsync(string filePath, bool multipleFilesDropped)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(fileName);

        var extensionValidationError = ValidateExtension(extension, multipleFilesDropped);
        if (extensionValidationError != null)
        {
            return extensionValidationError;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await ReadStreamAsync(stream, fileName, multipleFilesDropped).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Hl7FileImportResult.Fail($"Unable to read dropped file: {ex.Message}", multipleFilesDropped);
        }
    }

    private static async Task<Hl7FileImportResult> ReadStreamAsync(Stream stream, string fileName, bool multipleFilesDropped)
    {
        if (stream.CanSeek && stream.Length > MaxFileSizeBytes)
        {
            return Hl7FileImportResult.Fail(
                "The dropped file is too large. Maximum supported size is 2 MB.",
                multipleFilesDropped);
        }

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096,
            leaveOpen: false);

        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
        content = NormalizeText(content);

        if (string.IsNullOrWhiteSpace(content))
        {
            return Hl7FileImportResult.Fail("The dropped file is empty.", multipleFilesDropped);
        }

        return Hl7FileImportResult.Ok(content, fileName, multipleFilesDropped);
    }

    private static Hl7FileImportResult? ValidateExtension(string extension, bool multipleFilesDropped)
    {
        if (SupportedExtensions.Contains(extension))
        {
            return null;
        }

        return Hl7FileImportResult.Fail(
            $"Unsupported file type '{extension}'. Supported types: {string.Join(", ", SupportedExtensions.OrderBy(x => x))}.",
            multipleFilesDropped);
    }

    private static Task<Hl7FileImportResult?> TryImportPlatformFilesAsync(DropEventArgs dropEventArgs)
    {
#if WINDOWS
        return TryImportWindowsFilesAsync(dropEventArgs);
#elif MACCATALYST
        return TryImportMacCatalystFilesAsync(dropEventArgs);
#else
        return Task.FromResult<Hl7FileImportResult?>(null);
#endif
    }

#if WINDOWS
    private static async Task<Hl7FileImportResult?> TryImportWindowsFilesAsync(DropEventArgs dropEventArgs)
    {
        var platformDragEventArgs = dropEventArgs.PlatformArgs?.DragEventArgs;
        if (platformDragEventArgs?.DataView == null || !platformDragEventArgs.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
        {
            return null;
        }

        var storageItems = await platformDragEventArgs.DataView.GetStorageItemsAsync().AsTask().ConfigureAwait(false);
        if (storageItems.Count == 0)
        {
            return Hl7FileImportResult.Fail("No file was dropped.");
        }

        bool multipleFilesDropped = storageItems.Count > 1;
        var firstItem = storageItems[0];

        if (firstItem is StorageFile storageFile)
        {
            var extensionValidationError = ValidateExtension(storageFile.FileType, multipleFilesDropped);
            if (extensionValidationError != null)
            {
                return extensionValidationError;
            }

            await using var stream = await storageFile.OpenStreamForReadAsync().ConfigureAwait(false);
            return await ReadStreamAsync(stream, storageFile.Name, multipleFilesDropped).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(firstItem.Path) && File.Exists(firstItem.Path))
        {
            return await ImportFilePathAsync(firstItem.Path, multipleFilesDropped).ConfigureAwait(false);
        }

        return Hl7FileImportResult.Fail("The dropped item is not a supported file.", multipleFilesDropped);
    }
#endif

#if MACCATALYST
    private static async Task<Hl7FileImportResult?> TryImportMacCatalystFilesAsync(DropEventArgs dropEventArgs)
    {
        var items = dropEventArgs.PlatformArgs?.DropSession?.Items;
        if (items == null || items.Length == 0)
        {
            return null;
        }

        bool multipleFilesDropped = items.Length > 1;
        var provider = items[0].ItemProvider;
        if (provider == null)
        {
            return Hl7FileImportResult.Fail("The dropped item is not a supported file.", multipleFilesDropped);
        }

        foreach (var typeIdentifier in provider.RegisteredTypeIdentifiers ?? Array.Empty<string>())
        {
            var fileResult = await TryImportMacCatalystProviderFileAsync(provider, typeIdentifier, multipleFilesDropped).ConfigureAwait(false);
            if (fileResult != null)
            {
                return fileResult;
            }
        }

        foreach (var typeIdentifier in provider.RegisteredTypeIdentifiers ?? Array.Empty<string>())
        {
            var dataResult = await TryImportMacCatalystProviderDataAsync(provider, typeIdentifier, multipleFilesDropped).ConfigureAwait(false);
            if (dataResult != null)
            {
                return dataResult;
            }
        }

        return Hl7FileImportResult.Fail("No supported text file or HL7 text content was dropped.", multipleFilesDropped);
    }

    private static async Task<Hl7FileImportResult?> TryImportMacCatalystProviderFileAsync(
        NSItemProvider provider,
        string typeIdentifier,
        bool multipleFilesDropped)
    {
        try
        {
            var fileUrl = await provider.LoadFileRepresentationAsync(typeIdentifier).ConfigureAwait(false);
            var path = fileUrl?.Path;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            var fileName = Path.GetFileName(path);
            var extension = Path.GetExtension(fileName);

            if (!SupportedExtensions.Contains(extension))
            {
                return null;
            }

            return await ImportFilePathAsync(path, multipleFilesDropped).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<Hl7FileImportResult?> TryImportMacCatalystProviderDataAsync(
        NSItemProvider provider,
        string typeIdentifier,
        bool multipleFilesDropped)
    {
        try
        {
            var data = await provider.LoadDataRepresentationAsync(typeIdentifier).ConfigureAwait(false);
            if (data == null || data.Length == 0)
            {
                return null;
            }

            var bytes = data.ToArray();
            await using var stream = new MemoryStream(bytes, writable: false);
            return await ReadStreamAsync(stream, "Dropped text", multipleFilesDropped).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
#endif

    private static string NormalizeText(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        return content
            .TrimStart('\uFEFF')
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Replace("\n", Environment.NewLine, StringComparison.Ordinal);
    }
}