using DbTransistorsApp.Models.Base;
using NPOI.SS.UserModel;
using System.Globalization;
using System.Reflection;

namespace DbTransistorsApp.Services;

public class ExcelImportService
{
    private readonly DatabaseService _databaseService;

    public ExcelImportService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task<ImportResult> ImportTransistorsAsync(
        Stream input,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        if (input.CanSeek)
            input.Position = 0;

        IWorkbook workbook = WorkbookFactory.Create(input);
        try
        {
            ISheet sheet = FindTransistorSheet(workbook)
                ?? throw new InvalidDataException("El archivo no contiene una hoja llamada 'Transistores'.");

            IRow? headerRow = sheet.GetRow(sheet.FirstRowNum);
            if (headerRow == null)
                throw new InvalidDataException("La hoja Transistores no contiene encabezados.");

            var headers = ReadHeaders(headerRow);
            if (!headers.ContainsKey("name") || !headers.ContainsKey("struct_id"))
                throw new InvalidDataException("La plantilla debe contener las columnas name y struct_id.");

            var metadataColumns = TransistorMetadata.GetImportColumns(table);

            HashSet<string> existingNames = await _databaseService.GetAllTransistorNamesAsync();
            HashSet<int> allowedStructures = await _databaseService.GetAllowedStructureIdsForTableAsync(table);
            HashSet<int> allStructures = (await _databaseService.GetAllEstructurasAsync()).Select(x => x.Id).ToHashSet();
            HashSet<int> validCaps = (await _databaseService.GetAllEncapsuladosAsync()).Select(x => x.Id).ToHashSet();

            var result = new ImportResult();
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);

            for (int rowIndex = headerRow.RowNum + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IRow? row = sheet.GetRow(rowIndex);
                if (row == null || IsEmptyRow(row, headers.Values, formatter))
                    continue;

                result.ProcessedRows++;
                int excelRow = rowIndex + 1;
                string name = GetCellText(row, headers["name"], formatter).Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    AddValidationIssue(result, excelRow, string.Empty, "El nombre es obligatorio.");
                    continue;
                }

                if (existingNames.Contains(name))
                {
                    result.DuplicateRows++;
                    result.Issues.Add(new ImportIssue(
                        excelRow,
                        name,
                        ImportIssueKind.Duplicate,
                        "El nombre ya existe en la base de datos o se repite dentro del archivo."));
                    continue;
                }

                string structText = GetCellText(row, headers["struct_id"], formatter).Trim();
                if (!TryParseInt(structText, out int structId) || !allStructures.Contains(structId))
                {
                    AddValidationIssue(result, excelRow, name, "struct_id no corresponde a una estructura existente.");
                    continue;
                }

                if (!allowedStructures.Contains(structId))
                {
                    AddValidationIssue(result, excelRow, name, "La estructura indicada no está permitida para esta tabla.");
                    continue;
                }

                ITransistor transistor = (ITransistor)Activator.CreateInstance(TransistorMetadata.GetModelType(table))!;
                transistor.Name = name;
                transistor.StructId = structId;

                bool rowIsValid = true;
                foreach (var metadata in metadataColumns)
                {
                    if (metadata.ColumnName.Equals("name", StringComparison.OrdinalIgnoreCase) ||
                        metadata.ColumnName.Equals("struct_id", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!headers.TryGetValue(metadata.ColumnName, out int columnIndex))
                        continue;

                    string text = GetCellText(row, columnIndex, formatter).Trim();
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    PropertyInfo property = TransistorMetadata.GetModelType(table).GetProperty(metadata.PropertyName)!;
                    Type underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (underlying == typeof(string))
                    {
                        property.SetValue(transistor, text);
                    }
                    else if (underlying == typeof(double))
                    {
                        if (!TryParseDouble(text, out double value))
                        {
                            AddValidationIssue(result, excelRow, name, $"El valor '{text}' de {metadata.ColumnName} no es numérico.");
                            rowIsValid = false;
                            break;
                        }
                        property.SetValue(transistor, value);
                    }
                    else if (underlying == typeof(int))
                    {
                        if (!TryParseInt(text, out int value))
                        {
                            AddValidationIssue(result, excelRow, name, $"El valor '{text}' de {metadata.ColumnName} no es entero.");
                            rowIsValid = false;
                            break;
                        }
                        property.SetValue(transistor, value);
                    }
                }

                if (!rowIsValid)
                    continue;

                if (headers.TryGetValue("caps_ids", out int capsColumn))
                {
                    string capsText = GetCellText(row, capsColumn, formatter).Trim();
                    if (!string.IsNullOrWhiteSpace(capsText))
                    {
                        var parsedCaps = ParseCapsIds(capsText);
                        if (parsedCaps == null || parsedCaps.Any(id => !validCaps.Contains(id)))
                        {
                            AddValidationIssue(result, excelRow, name, "caps_ids contiene identificadores inexistentes o un formato no válido.");
                            continue;
                        }
                        transistor.CapsIds = parsedCaps;
                    }
                }

                try
                {
                    await _databaseService.InsertTransistorAsync(table, transistor);
                    existingNames.Add(name);
                    result.ImportedRows++;
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("existe", StringComparison.OrdinalIgnoreCase))
                {
                    result.DuplicateRows++;
                    result.Issues.Add(new ImportIssue(excelRow, name, ImportIssueKind.Duplicate, ex.Message));
                }
                catch (Exception ex)
                {
                    result.ErrorRows++;
                    result.Issues.Add(new ImportIssue(excelRow, name, ImportIssueKind.Database, ex.Message));
                }
            }

            return result;
        }
        finally
        {
            workbook.Close();
        }
    }

    private static ISheet? FindTransistorSheet(IWorkbook workbook)
    {
        for (int i = 0; i < workbook.NumberOfSheets; i++)
        {
            ISheet sheet = workbook.GetSheetAt(i);
            if (sheet.SheetName.Equals("Transistores", StringComparison.OrdinalIgnoreCase))
                return sheet;
        }
        return workbook.NumberOfSheets > 0 ? workbook.GetSheetAt(0) : null;
    }

    private static Dictionary<string, int> ReadHeaders(IRow row)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int col = row.FirstCellNum; col < row.LastCellNum; col++)
        {
            string value = row.GetCell(col)?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value) && !headers.ContainsKey(value))
                headers[value] = col;
        }
        return headers;
    }

    private static bool IsEmptyRow(IRow row, IEnumerable<int> columns, DataFormatter formatter)
        => columns.All(col => string.IsNullOrWhiteSpace(GetCellText(row, col, formatter)));

    private static string GetCellText(IRow row, int column, DataFormatter formatter)
    {
        ICell? cell = row.GetCell(column, MissingCellPolicy.RETURN_BLANK_AS_NULL);
        if (cell == null)
            return string.Empty;
        return formatter.FormatCellValue(cell) ?? string.Empty;
    }

    private static bool TryParseDouble(string text, out double value)
        => double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
           double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static bool TryParseInt(string text, out int value)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) ||
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        if (TryParseDouble(text, out double number) && Math.Abs(number % 1) < 0.000001)
        {
            value = Convert.ToInt32(number);
            return true;
        }
        return false;
    }

    private static List<int>? ParseCapsIds(string text)
    {
        var ids = new List<int>();
        foreach (string part in text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParseInt(part.Trim(), out int id) || id <= 0)
                return null;
            ids.Add(id);
        }
        return ids.Distinct().ToList();
    }

    private static void AddValidationIssue(ImportResult result, int row, string name, string message)
    {
        result.ErrorRows++;
        result.Issues.Add(new ImportIssue(row, name, ImportIssueKind.Validation, message));
    }
}
