// ViewModels/SearchViewModel.cs
//using Android.App.AppSearch;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
//using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
//using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class SearchViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;

        [ObservableProperty]
        private string _searchTerm;

        [ObservableProperty]
        private ObservableCollection<ByNameItem> _searchResults = new();

        [ObservableProperty]
        private int _totalMatches;

        public SearchViewModel(DatabaseService databaseService, NavigationService navigationService)
        {
            Title = "Buscar por Nombre";
            _databaseService = databaseService;
            _navigationService = navigationService;

            // Cargar todos al inicio
            LoadAllTransistors();
        }

        partial void OnSearchTermChanged(string value)
        {
            // Filtrar en tiempo real
            PerformSearch(value);
        }

        private async void LoadAllTransistors()
        {
            try
            {
                IsBusy = true;
                var all = await _databaseService.GetAllByNameAsync();
                SearchResults.Clear();
                foreach (var item in all)
                {
                    SearchResults.Add(new ByNameItem
                    {
                        Name = item.Name,
                        Type = item.Type,
                        Idx = item.Idx
                    });
                }
                TotalMatches = SearchResults.Count;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async void PerformSearch(string searchTerm)
        {
            try
            {
                IsBusy = true;
                var results = await _databaseService.SearchByNameAsync(searchTerm);
                SearchResults.Clear();
                foreach (var item in results)
                {
                    SearchResults.Add(new ByNameItem
                    {
                        Name = item.Name,
                        Type = item.Type,
                        Idx = item.Idx
                    });
                }
                TotalMatches = SearchResults.Count;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SelectTransistor(ByNameItem item)
        {
            if (item == null)
                return;

            try
            {
                // ✅ Pasar parámetros correctamente
                var parameters = new Dictionary<string, object>
        {
            { "type", item.Type },
            { "id", item.Idx }
        };

                await _navigationService.NavigateToAsync(nameof(TransistorDetailPage), parameters);
            }
            catch (Exception ex)
            {
                //await _dialogService.ShowAlertAsync("Error", $"Error al navegar: {ex.Message}", "OK");
            }
        }
    }

    public class ByNameItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Idx { get; set; }
    }
}