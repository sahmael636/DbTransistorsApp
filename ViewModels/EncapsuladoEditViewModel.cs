using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;

namespace DbTransistorsApp.ViewModels;

public partial class EncapsuladoEditViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly ImageStorageService _imageStorageService;
    private Encapsulado? _original;
    private FileResult? _selectedFile;
    private string? _temporaryPreview;
    private bool _isNew;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _imagen;
    [ObservableProperty] private string? _imagenPreview;

    public EncapsuladoEditViewModel(
        DatabaseService databaseService,
        NavigationService navigationService,
        DialogService dialogService,
        ImageStorageService imageStorageService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _imageStorageService = imageStorageService;
    }

    public async Task InitializeAsync(string mode, int id)
    {
        _isNew = !string.Equals(mode, "Edit", StringComparison.OrdinalIgnoreCase);
        Title = _isNew ? "Nuevo encapsulado" : "Editar encapsulado";
        _selectedFile = null;
        DeleteTemporaryPreview();

        if (_isNew)
        {
            _original = null;
            Nombre = string.Empty;
            Imagen = null;
            ImagenPreview = null;
            return;
        }

        _original = await _databaseService.GetEncapsuladoByIdAsync(id)
            ?? throw new InvalidOperationException("El encapsulado no existe.");
        Nombre = _original.Nombre;
        Imagen = _original.Imagen;
        ImagenPreview = _imageStorageService.GetImagePath(_original.Imagen);
    }

    [RelayCommand]
    private async Task SelectImage()
    {
        try
        {
            var types = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".png", ".jpg", ".jpeg" },
                [DevicePlatform.Android] = new[] { "image/png", "image/jpeg" },
                [DevicePlatform.iOS] = new[] { "public.png", "public.jpeg" },
                [DevicePlatform.macOS] = new[] { "png", "jpg", "jpeg" }
            });
            FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar imagen del encapsulado",
                FileTypes = types
            });
            if (file == null)
                return;

            _selectedFile = file;
            Imagen = file.FileName;
            DeleteTemporaryPreview();
            _temporaryPreview = Path.Combine(FileSystem.CacheDirectory, $"cap_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
            await using Stream input = await file.OpenReadAsync();
            await using FileStream output = File.Create(_temporaryPreview);
            await input.CopyToAsync(output);
            ImagenPreview = _temporaryPreview;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task RemoveImage()
    {
        _selectedFile = null;
        Imagen = null;
        ImagenPreview = null;
        DeleteTemporaryPreview();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Save()
    {
        string name = Nombre.Trim();
        if (name.Length == 0)
        {
            await _dialogService.ShowAlertAsync("Datos requeridos", "El nombre es obligatorio.", "OK");
            return;
        }

        int id = _original?.Id ?? 0;
        if (await _databaseService.EncapsuladoNameExistsAsync(name, id))
        {
            await _dialogService.ShowAlertAsync(
                "Nombre duplicado",
                "Ya existe un encapsulado con ese nombre.",
                "OK");
            return;
        }

        try
        {
            IsBusy = true;
            string? storedImage = _original?.Imagen;
            if (_selectedFile != null)
                storedImage = await _imageStorageService.SaveImageAsync(_selectedFile, _original?.Imagen);
            else if (string.IsNullOrWhiteSpace(Imagen))
            {
                _imageStorageService.DeleteImage(_original?.Imagen);
                storedImage = null;
            }

            if (_isNew)
            {
                await _databaseService.InsertEncapsuladoAsync(new Encapsulado
                {
                    Nombre = name,
                    Imagen = storedImage
                });
            }
            else
            {
                _original!.Nombre = name;
                _original.Imagen = storedImage;
                await _databaseService.UpdateEncapsuladoAsync(_original);
            }

            DeleteTemporaryPreview();
            await _navigationService.NavigateBackAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("No se pudo guardar", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        DeleteTemporaryPreview();
        await _navigationService.NavigateBackAsync();
    }

    private void DeleteTemporaryPreview()
    {
        if (!string.IsNullOrWhiteSpace(_temporaryPreview) && File.Exists(_temporaryPreview))
            File.Delete(_temporaryPreview);
        _temporaryPreview = null;
    }
}
