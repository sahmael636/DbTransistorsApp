// Services/DatabaseService.cs
using DbTransistorsApp.Models.Base;
using System.Diagnostics;
using SQLite;

namespace DbTransistorsApp.Services
{
    public partial class DatabaseService
    {
        private readonly SQLiteAsyncConnection _database;
        private readonly string _dbPath;

        public DatabaseService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, "dbtransistors.db");

            if (!File.Exists(_dbPath))
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("dbtransistors.db").Result;
                using var fileStream = File.Create(_dbPath);
                stream.CopyTo(fileStream);
            }

            _database = new SQLiteAsyncConnection(_dbPath);
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                await _database.ExecuteScalarAsync<int>("SELECT 1");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ==================== ESTRUCTURAS ====================
        public async Task<List<Estructura>> GetAllEstructurasAsync()
            => await _database.Table<Estructura>().OrderBy(x => x.Nombre).ToListAsync();

        public async Task<Estructura> GetEstructuraByIdAsync(int id)
            => await _database.FindAsync<Estructura>(id);

        public async Task<int> InsertEstructuraAsync(Estructura entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateEstructuraAsync(Estructura entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteEstructuraAsync(int id)
            => await _database.DeleteAsync<Estructura>(id);

        // ==================== ENCAPSULADOS ====================
        public async Task<List<Encapsulado>> GetAllEncapsuladosAsync()
            => await _database.Table<Encapsulado>().OrderBy(x => x.Nombre).ToListAsync();

        public async Task<Encapsulado> GetEncapsuladoByIdAsync(int id)
            => await _database.FindAsync<Encapsulado>(id);

        public async Task<int> InsertEncapsuladoAsync(Encapsulado entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateEncapsuladoAsync(Encapsulado entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteEncapsuladoAsync(int id)
            => await _database.DeleteAsync<Encapsulado>(id);

        // ==================== ByName ====================
        public async Task<List<ByName>> GetAllByNameAsync()
            => await _database.Table<ByName>().OrderBy(x => x.Name).ToListAsync();

        public async Task<List<ByName>> SearchByNameAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllByNameAsync();

            return await _database.Table<ByName>()
                .Where(x => x.Name.Contains(searchTerm))
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // ==================== BJT GERMANIUM ====================
        public async Task<List<BjtGe>> GetAllBjtGeAsync()
            => await _database.Table<BjtGe>().OrderBy(x => x.Name).ToListAsync();

        public async Task<BjtGe> GetBjtGeByIdAsync(int id)
            => await _database.FindAsync<BjtGe>(id);

        public async Task<int> InsertBjtGeAsync(BjtGe entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateBjtGeAsync(BjtGe entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteBjtGeAsync(int id)
            => await _database.DeleteAsync<BjtGe>(id);

        // ==================== BJT SILICIO ====================
        public async Task<List<BjtSi>> GetAllBjtSiAsync()
            => await _database.Table<BjtSi>().OrderBy(x => x.Name).ToListAsync();

        public async Task<BjtSi> GetBjtSiByIdAsync(int id)
            => await _database.FindAsync<BjtSi>(id);

        public async Task<int> InsertBjtSiAsync(BjtSi entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateBjtSiAsync(BjtSi entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteBjtSiAsync(int id)
            => await _database.DeleteAsync<BjtSi>(id);

        // ==================== BJT PREBIAS ====================
        public async Task<List<BjtPrebias>> GetAllBjtPrebiasAsync()
            => await _database.Table<BjtPrebias>().OrderBy(x => x.Name).ToListAsync();

        public async Task<BjtPrebias> GetBjtPrebiasByIdAsync(int id)
            => await _database.FindAsync<BjtPrebias>(id);

        public async Task<int> InsertBjtPrebiasAsync(BjtPrebias entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateBjtPrebiasAsync(BjtPrebias entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteBjtPrebiasAsync(int id)
            => await _database.DeleteAsync<BjtPrebias>(id);

        // ==================== BJT PREBIAS DUAL ====================
        public async Task<List<BjtPrebiasDual>> GetAllBjtPrebiasDualAsync()
            => await _database.Table<BjtPrebiasDual>().OrderBy(x => x.Name).ToListAsync();

        public async Task<BjtPrebiasDual> GetBjtPrebiasDualByIdAsync(int id)
            => await _database.FindAsync<BjtPrebiasDual>(id);

        public async Task<int> InsertBjtPrebiasDualAsync(BjtPrebiasDual entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateBjtPrebiasDualAsync(BjtPrebiasDual entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteBjtPrebiasDualAsync(int id)
            => await _database.DeleteAsync<BjtPrebiasDual>(id);

        // ==================== BJT SILICIO DUAL ====================
        public async Task<List<BjtSiDual>> GetAllBjtSiDualAsync()
            => await _database.Table<BjtSiDual>().OrderBy(x => x.Name).ToListAsync();

        public async Task<BjtSiDual> GetBjtSiDualByIdAsync(int id)
            => await _database.FindAsync<BjtSiDual>(id);

        public async Task<int> InsertBjtSiDualAsync(BjtSiDual entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateBjtSiDualAsync(BjtSiDual entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteBjtSiDualAsync(int id)
            => await _database.DeleteAsync<BjtSiDual>(id);

        // ==================== JFET ====================
        public async Task<List<Jfet>> GetAllJfetAsync()
            => await _database.Table<Jfet>().OrderBy(x => x.Name).ToListAsync();

        public async Task<Jfet> GetJfetByIdAsync(int id)
            => await _database.FindAsync<Jfet>(id);

        public async Task<int> InsertJfetAsync(Jfet entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateJfetAsync(Jfet entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteJfetAsync(int id)
            => await _database.DeleteAsync<Jfet>(id);

        // ==================== MOSFET ====================
        public async Task<List<Mosfet>> GetAllMosfetAsync()
            => await _database.Table<Mosfet>().OrderBy(x => x.Name).ToListAsync();

        public async Task<Mosfet> GetMosfetByIdAsync(int id)
            => await _database.FindAsync<Mosfet>(id);

        public async Task<int> InsertMosfetAsync(Mosfet entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateMosfetAsync(Mosfet entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteMosfetAsync(int id)
            => await _database.DeleteAsync<Mosfet>(id);

        // ==================== MOSFET DUAL ====================
        public async Task<List<MosfetDual>> GetAllMosfetDualAsync()
            => await _database.Table<MosfetDual>().OrderBy(x => x.Name).ToListAsync();

        public async Task<MosfetDual> GetMosfetDualByIdAsync(int id)
            => await _database.FindAsync<MosfetDual>(id);

        public async Task<int> InsertMosfetDualAsync(MosfetDual entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateMosfetDualAsync(MosfetDual entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteMosfetDualAsync(int id)
            => await _database.DeleteAsync<MosfetDual>(id);

        // ==================== IGBT ====================
        public async Task<List<Igbt>> GetAllIgbtAsync()
            => await _database.Table<Igbt>().OrderBy(x => x.Name).ToListAsync();

        public async Task<Igbt> GetIgbtByIdAsync(int id)
            => await _database.FindAsync<Igbt>(id);

        public async Task<int> InsertIgbtAsync(Igbt entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateIgbtAsync(Igbt entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteIgbtAsync(int id)
            => await _database.DeleteAsync<Igbt>(id);

        // ==================== IGBT DUAL ====================
        public async Task<List<IgbtDual>> GetAllIgbtDualAsync()
            => await _database.Table<IgbtDual>().OrderBy(x => x.Name).ToListAsync();

        public async Task<IgbtDual> GetIgbtDualByIdAsync(int id)
            => await _database.FindAsync<IgbtDual>(id);

        public async Task<int> InsertIgbtDualAsync(IgbtDual entity)
            => await _database.InsertAsync(entity);

        public async Task<int> UpdateIgbtDualAsync(IgbtDual entity)
            => await _database.UpdateAsync(entity);

        public async Task<int> DeleteIgbtDualAsync(int id)
            => await _database.DeleteAsync<IgbtDual>(id);


        // ==================== RELACIONES ====================
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

        // ==================== REEMPLAZOS ====================
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

            if (structId > 0)
            {
                conditions.Add("struct_id = ?");
                args.Add(structId);
            }

            if (parameters.ContainsKey("_id"))
            {
                conditions.Add("_id != ?");
                args.Add(parameters["_id"]);
            }

            string whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            string query = $"SELECT * FROM {tableName} {whereClause} ORDER BY name";

            return await ExecuteQueryAsync(tableName, query, args.ToArray());
        }

        // ==================== FILTRADO ====================
        public async Task<List<object>> GetFilteredTransistorsAsync(
            string tableName,
            Dictionary<string, object> numericFilters,
            Dictionary<string, string> textFilters)
        {
            var conditions = new List<string>();
            var parameters = new List<object>();

            foreach (var filter in numericFilters)
            {
                conditions.Add(filter.Key);
                parameters.Add(filter.Value);
            }

            foreach (var filter in textFilters)
            {
                conditions.Add($"{filter.Key} LIKE ?");
                parameters.Add($"%{filter.Value}%");
            }

            string whereClause = conditions.Any() ? $"WHERE {string.Join(" AND ", conditions)}" : "";
            string query = $"SELECT * FROM {tableName} {whereClause} ORDER BY name";

            return await ExecuteQueryAsync(tableName, query, parameters.ToArray());
        }

        // ==================== MÉTODOS GENÉRICOS POR NOMBRE DE TABLA ====================

        public async Task<List<object>> GetAllByTableAsync(string tableName)
        {
            return tableName?.ToLower() switch
            {
                "bjtge" => (await GetAllBjtGeAsync()).Cast<object>().ToList(),
                "bjtsi" => (await GetAllBjtSiAsync()).Cast<object>().ToList(),
                "bjtprebias" => (await GetAllBjtPrebiasAsync()).Cast<object>().ToList(),
                "bjtprebiasdual" => (await GetAllBjtPrebiasDualAsync()).Cast<object>().ToList(),
                "bjtsidual" => (await GetAllBjtSiDualAsync()).Cast<object>().ToList(),
                "jfet" => (await GetAllJfetAsync()).Cast<object>().ToList(),
                "mosfet" => (await GetAllMosfetAsync()).Cast<object>().ToList(),
                "mosfetdual" => (await GetAllMosfetDualAsync()).Cast<object>().ToList(),
                "igbt" => (await GetAllIgbtAsync()).Cast<object>().ToList(),
                "igbtdual" => (await GetAllIgbtDualAsync()).Cast<object>().ToList(),
                _ => throw new ArgumentException($"Tabla no válida: {tableName}")
            };
        }

        public async Task<ITransistor> GetTransistorByTypeAndIdAsync(string type, int id)
        {
            Debug.WriteLine($"GetTransistorByTypeAndIdAsync called: type={type}, id={id}");

            switch (type?.ToLower())
            {
                case "bjtge":
                {
                    var res = await GetBjtGeByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for bjtge id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "bjtsi":
                {
                    var res = await GetBjtSiByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for bjtsi id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "bjtprebias":
                {
                    var res = await GetBjtPrebiasByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for bjtprebias id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "bjtprebiasdual":
                {
                    var res = await GetBjtPrebiasDualByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for bjtprebiasdual id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "bjtsidual":
                {
                    var res = await GetBjtSiDualByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for bjtsidual id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "jfet":
                {
                    var res = await GetJfetByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for jfet id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "mosfet":
                {
                    var res = await GetMosfetByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for mosfet id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "mosfetdual":
                {
                    var res = await GetMosfetDualByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for mosfetdual id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "igbt":
                {
                    var res = await GetIgbtByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for igbt id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                case "igbtdual":
                {
                    var res = await GetIgbtDualByIdAsync(id);
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync result for igbtdual id={id}: {(res != null ? "found" : "null")}");
                    return res;
                }
                default:
                    Debug.WriteLine($"GetTransistorByTypeAndIdAsync: tipo no válido {type}");
                    throw new ArgumentException($"Tipo no válido: {type}");
            }
        }

        public async Task<int> InsertTransistorAsync(string tableName, ITransistor transistor)
        {
            return tableName?.ToLower() switch
            {
                "bjtge" => await InsertBjtGeAsync((BjtGe)transistor),
                "bjtsi" => await InsertBjtSiAsync((BjtSi)transistor),
                "bjtprebias" => await InsertBjtPrebiasAsync((BjtPrebias)transistor),
                "bjtprebiasdual" => await InsertBjtPrebiasDualAsync((BjtPrebiasDual)transistor),
                "bjtsidual" => await InsertBjtSiDualAsync((BjtSiDual)transistor),
                "jfet" => await InsertJfetAsync((Jfet)transistor),
                "mosfet" => await InsertMosfetAsync((Mosfet)transistor),
                "mosfetdual" => await InsertMosfetDualAsync((MosfetDual)transistor),
                "igbt" => await InsertIgbtAsync((Igbt)transistor),
                "igbtdual" => await InsertIgbtDualAsync((IgbtDual)transistor),
                _ => throw new ArgumentException($"Tabla no válida: {tableName}")
            };
        }

        public async Task<int> UpdateTransistorAsync(string tableName, ITransistor transistor)
        {
            return tableName?.ToLower() switch
            {
                "bjtge" => await UpdateBjtGeAsync((BjtGe)transistor),
                "bjtsi" => await UpdateBjtSiAsync((BjtSi)transistor),
                "bjtprebias" => await UpdateBjtPrebiasAsync((BjtPrebias)transistor),
                "bjtprebiasdual" => await UpdateBjtPrebiasDualAsync((BjtPrebiasDual)transistor),
                "bjtsidual" => await UpdateBjtSiDualAsync((BjtSiDual)transistor),
                "jfet" => await UpdateJfetAsync((Jfet)transistor),
                "mosfet" => await UpdateMosfetAsync((Mosfet)transistor),
                "mosfetdual" => await UpdateMosfetDualAsync((MosfetDual)transistor),
                "igbt" => await UpdateIgbtAsync((Igbt)transistor),
                "igbtdual" => await UpdateIgbtDualAsync((IgbtDual)transistor),
                _ => throw new ArgumentException($"Tabla no válida: {tableName}")
            };
        }

        public async Task<int> DeleteTransistorAsync(string tableName, int id)
        {
            return tableName?.ToLower() switch
            {
                "bjtge" => await DeleteBjtGeAsync(id),
                "bjtsi" => await DeleteBjtSiAsync(id),
                "bjtprebias" => await DeleteBjtPrebiasAsync(id),
                "bjtprebiasdual" => await DeleteBjtPrebiasDualAsync(id),
                "bjtsidual" => await DeleteBjtSiDualAsync(id),
                "jfet" => await DeleteJfetAsync(id),
                "mosfet" => await DeleteMosfetAsync(id),
                "mosfetdual" => await DeleteMosfetDualAsync(id),
                "igbt" => await DeleteIgbtAsync(id),
                "igbtdual" => await DeleteIgbtDualAsync(id),
                _ => throw new ArgumentException($"Tabla no válida: {tableName}")
            };
        }

        // ==================== AUXILIARES ====================
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

        private async Task<List<object>> ExecuteQueryAsync(string tableName, string query, params object[] args)
        {
            var type = GetModelType(tableName);
            var method = typeof(SQLiteAsyncConnection).GetMethod("QueryAsync");
            var genericMethod = method.MakeGenericMethod(type);
            var task = (Task)genericMethod.Invoke(_database, new object[] { query, args });
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty.GetValue(task);
            return (List<object>)result;
        }
    }
}