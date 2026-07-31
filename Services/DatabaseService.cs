using DbTransistorsApp.Models.Base;
using SQLite;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace DbTransistorsApp.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _database = null!;
    private readonly string _dbPath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _isInitialized;
    private int _maintenanceStarted;

    public DatabaseService()
    {
        // El constructor debe ser inmediato: este servicio se resuelve mientras
        // MAUI está creando la primera ventana.
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "dbtransistors.db");
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_isInitialized)
                return;

            if (!File.Exists(_dbPath) || new FileInfo(_dbPath).Length == 0)
            {
                string temporaryPath = _dbPath + ".tmp";
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                await using (Stream stream = await FileSystem
                    .OpenAppPackageFileAsync("dbtransistors.db")
                    .ConfigureAwait(false))
                await using (FileStream fileStream = File.Create(temporaryPath))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                    await fileStream.FlushAsync().ConfigureAwait(false);
                }

                File.Move(temporaryPath, _dbPath, true);
            }

            _database = new SQLiteAsyncConnection(_dbPath);
            await InitializeSchemaAsync().ConfigureAwait(false);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private async Task InitializeSchemaAsync()
    {
        // Solo se aplican migraciones imprescindibles para abrir la aplicación.
        // La creación de índices puede tardar varios minutos en algunos teléfonos
        // y nunca debe bloquear el primer fotograma de Android.
        var columns = await _database
            .QueryAsync<PragmaColumn>("PRAGMA table_info(encapsulados)")
            .ConfigureAwait(false);

        if (!columns.Any(c => string.Equals(c.Name, "ruta", StringComparison.OrdinalIgnoreCase)))
        {
            await _database
                .ExecuteAsync("ALTER TABLE encapsulados ADD COLUMN ruta TEXT")
                .ConfigureAwait(false);
        }
    }

    public void StartBackgroundMaintenance()
    {
        if (!_isInitialized || Interlocked.Exchange(ref _maintenanceStarted, 1) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                // La base distribuida ya incluye los índices. No se abre una
                // segunda operación de escritura durante las primeras consultas
                // si la migración ya fue aplicada.
                int version = await _database
                    .ExecuteScalarAsync<int>("PRAGMA user_version")
                    .ConfigureAwait(false);
                if (version >= 2)
                    return;

                await Task.Delay(TimeSpan.FromSeconds(8)).ConfigureAwait(false);

                var statements = new List<string>
                {
                    "CREATE INDEX IF NOT EXISTS idx_byname_name_nocase ON byname(name COLLATE NOCASE)"
                };

                foreach (string table in TransistorMetadata.TableNames)
                {
                    statements.Add($"CREATE INDEX IF NOT EXISTS idx_{table}_name_nocase ON {table}(name COLLATE NOCASE)");
                    statements.Add($"CREATE INDEX IF NOT EXISTS idx_{table}_struct ON {table}(struct_id)");
                }

                foreach (string statement in statements)
                {
                    await _database.ExecuteAsync(statement).ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                }

                await _database.ExecuteAsync("PRAGMA user_version = 2").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mantenimiento SQLite omitido: {ex}");
            }
        });
    }

    public string DatabasePath => _dbPath;

    public async Task<bool> TestConnection()
    {
        try
        {
            await InitializeAsync().ConfigureAwait(false);
            await _database.ExecuteScalarAsync<int>("SELECT 1").ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ==================== ESTRUCTURAS ====================
    public Task<List<Estructura>> GetAllEstructurasAsync()
        => _database.Table<Estructura>().OrderBy(x => x.Id).ToListAsync();

    public Task<Estructura?> GetEstructuraByIdAsync(int id)
        => _database.FindAsync<Estructura>(id);

    public async Task<bool> StructureNameExistsAsync(string name, int excludeId = 0)
    {
        string normalized = NormalizeName(name);
        int count = await _database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM estructuras WHERE LOWER(TRIM(nombre)) = ? AND id <> ?",
            normalized,
            excludeId);
        return count > 0;
    }

    public async Task<int> InsertEstructuraAsync(Estructura entity)
    {
        entity.Nombre = entity.Nombre.Trim();
        if (await StructureNameExistsAsync(entity.Nombre))
            throw new InvalidOperationException("Ya existe una estructura con ese nombre.");
        return await _database.InsertAsync(entity);
    }

    public async Task<int> UpdateEstructuraAsync(Estructura entity)
    {
        entity.Nombre = entity.Nombre.Trim();
        if (await StructureNameExistsAsync(entity.Nombre, entity.Id))
            throw new InvalidOperationException("Ya existe una estructura con ese nombre.");
        return await _database.UpdateAsync(entity);
    }

    public async Task<bool> IsStructureInUseAsync(int id)
    {
        foreach (string table in TransistorMetadata.TableNames)
        {
            int count = await _database.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM {table} WHERE struct_id = ?",
                id);
            if (count > 0)
                return true;
        }
        return false;
    }

    public async Task<int> DeleteEstructuraAsync(int id)
    {
        if (await IsStructureInUseAsync(id))
            throw new InvalidOperationException("La estructura está siendo utilizada por uno o más transistores y no puede eliminarse.");
        return await _database.DeleteAsync<Estructura>(id);
    }

    public async Task<List<Estructura>> GetAvailableStructuresForTableAsync(string tableName)
    {
        string safeTable = TransistorMetadata.NormalizeTableName(tableName);
        return await _database.QueryAsync<Estructura>($@"
            SELECT DISTINCT e.id, e.nombre
            FROM estructuras e
            INNER JOIN {safeTable} t ON t.struct_id = e.id
            ORDER BY e.id");
    }

    public async Task<HashSet<int>> GetAllowedStructureIdsForTableAsync(string tableName)
    {
        var available = await GetAvailableStructuresForTableAsync(tableName);
        if (available.Count == 0)
            available = await GetAllEstructurasAsync();
        return available.Select(x => x.Id).ToHashSet();
    }

    // ==================== ENCAPSULADOS ====================
    public Task<List<Encapsulado>> GetAllEncapsuladosAsync()
        => _database.Table<Encapsulado>().OrderBy(x => x.Id).ToListAsync();

    public Task<Encapsulado?> GetEncapsuladoByIdAsync(int id)
        => _database.FindAsync<Encapsulado>(id);

    public async Task<bool> EncapsuladoNameExistsAsync(string name, int excludeId = 0)
    {
        string normalized = NormalizeName(name);
        int count = await _database.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM encapsulados WHERE LOWER(TRIM(nombre)) = ? AND id <> ?",
            normalized,
            excludeId);
        return count > 0;
    }

    public async Task<int> InsertEncapsuladoAsync(Encapsulado entity)
    {
        entity.Nombre = entity.Nombre.Trim();
        if (await EncapsuladoNameExistsAsync(entity.Nombre))
            throw new InvalidOperationException("Ya existe un encapsulado con ese nombre.");
        return await _database.InsertAsync(entity);
    }

    public async Task<int> UpdateEncapsuladoAsync(Encapsulado entity)
    {
        entity.Nombre = entity.Nombre.Trim();
        if (await EncapsuladoNameExistsAsync(entity.Nombre, entity.Id))
            throw new InvalidOperationException("Ya existe un encapsulado con ese nombre.");
        return await _database.UpdateAsync(entity);
    }

    public async Task<int> DeleteEncapsuladoAsync(int id)
    {
        foreach (string table in TransistorMetadata.TableNames)
        {
            await _database.ExecuteAsync($"DELETE FROM {table}_caps WHERE caps_id = ?", id);
        }
        return await _database.DeleteAsync<Encapsulado>(id);
    }

    // ==================== BYNAME ====================
    public Task<List<ByName>> GetAllByNameAsync()
        => _database.QueryAsync<ByName>("SELECT * FROM byname ORDER BY name COLLATE NOCASE LIMIT 1000");

    public Task<List<ByName>> SearchByNameAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return GetAllByNameAsync();

        return _database.QueryAsync<ByName>(@"
            SELECT * FROM byname
            WHERE name LIKE ? COLLATE NOCASE
            ORDER BY name COLLATE NOCASE
            LIMIT 1000", $"%{searchTerm.Trim()}%");
    }

    private async Task UpsertByNameAsync(string tableName, ITransistor transistor)
    {
        await DeleteByNameAsync(tableName, transistor.Id);
        await _database.InsertAsync(new ByName
        {
            Name = transistor.Name,
            Type = TransistorMetadata.GetByNameType(tableName),
            Idx = transistor.Id
        });
    }

    private Task DeleteByNameAsync(string tableName, int id)
    {
        IReadOnlyList<string> aliases = TransistorMetadata.GetByNameTypeAliases(tableName);
        string placeholders = string.Join(",", aliases.Select(_ => "?"));
        var args = aliases.Cast<object>().ToList();
        args.Add(id);
        return _database.ExecuteAsync(
            $"DELETE FROM byname WHERE type COLLATE NOCASE IN ({placeholders}) AND idx = ?",
            args.ToArray());
    }

    // ==================== MÉTODOS TIPADOS ====================
    public Task<List<BjtGe>> GetAllBjtGeAsync() => _database.Table<BjtGe>().OrderBy(x => x.Name).ToListAsync();
    public Task<BjtGe?> GetBjtGeByIdAsync(int id) => _database.FindAsync<BjtGe>(id);
    public Task<int> InsertBjtGeAsync(BjtGe e) => _database.InsertAsync(e);
    public Task<int> UpdateBjtGeAsync(BjtGe e) => _database.UpdateAsync(e);
    public Task<int> DeleteBjtGeAsync(int id) => _database.DeleteAsync<BjtGe>(id);

    public Task<List<BjtSi>> GetAllBjtSiAsync() => _database.Table<BjtSi>().OrderBy(x => x.Name).ToListAsync();
    public Task<BjtSi?> GetBjtSiByIdAsync(int id) => _database.FindAsync<BjtSi>(id);
    public Task<int> InsertBjtSiAsync(BjtSi e) => _database.InsertAsync(e);
    public Task<int> UpdateBjtSiAsync(BjtSi e) => _database.UpdateAsync(e);
    public Task<int> DeleteBjtSiAsync(int id) => _database.DeleteAsync<BjtSi>(id);

    public Task<List<BjtPrebias>> GetAllBjtPrebiasAsync() => _database.Table<BjtPrebias>().OrderBy(x => x.Name).ToListAsync();
    public Task<BjtPrebias?> GetBjtPrebiasByIdAsync(int id) => _database.FindAsync<BjtPrebias>(id);
    public Task<int> InsertBjtPrebiasAsync(BjtPrebias e) => _database.InsertAsync(e);
    public Task<int> UpdateBjtPrebiasAsync(BjtPrebias e) => _database.UpdateAsync(e);
    public Task<int> DeleteBjtPrebiasAsync(int id) => _database.DeleteAsync<BjtPrebias>(id);

    public Task<List<BjtPrebiasDual>> GetAllBjtPrebiasDualAsync() => _database.Table<BjtPrebiasDual>().OrderBy(x => x.Name).ToListAsync();
    public Task<BjtPrebiasDual?> GetBjtPrebiasDualByIdAsync(int id) => _database.FindAsync<BjtPrebiasDual>(id);
    public Task<int> InsertBjtPrebiasDualAsync(BjtPrebiasDual e) => _database.InsertAsync(e);
    public Task<int> UpdateBjtPrebiasDualAsync(BjtPrebiasDual e) => _database.UpdateAsync(e);
    public Task<int> DeleteBjtPrebiasDualAsync(int id) => _database.DeleteAsync<BjtPrebiasDual>(id);

    public Task<List<BjtSiDual>> GetAllBjtSiDualAsync() => _database.Table<BjtSiDual>().OrderBy(x => x.Name).ToListAsync();
    public Task<BjtSiDual?> GetBjtSiDualByIdAsync(int id) => _database.FindAsync<BjtSiDual>(id);
    public Task<int> InsertBjtSiDualAsync(BjtSiDual e) => _database.InsertAsync(e);
    public Task<int> UpdateBjtSiDualAsync(BjtSiDual e) => _database.UpdateAsync(e);
    public Task<int> DeleteBjtSiDualAsync(int id) => _database.DeleteAsync<BjtSiDual>(id);

    public Task<List<Jfet>> GetAllJfetAsync() => _database.Table<Jfet>().OrderBy(x => x.Name).ToListAsync();
    public Task<Jfet?> GetJfetByIdAsync(int id) => _database.FindAsync<Jfet>(id);
    public Task<int> InsertJfetAsync(Jfet e) => _database.InsertAsync(e);
    public Task<int> UpdateJfetAsync(Jfet e) => _database.UpdateAsync(e);
    public Task<int> DeleteJfetAsync(int id) => _database.DeleteAsync<Jfet>(id);

    public Task<List<Mosfet>> GetAllMosfetAsync() => _database.Table<Mosfet>().OrderBy(x => x.Name).ToListAsync();
    public Task<Mosfet?> GetMosfetByIdAsync(int id) => _database.FindAsync<Mosfet>(id);
    public Task<int> InsertMosfetAsync(Mosfet e) => _database.InsertAsync(e);
    public Task<int> UpdateMosfetAsync(Mosfet e) => _database.UpdateAsync(e);
    public Task<int> DeleteMosfetAsync(int id) => _database.DeleteAsync<Mosfet>(id);

    public Task<List<MosfetDual>> GetAllMosfetDualAsync() => _database.Table<MosfetDual>().OrderBy(x => x.Name).ToListAsync();
    public Task<MosfetDual?> GetMosfetDualByIdAsync(int id) => _database.FindAsync<MosfetDual>(id);
    public Task<int> InsertMosfetDualAsync(MosfetDual e) => _database.InsertAsync(e);
    public Task<int> UpdateMosfetDualAsync(MosfetDual e) => _database.UpdateAsync(e);
    public Task<int> DeleteMosfetDualAsync(int id) => _database.DeleteAsync<MosfetDual>(id);

    public Task<List<Igbt>> GetAllIgbtAsync() => _database.Table<Igbt>().OrderBy(x => x.Name).ToListAsync();
    public Task<Igbt?> GetIgbtByIdAsync(int id) => _database.FindAsync<Igbt>(id);
    public Task<int> InsertIgbtAsync(Igbt e) => _database.InsertAsync(e);
    public Task<int> UpdateIgbtAsync(Igbt e) => _database.UpdateAsync(e);
    public Task<int> DeleteIgbtAsync(int id) => _database.DeleteAsync<Igbt>(id);

    public Task<List<IgbtDual>> GetAllIgbtDualAsync() => _database.Table<IgbtDual>().OrderBy(x => x.Name).ToListAsync();
    public Task<IgbtDual?> GetIgbtDualByIdAsync(int id) => _database.FindAsync<IgbtDual>(id);
    public Task<int> InsertIgbtDualAsync(IgbtDual e) => _database.InsertAsync(e);
    public Task<int> UpdateIgbtDualAsync(IgbtDual e) => _database.UpdateAsync(e);
    public Task<int> DeleteIgbtDualAsync(int id) => _database.DeleteAsync<IgbtDual>(id);

    // ==================== RELACIONES ====================
    public async Task<List<Encapsulado>> GetEncapsuladosByTransistorIdAsync(string tableName, int transistorId)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        return await _database.QueryAsync<Encapsulado>($@"
            SELECT e.*
            FROM encapsulados e
            INNER JOIN {table}_caps tc ON e.id = tc.caps_id
            WHERE tc.{table}_id = ?
            ORDER BY e.nombre COLLATE NOCASE", transistorId);
    }

    private async Task SaveCapsRelationsAsync(string tableName, int transistorId, IEnumerable<int>? capsIds)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        await _database.ExecuteAsync($"DELETE FROM {table}_caps WHERE {table}_id = ?", transistorId);

        if (capsIds == null)
            return;

        foreach (int capId in capsIds.Where(x => x > 0).Distinct())
        {
            int exists = await _database.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM encapsulados WHERE id = ?", capId);
            if (exists == 0)
                continue;

            await _database.ExecuteAsync(
                $"INSERT INTO {table}_caps ({table}_id, caps_id) VALUES (?, ?)",
                transistorId,
                capId);
        }
    }

    // ==================== REEMPLAZOS Y FILTROS ====================
    public async Task<List<object>> GetReplacementsAsync(
        string tableName,
        Dictionary<string, object> parameters,
        int structId,
        List<int>? capsIds)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        Type type = TransistorMetadata.GetModelType(table);
        var conditions = new List<string>();
        var args = new List<object>();

        foreach (var param in parameters)
        {
            if (param.Key == "_id" || param.Value == null)
                continue;

            if (param.Value is double doubleValue && doubleValue > 0)
            {
                conditions.Add($"t.{GetMappedColumnName(type, param.Key)} >= ?");
                args.Add(doubleValue);
            }
        }

        if (structId > 0)
        {
            conditions.Add("t.struct_id = ?");
            args.Add(structId);
        }

        if (parameters.TryGetValue("_id", out object? currentId))
        {
            conditions.Add("t._id <> ?");
            args.Add(currentId);
        }

        string from = $"FROM {table} t";
        if (capsIds is { Count: > 0 })
        {
            var placeholders = string.Join(",", capsIds.Select(_ => "?"));
            from += $" INNER JOIN {table}_caps tc ON t._id = tc.{table}_id";
            conditions.Insert(0, $"tc.caps_id IN ({placeholders})");
            args.InsertRange(0, capsIds.Cast<object>());
        }

        string where = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;
        string query = $"SELECT DISTINCT t.* {from} {where} ORDER BY t.name COLLATE NOCASE";
        return await ExecuteQueryAsync(table, query, args.ToArray());
    }

    public async Task<PagedResult<object>> GetReplacementPageAsync(
        string tableName,
        Dictionary<string, object> parameters,
        int structId,
        List<int>? capsIds,
        int limit,
        int offset)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        Type type = TransistorMetadata.GetModelType(table);
        limit = Math.Clamp(limit, 1, 500);
        offset = Math.Max(0, offset);

        var conditions = new List<string>();
        var args = new List<object>();

        foreach (var param in parameters)
        {
            if (param.Key == "_id" || param.Value == null)
                continue;

            if (param.Value is double doubleValue && doubleValue > 0)
            {
                conditions.Add($"t.{GetMappedColumnName(type, param.Key)} >= ?");
                args.Add(doubleValue);
            }
        }

        if (structId > 0)
        {
            conditions.Add("t.struct_id = ?");
            args.Add(structId);
        }

        if (parameters.TryGetValue("_id", out object? currentId))
        {
            conditions.Add("t._id <> ?");
            args.Add(currentId);
        }

        string from = $"FROM {table} t";
        if (capsIds is { Count: > 0 })
        {
            var placeholders = string.Join(",", capsIds.Select(_ => "?"));
            from += $" INNER JOIN {table}_caps tc ON t._id = tc.{table}_id";
            conditions.Insert(0, $"tc.caps_id IN ({placeholders})");
            args.InsertRange(0, capsIds.Cast<object>());
        }

        string where = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty;
        int total = await _database.ExecuteScalarAsync<int>(
            $"SELECT COUNT(DISTINCT t._id) {from} {where}",
            args.ToArray()).ConfigureAwait(false);

        var pageArgs = new List<object>(args) { limit, offset };
        string query = $"SELECT DISTINCT t.* {from} {where} ORDER BY t.name COLLATE NOCASE LIMIT ? OFFSET ?";
        var items = await ExecuteQueryAsync(table, query, pageArgs.ToArray()).ConfigureAwait(false);
        return new PagedResult<object>(items, total);
    }

    public async Task<PagedResult<object>> GetFilteredTransistorPageAsync(
        string tableName,
        IReadOnlyDictionary<string, double> minimumFilters,
        IReadOnlyDictionary<string, double> maximumFilters,
        int structId,
        int limit,
        int offset)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        if (structId <= 0)
            return new PagedResult<object>(Array.Empty<object>(), 0);

        limit = Math.Clamp(limit, 1, 500);
        offset = Math.Max(0, offset);

        Type modelType = TransistorMetadata.GetModelType(table);
        var conditions = new List<string> { "struct_id = ?" };
        var args = new List<object> { structId };

        foreach (var filter in minimumFilters)
        {
            conditions.Add($"{GetMappedColumnName(modelType, filter.Key)} >= ?");
            args.Add(filter.Value);
        }

        foreach (var filter in maximumFilters)
        {
            conditions.Add($"{GetMappedColumnName(modelType, filter.Key)} <= ?");
            args.Add(filter.Value);
        }

        string where = string.Join(" AND ", conditions);
        int total = await _database.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM {table} WHERE {where}",
            args.ToArray()).ConfigureAwait(false);

        var pageArgs = new List<object>(args) { limit, offset };
        string query = $"SELECT * FROM {table} WHERE {where} ORDER BY name COLLATE NOCASE LIMIT ? OFFSET ?";
        var items = await ExecuteQueryAsync(table, query, pageArgs.ToArray()).ConfigureAwait(false);
        return new PagedResult<object>(items, total);
    }

    public async Task<List<object>> GetFilteredTransistorsAsync(
        string tableName,
        IReadOnlyDictionary<string, double> minimumFilters,
        IReadOnlyDictionary<string, double> maximumFilters,
        int structId)
    {
        var page = await GetFilteredTransistorPageAsync(
            tableName, minimumFilters, maximumFilters, structId, 500, 0).ConfigureAwait(false);
        return page.Items.ToList();
    }

    // ==================== CRUD GENÉRICO ====================
    public async Task<List<object>> GetAllByTableAsync(string tableName)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        return await ExecuteQueryAsync(table, $"SELECT * FROM {table} ORDER BY name COLLATE NOCASE");
    }

    public async Task<ITransistor?> GetTransistorByTypeAndIdAsync(string type, int id)
    {
        string table = TransistorMetadata.NormalizeTableName(type);
        var result = (await ExecuteQueryAsync(table, $"SELECT * FROM {table} WHERE _id = ? LIMIT 1", id))
            .OfType<ITransistor>()
            .FirstOrDefault();

        if (result != null)
        {
            result.CapsIds = (await GetEncapsuladosByTransistorIdAsync(table, id))
                .Select(x => x.Id)
                .ToList();
        }

        return result;
    }

    public async Task<bool> TransistorNameExistsAsync(
        string name,
        string? excludeTable = null,
        int excludeId = 0)
    {
        string normalized = NormalizeName(name);
        string? excluded = string.IsNullOrWhiteSpace(excludeTable)
            ? null
            : TransistorMetadata.NormalizeTableName(excludeTable);

        foreach (string table in TransistorMetadata.TableNames)
        {
            string query = $"SELECT COUNT(*) FROM {table} WHERE LOWER(TRIM(CAST(name AS TEXT))) = ?";
            var args = new List<object> { normalized };
            if (table == excluded && excludeId > 0)
            {
                query += " AND _id <> ?";
                args.Add(excludeId);
            }

            int count = await _database.ExecuteScalarAsync<int>(query, args.ToArray());
            if (count > 0)
                return true;
        }

        return false;
    }

    public async Task<int> GetNextTransistorIdAsync(string tableName)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        return await _database.ExecuteScalarAsync<int>($"SELECT COALESCE(MAX(_id), 0) + 1 FROM {table}");
    }

    public async Task<int> InsertTransistorAsync(string tableName, ITransistor transistor)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        transistor.Name = transistor.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(transistor.Name))
            throw new InvalidOperationException("El nombre del transistor es obligatorio.");
        if (await TransistorNameExistsAsync(transistor.Name))
            throw new InvalidOperationException($"Ya existe un transistor llamado '{transistor.Name}'.");

        if (transistor.Id <= 0)
            transistor.Id = await GetNextTransistorIdAsync(table);

        await _database.InsertAsync(transistor);
        await SaveCapsRelationsAsync(table, transistor.Id, transistor.CapsIds);
        await UpsertByNameAsync(table, transistor);
        return transistor.Id;
    }

    public async Task<int> UpdateTransistorAsync(string tableName, ITransistor transistor)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        transistor.Name = transistor.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(transistor.Name))
            throw new InvalidOperationException("El nombre del transistor es obligatorio.");

        string? storedName = await _database.ExecuteScalarAsync<string?>(
            $"SELECT CAST(name AS TEXT) FROM {table} WHERE _id = ? LIMIT 1",
            transistor.Id);
        bool nameChanged = !string.Equals(
            storedName?.Trim(),
            transistor.Name,
            StringComparison.OrdinalIgnoreCase);
        if (nameChanged && await TransistorNameExistsAsync(transistor.Name, table, transistor.Id))
            throw new InvalidOperationException($"Ya existe un transistor llamado '{transistor.Name}'.");

        int updated = await _database.UpdateAsync(transistor);
        await SaveCapsRelationsAsync(table, transistor.Id, transistor.CapsIds);
        await UpsertByNameAsync(table, transistor);
        return updated;
    }

    public async Task<int> DeleteTransistorAsync(string tableName, int id)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        await _database.ExecuteAsync($"DELETE FROM {table}_caps WHERE {table}_id = ?", id);
        await DeleteByNameAsync(table, id);
        return await _database.ExecuteAsync($"DELETE FROM {table} WHERE _id = ?", id);
    }

    public async Task<HashSet<string>> GetAllTransistorNamesAsync()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string table in TransistorMetadata.TableNames)
        {
            var values = await _database.QueryAsync<NameValue>(
                $"SELECT TRIM(CAST(name AS TEXT)) AS value FROM {table} WHERE name IS NOT NULL");
            foreach (var item in values)
            {
                if (!string.IsNullOrWhiteSpace(item.Value))
                    names.Add(item.Value.Trim());
            }
        }
        return names;
    }

    // ==================== EXPORTACIÓN COMPLETA ====================
    public IReadOnlyList<string> GetExportTableNames()
    {
        var result = new List<string>();
        foreach (string table in TransistorMetadata.TableNames)
        {
            result.Add(table);
            result.Add($"{table}_caps");
        }
        result.Add("byname");
        result.Add("encapsulados");
        result.Add("estructuras");
        return result;
    }

    public async Task<DatabaseTableData> GetTableDataForExportAsync(string tableName)
    {
        string normalized = tableName.Trim().ToLowerInvariant();

        if (TransistorMetadata.TableNames.Contains(normalized))
        {
            Type modelType = TransistorMetadata.GetModelType(normalized);
            var properties = modelType.GetProperties()
                .Where(p => p.Name != "CapsIds")
                .ToList();
            var columns = properties
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
                .ToList();
            var items = await GetAllByTableAsync(normalized);
            var rows = items
                .Select(item => (IReadOnlyList<object?>)properties.Select(p => p.GetValue(item)).ToList())
                .ToList();
            return new DatabaseTableData(normalized, columns, rows);
        }

        if (normalized.EndsWith("_caps", StringComparison.Ordinal) &&
            TransistorMetadata.RelationTableNames.Contains(normalized))
        {
            string transistorTable = normalized[..^5];
            var pairs = await _database.QueryAsync<RelationPair>(
                $"SELECT {transistorTable}_id AS transistor_id, caps_id FROM {normalized} ORDER BY {transistorTable}_id, caps_id");
            return new DatabaseTableData(
                normalized,
                new[] { $"{transistorTable}_id", "caps_id" },
                pairs.Select(x => (IReadOnlyList<object?>)new object?[] { x.TransistorId, x.CapsId }).ToList());
        }

        if (normalized == "byname")
        {
            var rows = await _database.QueryAsync<ByName>("SELECT * FROM byname ORDER BY _id");
            return new DatabaseTableData(
                normalized,
                new[] { "_id", "name", "type", "idx" },
                rows.Select(x => (IReadOnlyList<object?>)new object?[] { x.Id, x.Name, x.Type, x.Idx }).ToList());
        }

        if (normalized == "estructuras")
        {
            var rows = await GetAllEstructurasAsync();
            return new DatabaseTableData(
                normalized,
                new[] { "id", "nombre" },
                rows.Select(x => (IReadOnlyList<object?>)new object?[] { x.Id, x.Nombre }).ToList());
        }

        if (normalized == "encapsulados")
        {
            var rows = await GetAllEncapsuladosAsync();
            return new DatabaseTableData(
                normalized,
                new[] { "id", "nombre", "ruta" },
                rows.Select(x => (IReadOnlyList<object?>)new object?[] { x.Id, x.Nombre, x.Imagen }).ToList());
        }

        throw new ArgumentException($"Tabla no exportable: {tableName}", nameof(tableName));
    }

    // ==================== AUXILIARES ====================
    private static string GetMappedColumnName(Type modelType, string propertyName)
    {
        var property = modelType.GetProperty(propertyName)
            ?? throw new ArgumentException($"Propiedad no válida: {propertyName}");
        return property.GetCustomAttribute<ColumnAttribute>()?.Name
            ?? property.Name.ToLowerInvariant();
    }

    private async Task<List<object>> ExecuteQueryAsync(string tableName, string query, params object[] args)
    {
        try
        {
            Type type = TransistorMetadata.GetModelType(tableName);
            MethodInfo method = typeof(SQLiteAsyncConnection)
                .GetMethods()
                .First(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return m.Name == nameof(SQLiteAsyncConnection.QueryAsync) &&
                           m.IsGenericMethodDefinition &&
                           parameters.Length == 2 &&
                           parameters[0].ParameterType == typeof(string) &&
                           parameters[1].ParameterType == typeof(object[]);
                });
            MethodInfo genericMethod = method.MakeGenericMethod(type);
            var task = (Task)genericMethod.Invoke(_database, new object[] { query, args })!;
            await task.ConfigureAwait(false);
            object? result = task.GetType().GetProperty("Result")?.GetValue(task);

            if (result is IEnumerable enumerable)
                return enumerable.Cast<object>().ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ExecuteQueryAsync: {ex}");
        }
        return new List<object>();
    }

    private static string NormalizeName(string name)
        => (name ?? string.Empty).Trim().ToLowerInvariant();

    private sealed class PragmaColumn
    {
        [Column("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NameValue
    {
        [Column("value")]
        public string Value { get; set; } = string.Empty;
    }

    private sealed class RelationPair
    {
        [Column("transistor_id")]
        public int TransistorId { get; set; }

        [Column("caps_id")]
        public int CapsId { get; set; }
    }
}
