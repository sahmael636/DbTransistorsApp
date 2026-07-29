using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;

namespace DbTransistorsApp.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly NavigationService _navigationService;
    private readonly DatabaseService _databaseService;
    private readonly ExcelExportService _excelExportService;
    private readonly DownloadFileService _downloadFileService;
    private readonly DialogService _dialogService;

    public MainViewModel(
        NavigationService navigationService,
        DatabaseService databaseService,
        ExcelExportService excelExportService,
        DownloadFileService downloadFileService,
        DialogService dialogService)
    {
        Title = "Transistor Database";
        _navigationService = navigationService;
        _databaseService = databaseService;
        _excelExportService = excelExportService;
        _downloadFileService = downloadFileService;
        _dialogService = dialogService;
    }

    public List<TableButton> TableButtons { get; } = new()
    {
        new() { TableType = TableType.BjtGe, Icon = "bjt_icon.png" },
        new() { TableType = TableType.BjtSi, Icon = "bjt_icon.png" },
        new() { TableType = TableType.BjtPrebias, Icon = "bjt_icon.png" },
        new() { TableType = TableType.BjtSiDual, Icon = "bjt_dual_icon.png" },
        new() { TableType = TableType.BjtPrebiasDual, Icon = "bjt_dual_icon.png" },
        new() { TableType = TableType.Jfet, Icon = "jfet_icon.png" },
        new() { TableType = TableType.Mosfet, Icon = "mosfet_icon.png" },
        new() { TableType = TableType.MosfetDual, Icon = "mosfet_dual_icon.png" },
        new() { TableType = TableType.Igbt, Icon = "igbt_icon.png" },
        new() { TableType = TableType.IgbtDual, Icon = "igbt_dual_icon.png" }
    };

    [RelayCommand]
    private Task NavigateToTable(TableButton button)
        => _navigationService.NavigateToAsync(nameof(TransistorListPage),
            new Dictionary<string, object> { { "TableType", button.TableType } });

    [RelayCommand]
    private Task NavigateToSearch() => _navigationService.NavigateToAsync(nameof(SearchPage));

    [RelayCommand]
    private Task NavigateToEncapsulados() => _navigationService.NavigateToAsync(nameof(EncapsuladosPage));

    [RelayCommand]
    private Task NavigateToEstructuras() => _navigationService.NavigateToAsync(nameof(EstructurasPage));

    [RelayCommand]
    private async Task ExportDatabase()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            using MemoryStream workbook = await _excelExportService.CreateDatabaseWorkbookAsync(_databaseService);
            string fileName = $"dbtransistors_completa_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            SavedFileInfo saved = await _downloadFileService.SaveToDownloadsAsync(
                fileName,
                workbook,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            await _dialogService.ShowAlertAsync(
                "Exportación completada",
                $"La base de datos se exportó como '{saved.FileName}' en {saved.DisplayLocation}.",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo exportar la base de datos: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class TableButton
{
    public TableType TableType { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string DisplayName => TableType.GetDisplayName();
}
