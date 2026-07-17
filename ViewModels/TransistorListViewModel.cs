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
            public object Original { get; set; }
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

        public TransistorListViewModel(DatabaseService databaseService, NavigationService navigationService)
        {
            _databaseService = databaseService;
            _navigationService = navigationService;
        }

        public async Task InitializeAsync(TableType tableType)
        {
            _tableType = tableType;
            _modelType = tableType.GetModelType();
            TableDisplayName = tableType.GetDisplayName();
            Title = $"Transistores {TableDisplayName}";

            // Configurar propiedades a mostrar
            ConfigureDisplayProperties();

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
            HeaderFields.Add("Nombre");
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
                    // Construir fila con valores según _displayProperties
                    var row = new TransistorRow();
                    var propId = item.GetType().GetProperty("Id");
                    if (propId != null)
                        row.Id = (int)propId.GetValue(item);
                    var propName = item.GetType().GetProperty("Name");
                    row.Name = propName?.GetValue(item)?.ToString() ?? string.Empty;
                    // Añadir valores hasta el máximo de parámetros, rellenando con string.Empty si faltan
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
                        // Filtro de texto para nombre
                        if (!string.IsNullOrWhiteSpace(filter.TextValue))
                        {
                            textFilters[filter.Field] = filter.TextValue;
                        }
                    }
                    else
                    {
                        // Filtros numéricos
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
                    Transistors.Add(item);
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

            // Recargar todos los transistores
            LoadTransistorsAsync();
        }

        [RelayCommand]
        private async Task SelectTransistor(object transistor)
        {
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
    }
}