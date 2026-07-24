// ViewModels/TransistorListViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DbTransistorsApp.Services;
using DbTransistorsApp.ViewModels.Base;
using DbTransistorsApp.Views;

//using IntelliJ.Lang.Annotations;
using System.Collections.ObjectModel;
using System.Reflection;
//using static Android.Icu.Text.CaseMap;

namespace DbTransistorsApp.ViewModels
{
    public partial class TransistorListViewModel : BaseViewModel
    {
        private readonly DatabaseService _databaseService;
        private readonly NavigationService _navigationService;
        private TableType _tableType;
        private Type _modelType;
        private List<PropertyInfo> _displayProperties;

        // Fila preparada para la vista: Id, Name y valores de columnas dinámicas
        public class TransistorRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<string> Values { get; set; } = new();
            public List<CellItem> Cells { get; set; } = new();
            public object Original { get; set; }
            public class CellItem
            {
                public int Index { get; set; }
                public string Text { get; set; }
            }
        }

        [ObservableProperty]
        private ObservableCollection<object> _transistors = new();

        [ObservableProperty]
        private string _tableDisplayName;

        [ObservableProperty]
        private ObservableCollection<FilterField> _filterFields = new();

        [ObservableProperty]
        private int _totalMatches;

        [ObservableProperty]
        private ObservableCollection<string> _headerFields = new();

        [ObservableProperty]
        private string _headerColumns;

        [ObservableProperty]
        private string _columnDefinitions;

        [ObservableProperty]
        private double _columnWidth;

        public TransistorListViewModel(DatabaseService databaseService, NavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
            // ancho aproximado por columna en device-independent units
            ColumnWidth = 80; // reducir para mejor ajuste y rendimiento
        }

        public async Task InitializeAsync(TableType tableType)
        {
            _tableType = tableType;
            _modelType = tableType.GetModelType();
            TableDisplayName = tableType.GetDisplayName();
            Title = $"Transistores {TableDisplayName}";

            // Configurar propiedades a mostrar
            ConfigureDisplayProperties();

            // Calcular ancho de columna dinámicamente según ancho de pantalla
            try
            {
                var main = Microsoft.Maui.Devices.DeviceDisplay.MainDisplayInfo;
                double screenDp = main.Width / main.Density;
                double nameWidth = 150;
                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                double available = Math.Max(screenDp - nameWidth - 40, 200);
                ColumnWidth = Math.Max(50, available / Math.Max(1, maxParams));
            }
            catch
            {
                ColumnWidth = 80; // fallback
            }

            // Configurar filtros
            ConfigureFilters();

            // Configurar encabezados
            ConfigureHeaders();

            // Cargar todos los transistores
            await LoadTransistorsAsync();
        }

        private void ConfigureDisplayProperties()
        {
            var props = _modelType.GetProperties()
                .Where(p => p.Name != "Id" && p.Name != "Name" && p.Name != "StructId" &&
                           p.Name != "CapsIds" && p.Name != "R1" && p.Name != "R2")
                .ToList();

            _displayProperties = props;

            // Usar número máximo de columnas entre todas las tablas para alinear
            int maxParams = ColumnLayoutHelper.MaxParameterCount;

            // Configurar columnas para el grid (1 nombre + maxParams)
            var columns = new List<string> { "Auto" };
            for (int i = 0; i < maxParams; i++) columns.Add("Auto");
            ColumnDefinitions = string.Join(",", columns);

            // Configurar encabezados fijos hasta maxParams (llenar con vacíos si faltan)
            HeaderFields.Clear();
            for (int i = 0; i < maxParams; i++)
            {
                if (i < _displayProperties.Count)
                    HeaderFields.Add(GetParameterDisplayName(_displayProperties[i].Name));
                else
                    HeaderFields.Add(string.Empty);
            }
            HeaderColumns = string.Join(",", HeaderFields.Select(_ => "Auto"));
        }

        private void ConfigureFilters()
        {
            FilterFields.Clear();

            // Siempre incluir el nombre como filtro
            FilterFields.Add(new FilterField
            {
                DisplayName = "Nombre",
                Field = "Name",
                Unit = "",
                IsTextFilter = true
            });

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
                    AddNumericFilter("ID", "A", "Id");
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
            // Ya configurado en ConfigureDisplayProperties
        }
        private async Task LoadTransistorsAsync()
        {
            try
            {
                IsBusy = true;
                var all = await _databaseService.GetAllByTableAsync(_tableType.GetTableName());
                Transistors.Clear();
                foreach (var item in all)
                {
                    var row = CreateRowFromObject(item);
                    Transistors.Add(row);
                }
                TotalMatches = Transistors.Count;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ApplyFilters()
        {
            try
            {
                IsBusy = true;

                var parameters = new Dictionary<string, object>();
                var textFilters = new Dictionary<string, string>();

                foreach (var filter in FilterFields)
                {
                    if (filter.IsTextFilter)
                    {
                        if (!string.IsNullOrWhiteSpace(filter.TextValue))
                        {
                            textFilters[filter.Field] = filter.TextValue;
                        }
                    }
                    else
                    {
                        if (double.TryParse(filter.MinValue, out double min) && min > 0)
                        {
                            parameters[$"{filter.Field} >= @{filter.Field}_min"] = min;
                        }
                        if (double.TryParse(filter.MaxValue, out double max) && max < 9999)
                        {
                            parameters[$"{filter.Field} <= @{filter.Field}_max"] = max;
                        }
                    }
                }

                var results = await _databaseService.GetFilteredTransistorsAsync(
                    _tableType.GetTableName(),
                    parameters,
                    textFilters);

                Transistors.Clear();
                foreach (var item in results)
                {
                    // ✅ CORRECCIÓN: Crear TransistorRow en lugar de agregar item directamente
                    var row = CreateRowFromObject(item);
                    Transistors.Add(row);
                }
                TotalMatches = Transistors.Count;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ClearFilters()
        {
            foreach (var filter in FilterFields)
            {
                if (filter.IsTextFilter)
                {
                    filter.TextValue = string.Empty;
                }
                else
                {
                    filter.MinValue = "0";
                    filter.MaxValue = "9999";
                }
            }

            // ✅ Recargar todos los transistores con CreateRowFromObject
            LoadTransistorsAsync();
        }

        [RelayCommand]
        private async Task SelectTransistor(object transistor)
        {
            try
            {
                if (transistor is TransistorRow row)
                {
                    // Obtener el objeto original del row
                    var original = row.Original;
                    var prop = original.GetType().GetProperty("Id");
                    if (prop != null)
                    {
                        int id = (int)prop.GetValue(original);
                        await _navigationService.NavigateToAsync(nameof(TransistorDetailPage),
                            new Dictionary<string, object>
                            {
                        { "Type", _tableType.GetTableName() },
                        { "Id", id }
                            });
                    }
                }
                else
                {
                    // Fallback para compatibilidad
                    var prop = transistor.GetType().GetProperty("Id");
                    if (prop != null)
                    {
                        int id = (int)prop.GetValue(transistor);
                        await _navigationService.NavigateToAsync(nameof(TransistorDetailPage),
                            new Dictionary<string, object>
                            {
                        { "Type", _tableType.GetTableName() },
                        { "Id", id }
                            });
                    }
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
                "Ic" or "Id" => "IC",
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

        // ViewModels/TransistorListViewModel.cs

        private TransistorRow CreateRowFromObject(object item)
        {
            var row = new TransistorRow();

            try
            {
                // Obtener Id
                var propId = item.GetType().GetProperty("Id");
                if (propId != null)
                    row.Id = (int)propId.GetValue(item);

                // Obtener Name
                var propName = item.GetType().GetProperty("Name");
                row.Name = propName?.GetValue(item)?.ToString() ?? string.Empty;

                // Obtener valores de parámetros
                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                for (int i = 0; i < maxParams; i++)
                {
                    if (i < _displayProperties.Count)
                    {
                        var p = _displayProperties[i];
                        var v = p.GetValue(item);
                        row.Values.Add(v?.ToString() ?? string.Empty);
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
                // Asegurar que Values tenga al menos el número máximo de elementos
                int maxParams = ColumnLayoutHelper.MaxParameterCount;
                while (row.Values.Count < maxParams)
                    row.Values.Add(string.Empty);
            }

            return row;
        }


    }
}