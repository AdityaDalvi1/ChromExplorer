using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>
    /// Renders the forensic report directly to a PDF using PdfSharp/XGraphics, matching the
    /// same header/footer/title-banner visual style as ChromatogramPdfExporter (the General
    /// Report exporter), so both report types look like they belong to the same product.
    /// </summary>
    public class ForensicPdfExporter
    {
        private const double Margin = 36;
        private const double FooterReserve = 24;
        private const double LineHeight = 11;
        private const double CellVerticalPadding = 4;

        private static readonly XColor BrandDark = XColor.FromArgb(26, 74, 138);
        private static readonly XColor BrandLight = XColor.FromArgb(37, 99, 176);
        private static readonly XColor HeaderText = XColor.FromArgb(255, 255, 255);
        private static readonly XColor AltRow = XColor.FromArgb(240, 245, 251);
        private static readonly XColor GridLine = XColor.FromArgb(208, 216, 232);

        private readonly XFont _bannerFont = new XFont("Verdana", 15, XFontStyle.Bold);
        private readonly XFont _metaFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _sectionFont = new XFont("Verdana", 9.5, XFontStyle.Bold);
        private readonly XFont _headerTimeFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _labelFont = new XFont("Verdana", 8, XFontStyle.Bold);
        private readonly XFont _valueFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _tableHeaderFont = new XFont("Verdana", 8, XFontStyle.Bold);
        private readonly XFont _tableCellFont = new XFont("Verdana", 7.5, XFontStyle.Regular);
        private readonly XFont _placeholderFont = new XFont("Verdana", 9, XFontStyle.Italic);
        private readonly XFont _footerFont = new XFont("Verdana", 7.5, XFontStyle.Regular);

        private readonly PdfDocument _document = new PdfDocument();
        private PdfPage _page;
        private XGraphics _gfx;
        private double _y;
        private int _pageNumber;
        private ForensicReportData _data;

        public void Export(ForensicReportData data, string filePath)
        {
            _data = data;
            _document.Info.Title = "ChromExplorer — Forensic Report";
            _pageNumber = 0;

            BeginPage();

            DrawTitleBanner();

            DrawSingleKeyValueTable("Report & Sample Information", new[]
            {
                ("Report No.", data.ReportNo),
                ("Case No.", data.CaseNo),
                ("Sample Description", data.SampleDescription),
                ("Received Date", data.ReceivedDate),
                ("Analysis Date", data.AnalysisDate),
                ("Analyst", data.Analyst),
            });

            DrawDataTable("Results",
                new[] { "Target", "Status", "Confidence", "Remarks" },
                new double[] { 0.28, 0.18, 0.18, 0.36 },
                data.ResultRows);

            DrawParagraphSection("Conclusion", data.Conclusion);

            DrawSingleKeyValueTable("Signatures", new[]
            {
                ("Analyst Signature", data.AnalystSignature),
                ("Reviewer", data.Reviewer),
            });

            if (data.ShowMassSpectrum || data.ShowChromatogram)
            {
                BeginPage();

                if (data.ShowMassSpectrum)
                    DrawImageSection("Mass Spectrum", data.SpectrumImage, 260);

                if (data.ShowChromatogram)
                    DrawImageSection("Chromatogram", data.ChromatogramImage, 260);
            }

            if (data.ShowLibrarySearch)
            {
                BeginPage();
                DrawSectionHeader("Library Search Results");
                EnsureSpace(LineHeight + 6);
                _gfx.DrawString("Library search comparison is not available in this version.", _placeholderFont, XBrushes.Gray,
                    new XPoint(Margin, _y + LineHeight));
                _y += LineHeight + 10;
            }

            FinalizePage();
            _document.Save(filePath);
        }

        // ── Page / header / footer ─────────────────────────────────────────────

        private void BeginPage()
        {
            if (_page != null)
                FinalizePage();

            _page = _document.AddPage();
            _page.Size = PdfSharp.PageSize.A4;
            _page.Orientation = PdfSharp.PageOrientation.Portrait;
            _gfx = XGraphics.FromPdfPage(_page);
            _pageNumber++;
            _y = Margin;

            DrawPageHeader();
        }

        private void FinalizePage()
        {
            var footerText = $"Page {_pageNumber}  |  ChromExplorer Report";
            _gfx.DrawLine(new XPen(XColors.Black, 0.75), Margin, _page.Height.Point - FooterReserve + 4,
                _page.Width.Point - Margin, _page.Height.Point - FooterReserve + 4);
            _gfx.DrawString(footerText, _footerFont, XBrushes.Gray,
                new XRect(Margin, _page.Height.Point - FooterReserve + 6, _page.Width.Point - Margin * 2, FooterReserve),
                XStringFormats.TopCenter);
        }

        private void DrawPageHeader()
        {
            double usableWidth = _page.Width.Point - Margin * 2;

            if (_data.LogoImage != null)
            {
                try
                {
                    using (var ms = new MemoryStream(_data.LogoImage))
                    {
                        var logo = XImage.FromStream(ms);
                        double logoHeight = 26;
                        double logoWidth = logo.PixelWidth * (logoHeight / logo.PixelHeight);
                        _gfx.DrawImage(logo, Margin, _y, logoWidth, logoHeight);
                    }
                }
                catch { /* logo is decorative — skip silently if it can't be decoded */ }
            }

            _gfx.DrawString(_data.GeneratedDate, _headerTimeFont, XBrushes.DimGray,
                new XRect(Margin, _y, usableWidth, 26), XStringFormats.CenterRight);

            _y += 26;
            _gfx.DrawLine(new XPen(XColors.Black, 1.25), Margin, _y, _page.Width.Point - Margin, _y);
            _y += 10;
        }

        // ── Space management ───────────────────────────────────────────────────

        private void EnsureSpace(double neededHeight)
        {
            if (_y + neededHeight > _page.Height.Point - FooterReserve)
            {
                BeginPage();
            }
        }

        // ── Sections ────────────────────────────────────────────────────────────

        private void DrawTitleBanner()
        {
            double usableWidth = _page.Width.Point - Margin * 2;
            double bannerHeight = 40;
            EnsureSpace(bannerHeight + 6);

            _gfx.DrawRectangle(new XSolidBrush(BrandDark), Margin, _y, usableWidth, bannerHeight);
            _gfx.DrawString("ChromExplorer — Forensic Report", _bannerFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 4, usableWidth, 18), XStringFormats.TopCenter);
            _gfx.DrawString($"Report No: {_data.ReportNo}   |   Generated: {_data.GeneratedDate}", _metaFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 22, usableWidth, 10), XStringFormats.TopCenter);
            _gfx.DrawString($"Case No: {_data.CaseNo}   |   Analyst: {_data.Analyst}", _metaFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 31, usableWidth, 10), XStringFormats.TopCenter);

            _y += bannerHeight + 6;
        }

        private void DrawSectionHeader(string title)
        {
            double usableWidth = _page.Width.Point - Margin * 2;
            double headerHeight = 18;
            EnsureSpace(headerHeight);

            _gfx.DrawRectangle(new XSolidBrush(BrandLight), Margin, _y, usableWidth, headerHeight);
            _gfx.DrawString(title, _sectionFont, new XSolidBrush(HeaderText),
                new XRect(Margin + 8, _y, usableWidth - 16, headerHeight), XStringFormats.CenterLeft);
            _y += headerHeight;
        }

        private void DrawImageSection(string title, byte[] imageBytes, double blockHeight = 300)
        {
            DrawSectionHeader(title);
            DrawImageBlock(imageBytes, blockHeight);
        }

        private void DrawImageBlock(byte[] imageBytes, double blockHeight)
        {
            double usableWidth = _page.Width.Point - Margin * 2;
            EnsureSpace(blockHeight + 4);

            _gfx.DrawRectangle(new XPen(XColor.FromArgb(187, 187, 187), 0.75), XBrushes.White, Margin, _y, usableWidth, blockHeight);

            if (imageBytes != null && imageBytes.Length > 0)
            {
                try
                {
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var img = XImage.FromStream(ms);
                        double scale = Math.Min((usableWidth - 10) / img.PixelWidth, (blockHeight - 10) / img.PixelHeight);
                        double w = img.PixelWidth * scale;
                        double h = img.PixelHeight * scale;
                        _gfx.DrawImage(img, Margin + (usableWidth - w) / 2, _y + (blockHeight - h) / 2, w, h);
                    }
                }
                catch
                {
                    DrawPlaceholder("Image could not be rendered", usableWidth, blockHeight);
                }
            }
            else
            {
                DrawPlaceholder("Plot Not Available", usableWidth, blockHeight);
            }

            _y += blockHeight + 8;
        }

        private void DrawPlaceholder(string text, double usableWidth, double blockHeight)
        {
            _gfx.DrawString(text, _placeholderFont, XBrushes.Gray,
                new XRect(Margin, _y, usableWidth, blockHeight), XStringFormats.Center);
        }

        private void DrawSingleKeyValueTable(string caption, (string label, string value)[] rows)
        {
            DrawSectionHeader(caption);

            double usableWidth = _page.Width.Point - Margin * 2;
            double rowHeight = 16;
            double labelWidth = usableWidth * 0.32;

            for (int i = 0; i < rows.Length; i++)
            {
                EnsureSpace(rowHeight);

                if (i % 2 == 1)
                    _gfx.DrawRectangle(new XSolidBrush(AltRow), Margin, _y, usableWidth, rowHeight);

                _gfx.DrawRectangle(new XPen(GridLine, 0.5), Margin, _y, usableWidth, rowHeight);
                _gfx.DrawLine(new XPen(GridLine, 0.5), Margin + labelWidth, _y, Margin + labelWidth, _y + rowHeight);

                _gfx.DrawString(rows[i].label, _labelFont, new XSolidBrush(BrandDark),
                    new XRect(Margin + 6, _y, labelWidth - 10, rowHeight), XStringFormats.CenterLeft);
                _gfx.DrawString(rows[i].value ?? "", _valueFont, XBrushes.Black,
                    new XRect(Margin + labelWidth + 6, _y, usableWidth - labelWidth - 12, rowHeight), XStringFormats.CenterLeft);

                _y += rowHeight;
            }

            _y += 8;
        }

        private void DrawParagraphSection(string title, string text)
        {
            // Only include this section at all when the user actually wrote something.
            if (string.IsNullOrWhiteSpace(text))
                return;

            DrawSectionHeader(title);

            double usableWidth = _page.Width.Point - Margin * 2;

            foreach (var line in WrapToWidth(text, _valueFont, usableWidth))
            {
                EnsureSpace(LineHeight + 2);
                _gfx.DrawString(line, _valueFont, XBrushes.Black, new XPoint(Margin, _y + LineHeight));
                _y += LineHeight + 2;
            }

            _y += 8;
        }

        // ── Generic paginated data table (results rows) ────────────────────────

        private void DrawDataTable(string title, string[] headers, double[] colWeights, List<string[]> rows)
        {
            DrawSectionHeader(title);

            if (rows == null || rows.Count == 0)
            {
                EnsureSpace(LineHeight + 6);
                _gfx.DrawString("No data available", _placeholderFont, XBrushes.Gray, new XPoint(Margin, _y + LineHeight));
                _y += LineHeight + 10;
                return;
            }

            double usableWidth = _page.Width.Point - Margin * 2;
            double totalWeight = colWeights.Sum();
            double[] colWidths = colWeights.Select(w => w / totalWeight * usableWidth).ToArray();

            DrawTableHeaderRow(headers, colWidths, usableWidth);

            int rowIndex = 0;
            foreach (var row in rows)
            {
                var wrapped = WrapRow(row, colWidths, _tableCellFont);
                int lineCount = wrapped.Count == 0 ? 1 : wrapped.Max(c => c.Count);
                double rowHeight = Math.Max(1, lineCount) * LineHeight + CellVerticalPadding * 2;

                if (_y + rowHeight > _page.Height.Point - FooterReserve)
                {
                    BeginPage();
                    DrawTableHeaderRow(headers, colWidths, usableWidth);
                    rowIndex = 0;
                }

                DrawDataRow(wrapped, colWidths, rowHeight, rowIndex % 2 == 1, usableWidth);
                rowIndex++;
            }

            _y += 8;
        }

        private void DrawTableHeaderRow(string[] headers, double[] colWidths, double usableWidth)
        {
            double headerRowHeight = 16;
            EnsureSpace(headerRowHeight);

            _gfx.DrawRectangle(new XSolidBrush(BrandDark), Margin, _y, usableWidth, headerRowHeight);

            double x = Margin;
            for (int i = 0; i < headers.Length; i++)
            {
                var rect = new XRect(x + 3, _y, colWidths[i] - 6, headerRowHeight);
                _gfx.DrawString(headers[i] ?? "", _tableHeaderFont, new XSolidBrush(HeaderText), rect, XStringFormats.CenterLeft);
                x += colWidths[i];
            }

            _y += headerRowHeight;
        }

        private List<List<string>> WrapRow(string[] row, double[] colWidths, XFont font)
        {
            var wrapped = new List<List<string>>();
            for (int i = 0; i < row.Length && i < colWidths.Length; i++)
            {
                wrapped.Add(WrapToWidth(row[i] ?? "", font, colWidths[i] - 6));
            }
            return wrapped;
        }

        private List<string> WrapToWidth(string text, XFont font, double maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
            {
                lines.Add(text ?? "");
                return lines;
            }

            var words = text.Split(' ');
            string current = "";

            foreach (var word in words)
            {
                string candidate = current.Length == 0 ? word : current + " " + word;

                if (_gfx.MeasureString(candidate, font).Width <= maxWidth)
                {
                    current = candidate;
                }
                else
                {
                    if (current.Length > 0)
                    {
                        lines.Add(current);
                        current = "";
                    }

                    if (_gfx.MeasureString(word, font).Width > maxWidth)
                    {
                        string remaining = word;
                        while (_gfx.MeasureString(remaining, font).Width > maxWidth && remaining.Length > 1)
                        {
                            int cut = remaining.Length;
                            while (cut > 1 && _gfx.MeasureString(remaining.Substring(0, cut), font).Width > maxWidth)
                                cut--;

                            lines.Add(remaining.Substring(0, cut));
                            remaining = remaining.Substring(cut);
                        }
                        current = remaining;
                    }
                    else
                    {
                        current = word;
                    }
                }
            }

            if (current.Length > 0 || lines.Count == 0)
                lines.Add(current);

            return lines;
        }

        private void DrawDataRow(List<List<string>> wrappedCells, double[] colWidths, double rowHeight, bool alternateShading, double usableWidth)
        {
            if (alternateShading)
                _gfx.DrawRectangle(new XSolidBrush(AltRow), Margin, _y, usableWidth, rowHeight);

            double x = Margin;
            for (int i = 0; i < wrappedCells.Count && i < colWidths.Length; i++)
            {
                double lineY = _y + CellVerticalPadding;
                foreach (var line in wrappedCells[i])
                {
                    var rect = new XRect(x + 3, lineY, colWidths[i] - 6, LineHeight);
                    _gfx.DrawString(line, _tableCellFont, XBrushes.Black, rect, XStringFormats.CenterLeft);
                    lineY += LineHeight;
                }
                x += colWidths[i];
            }

            _gfx.DrawLine(new XPen(GridLine, 0.5), Margin, _y + rowHeight, Margin + usableWidth, _y + rowHeight);
            _y += rowHeight;
        }
    }
}
