// Services/PdfExportService.cs
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Reflection;

//using static Android.Icu.Util.LocaleData;

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
            string fileName = $"Reemplazos_{transistorName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            try
            {
                using var stream = File.Create(filePath);
                var document = new Document(PageSize.A4);
                PdfWriter.GetInstance(document, stream);
                document.Open();

                // Título
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                var title = new Paragraph($"Reemplazos para {transistorName}", titleFont);
                title.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                document.Add(title);

                // Subtítulo
                var subtitleFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var subtitle = new Paragraph($"Tipo: {transistorType}", subtitleFont);
                subtitle.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                document.Add(subtitle);

                // Fecha
                var dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                var date = new Paragraph($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}", dateFont);
                date.Alignment = iTextSharp.text.Element.ALIGN_RIGHT;
                document.Add(date);

                document.Add(new Paragraph(" "));

                // Tabla
                var table = new PdfPTable(columnHeaders.Count);
                table.WidthPercentage = 100;

                // Encabezados
                var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                foreach (var header in columnHeaders.Values)
                {
                    var cell = new PdfPCell(new Phrase(header, headerFont));
                    cell.BackgroundColor = new BaseColor(200, 200, 200);
                    cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                    table.AddCell(cell);
                }

                // Datos
                var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);
                foreach (var item in replacements)
                {
                    foreach (var header in columnHeaders.Keys)
                    {
                        var value = item.GetType().GetProperty(header)?.GetValue(item);
                        var cell = new PdfPCell(new Phrase(value?.ToString() ?? "", dataFont));
                        cell.HorizontalAlignment = iTextSharp.text.Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }
                }

                document.Add(table);

                // Nota al pie
                var noteFont = FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8);
                var note = new Paragraph("Nota: Los valores mostrados son los parámetros técnicos de los transistores.", noteFont);
                note.Alignment = iTextSharp.text.Element.ALIGN_CENTER;
                document.Add(note);

                document.Close();
                return filePath;
            }
            catch
            {
                throw;
            }
        }
    }
}