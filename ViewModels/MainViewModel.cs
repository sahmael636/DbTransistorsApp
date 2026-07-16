// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
//using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly NavigationService _navigationService;

        public MainViewModel(NavigationService navigationService)
        {
            Title = "Transistor Database";
            _navigationService = navigationService;
        }

        public List<TableButton> TableButtons { get; } = new()
        {
            new TableButton { TableType = TableType.BjtGe, Icon = "bjt_icon.png" },
            new TableButton { TableType = TableType.BjtSi, Icon = "bjt_icon.png" },
            new TableButton { TableType = TableType.BjtPrebias, Icon = "bjt_icon.png" },
            new TableButton { TableType = TableType.BjtSiDual, Icon = "bjt_dual_icon.png" },
            new TableButton { TableType = TableType.BjtPrebiasDual, Icon = "bjt_dual_icon.png" },
            new TableButton { TableType = TableType.Jfet, Icon = "jfet_icon.png" },
            new TableButton { TableType = TableType.Mosfet, Icon = "mosfet_icon.png" },
            new TableButton { TableType = TableType.MosfetDual, Icon = "mosfet_dual_icon.png" },
            new TableButton { TableType = TableType.Igbt, Icon = "igbt_icon.png" },
            new TableButton { TableType = TableType.IgbtDual, Icon = "igbt_dual_icon.png" },
        };

        [RelayCommand]
        private async Task NavigateToTable(TableButton button)
        {
            await _navigationService.NavigateToAsync(nameof(TransistorListPage),
                new Dictionary<string, object>
                {
                    { "TableType", button.TableType }
                });
        }

        [RelayCommand]
        private async Task NavigateToSearch()
        {
            await _navigationService.NavigateToAsync(nameof(SearchPage));
        }

        [RelayCommand]
        private async Task NavigateToEncapsulados()
        {
            await _navigationService.NavigateToAsync(nameof(EncapsuladosPage));
        }

        [RelayCommand]
        private async Task NavigateToEstructuras()
        {
            await _navigationService.NavigateToAsync(nameof(EstructurasPage));
        }
    }

    public class TableButton
    {
        public TableType TableType { get; set; }
        public string Icon { get; set; }
        public string DisplayName => TableType.GetDisplayName();
    }
}