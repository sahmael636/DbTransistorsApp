// Services/ExcelImportService.cs
using DbTransistorsApp.Helpers;
using ExcelDataReader;
using System.Text;

namespace DbTransistorsApp.Services
{
    public class ExcelImportService
    {
        public async Task<List<Dictionary<string, object>>> ImportFromExcelAsync(
            string filePath,
            string tableName,
            IProgress<int> progress = null)
        {
            var results = new List<Dictionary<string, object>>();
            var errors = new List<string>();

            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);

                var conf = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true
                    }
                };

                var dataSet = reader.AsDataSet(conf);
                var dataTable = dataSet.Tables[0];

                // Obtener los nombres de las columnas
                var columnNames = new List<string>();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    columnNames.Add(dataTable.Columns[i].ColumnName.ToLower());
                }

                // Obtener las propiedades del modelo
                var modelType = GetModelType(tableName);
                var properties = modelType.GetProperties()
                    .Where(p => p.Name != "Id" && p.Name != "CapsIds" && p.Name != "StructId")
                    .ToList();

                // Procesar filas
                int totalRows = dataTable.Rows.Count;
                int processedRows = 0;

                foreach (System.Data.DataRow row in dataTable.Rows)
                {
                    try
                    {
                        var rowData = new Dictionary<string, object>();
                        bool hasValidData = false;

                        // Verificar cada campo
                        foreach (var prop in properties)
                        {
                            if (columnNames.Contains(prop.Name.ToLower()))
                            {
                                var value = row[prop.Name.ToLower()];
                                if (value != null && value != DBNull.Value)
                                {
                                    // Validar valor según el tipo
                                    if (prop.PropertyType == typeof(string))
                                    {
                                        var stringValue = value.ToString().Trim();
                                        if (!string.IsNullOrEmpty(stringValue))
                                        {
                                            rowData[prop.Name] = stringValue;
                                            hasValidData = true;
                                        }
                                    }
                                    else if (prop.PropertyType.IsNumericType())
                                    {
                                        if (double.TryParse(value.ToString(), out double numericValue))
                                        {
                                            rowData[prop.Name] = numericValue;
                                            hasValidData = true;
                                        }
                                    }
                                }
                            }
                        }

                        // Guardar solo si tiene datos válidos
                        if (hasValidData)
                        {
                            results.Add(rowData);
                        }

                        processedRows++;
                        progress?.Report((processedRows * 100) / totalRows);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error en fila {processedRows + 1}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al importar: {ex.Message}");
            }

            if (errors.Any())
            {
                // Log de errores
                Console.WriteLine($"Errores de importación: {string.Join("\n", errors)}");
            }

            return results;
        }

        private Type GetModelType(string tableName)
        {
            return tableName switch
            {
                "bjtge" => typeof(BjtGe),
                "bjtsi" => typeof(BjtSi),
                "bjtprebias" => typeof(BjtPrebias),
                "bjtprebiasdual" => typeof(BjtPrebiasDual),
                "bjtsidual" => typeof(BjtSiDual),
                "jfet" => typeof(Jfet),
                "mosfet" => typeof(Mosfet),
                "mosfetdual" => typeof(MosfetDual),
                "igbt" => typeof(Igbt),
                "igbtdual" => typeof(IgbtDual),
                _ => throw new ArgumentException($"Tabla no válida: {tableName}")
            };
        }
    }
}