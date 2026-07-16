// ViewModels/EstructurasViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
//using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
//using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class EstructurasViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly DialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<Estructura> _estructuras = new();

        public EstructurasViewModel(DatabaseService databaseService, NavigationService navigationService, DialogService dialogService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            Title = "Administración de Estructuras";
        }

        public async Task InitializeAsync()
        {
            await LoadEstructurasAsync();
        }

        private async Task LoadEstructurasAsync()
        {
            try
            {
                IsBusy = true;
                var all = await _databaseService.GetAllEstructurasAsync();
                Estructuras.Clear();
                foreach (var item in all.OrderBy(e => e.Nombre))
                {
                    Estructuras.Add(item);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task EditEstructura(Estructura estructura)
        {
            var newName = await _dialogService.ShowPromptAsync(
                "Editar Estructura",
                $"Editar nombre de '{estructura.Nombre}'",
                estructura.Nombre);

            if (!string.IsNullOrWhiteSpace(newName) && newName != estructura.Nombre)
            {
                try
                {
                    IsBusy = true;
                    estructura.Nombre = newName;
                    await _databaseService.UpdateEstructuraAsync(estructura);
                    await LoadEstructurasAsync();
                    await _dialogService.ShowToastAsync("Estructura actualizada correctamente");
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowAlertAsync("Error", $"Error al actualizar: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        [RelayCommand]
        private async Task DeleteEstructura(Estructura estructura)
        {
            var result = await _dialogService.ShowConfirmationAsync(
                "Confirmar eliminación",
                $"¿Está seguro de eliminar la estructura '{estructura.Nombre}'? Esta acción eliminará también las relaciones en cascada.");

            if (result)
            {
                try
                {
                    IsBusy = true;
                    await _databaseService.DeleteEstructuraAsync(estructura.Id);
                    await LoadEstructurasAsync();
                    await _dialogService.ShowToastAsync("Estructura eliminada correctamente");
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
        private async Task NewEstructura()
        {
            var newName = await _dialogService.ShowPromptAsync(
                "Nueva Estructura",
                "Ingrese el nombre de la nueva estructura",
                "Ej: NPN, PNP, N");

            if (!string.IsNullOrWhiteSpace(newName))
            {
                try
                {
                    IsBusy = true;
                    var nueva = new Estructura { Nombre = newName };
                    await _databaseService.InsertEstructuraAsync(nueva);
                    await LoadEstructurasAsync();
                    await _dialogService.ShowToastAsync("Estructura creada correctamente");
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowAlertAsync("Error", $"Error al crear: {ex.Message}", "OK");
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }
}