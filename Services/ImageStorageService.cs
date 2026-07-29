namespace DbTransistorsApp.Services;

public class ImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg" };

    public string ImagesDirectory => Path.Combine(
        FileSystem.AppDataDirectory,
        "Images",
        "Packages");

    public async Task<string> SaveImageAsync(
        FileResult file,
        string? previousFileName = null,
        CancellationToken cancellationToken = default)
    {
        string extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidDataException("Solo se permiten imágenes PNG, JPG o JPEG.");

        Directory.CreateDirectory(ImagesDirectory);
        string safeStem = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
        string storedFileName = $"{safeStem}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        string targetPath = Path.Combine(ImagesDirectory, storedFileName);

        await using (var source = await file.OpenReadAsync())
        await using (var target = File.Create(targetPath))
        {
            await source.CopyToAsync(target, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(previousFileName) &&
            !string.Equals(previousFileName, storedFileName, StringComparison.OrdinalIgnoreCase))
        {
            DeleteImage(previousFileName);
        }

        return storedFileName;
    }

    public string? GetImagePath(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
            return null;

        string path = Path.Combine(ImagesDirectory, Path.GetFileName(storedFileName));
        return File.Exists(path) ? path : null;
    }

    public void DeleteImage(string? storedFileName)
    {
        string? path = GetImagePath(storedFileName);
        if (path != null)
            File.Delete(path);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Where(ch => !invalid.Contains(ch)).ToArray();
        string cleaned = new(chars);
        return string.IsNullOrWhiteSpace(cleaned) ? "encapsulado" : cleaned;
    }
}
