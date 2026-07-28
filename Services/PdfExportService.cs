using SkiaSharp;

namespace DbTransistorsApp.Services
{
    /// <summary>
    /// Genera reportes PDF mediante SkiaSharp. SkiaSharp funciona en Android,
    /// Windows y las demás plataformas objetivo de .NET MAUI, sin depender de
    /// APIs gráficas exclusivas de Windows.
    /// </summary>
    public sealed class PdfExportService
    {
        private const float PageWidth = 842f;   // A4 horizontal, en puntos
        private const float PageHeight = 595f;
        private const float Margin = 24f;
        private const float FooterHeight = 24f;
        private const float ContentBottom = PageHeight - Margin - FooterHeight;

        private static readonly SKColor HeaderBackground = new(217, 234, 247);
        private static readonly SKColor AlternateRowBackground = new(247, 249, 251);
        private static readonly SKColor BorderColor = new(90, 90, 90);
        private static readonly SKColor SecondaryTextColor = new(75, 75, 75);

        public Task<string> ExportReplacementsToPdfAsync(
            string filePath,
            string transistorName,
            string transistorType,
            string transistorStructure,
            IReadOnlyDictionary<string, string> originalParameters,
            IReadOnlyList<string> replacementHeaders,
            IReadOnlyList<PdfReplacementRow> replacements)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            ArgumentNullException.ThrowIfNull(originalParameters);
            ArgumentNullException.ThrowIfNull(replacementHeaders);
            ArgumentNullException.ThrowIfNull(replacements);

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            return Task.Run(() =>
            {
                GeneratePdf(
                    filePath,
                    transistorName,
                    transistorType,
                    transistorStructure,
                    originalParameters,
                    replacementHeaders,
                    replacements);

                return filePath;
            });
        }

        private static void GeneratePdf(
            string filePath,
            string transistorName,
            string transistorType,
            string transistorStructure,
            IReadOnlyDictionary<string, string> originalParameters,
            IReadOnlyList<string> replacementHeaders,
            IReadOnlyList<PdfReplacementRow> replacements)
        {
            using var output = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            using SKDocument document = SKDocument.CreatePdf(output)
                ?? throw new InvalidOperationException("SkiaSharp no pudo inicializar el documento PDF.");

            using SKTypeface regularTypeface =
                SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Normal)
                ?? throw new InvalidOperationException("No se pudo cargar una tipografía para el PDF.");

            using SKTypeface boldTypeface =
                SKTypeface.FromFamilyName("sans-serif", SKFontStyle.Bold)
                ?? throw new InvalidOperationException("No se pudo cargar la tipografía en negrita para el PDF.");

            using var titleFont = new SKFont(boldTypeface, 18f);
            using var subtitleFont = new SKFont(regularTypeface, 9f);
            using var sectionFont = new SKFont(boldTypeface, 11f);
            using var bodyFont = new SKFont(regularTypeface, 8.5f);
            using var bodyBoldFont = new SKFont(boldTypeface, 8.5f);
            using var tableFont = new SKFont(regularTypeface, 7.5f);
            using var tableBoldFont = new SKFont(boldTypeface, 7.5f);
            using var footerFont = new SKFont(regularTypeface, 7f);

            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var secondaryTextPaint = new SKPaint
            {
                Color = SecondaryTextColor,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            };

            using var borderPaint = new SKPaint
            {
                Color = BorderColor,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.7f
            };

            using var headerFillPaint = new SKPaint
            {
                Color = HeaderBackground,
                Style = SKPaintStyle.Fill
            };

            using var alternateFillPaint = new SKPaint
            {
                Color = AlternateRowBackground,
                Style = SKPaintStyle.Fill
            };

            int pageNumber = 0;
            PageState page = StartPage(document, ++pageNumber);

            DrawReportHeader(
                page,
                transistorName,
                transistorType,
                transistorStructure,
                titleFont,
                subtitleFont,
                textPaint,
                secondaryTextPaint);

            DrawOriginalParameters(
                document,
                ref page,
                ref pageNumber,
                transistorName,
                originalParameters,
                sectionFont,
                bodyFont,
                bodyBoldFont,
                footerFont,
                textPaint,
                secondaryTextPaint,
                borderPaint,
                headerFillPaint);

            DrawReplacementTable(
                document,
                ref page,
                ref pageNumber,
                transistorName,
                replacementHeaders,
                replacements,
                sectionFont,
                tableFont,
                tableBoldFont,
                footerFont,
                textPaint,
                secondaryTextPaint,
                borderPaint,
                headerFillPaint,
                alternateFillPaint);

            FinishPage(document, page, footerFont, secondaryTextPaint);
            document.Close();
        }

        private static void DrawReportHeader(
            PageState page,
            string transistorName,
            string transistorType,
            string transistorStructure,
            SKFont titleFont,
            SKFont subtitleFont,
            SKPaint textPaint,
            SKPaint secondaryTextPaint)
        {
            string safeName = string.IsNullOrWhiteSpace(transistorName)
                ? "Transistor"
                : transistorName;

            DrawText(
                page.Canvas,
                $"Reemplazos para {safeName}",
                PageWidth / 2f,
                page.Y + 18f,
                SKTextAlign.Center,
                titleFont,
                textPaint);

            page.Y += 32f;

            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(transistorType))
                details.Add($"Tipo: {transistorType}");
            if (!string.IsNullOrWhiteSpace(transistorStructure))
                details.Add($"Estructura: {transistorStructure}");

            if (details.Count > 0)
            {
                DrawText(
                    page.Canvas,
                    string.Join("   |   ", details),
                    PageWidth / 2f,
                    page.Y,
                    SKTextAlign.Center,
                    subtitleFont,
                    secondaryTextPaint);
                page.Y += 16f;
            }

            DrawText(
                page.Canvas,
                $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                PageWidth - Margin,
                page.Y,
                SKTextAlign.Right,
                subtitleFont,
                secondaryTextPaint);

            page.Y += 20f;
        }

        private static void DrawOriginalParameters(
            SKDocument document,
            ref PageState page,
            ref int pageNumber,
            string transistorName,
            IReadOnlyDictionary<string, string> originalParameters,
            SKFont sectionFont,
            SKFont bodyFont,
            SKFont bodyBoldFont,
            SKFont footerFont,
            SKPaint textPaint,
            SKPaint secondaryTextPaint,
            SKPaint borderPaint,
            SKPaint headerFillPaint)
        {
            const float sectionSpacing = 18f;
            const float rowHeight = 20f;
            const float keyWidth = 220f;
            float tableWidth = PageWidth - (Margin * 2f);

            EnsureSpace(
                document,
                ref page,
                ref pageNumber,
                rowHeight * 2f + sectionSpacing,
                transistorName,
                footerFont,
                secondaryTextPaint,
                sectionFont,
                textPaint);

            DrawText(
                page.Canvas,
                "Parámetros del transistor original",
                Margin,
                page.Y + 11f,
                SKTextAlign.Left,
                sectionFont,
                textPaint);
            page.Y += sectionSpacing;

            DrawCell(
                page.Canvas,
                new SKRect(Margin, page.Y, Margin + keyWidth, page.Y + rowHeight),
                "Parámetro",
                bodyBoldFont,
                textPaint,
                borderPaint,
                headerFillPaint,
                SKTextAlign.Left);

            DrawCell(
                page.Canvas,
                new SKRect(Margin + keyWidth, page.Y, Margin + tableWidth, page.Y + rowHeight),
                "Valor",
                bodyBoldFont,
                textPaint,
                borderPaint,
                headerFillPaint,
                SKTextAlign.Left);

            page.Y += rowHeight;

            foreach (KeyValuePair<string, string> parameter in originalParameters)
            {
                if (page.Y + rowHeight > ContentBottom)
                {
                    FinishPage(document, page, footerFont, secondaryTextPaint);
                    page = StartPage(document, ++pageNumber);
                    DrawContinuationTitle(
                        page,
                        transistorName,
                        "Parámetros del transistor original (continuación)",
                        sectionFont,
                        textPaint);

                    DrawCell(
                        page.Canvas,
                        new SKRect(Margin, page.Y, Margin + keyWidth, page.Y + rowHeight),
                        "Parámetro",
                        bodyBoldFont,
                        textPaint,
                        borderPaint,
                        headerFillPaint,
                        SKTextAlign.Left);

                    DrawCell(
                        page.Canvas,
                        new SKRect(Margin + keyWidth, page.Y, Margin + tableWidth, page.Y + rowHeight),
                        "Valor",
                        bodyBoldFont,
                        textPaint,
                        borderPaint,
                        headerFillPaint,
                        SKTextAlign.Left);

                    page.Y += rowHeight;
                }

                DrawCell(
                    page.Canvas,
                    new SKRect(Margin, page.Y, Margin + keyWidth, page.Y + rowHeight),
                    parameter.Key,
                    bodyBoldFont,
                    textPaint,
                    borderPaint,
                    null,
                    SKTextAlign.Left);

                DrawCell(
                    page.Canvas,
                    new SKRect(Margin + keyWidth, page.Y, Margin + tableWidth, page.Y + rowHeight),
                    parameter.Value,
                    bodyFont,
                    textPaint,
                    borderPaint,
                    null,
                    SKTextAlign.Left);

                page.Y += rowHeight;
            }

            page.Y += 14f;
        }

        private static void DrawReplacementTable(
            SKDocument document,
            ref PageState page,
            ref int pageNumber,
            string transistorName,
            IReadOnlyList<string> replacementHeaders,
            IReadOnlyList<PdfReplacementRow> replacements,
            SKFont sectionFont,
            SKFont tableFont,
            SKFont tableBoldFont,
            SKFont footerFont,
            SKPaint textPaint,
            SKPaint secondaryTextPaint,
            SKPaint borderPaint,
            SKPaint headerFillPaint,
            SKPaint alternateFillPaint)
        {
            const float sectionHeight = 22f;
            const float headerHeight = 28f;
            const float rowHeight = 20f;
            float tableWidth = PageWidth - (Margin * 2f);
            int parameterCount = Math.Max(1, replacementHeaders.Count);
            float nameWidth = Math.Clamp(tableWidth * 0.18f, 110f, 145f);
            float valueWidth = (tableWidth - nameWidth) / parameterCount;

            EnsureSpace(
                document,
                ref page,
                ref pageNumber,
                sectionHeight + headerHeight + rowHeight,
                transistorName,
                footerFont,
                secondaryTextPaint,
                sectionFont,
                textPaint);

            DrawText(
                page.Canvas,
                $"Transistores de reemplazo ({replacements.Count})",
                Margin,
                page.Y + 12f,
                SKTextAlign.Left,
                sectionFont,
                textPaint);
            page.Y += sectionHeight;

            DrawReplacementHeader(
                page,
                replacementHeaders,
                nameWidth,
                valueWidth,
                headerHeight,
                tableBoldFont,
                textPaint,
                borderPaint,
                headerFillPaint);

            for (int rowIndex = 0; rowIndex < replacements.Count; rowIndex++)
            {
                if (page.Y + rowHeight > ContentBottom)
                {
                    FinishPage(document, page, footerFont, secondaryTextPaint);
                    page = StartPage(document, ++pageNumber);
                    DrawContinuationTitle(
                        page,
                        transistorName,
                        "Transistores de reemplazo (continuación)",
                        sectionFont,
                        textPaint);

                    DrawReplacementHeader(
                        page,
                        replacementHeaders,
                        nameWidth,
                        valueWidth,
                        headerHeight,
                        tableBoldFont,
                        textPaint,
                        borderPaint,
                        headerFillPaint);
                }

                PdfReplacementRow replacement = replacements[rowIndex];
                SKPaint? rowFill = rowIndex % 2 == 1 ? alternateFillPaint : null;
                float x = Margin;

                DrawCell(
                    page.Canvas,
                    new SKRect(x, page.Y, x + nameWidth, page.Y + rowHeight),
                    replacement.Name,
                    tableBoldFont,
                    textPaint,
                    borderPaint,
                    rowFill,
                    SKTextAlign.Left);
                x += nameWidth;

                for (int columnIndex = 0; columnIndex < replacementHeaders.Count; columnIndex++)
                {
                    string value = columnIndex < replacement.Values.Count
                        ? replacement.Values[columnIndex] ?? string.Empty
                        : string.Empty;

                    DrawCell(
                        page.Canvas,
                        new SKRect(x, page.Y, x + valueWidth, page.Y + rowHeight),
                        value,
                        tableFont,
                        textPaint,
                        borderPaint,
                        rowFill,
                        SKTextAlign.Center);
                    x += valueWidth;
                }

                page.Y += rowHeight;
            }
        }

        private static void DrawReplacementHeader(
            PageState page,
            IReadOnlyList<string> replacementHeaders,
            float nameWidth,
            float valueWidth,
            float headerHeight,
            SKFont tableBoldFont,
            SKPaint textPaint,
            SKPaint borderPaint,
            SKPaint headerFillPaint)
        {
            float x = Margin;

            DrawCell(
                page.Canvas,
                new SKRect(x, page.Y, x + nameWidth, page.Y + headerHeight),
                "Nombre",
                tableBoldFont,
                textPaint,
                borderPaint,
                headerFillPaint,
                SKTextAlign.Left);
            x += nameWidth;

            foreach (string header in replacementHeaders)
            {
                DrawCell(
                    page.Canvas,
                    new SKRect(x, page.Y, x + valueWidth, page.Y + headerHeight),
                    header,
                    tableBoldFont,
                    textPaint,
                    borderPaint,
                    headerFillPaint,
                    SKTextAlign.Center);
                x += valueWidth;
            }

            page.Y += headerHeight;
        }

        private static void EnsureSpace(
            SKDocument document,
            ref PageState page,
            ref int pageNumber,
            float requiredHeight,
            string transistorName,
            SKFont footerFont,
            SKPaint secondaryTextPaint,
            SKFont sectionFont,
            SKPaint textPaint)
        {
            if (page.Y + requiredHeight <= ContentBottom)
                return;

            FinishPage(document, page, footerFont, secondaryTextPaint);
            page = StartPage(document, ++pageNumber);
            DrawPageTitle(page, transistorName, sectionFont, textPaint);
        }

        private static PageState StartPage(SKDocument document, int pageNumber)
        {
            SKCanvas canvas = document.BeginPage(PageWidth, PageHeight);
            canvas.Clear(SKColors.White);
            return new PageState(canvas, pageNumber, Margin);
        }

        private static void FinishPage(
            SKDocument document,
            PageState page,
            SKFont footerFont,
            SKPaint secondaryTextPaint)
        {
            float footerBaseline = PageHeight - Margin + 2f;

            DrawText(
                page.Canvas,
                "Nota: los valores mostrados corresponden a los parámetros técnicos de los transistores.",
                Margin,
                footerBaseline,
                SKTextAlign.Left,
                footerFont,
                secondaryTextPaint);

            DrawText(
                page.Canvas,
                $"Página {page.PageNumber}",
                PageWidth - Margin,
                footerBaseline,
                SKTextAlign.Right,
                footerFont,
                secondaryTextPaint);

            document.EndPage();
        }

        private static void DrawPageTitle(
            PageState page,
            string transistorName,
            SKFont sectionFont,
            SKPaint textPaint)
        {
            string safeName = string.IsNullOrWhiteSpace(transistorName)
                ? "Transistor"
                : transistorName;

            DrawText(
                page.Canvas,
                $"Reemplazos para {safeName}",
                Margin,
                page.Y + 11f,
                SKTextAlign.Left,
                sectionFont,
                textPaint);
            page.Y += 18f;
        }

        private static void DrawContinuationTitle(
            PageState page,
            string transistorName,
            string sectionTitle,
            SKFont sectionFont,
            SKPaint textPaint)
        {
            string safeName = string.IsNullOrWhiteSpace(transistorName)
                ? "Transistor"
                : transistorName;

            DrawText(
                page.Canvas,
                $"Reemplazos para {safeName}",
                Margin,
                page.Y + 11f,
                SKTextAlign.Left,
                sectionFont,
                textPaint);
            page.Y += 18f;

            DrawText(
                page.Canvas,
                sectionTitle,
                Margin,
                page.Y + 10f,
                SKTextAlign.Left,
                sectionFont,
                textPaint);
            page.Y += 20f;
        }

        private static void DrawCell(
            SKCanvas canvas,
            SKRect rect,
            string? text,
            SKFont font,
            SKPaint textPaint,
            SKPaint borderPaint,
            SKPaint? fillPaint,
            SKTextAlign alignment)
        {
            if (fillPaint is not null)
                canvas.DrawRect(rect, fillPaint);

            canvas.DrawRect(rect, borderPaint);

            const float horizontalPadding = 4f;
            float availableWidth = Math.Max(1f, rect.Width - (horizontalPadding * 2f));
            string fittedText = FitText(text ?? string.Empty, availableWidth, font, textPaint);

            float x = alignment switch
            {
                SKTextAlign.Center => rect.MidX,
                SKTextAlign.Right => rect.Right - horizontalPadding,
                _ => rect.Left + horizontalPadding
            };

            SKFontMetrics metrics = font.Metrics;
            float baseline = rect.MidY - ((metrics.Ascent + metrics.Descent) / 2f);

            canvas.Save();
            canvas.ClipRect(rect);
            DrawText(canvas, fittedText, x, baseline, alignment, font, textPaint);
            canvas.Restore();
        }

        private static string FitText(
            string text,
            float maxWidth,
            SKFont font,
            SKPaint paint)
        {
            if (string.IsNullOrEmpty(text) || font.MeasureText(text, paint) <= maxWidth)
                return text;

            const string ellipsis = "…";
            if (font.MeasureText(ellipsis, paint) > maxWidth)
                return string.Empty;

            int low = 0;
            int high = text.Length;

            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                string candidate = text[..middle] + ellipsis;

                if (font.MeasureText(candidate, paint) <= maxWidth)
                    low = middle;
                else
                    high = middle - 1;
            }

            return text[..low] + ellipsis;
        }

        private static void DrawText(
            SKCanvas canvas,
            string text,
            float x,
            float y,
            SKTextAlign alignment,
            SKFont font,
            SKPaint paint)
        {
            canvas.DrawText(text, x, y, alignment, font, paint);
        }

        private sealed class PageState
        {
            public PageState(SKCanvas canvas, int pageNumber, float y)
            {
                Canvas = canvas;
                PageNumber = pageNumber;
                Y = y;
            }

            public SKCanvas Canvas { get; }
            public int PageNumber { get; }
            public float Y { get; set; }
        }
    }

    public sealed record PdfReplacementRow(
        string Name,
        IReadOnlyList<string> Values);
}
