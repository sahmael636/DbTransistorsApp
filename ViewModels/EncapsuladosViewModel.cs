// ViewModels/EncapsuladosViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class EncapsuladosViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly DialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Encapsulado> _encapsulados = new();

        [ObservableProperty]
        private Encapsulado _selectedEncapsulado;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editNombre;

        [ObservableProperty]
        private string _editImagen;

        [ObservableProperty]
        private string _editImagenPreview;

        public EncapsuladosViewModel(DatabaseService databaseService, NavigationService navigationService, DialogService dialogService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            Title = "Administración de Encapsulados";
        }

        public async Task InitializeAsync()
        {
            await LoadEncapsuladosAsync();
        }

        private async Task LoadEncapsuladosAsync()
        {
            try
            {
                IsBusy = true;
                var all = await _databaseService.GetAllEncapsuladosAsync();
                Encapsulados.Clear();
                foreach (var item in all.OrderBy(e => e.Nombre))
                {
                    Encapsulados.Add(item);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditEncapsulado(Encapsulado encapsulado)
        {
            SelectedEncapsulado = encapsulado;
            EditNombre = encapsulado.Nombre;
            EditImagen = encapsulado.Imagen;
            EditImagenPreview = GetImagePreview(encapsulado.Imagen);
            IsEditing = true;

            // Navegar a la página de edición o mostrar popup
            await _navigationService.NavigateToAsync(nameof(EncapsuladoEditPage),
                new Dictionary<string, object>
                {
                    { "Encapsulado", encapsulado },
                    { "Mode", "Edit" }
                });
        }

        [RelayCommand]
        private async Task DeleteEncapsulado(Encapsulado encapsulado)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Confirmar eliminación",
                $"¿Está seguro de eliminar el encapsulado '{encapsulado.Nombre}'? Esta acción eliminará también las relaciones en cascada.");

            if (result)
            {
                try
                {
                    IsBusy = true;
                    await _databaseService.DeleteEncapsuladoAsync(encapsulado.Id);
                    await LoadEncapsuladosAsync();
                    await _dialogService.ShowToastAsync("Encapsulado eliminado correctamente");
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowAlertAsync("Error", $"Error al eliminar: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task NewEncapsulado()
        {
            await _navigationService.NavigateToAsync(nameof(EncapsuladoEditPage),
                new Dictionary<string, object>
                {
                    { "Mode", "New" }
                });
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

                    EditImagen = fileName;
                    EditImagenPreview = GetImagePreview(fileName);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowAlertAsync("Error", $"Error al seleccionar la imagen: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task SaveEncapsulado()
        {
            if (string.IsNullOrWhiteSpace(EditNombre))
            {
                await _dialogService.ShowAlertAsync("Error", "El nombre del encapsulado es obligatorio", "OK");
                return;
            }

            try
            {
                IsBusy = true;

                if (IsEditing && SelectedEncapsulado != null)
                {
                    // Actualizar
                    SelectedEncapsulado.Nombre = EditNombre;
                    SelectedEncapsulado.Imagen = EditImagen;
                    await _databaseService.UpdateEncapsuladoAsync(SelectedEncapsulado);
                }
                else
                {
                    // Nuevo
                    var nuevo = new Encapsulado
                    {
                        Nombre = EditNombre,
                        Imagen = EditImagen
                    };
                    await _databaseService.InsertEncapsuladoAsync(nuevo);
                }

                await LoadEncapsuladosAsync();
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
        private async Task CancelEdit()
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