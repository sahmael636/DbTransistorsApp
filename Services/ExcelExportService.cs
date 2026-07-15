// Services/ExcelExportService.cs
using OfficeOpenXml;
using System.Reflection;

namespace DbTransistorsApp.Services
{
    public class ExcelExportService
    {
        public async Task<string> ExportReplacementsToExcelAsync(
            string transistorName,
            string transistorType,
            List<object> replacements,
            Dictionary<string, string> columnHeaders)
        {
            string fileName = $"Reemplazos_{transistorName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Reemplazos");

                // Título
                worksheet.Cells[1, 1].Value = $"Reemplazos para {transistorName}";
                worksheet.Cells[1, 1, 1, columnHeaders.Count].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // Subtítulo
                worksheet.Cells[2, 1].Value = $"Tipo: {transistorType}";
                worksheet.Cells[2, 1, 2, columnHeaders.Count].Merge = true;
                worksheet.Cells[2, 1].Style.Font.Size = 12;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                // Fecha
                worksheet.Cells[3, columnHeaders.Count].Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                worksheet.Cells[3, columnHeaders.Count].Style.Font.Size = 10;
                worksheet.Cells[3, columnHeaders.Count].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                // Encabezados
                int col = 1;
                foreach (var header in columnHeaders.Values)
                {
                    worksheet.Cells[5, col].Value = header;
                    worksheet.Cells[5, col].Style.Font.Bold = true;
                    worksheet.Cells[5, col].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[5, col].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    worksheet.Cells[5, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    col++;
                }

                // Datos
                int row = 6;
                foreach (var item in replacements)
                {
                    col = 1;
                    foreach (var header in columnHeaders.Keys)
                    {
                        var value = item.GetType().GetProperty(header)?.GetValue(item);
                        worksheet.Cells[row, col].Value = value?.ToString() ?? "";
                        worksheet.Cells[row, col].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        col++;
                    }
                    row++;
                }

                // Autoajustar columnas
                worksheet.Cells.AutoFitColumns();

                await package.SaveAsAsync(filePath);
                return filePath;
            }
            catch
            {
                throw;
            }
        }
    }
}