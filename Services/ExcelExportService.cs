using DbTransistorsApp.Models.Base;
using DbTransistorsApp.ViewModels;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using NPOI.XSSF.Streaming;
using VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;
using IFont = NPOI.SS.UserModel.IFont;

namespace DbTransistorsApp.Services;

public class ExcelExportService
{
    public Task<MemoryStream> CreateImportTemplateAsync(
        string tableName,
        IReadOnlyList<Estructura> estructuras,
        IReadOnlyCollection<int> allowedStructureIds,
        IReadOnlyList<Encapsulado> encapsulados)
    {
        string table = TransistorMetadata.NormalizeTableName(tableName);
        IWorkbook workbook = new XSSFWorkbook();
        var styles = CreateStyles(workbook);

        ISheet transistorSheet = workbook.CreateSheet("Transistores");
        var columns = TransistorMetadata.GetImportColumns(table).ToList();
        var headers = columns.Select(c => c.ColumnName).Concat(new[] { "caps_ids" }).ToList();
        WriteHeaderRow(transistorSheet, 0, headers, styles.Header);
        transistorSheet.CreateFreezePane(0, 1);

        for (int i = 0; i < headers.Count; i++)
        {
            int width = headers[i] == "name" ? 24 : headers[i] == "caps_ids" ? 22 : 14;
            transistorSheet.SetColumnWidth(i, width * 256);
        }

        ISheet structureSheet = workbook.CreateSheet("Estructuras");
        WriteHeaderRow(structureSheet, 0, new[] { "id", "nombre", "permitida_en_esta_tabla" }, styles.Header);
        int structureRow = 1;
        foreach (var estructura in estructuras.OrderBy(x => x.Id))
        {
            IRow row = structureSheet.CreateRow(structureRow++);
            row.CreateCell(0).SetCellValue(estructura.Id);
            row.CreateCell(1).SetCellValue(estructura.Nombre);
            row.CreateCell(2).SetCellValue(allowedStructureIds.Contains(estructura.Id) ? "Sí" : "No");
        }
        structureSheet.SetColumnWidth(0, 10 * 256);
        structureSheet.SetColumnWidth(1, 22 * 256);
        structureSheet.SetColumnWidth(2, 26 * 256);
        structureSheet.CreateFreezePane(0, 1);

        ISheet capsSheet = workbook.CreateSheet("Encapsulados");
        WriteHeaderRow(capsSheet, 0, new[] { "id", "nombre" }, styles.Header);
        int capRow = 1;
        foreach (var encapsulado in encapsulados.OrderBy(x => x.Id))
        {
            IRow row = capsSheet.CreateRow(capRow++);
            row.CreateCell(0).SetCellValue(encapsulado.Id);
            row.CreateCell(1).SetCellValue(encapsulado.Nombre);
        }
        capsSheet.SetColumnWidth(0, 10 * 256);
        capsSheet.SetColumnWidth(1, 28 * 256);
        capsSheet.CreateFreezePane(0, 1);

        ISheet instructions = workbook.CreateSheet("Instrucciones");
        string[] lines =
        {
            $"Plantilla de importación para: {TransistorMetadata.GetDisplayName(table)} ({table}).",
            "Complete la hoja Transistores sin cambiar los encabezados.",
            "name y struct_id son obligatorios. Los demás campos pueden quedar vacíos.",
            "No incluya _id: la aplicación asigna el siguiente identificador disponible.",
            "caps_ids es opcional. Para varios encapsulados use comas, por ejemplo: 2,5,8.",
            "Los nombres se validan globalmente, sin distinguir mayúsculas y minúsculas.",
            "Consulte las hojas Estructuras y Encapsulados para conocer los identificadores válidos.",
            "Las estructuras marcadas con No no corresponden normalmente a esta familia de transistores."
        };
        for (int i = 0; i < lines.Length; i++)
        {
            IRow row = instructions.CreateRow(i);
            ICell cell = row.CreateCell(0);
            cell.SetCellValue(lines[i]);
            cell.CellStyle = i == 0 ? styles.Title : styles.Wrap;
        }
        instructions.SetColumnWidth(0, 105 * 256);

        return Task.FromResult(ToStream(workbook));
    }

    public Task<MemoryStream> CreateReplacementsWorkbookAsync(
        string transistorName,
        string transistorType,
        string transistorStructure,
        IReadOnlyDictionary<string, string> originalParameters,
        IReadOnlyList<string> headers,
        IReadOnlyList<ReplacementRow> rows)
    {
        IWorkbook workbook = new XSSFWorkbook();
        var styles = CreateStyles(workbook);
        ISheet sheet = workbook.CreateSheet("Reemplazos");

        int columnCount = Math.Max(2, headers.Count + 1);
        IRow titleRow = sheet.CreateRow(0);
        ICell title = titleRow.CreateCell(0);
        title.SetCellValue($"Reemplazos para {transistorName}");
        title.CellStyle = styles.Title;
        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, columnCount - 1));

        IRow info = sheet.CreateRow(1);
        info.CreateCell(0).SetCellValue($"Tipo: {transistorType}");
        info.CreateCell(1).SetCellValue($"Estructura: {transistorStructure}");
        info.CreateCell(Math.Min(columnCount - 1, 2)).SetCellValue($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

        int rowIndex = 3;
        IRow paramsTitle = sheet.CreateRow(rowIndex++);
        paramsTitle.CreateCell(0).SetCellValue("Parámetros usados para la búsqueda");
        paramsTitle.GetCell(0).CellStyle = styles.Section;
        sheet.AddMergedRegion(new CellRangeAddress(rowIndex - 1, rowIndex - 1, 0, columnCount - 1));

        foreach (var parameter in originalParameters)
        {
            IRow row = sheet.CreateRow(rowIndex++);
            row.CreateCell(0).SetCellValue(TransistorMetadata.GetDisplayNameForProperty(parameter.Key));
            row.CreateCell(1).SetCellValue(parameter.Value ?? string.Empty);
        }

        rowIndex++;
        var tableHeaders = new List<string> { "Nombre" };
        tableHeaders.AddRange(headers);
        WriteHeaderRow(sheet, rowIndex++, tableHeaders, styles.Header);

        foreach (var replacement in rows)
        {
            IRow row = sheet.CreateRow(rowIndex++);
            row.CreateCell(0).SetCellValue(replacement.Name);
            for (int i = 0; i < headers.Count; i++)
            {
                row.CreateCell(i + 1).SetCellValue(i < replacement.Values.Count
                    ? replacement.Values[i]
                    : string.Empty);
            }
        }

        sheet.CreateFreezePane(0, rowIndex - rows.Count);
        sheet.SetColumnWidth(0, 24 * 256);
        for (int i = 1; i < columnCount; i++)
            sheet.SetColumnWidth(i, 14 * 256);

        return Task.FromResult(ToStream(workbook));
    }

    public async Task<MemoryStream> CreateDatabaseWorkbookAsync(DatabaseService databaseService)
    {
        // La base contiene más de cien mil registros. El libro en streaming evita
        // conservar todas las filas en memoria, algo especialmente importante en Android.
        IWorkbook workbook = new SXSSFWorkbook(100)
        {
            CompressTempFiles = true
        };
        var styles = CreateStyles(workbook);

        foreach (string tableName in databaseService.GetExportTableNames())
        {
            DatabaseTableData data = await databaseService.GetTableDataForExportAsync(tableName);
            ISheet sheet = workbook.CreateSheet(SanitizeSheetName(data.Name));
            WriteHeaderRow(sheet, 0, data.Columns, styles.Header);

            int rowIndex = 1;
            foreach (var values in data.Rows)
            {
                IRow row = sheet.CreateRow(rowIndex++);
                for (int col = 0; col < data.Columns.Count; col++)
                {
                    object? value = col < values.Count ? values[col] : null;
                    SetCellValue(row.CreateCell(col), value);
                }
            }

            sheet.CreateFreezePane(0, 1);
            for (int col = 0; col < data.Columns.Count; col++)
            {
                int width = data.Columns[col] is "name" or "nombre" or "ruta" ? 28 : 14;
                sheet.SetColumnWidth(col, width * 256);
            }
        }

        return ToStream(workbook);
    }

    public Task<MemoryStream> CreateImportReportAsync(string tableName, ImportResult result)
    {
        IWorkbook workbook = new XSSFWorkbook();
        var styles = CreateStyles(workbook);

        ISheet summary = workbook.CreateSheet("Resumen");
        WriteHeaderRow(summary, 0, new[] { "Concepto", "Cantidad" }, styles.Header);
        string[,] values =
        {
            { "Tabla", TransistorMetadata.NormalizeTableName(tableName) },
            { "Filas procesadas", result.ProcessedRows.ToString() },
            { "Importadas", result.ImportedRows.ToString() },
            { "Duplicadas", result.DuplicateRows.ToString() },
            { "Con errores", result.ErrorRows.ToString() }
        };
        for (int i = 0; i < values.GetLength(0); i++)
        {
            IRow row = summary.CreateRow(i + 1);
            row.CreateCell(0).SetCellValue(values[i, 0]);
            row.CreateCell(1).SetCellValue(values[i, 1]);
        }
        summary.SetColumnWidth(0, 24 * 256);
        summary.SetColumnWidth(1, 30 * 256);

        ISheet issues = workbook.CreateSheet("Incidencias");
        WriteHeaderRow(issues, 0, new[] { "fila", "nombre", "tipo", "motivo" }, styles.Header);
        int rowIndex = 1;
        foreach (var issue in result.Issues)
        {
            IRow row = issues.CreateRow(rowIndex++);
            row.CreateCell(0).SetCellValue(issue.RowNumber);
            row.CreateCell(1).SetCellValue(issue.Name);
            row.CreateCell(2).SetCellValue(issue.Kind.ToString());
            row.CreateCell(3).SetCellValue(issue.Message);
        }
        issues.SetColumnWidth(0, 10 * 256);
        issues.SetColumnWidth(1, 24 * 256);
        issues.SetColumnWidth(2, 16 * 256);
        issues.SetColumnWidth(3, 70 * 256);
        issues.CreateFreezePane(0, 1);

        return Task.FromResult(ToStream(workbook));
    }

    private static void WriteHeaderRow(ISheet sheet, int rowIndex, IEnumerable<string> headers, ICellStyle style)
    {
        IRow row = sheet.CreateRow(rowIndex);
        int col = 0;
        foreach (string header in headers)
        {
            ICell cell = row.CreateCell(col++);
            cell.SetCellValue(header);
            cell.CellStyle = style;
        }
    }

    private static void SetCellValue(ICell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.SetCellValue(string.Empty);
                break;
            case byte or short or int or long or float or double or decimal:
                cell.SetCellValue(Convert.ToDouble(value));
                break;
            case bool boolean:
                cell.SetCellValue(boolean);
                break;
            case DateTime date:
                cell.SetCellValue(date);
                break;
            default:
                cell.SetCellValue(value.ToString() ?? string.Empty);
                break;
        }
    }

    private static MemoryStream ToStream(IWorkbook workbook)
    {
        var stream = new MemoryStream();
        try
        {
            workbook.Write(stream, true);
            stream.Position = 0;
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
        finally
        {
            // Close finaliza los escritores de hojas; después Dispose elimina
            // los archivos temporales creados por SXSSFWorkbook.
            workbook.Close();
            if (workbook is SXSSFWorkbook streamingWorkbook)
                streamingWorkbook.Dispose();
        }
    }

    private static string SanitizeSheetName(string name)
    {
        string cleaned = WorkbookUtil.CreateSafeSheetName(name);
        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }

    private static WorkbookStyles CreateStyles(IWorkbook workbook)
    {
        ICellStyle header = workbook.CreateCellStyle();
        header.FillForegroundColor = IndexedColors.RoyalBlue.Index;
        header.FillPattern = FillPattern.SolidForeground;
        header.Alignment = HorizontalAlignment.Center;
        header.VerticalAlignment = VerticalAlignment.Center;
        header.BorderBottom = BorderStyle.Thin;
        header.BorderTop = BorderStyle.Thin;
        header.BorderLeft = BorderStyle.Thin;
        header.BorderRight = BorderStyle.Thin;
        IFont headerFont = workbook.CreateFont();
        headerFont.IsBold = true;
        headerFont.Color = IndexedColors.White.Index;
        header.SetFont(headerFont);

        ICellStyle title = workbook.CreateCellStyle();
        title.Alignment = HorizontalAlignment.Center;
        IFont titleFont = workbook.CreateFont();
        titleFont.IsBold = true;
        titleFont.FontHeightInPoints = 16;
        title.SetFont(titleFont);

        ICellStyle section = workbook.CreateCellStyle();
        section.FillForegroundColor = IndexedColors.LightCornflowerBlue.Index;
        section.FillPattern = FillPattern.SolidForeground;
        IFont sectionFont = workbook.CreateFont();
        sectionFont.IsBold = true;
        section.SetFont(sectionFont);

        ICellStyle wrap = workbook.CreateCellStyle();
        wrap.WrapText = true;

        return new WorkbookStyles(header, title, section, wrap);
    }

    private sealed record WorkbookStyles(
        ICellStyle Header,
        ICellStyle Title,
        ICellStyle Section,
        ICellStyle Wrap);
}
