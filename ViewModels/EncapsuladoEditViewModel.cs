// ViewModels/EncapsuladoEditViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
//using IntelliJ.Lang.Annotations;
//using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class EncapsuladoEditViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly DialogService _dialogService;
        private Encapsulado _originalEncapsulado;
        private string _mode;

        [ObservableProperty]
        private string _nombre;

        [ObservableProperty]
        private string _imagen;

        [ObservableProperty]
        private string _imagenPreview;

        [ObservableProperty]
        private bool _isNewMode;

        [ObservableProperty]
        private string _title;

        public EncapsuladoEditViewModel(DatabaseService databaseService, NavigationService navigationService, DialogService dialogService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _dialogService = dialogService;
        }

        public async Task InitializeAsync(string mode, Encapsulado encapsulado = null)
        {
            _mode = mode;
            _isNewMode = mode == "New";

            if (_isNewMode)
            {
                Title = "Nuevo Encapsulado";
                Nombre = string.Empty;
                Imagen = string.Empty;
            }
            else
            {
                Title = $"Editar Encapsulado";
                _originalEncapsulado = encapsulado;
                Nombre = encapsulado.Nombre;
                Imagen = encapsulado.Imagen;
                ImagenPreview = GetImagePreview(encapsulado.Imagen);
            }
        }

        [RelayCommand]
        private async Task SelectImage()
        {
            try
            {
                var options = new PickOptions
                {
                    PickerTitle = "Seleccionar imagen del encapsulado",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            { DevicePlatform.WinUI, new[] { ".png", ".jpg", ".jpeg", ".gif" } },
                            { DevicePlatform.macOS, new[] { "png", "jpg", "jpeg", "gif" } },
                            { DevicePlatform.Android, new[] { "image/png", "image/jpeg", "image/gif" } },
                            { DevicePlatform.iOS, new[] { "public.png", "public.jpeg", "public.gif" } },
                        })
                };

                var file = await FilePicker.PickAsync(options);
                if (file != null)
                {
                    // Guardar la imagen en la carpeta de recursos
                    string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    string targetPath = Path.Combine(FileSystem.AppDataDirectory, "Images", "Packages", fileName);

                    // Crear directorio si no existe
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                    // Copiar el archivo
                    using var sourceStream = await file.OpenReadAsync();
                    using var targetStream = File.Create(targetPath);
                    await sourceStream.CopyToAsync(targetStream);

                    Imagen = fileName;
                    ImagenPreview = GetImagePreview(fileName);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al seleccionar la imagen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                await _dialogService.ShowAlertAsync("Error", "El nombre del encapsulado es obligatorio", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                if (_isNewMode)
                {
                    var nuevo = new Encapsulado
                    {
                        Nombre = Nombre,
                        Imagen = Imagen
                    };
                    await _databaseService.InsertEncapsuladoAsync(nuevo);
                }
                else
                {
                    _originalEncapsulado.Nombre = Nombre;
                    _originalEncapsulado.Imagen = Imagen;
                    await _databaseService.UpdateEncapsuladoAsync(_originalEncapsulado);
                }

                await _navigationService.NavigateBackAsync();
                await _dialogService.ShowToastAsync("Encapsulado guardado correctamente");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al guardar: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await _navigationService.NavigateBackAsync();
        }

        private string GetImagePreview(string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return string.Empty;

            string imagePath = Path.Combine(FileSystem.AppDataDirectory, "Images", "Packages", imageName);
            if (File.Exists(imagePath))
                return imagePath;

            return string.Empty;
        }
    }
}