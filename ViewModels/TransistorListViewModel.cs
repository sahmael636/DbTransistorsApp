// ViewModels/TransistorListViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Models.Base;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace DbTransistorsApp.ViewModels
{
    public partial class TransistorListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private readonly SemaphoreSlim _filterSemaphore = new(1, 1);
        private TableType _tableType;
        private Type _modelType = null!;
        private List<PropertyInfo> _displayProperties = new();
        private bool _isInitializingStructures;

        // Fila preparada para la vista: Id, Name y valores de columnas dinámicas.
        public class TransistorRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public List<string> Values { get; set; } = new();
            public object Original { get; set; } = null!;
        }

        [ObservableProperty]
        private ObservableCollection<object> _transistors = new();

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
        private ObservableCollection<string> _headerFields = new();

        [ObservableProperty]
        private string _headerColumns = string.Empty;

        [ObservableProperty]
        private string _columnDefinitions = string.Empty;

        [ObservableProperty]
        private double _columnWidth;

        public TransistorListViewModel(DatabaseService databaseService, NavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            ColumnWidth = 80;
        }

        public async Task InitializeAsync(TableType tableType)
        {
            _tableType = tableType;
            _modelType = tableType.GetModelType();
            TableDisplayName = tableType.GetDisplayName();
            Title = $"Transistores {TableDisplayName}";

            ConfigureDisplayProperties();
            ConfigureColumnWidth();
            ConfigureFilters();
            ConfigureHeaders();

            await LoadAvailableStructuresAsync();
            await LoadSelectedStructureAsync();
        }

        private void ConfigureColumnWidth()
        {
            try
            {
                var main = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo;
                double screenDp = main.Width / main.Density;
                const double nameWidth = 150;
                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                double available = Math.Max(screenDp - nameWidth - 40, 200);
                ColumnWidth = Math.Max(50, available / Math.Max(1, maxParams));
            }
            catch
            {
                ColumnWidth = 80;
            }
        }

        private void ConfigureDisplayProperties()
        {
            _displayProperties = _modelType.GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                            p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2")
                .ToList();

            int maxParams = ColumnLayoutHelper.MaxParameterCount;

            var columns = new List<string> { "Auto" };
            for (int i = 0; i < maxParams; i++)
            {
                columns.Add("Auto");
            }
            ColumnDefinitions = string.Join(",", columns);

            HeaderFields.Clear();
            for (int i = 0; i < maxParams; i++)
            {
                HeaderFields.Add(i < _displayProperties.Count
                    ? GetParameterDisplayName(_displayProperties[i].Name)
                    : string.Empty);
            }

            HeaderColumns = string.Join(",", HeaderFields.Select(_ => "Auto"));
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

        private void ConfigureHeaders()
        {
            // Los encabezados se configuran en ConfigureDisplayProperties.
        }

        private async Task LoadAvailableStructuresAsync()
        {
            var structures = await _databaseService.GetAvailableStructuresForTableAsync(
                _tableType.GetTableName());

            _isInitializingStructures = true;
            try
            {
                SelectedStructure = null;
                AvailableStructures.Clear();

                foreach (var structure in structures)
                {
                    AvailableStructures.Add(structure);
                }

                // No se ofrece la opción "Todas": siempre se selecciona la primera disponible.
                SelectedStructure = AvailableStructures.FirstOrDefault();
            }
            finally
            {
                _isInitializingStructures = false;
            }
        }

        partial void OnSelectedStructureChanged(Estructura? value)
        {
            if (!_isInitializingStructures && value != null)
            {
                // Cambiar la estructura filtra inmediatamente, pero no aplica los rangos numéricos.
                _ = LoadSelectedStructureAsync();
            }
        }

        private async Task LoadSelectedStructureAsync()
        {
            await _filterSemaphore.WaitAsync();
            try
            {
                IsBusy = true;

                if (SelectedStructure == null)
                {
                    SetTransistors(Array.Empty<object>());
                    return;
                }

                var results = await _databaseService.GetFilteredTransistorsAsync(
                    _tableType.GetTableName(),
                    new Dictionary<string, double>(),
                    new Dictionary<string, double>(),
                    SelectedStructure.Id);

                SetTransistors(results);
            }
            finally
            {
                IsBusy = false;
                _filterSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task ApplyFilters()
        {
            await _filterSemaphore.WaitAsync();
            try
            {
                IsBusy = true;

                if (SelectedStructure == null)
                {
                    SetTransistors(Array.Empty<object>());
                    return;
                }

                var minimumFilters = new Dictionary<string, double>();
                var maximumFilters = new Dictionary<string, double>();

                foreach (var filter in FilterFields)
                {
                    if (TryParseFilterValue(filter.MinValue, out double min) && min > 0)
                    {
                        minimumFilters[filter.Field] = min;
                    }

                    if (TryParseFilterValue(filter.MaxValue, out double max) && max < 9999)
                    {
                        maximumFilters[filter.Field] = max;
                    }
                }

                var results = await _databaseService.GetFilteredTransistorsAsync(
                    _tableType.GetTableName(),
                    minimumFilters,
                    maximumFilters,
                    SelectedStructure.Id);

                SetTransistors(results);
            }
            finally
            {
                IsBusy = false;
                _filterSemaphore.Release();
            }
        }

        [RelayCommand]
        private async Task ClearFilters()
        {
            foreach (var filter in FilterFields)
            {
                filter.MinValue = "0";
                filter.MaxValue = "9999";
            }

            // Se conserva la estructura seleccionada y se muestran todos sus registros.
            await LoadSelectedStructureAsync();
        }

        private static bool TryParseFilterValue(string? text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                   double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void SetTransistors(IEnumerable<object> items)
        {
            Transistors.Clear();

            foreach (var item in items)
            {
                Transistors.Add(CreateRowFromObject(item));
            }

            TotalMatches = Transistors.Count;
        }

        [RelayCommand]
        private async Task SelectTransistor(object transistor)
        {
            try
            {
                object original = transistor is TransistorRow row ? row.Original : transistor;
                var prop = original.GetType().GetProperty("Id");

                if (prop?.GetValue(original) is int id)
                {
                    await _navigationService.NavigateToAsync(nameof(TransistorDetailPage),
                        new Dictionary<string, object>
                        {
                            { "Type", _tableType.GetTableName() },
                            { "Id", id }
                        });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectTransistor error: {ex}");
            }
        }

        private string GetParameterDisplayName(string fieldName)
        {
            return fieldName switch
            {
                "Pc" or "Pd" => "Potencia",
                "Vcb" => "VCB",
                "Vce" => "VCE",
                "Veb" => "VEB",
                "Vds" => "VDS",
                "Vgs" => "VGS",
                "Vgsth" => "VGSTH",
                "Vcesat" => "VCESAT",
                "Veg" => "VEG",
                "Ic" => "IC",
                "CurrentId" => "ID",
                "Tj" => "TJ",
                "Ft" => "Ft",
                "Cc" => "CC",
                "Hfe" => "Hfe",
                "Qg" => "QG",
                "Tr" => "Tr",
                "Cd" => "CD",
                "Rds" => "RDS",
                _ => fieldName
            };
        }

        private TransistorRow CreateRowFromObject(object item)
        {
            var row = new TransistorRow();

            try
            {
                var propId = item.GetType().GetProperty("Id");
                if (propId?.GetValue(item) is int id)
                {
                    row.Id = id;
                }

                var propName = item.GetType().GetProperty("Name");
                row.Name = propName?.GetValue(item)?.ToString() ?? string.Empty;

                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                for (int i = 0; i < maxParams; i++)
                {
                    if (i < _displayProperties.Count)
                    {
                        var value = _displayProperties[i].GetValue(item);
                        row.Values.Add(value?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        row.Values.Add(string.Empty);
                    }
                }

                row.Original = item;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreateRowFromObject error: {ex}");
                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                while (row.Values.Count < maxParams)
                {
                    row.Values.Add(string.Empty);
                }
            }

            return row;
        }
    }
}
