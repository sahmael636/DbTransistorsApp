// Services/DatabaseServiceExtensions.cs
using SQLite;
using System.Collections.ObjectModel;
using System.Text;

namespace DbTransistorsApp.Services
{
    public partial class DatabaseService
    {
        // Métodos para Encapsulados
        public async Task<List<Encapsulado>> GetAllEncapsuladosAsync()
        {
            return await _database.Table<Encapsulado>().ToListAsync();
        }

        public async Task<Encapsulado> GetEncapsuladoByIdAsync(int id)
        {
            return await _database.FindAsync<Encapsulado>(id);
        }

        public async Task<int> InsertEncapsuladoAsync(Encapsulado encapsulado)
        {
            return await _database.InsertAsync(encapsulado);
        }

        public async Task<int> UpdateEncapsuladoAsync(Encapsulado encapsulado)
        {
            return await _database.UpdateAsync(encapsulado);
        }

        public async Task<int> DeleteEncapsuladoAsync(int id)
        {
            return await _database.DeleteAsync<Encapsulado>(id);
        }

        // Métodos para Estructuras
        public async Task<List<Estructura>> GetAllEstructurasAsync()
        {
            return await _database.Table<Estructura>().ToListAsync();
        }

        public async Task<Estructura> GetEstructuraByIdAsync(int id)
        {
            return await _database.FindAsync<Estructura>(id);
        }

        public async Task<int> InsertEstructuraAsync(Estructura estructura)
        {
            return await _database.InsertAsync(estructura);
        }

        public async Task<int> UpdateEstructuraAsync(Estructura estructura)
        {
            return await _database.UpdateAsync(estructura);
        }

        public async Task<int> DeleteEstructuraAsync(int id)
        {
            return await _database.DeleteAsync<Estructura>(id);
        }

        // Métodos genéricos para transistores
        public async Task<List<object>> GetAllByTableAsync(string tableName)
        {
            var type = GetModelType(tableName);
            var query = $"SELECT * FROM {tableName} ORDER BY name";
            var results = await _database.QueryAsync(type, query);
            return results.Cast<object>().ToList();
        }

        public async Task<ITransistor> GetTransistorByTypeAndIdAsync(string type, int id)
        {
            var modelType = GetModelType(type);
            return await _database.FindAsync(modelType, id) as ITransistor;
        }

        public async Task<int> InsertTransistorAsync(string tableName, ITransistor transistor)
        {
            return await _database.InsertAsync(transistor);
        }

        public async Task<int> UpdateTransistorAsync(string tableName, ITransistor transistor)
        {
            return await _database.UpdateAsync(transistor);
        }

        public async Task<int> DeleteTransistorAsync(string tableName, int id)
        {
            var modelType = GetModelType(tableName);
            var transistor = await _database.FindAsync(modelType, id);
            if (transistor != null)
            {
                return await _database.DeleteAsync(transistor);
            }
            return 0;
        }

        // Métodos para obtener transistores filtrados
        public async Task<List<object>> GetFilteredTransistorsAsync(
            string tableName,
            Dictionary<string, object> numericFilters,
            Dictionary<string, string> textFilters)
        {
            var type = GetModelType(tableName);
            var conditions = new List<string>();
            var parameters = new List<object>();

            // Agregar filtros numéricos
            foreach (var filter in numericFilters)
            {
                conditions.Add(filter.Key);
                parameters.Add(filter.Value);
            }

            // Agregar filtros de texto
            foreach (var filter in textFilters)
            {
                conditions.Add($"{filter.Key} LIKE ?");
                parameters.Add($"%{filter.Value}%");
            }

            string whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            string query = $"SELECT * FROM {tableName} {whereClause} ORDER BY name";

            var results = await _database.QueryAsync(type, query, parameters.ToArray());
            return results.Cast<object>().ToList();
        }

        // Métodos para obtener reemplazos
        public async Task<List<object>> GetReplacementsAsync(
            string tableName,
            Dictionary<string, object> parameters,
            int structId,
            List<int> capsIds)
        {
            var type = GetModelType(tableName);
            var conditions = new List<string>();
            var args = new List<object>();

            foreach (var param in parameters)
            {
                if (param.Key != "_id" && param.Value != null)
                {
                    if (param.Value is double doubleValue && doubleValue > 0)
                    {
                        conditions.Add($"{param.Key} >= ?");
                        args.Add(doubleValue);
                    }
                }
            }

            // Añadir condición de estructura
            if (structId > 0)
            {
                conditions.Add("struct_id = ?");
                args.Add(structId);
            }

            // Excluir el transistor actual
            if (parameters.ContainsKey("_id"))
            {
                conditions.Add("_id != ?");
                args.Add(parameters["_id"]);
            }

            string whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            string query = $"SELECT * FROM {tableName} {whereClause} ORDER BY name";

            var results = await _database.QueryAsync(type, query, args.ToArray());
            return results.Cast<object>().ToList();
        }

        // Métodos para obtener encapsulados por transistor
        public async Task<List<Encapsulado>> GetEncapsuladosByTransistorIdAsync(string tableName, int transistorId)
        {
            string joinTable = $"{tableName}_caps";
            var query = $@"
                SELECT e.* 
                FROM encapsulados e
                INNER JOIN {joinTable} tc ON e.id = tc.caps_id
                WHERE tc.{tableName}_id = ?
            ";

            return await _database.QueryAsync<Encapsulado>(query, transistorId);
        }

        // Método para obtener el tipo de modelo
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

        // Método para importar desde Excel
        public async Task<int> ImportTransistorsFromExcelAsync(
            string tableName,
            List<Dictionary<string, object>> rows,
            List<int> capsIds)
        {
            var type = GetModelType(tableName);
            int importedCount = 0;

            foreach (var row in rows)
            {
                try
                {
                    var transistor = (ITransistor)Activator.CreateInstance(type);

                    // Asignar propiedades
                    foreach (var prop in type.GetProperties())
                    {
                        if (row.ContainsKey(prop.Name))
                        {
                            var value = row[prop.Name];
                            if (value != null)
                            {
                                // Convertir el valor al tipo adecuado
                                var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                                prop.SetValue(transistor, convertedValue);
                            }
                        }
                    }

                    // Asignar IDs de encapsulados
                    transistor.CapsIds = capsIds;

                    // Insertar en la base de datos
                    await _database.InsertAsync(transistor);
                    importedCount++;
                }
                catch (Exception ex)
                {
                    // Registrar error pero continuar
                    Console.WriteLine($"Error al importar fila: {ex.Message}");
                }
            }

            return importedCount;
        }
    }
}