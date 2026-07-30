using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace DbTransistorsApp.ViewModels;

public partial class TransistorListViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly NavigationService _navigationService;
    private readonly DialogService _dialogService;
    private readonly ExcelExportService _excelExportService;
    private readonly ExcelImportService _excelImportService;
    private readonly DownloadFileService _downloadFileService;
    private readonly SemaphoreSlim _filterSemaphore = new(1, 1);

    private TableType _tableType;
    private Type _modelType = null!;
    private List<PropertyInfo> _displayProperties = new();
    private bool _isInitializingStructures;
    private bool _initialized;
    private readonly Dictionary<string, double> _activeMinimums = new();
    private readonly Dictionary<string, double> _activeMaximums = new();
    private int _pageSize;

    public class TransistorRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
        public object Original { get; set; } = null!;
    }

    [ObservableProperty]
    private ObservableCollection<TransistorRow> _transistors = new();

    [ObservableProperty]
    private string _tableDisplayName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FilterField> _filterFields = new();

    [ObservableProperty]
    private ObservableCollection<Estructura> _availableStructures = new();

    [ObservableProperty]
    private Estructura? _selectedStructure;

    [ObservableProperty]
    private int _totalMatches;

    [ObservableProperty]
    private int _loadedMatches;

    [ObservableProperty]
    private bool _hasMoreResults;

    [ObservableProperty]
    private bool _isLoadingMore;

    public string ResultsSummary => TotalMatches == 0
        ? "Sin coincidencias"
        : $"Mostrando {LoadedMatches:N0} de {TotalMatches:N0}";

    partial void OnTotalMatchesChanged(int value)
    {
        HasMoreResults = LoadedMatches < value;
        OnPropertyChanged(nameof(ResultsSummary));
    }

    partial void OnLoadedMatchesChanged(int value)
    {
        HasMoreResults = value < TotalMatches;
        OnPropertyChanged(nameof(ResultsSummary));
    }

    [ObservableProperty]
    private ObservableCollection<string> _headerFields = new();

    [ObservableProperty]
    private double _columnWidth = 90;

    [ObservableProperty]
    private double _tableWidth = 600;

    [ObservableProperty]
    private bool _areFiltersExpanded;

    public string FiltersToggleText => AreFiltersExpanded
        ? "▼ Filtros de búsqueda"
        : "▶ Filtros de búsqueda";

    partial void OnAreFiltersExpandedChanged(bool value)
        => OnPropertyChanged(nameof(FiltersToggleText));

    public TransistorListViewModel(
        DatabaseService databaseService,
        NavigationService navigationService,
        DialogService dialogService,
        ExcelExportService excelExportService,
        ExcelImportService excelImportService,
        DownloadFileService downloadFileService)
    {
        _databaseService = databaseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _excelExportService = excelExportService;
        _excelImportService = excelImportService;
        _downloadFileService = downloadFileService;
        AreFiltersExpanded = DeviceInfo.Idiom != DeviceIdiom.Phone;
        _pageSize = DeviceInfo.Idiom == DeviceIdiom.Phone ? 75 : 200;
    }

    public async Task InitializeAsync(TableType tableType)
    {
        _tableType = tableType;
        _modelType = tableType.GetModelType();
        TableDisplayName = tableType.GetDisplayName();
        Title = $"Transistores {TableDisplayName}";

        ConfigureDisplayProperties();
        ConfigureFilters();
        await LoadAvailableStructuresAsync();
        await LoadSelectedStructureAsync();
        _initialized = true;
    }

    public override async Task OnAppearingAsync()
    {
        if (!_initialized)
            return;

        int selectedId = SelectedStructure?.Id ?? 0;
        await LoadAvailableStructuresAsync(selectedId);
        await LoadSelectedStructureAsync();
    }

    private void ConfigureDisplayProperties()
    {
        _displayProperties = TransistorMetadata.GetDisplayProperties(_tableType.GetTableName()).ToList();
        HeaderFields.Clear();
        foreach (var property in _displayProperties)
            HeaderFields.Add(TransistorMetadata.GetDisplayNameForProperty(property.Name));

        ColumnWidth = DeviceInfo.Idiom == DeviceIdiom.Phone ? 90 : 110;
        TableWidth = 150 + HeaderFields.Count * ColumnWidth;
    }

    private void ConfigureFilters()
    {
        FilterFields.Clear();
        switch (_tableType)
        {
            case TableType.BjtGe:
            case TableType.BjtSi:
            case TableType.BjtPrebias:
            case TableType.BjtPrebiasDual:
            case TableType.BjtSiDual:
                AddNumericFilter("Potencia (Pc)", "W", "Pc");
                AddNumericFilter("VCE", "V", "Vce");
                AddNumericFilter("VCB", "V", "Vcb");
                AddNumericFilter("VEB", "V", "Veb");
                AddNumericFilter("IC", "A", "Ic");
                AddNumericFilter("Ft", "MHz", "Ft");
                AddNumericFilter("Hfe", "", "Hfe");
                break;

            case TableType.Jfet:
            case TableType.Mosfet:
            case TableType.MosfetDual:
                AddNumericFilter("Potencia (Pd)", "W", "Pd");
                AddNumericFilter("VDS", "V", "Vds");
                AddNumericFilter("VGS", "V", "Vgs");
                AddNumericFilter("VGSTH", "V", "Vgsth");
                AddNumericFilter("ID", "A", "CurrentId");
                AddNumericFilter("RDS", "Ω", "Rds");
                break;

            case TableType.Igbt:
            case TableType.IgbtDual:
                AddNumericFilter("Potencia (Pc)", "W", "Pc");
                AddNumericFilter("VCE", "V", "Vce");
                AddNumericFilter("VCESAT", "V", "Vcesat");
                AddNumericFilter("VEG", "V", "Veg");
                AddNumericFilter("IC", "A", "Ic");
                AddNumericFilter("Tr", "ns", "Tr");
                break;
        }
    }

    private void AddNumericFilter(string displayName, string unit, string field)
    {
        FilterFields.Add(new FilterField
        {
            DisplayName = displayName,
            Field = field,
            Unit = unit,
            MinValue = "0",
            MaxValue = "9999"
        });
    }

    private async Task LoadAvailableStructuresAsync(int preferredId = 0)
    {
        var structures = await _databaseService.GetAvailableStructuresForTableAsync(_tableType.GetTableName());
        _isInitializingStructures = true;
        try
        {
            AvailableStructures.Clear();
            foreach (var structure in structures)
                AvailableStructures.Add(structure);

            SelectedStructure = AvailableStructures.FirstOrDefault(x => x.Id == preferredId)
                ?? AvailableStructures.FirstOrDefault();
        }
        finally
        {
            _isInitializingStructures = false;
        }
    }

    partial void OnSelectedStructureChanged(Estructura? value)
    {
        if (!_isInitializingStructures && value != null)
            _ = LoadSelectedStructureAsync();
    }

    private async Task LoadSelectedStructureAsync()
    {
        _activeMinimums.Clear();
        _activeMaximums.Clear();
        await ReloadResultsAsync();
    }

    private async Task ReloadResultsAsync()
    {
        await _filterSemaphore.WaitAsync();
        try
        {
            IsBusy = true;
            if (SelectedStructure == null)
            {
                SetTransistors(Array.Empty<object>(), 0);
                return;
            }

            PagedResult<object> page = await _databaseService.GetFilteredTransistorPageAsync(
                _tableType.GetTableName(),
                _activeMinimums,
                _activeMaximums,
                SelectedStructure.Id,
                _pageSize,
                0);
            SetTransistors(page.Items, page.TotalCount);
        }
        finally
        {
            IsBusy = false;
            _filterSemaphore.Release();
        }
    }

    [RelayCommand]
    private void ToggleFilters() => AreFiltersExpanded = !AreFiltersExpanded;

    [RelayCommand]
    private async Task ApplyFilters()
    {
        _activeMinimums.Clear();
        _activeMaximums.Clear();

        foreach (var filter in FilterFields)
        {
            if (TryParseFilterValue(filter.MinValue, out double min) && min > 0)
                _activeMinimums[filter.Field] = min;
            if (TryParseFilterValue(filter.MaxValue, out double max) && max < 9999)
                _activeMaximums[filter.Field] = max;
        }

        await ReloadResultsAsync();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        foreach (var filter in FilterFields)
            filter.Clear();
        await LoadSelectedStructureAsync();
    }

    [RelayCommand]
    private Task New()
    {
        return _navigationService.NavigateToAsync(nameof(TransistorEditPage),
            new Dictionary<string, object>
            {
                { "Type", _tableType.GetTableName() },
                { "Mode", "New" }
            });
    }

    [RelayCommand]
    private async Task Import()
    {
        string option = await _dialogService.ShowActionSheetAsync(
            "Importar transistores",
            "Cancelar",
            "Generar plantilla XLSX",
            "Seleccionar archivo XLSX");

        if (option == "Generar plantilla XLSX")
            await GenerateTemplateAsync();
        else if (option == "Seleccionar archivo XLSX")
            await SelectAndImportAsync();
    }

    private async Task GenerateTemplateAsync()
    {
        try
        {
            IsBusy = true;
            var structures = await _databaseService.GetAllEstructurasAsync();
            var allowed = await _databaseService.GetAllowedStructureIdsForTableAsync(_tableType.GetTableName());
            var caps = await _databaseService.GetAllEncapsuladosAsync();
            using MemoryStream workbook = await _excelExportService.CreateImportTemplateAsync(
                _tableType.GetTableName(), structures, allowed, caps);

            string fileName = $"Plantilla_{_tableType.GetTableName()}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            SavedFileInfo saved = await _downloadFileService.SaveToDownloadsAsync(
                fileName,
                workbook,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            await _dialogService.ShowAlertAsync(
                "Plantilla generada",
                $"Se guardó '{saved.FileName}' en {saved.DisplayLocation}. Complétela y luego use Importar → Seleccionar archivo XLSX.",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo generar la plantilla: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectAndImportAsync()
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
                { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
                { DevicePlatform.macOS, new[] { "xlsx" } }
            });

            FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar archivo XLSX",
                FileTypes = fileTypes
            });
            if (file == null)
                return;

            IsBusy = true;
            await using Stream input = await file.OpenReadAsync();
            ImportResult result = await _excelImportService.ImportTransistorsAsync(
                input,
                _tableType.GetTableName());

            using MemoryStream report = await _excelExportService.CreateImportReportAsync(
                _tableType.GetTableName(), result);
            string reportName = $"Informe_importacion_{_tableType.GetTableName()}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            SavedFileInfo savedReport = await _downloadFileService.SaveToDownloadsAsync(
                reportName,
                report,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            await LoadAvailableStructuresAsync(SelectedStructure?.Id ?? 0);
            await LoadSelectedStructureAsync();

            await _dialogService.ShowAlertAsync(
                "Importación terminada",
                $"Importados: {result.ImportedRows}\nDuplicados: {result.DuplicateRows}\nCon errores: {result.ErrorRows}\n\nInforme: {savedReport.FileName} en {savedReport.DisplayLocation}.",
                "OK");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", $"No se pudo importar el archivo: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (IsBusy || IsLoadingMore || !HasMoreResults || SelectedStructure == null)
            return;

        await _filterSemaphore.WaitAsync();
        try
        {
            IsLoadingMore = true;
            PagedResult<object> page = await _databaseService.GetFilteredTransistorPageAsync(
                _tableType.GetTableName(),
                _activeMinimums,
                _activeMaximums,
                SelectedStructure.Id,
                _pageSize,
                Transistors.Count);

            AppendTransistors(page.Items, page.TotalCount);
        }
        finally
        {
            IsLoadingMore = false;
            _filterSemaphore.Release();
        }
    }

    [RelayCommand]
    private Task SelectTransistor(TransistorRow row)
    {
        return _navigationService.NavigateToAsync(nameof(TransistorDetailPage),
            new Dictionary<string, object>
            {
                { "Type", _tableType.GetTableName() },
                { "Id", row.Id }
            });
    }

    private void SetTransistors(IEnumerable<object> items, int totalCount)
    {
        Transistors.Clear();
        foreach (var item in items)
            Transistors.Add(CreateRowFromObject(item));
        TotalMatches = totalCount;
        LoadedMatches = Transistors.Count;
    }

    private void AppendTransistors(IEnumerable<object> items, int totalCount)
    {
        foreach (var item in items)
            Transistors.Add(CreateRowFromObject(item));
        TotalMatches = totalCount;
        LoadedMatches = Transistors.Count;
    }

    private TransistorRow CreateRowFromObject(object item)
    {
        var row = new TransistorRow
        {
            Id = Convert.ToInt32(item.GetType().GetProperty("Id")?.GetValue(item) ?? 0),
            Name = item.GetType().GetProperty("Name")?.GetValue(item)?.ToString() ?? string.Empty,
            Original = item
        };

        foreach (var property in _displayProperties)
            row.Values.Add(FormatValue(property.GetValue(item)));
        return row;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            double number => number.ToString("0.####", CultureInfo.CurrentCulture),
            float number => number.ToString("0.####", CultureInfo.CurrentCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static bool TryParseFilterValue(string? text, out double value)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
           double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
