namespace DbTransistorsApp.Services;

public sealed record SavedFileInfo(
    string FileName,
    string DisplayLocation,
    string? LocalPath = null,
    string? ContentUri = null);

public class DownloadFileService
{
    public async Task<SavedFileInfo> SaveToDownloadsAsync(
        string fileName,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("El nombre del archivo es obligatorio.", nameof(fileName));

        if (content.CanSeek)
            content.Position = 0;

#if ANDROID
        return await SaveOnAndroidAsync(fileName, content, mimeType, cancellationToken);
#elif WINDOWS
        return await SaveOnWindowsAsync(fileName, content, cancellationToken);
#else
        string folder = Path.Combine(FileSystem.AppDataDirectory, "Exports");
        Directory.CreateDirectory(folder);
        string path = GetNonConflictingPath(folder, fileName);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return new SavedFileInfo(Path.GetFileName(path), folder, path);
#endif
    }

#if ANDROID
    private static async Task<SavedFileInfo> SaveOnAndroidAsync(
        string fileName,
        Stream content,
        string mimeType,
        CancellationToken cancellationToken)
    {
        var context = Android.App.Application.Context;

        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q)
        {
            var resolver = context.ContentResolver
                ?? throw new InvalidOperationException("No se pudo acceder al almacenamiento de Android.");

            using var values = new Android.Content.ContentValues();
            values.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, fileName);
            values.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, mimeType);
            values.Put(
                Android.Provider.MediaStore.IMediaColumns.RelativePath,
                $"{Android.OS.Environment.DirectoryDownloads}/DbTransistorsApp");
            values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 1);

            var uri = resolver.Insert(Android.Provider.MediaStore.Downloads.ExternalContentUri, values)
                ?? throw new IOException("Android no pudo crear el archivo en Descargas.");

            try
            {
                await using (var output = resolver.OpenOutputStream(uri, "w")
                    ?? throw new IOException("No se pudo abrir el archivo de salida."))
                {
                    await content.CopyToAsync(output, cancellationToken);
                    await output.FlushAsync(cancellationToken);
                }

                values.Clear();
                values.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, 0);
                resolver.Update(uri, values, null, null);

                return new SavedFileInfo(
                    fileName,
                    "Descargas/DbTransistorsApp",
                    ContentUri: uri.ToString());
            }
            catch
            {
                resolver.Delete(uri, null, null);
                throw;
            }
        }

        PermissionStatus permission = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
        if (permission != PermissionStatus.Granted)
            permission = await Permissions.RequestAsync<Permissions.StorageWrite>();
        if (permission != PermissionStatus.Granted)
            throw new UnauthorizedAccessException("Se requiere permiso de almacenamiento para guardar en Descargas.");

#pragma warning disable CA1422
        var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory(
            Android.OS.Environment.DirectoryDownloads);
#pragma warning restore CA1422
        string folder = Path.Combine(downloads?.AbsolutePath ?? FileSystem.AppDataDirectory, "DbTransistorsApp");
        Directory.CreateDirectory(folder);
        string path = GetNonConflictingPath(folder, fileName);
        await using (var output = File.Create(path))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        return new SavedFileInfo(Path.GetFileName(path), folder, path);
    }
#endif

#if WINDOWS
    private static async Task<SavedFileInfo> SaveOnWindowsAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(profile, "Downloads", "DbTransistorsApp");
        Directory.CreateDirectory(downloads);
        string path = GetNonConflictingPath(downloads, fileName);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return new SavedFileInfo(Path.GetFileName(path), downloads, path);
    }
#endif

    private static string GetNonConflictingPath(string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName);
        if (!File.Exists(path))
            return path;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        int suffix = 2;
        do
        {
            path = Path.Combine(folder, $"{stem}_{suffix}{extension}");
            suffix++;
        } while (File.Exists(path));

        return path;
    }
}
