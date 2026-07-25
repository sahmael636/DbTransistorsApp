using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Colors = QuestPDF.Helpers.Colors;

namespace DbTransistorsApp.Services
{
    public class PdfExportService
    {
        public async Task<string> ExportReplacementsToPdfAsync(
            string transistorName,
            string transistorType,
            List<object> replacements,
            Dictionary<string, string> columnHeaders)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string fileName = $"Reemplazos_{transistorName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);            

            await Task.Run(() =>
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(20);

                        page.Header()
                            .Column(col =>
                            {
                                col.Item()
                                    .Text($"Reemplazos para {transistorName}")
                                    .FontSize(18)
                                    .Bold()
                                    .AlignCenter();

                                col.Item()
                                    .Text($"Tipo: {transistorType}")
                                    .FontSize(12)
                                    .AlignCenter();

                                col.Item()
                                    .Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                                    .FontSize(10)
                                    .AlignRight();

                                col.Item().PaddingBottom(10);
                            });

                        page.Content()
                            .Table(table =>
                            {
                                // Una columna por cada encabezado
                                table.ColumnsDefinition(columns =>
                                {
                                    foreach (var _ in columnHeaders)
                                        columns.RelativeColumn();
                                });

                                // Encabezados
                                table.Header(header =>
                                {
                                    foreach (var title in columnHeaders.Values)
                                    {
                                        header.Cell()
                                            .Background(Colors.Grey.Lighten2)
                                            .Border(1)
                                            .Padding(5)
                                            .Text(title)
                                            .Bold()
                                            .AlignCenter();
                                    }
                                });

                                // Datos
                                foreach (var item in replacements)
                                {
                                    foreach (var propertyName in columnHeaders.Keys)
                                    {
                                        var value = item.GetType()
                                            .GetProperty(propertyName)?
                                            .GetValue(item)?
                                            .ToString() ?? "";

                                        table.Cell()
                                            .Border(1)
                                            .Padding(4)
                                            .Text(value)
                                            .FontSize(9)
                                            .AlignCenter();
                                    }
                                }
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text("Nota: Los valores mostrados son los parámetros técnicos de los transistores.")
                            .FontSize(8);
                    });
                })
                .GeneratePdf(filePath);
            });

            return filePath;
        }
    }
}