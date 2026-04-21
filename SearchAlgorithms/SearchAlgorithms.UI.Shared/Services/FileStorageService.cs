using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SearchAlgorithms.UI.Shared.Services;

public sealed class FileStorageService
{
    public async Task<string?> PickAndReadTextAsync(Window owner, string title, string extension)
    {
        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel?.StorageProvider is null)
            return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType($"{extension.ToUpperInvariant()} files")
                {
                    Patterns = [$"*.{extension.TrimStart('.')}"],
                    AppleUniformTypeIdentifiers = [$"public.{extension.TrimStart('.')}"],
                    MimeTypes = ["application/json", "text/plain"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        if (file is null)
            return null;

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public async Task SaveTextAsync(Window owner, string title, string suggestedFileName, string extension, string content)
    {
        var topLevel = TopLevel.GetTopLevel(owner);
        if (topLevel?.StorageProvider is null)
            return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension.TrimStart('.'),
            FileTypeChoices =
            [
                new FilePickerFileType($"{extension.ToUpperInvariant()} files")
                {
                    Patterns = [$"*.{extension.TrimStart('.')}"],
                    AppleUniformTypeIdentifiers = [$"public.{extension.TrimStart('.')}"],
                    MimeTypes = ["application/json", "text/plain"]
                }
            ]
        });

        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(content);
    }
}
