using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>
    /// Renders the chromatogram analysis report directly to a PDF using PdfSharp/XGraphics,
    /// the same approach as ChromExplorer.Modules.Auth.Services.AuditPdfExporter, instead of
    /// populating an HTML template and printing it through headless Microsoft Edge.
    ///
    /// Note: this installed PdfSharp version (1.50.5147) can only draw onto a PdfPage/XForm —
    /// it has no XGraphics.FromGraphics(Graphics) to rasterize onto an arbitrary bitmap — so it
    /// cannot double as an in-app preview renderer. The in-app preview (ReportPreviewWindow) is
    /// built separately as a native WPF FlowDocument from the same ChromatogramReportData;
    /// this class is PDF output only.
    /// </summary>
    public class ChromatogramPdfExporter
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
        private static readonly XColor StatusActiveBg = XColor.FromArgb(226, 239, 218);
        private static readonly XColor StatusActiveText = XColor.FromArgb(56, 87, 35);
        private static readonly XColor StatusInactiveBg = XColor.FromArgb(242, 242, 242);
        private static readonly XColor StatusInactiveText = XColor.FromArgb(166, 166, 166);

        private readonly XFont _bannerFont = new XFont("Verdana", 15, XFontStyle.Bold);
        private readonly XFont _metaFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _descFont = new XFont("Verdana", 8.5, XFontStyle.Regular);
        private readonly XFont _sectionFont = new XFont("Verdana", 9.5, XFontStyle.Bold);
        //private readonly XFont _headerTitleFont = new XFont("Verdana", 10, XFontStyle.Bold);
        private readonly XFont _headerTimeFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _captionFont = new XFont("Verdana", 8.5, XFontStyle.Bold);
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
        private ChromatogramReportData _data;

        public void Export(ChromatogramReportData data, string filePath)
        {
            Export(new[] { data }, filePath);
        }

        /// <summary>Exports every queued entry into a single combined PDF — each file gets its
        /// own run of pages (RenderEntry always starts with BeginPage(), which finalizes the
        /// previous entry's last page first), with page numbers running continuously across the
        /// whole document. The Quan calibration section (chart/equation/results table) reflects
        /// one shared calibration session, not any single file — every entry already carries an
        /// identical copy of it (see ChromatogramViewViewModel.BuildReportData), so repeating it
        /// per file was pure duplication. It's drawn exactly once, after every file's pages,
        /// instead of once per file.</summary>
        public void Export(IList<ChromatogramReportData> entries, string filePath)
        {
            _document.Info.Title = "ChromExplorer — Analysis Report";
            _pageNumber = 0;

            foreach (var data in entries)
            {
                RenderEntry(data);
            }

            if (entries.Count > 0)
            {
                DrawSharedRsdSection(entries[entries.Count - 1]);
                DrawSharedQuanSection(entries[entries.Count - 1]);
            }

            FinalizePage();
            _document.Save(filePath);
        }

        private void RenderEntry(ChromatogramReportData data)
        {
            _data = data;

            BeginPage();

            DrawTitleBanner();
            DrawDescription();

            DrawImageSection("Chromatogram", data.ChromatogramImage);
            DrawImageSection("Mass Spectrum", data.SpectrumImage);

            DrawParamTablesSideBySide(
                "Chromatogram Output",
                new[]
                {
                    ("File / Title", data.FileName),
                    ("Total Scans", data.TotalScans),
                    ("Data Points", data.DataPoints),
                    ("Selected Index", data.SelectedIndex),
                    ("Selected SIC", data.SelectedSIC),
                    ("Visible Area", data.VisibleArea),
                    ("X-Axis Units", data.XAxisUnits),
                },
                "Spectrum Output",
                new[]
                {
                    ("Retention Time", data.RetentionTime + " Min"),
                    ("Scan Index", data.ScanIndex),
                    ("Scan ID", data.ScanID),
                    ("Injection Time (ms)", data.InjectionTimeMs),
                    ("TIC", data.TIC),
                    ("Parent Ion (m/z)", data.ParentIon),
                    ("Baseline Removed", data.BaselineRemoved),
                });

            // Each major section below starts on its own fresh page — matching the fixed
            // page-per-section layout of the original HTML template — rather than flowing
            // continuously and only breaking on overflow.
            BeginPage();

            DrawDataTable("Detected Chromatogram Peaks",
                new[] { "#", "Retention Time", "Label" },
                new double[] { 0.08, 0.30, 0.62 },
                data.ChromPeakRows);

            DrawDataTable("Spectrum Peak List",
                new[] { "#", "m/z", "Intensity" },
                new double[] { 0.15, 0.42, 0.43 },
                data.SpectrumPeakRows);

            BeginPage();

            DrawPostProcessingSection("Spectrum Post-Processing Operations", data.SpectrumPostProcessing);
            DrawPostProcessingSection("EIC Post-Processing Operations", data.EicPostProcessing);

            BeginPage();

            DrawParamTablesSideBySide(
                "Peak Integration Result",
                new[]
                {
                    ("Status", data.IntegrationStatus),
                    ("Start Time", data.IntegrationStartTime),
                    ("End Time", data.IntegrationEndTime),
                },
                "Signal-to-Noise (SNR) Result",
                new[]
                {
                    ("Status", data.SNRStatus),
                    ("Signal Definition", data.SNRSignalDefinition),
                    ("Signal Value", data.SNRSignalValue),
                    ("Noise Definition", data.SNRNoiseDefinition),
                    ("Noise Value", data.SNRNoiseValue),
                    ("SNR Ratio", data.SNRRatio),
                    ("Noise Region Used", data.SNRNoiseRegionUsed),
                });
        }

        /// <summary>%RSD-by-sample summary (same table as the RSD tab) — drawn once for the
        /// whole report, immediately before the Quan section, not once per file, since every
        /// entry already carries an identical copy of the same shared RSD queue.</summary>
        private void DrawSharedRsdSection(ChromatogramReportData data)
        {
            _data = data;

            BeginPage();

            DrawDataTable("%RSD by Concentration",
                new[] { "Concentration", "N", "Mean Area", "Std Dev", "%RSD", "Note" },
                new double[] { 0.28, 0.08, 0.16, 0.16, 0.12, 0.20 },
                data.RsdRows);

            DrawDataTable("Queued Files",
                new[] { "#", "Sample ID", "Concentration", "Area", "File", "Added" },
                new double[] { 0.05, 0.17, 0.12, 0.10, 0.38, 0.18 },
                data.RsdQueuedRows);
        }

        /// <summary>Quan calibration section (chart, equation, results table) — drawn once for
        /// the whole report (see Export), not once per file, since every entry already carries
        /// an identical copy of the same shared calibration session.</summary>
        private void DrawSharedQuanSection(ChromatogramReportData data)
        {
            _data = data;

            BeginPage();

            DrawSectionHeader("Quantitation (Quan) — Calibration Curve");
            DrawImageBlock(data.QuanChartImage, 200);
            EnsureSpace(LineHeight + 4);
            _gfx.DrawString("Calibration Equation: " + (data.QuanEquation ?? "N/A"), _descFont, XBrushes.Black, new XPoint(Margin, _y + LineHeight));
            _y += LineHeight + 8;

            DrawDataTable("Quan Sample Results",
                new[] { "#", "Sample", "Sample Type", "Exp Amt", "Area", "Calc Amt", "% Recovery", "% Diff", "File" },
                new double[] { 0.05, 0.12, 0.11, 0.09, 0.09, 0.09, 0.10, 0.09, 0.26 },
                data.QuanRows);
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

            //_gfx.DrawString("MassTech", _headerTitleFont, new XSolidBrush(BrandDark),
            //    new XRect(Margin, _y, usableWidth, 26), XStringFormats.CenterLeft);
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

            // One extra line, and the banner grows to fit it, only when this entry actually went
            // through the review workflow (ReviewedByName stamped by ReportPreviewWindow right
            // before export) — a report saved without going through review just keeps the
            // original 3-line banner.
            bool hasReviewer = !string.IsNullOrWhiteSpace(_data.ReviewedByName);
            double bannerHeight = hasReviewer ? 49 : 40;
            EnsureSpace(bannerHeight + 6);

            _gfx.DrawRectangle(new XSolidBrush(BrandDark), Margin, _y, usableWidth, bannerHeight);
            _gfx.DrawString("ChromExplorer — Analysis Report", _bannerFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 4, usableWidth, 18), XStringFormats.TopCenter);
            _gfx.DrawString($"File: {_data.FileName}   |   Generated: {_data.GeneratedDate}", _metaFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 22, usableWidth, 10), XStringFormats.TopCenter);
            _gfx.DrawString($"Instrument: {_data.InstrumentName}   |   User: {_data.UserName}", _metaFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 31, usableWidth, 10), XStringFormats.TopCenter);

            if (hasReviewer)
            {
                string reviewedLine = string.IsNullOrWhiteSpace(_data.ReviewedDate)
                    ? $"Reviewed by: {_data.ReviewedByName}"
                    : $"Reviewed by: {_data.ReviewedByName}   |   Reviewed on: {_data.ReviewedDate}";

                _gfx.DrawString(reviewedLine, _metaFont, new XSolidBrush(HeaderText),
                    new XRect(Margin, _y + 40, usableWidth, 10), XStringFormats.TopCenter);
            }

            _y += bannerHeight + 6;
        }

        private void DrawDescription()
        {
            EnsureSpace(LineHeight + 4);
            _gfx.DrawString("Description: " + (_data.Description ?? ""), _descFont, XBrushes.DimGray, new XPoint(Margin, _y + LineHeight));
            _y += LineHeight + 8;
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

        private void DrawImageSection(string title, byte[] imageBytes)
        {
            DrawSectionHeader(title);
            DrawImageBlock(imageBytes, 170);
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

        private void DrawParamTablesSideBySide(string captionLeft, (string label, string value)[] rowsLeft,
            string captionRight, (string label, string value)[] rowsRight)
        {
            double usableWidth = _page.Width.Point - Margin * 2;
            double colWidth = (usableWidth - 10) / 2;
            double rowHeight = 14;
            double captionHeight = 16;

            int rowCount = Math.Max(rowsLeft.Length, rowsRight.Length);
            double blockHeight = captionHeight + rowCount * rowHeight;
            EnsureSpace(blockHeight + 8);

            double leftX = Margin;
            double rightX = Margin + colWidth + 10;

            DrawKeyValueTable(leftX, colWidth, captionLeft, rowsLeft, rowHeight, captionHeight);
            DrawKeyValueTable(rightX, colWidth, captionRight, rowsRight, rowHeight, captionHeight);

            _y += blockHeight + 8;
        }

        private void DrawKeyValueTable(double x, double width, string caption, (string label, string value)[] rows, double rowHeight, double captionHeight)
        {
            double y = _y;

            _gfx.DrawRectangle(new XSolidBrush(BrandLight), x, y, width, captionHeight);
            _gfx.DrawString(caption, _captionFont, new XSolidBrush(HeaderText),
                new XRect(x + 6, y, width - 12, captionHeight), XStringFormats.CenterLeft);
            y += captionHeight;

            double labelWidth = width * 0.5;

            for (int i = 0; i < rows.Length; i++)
            {
                if (i % 2 == 1)
                    _gfx.DrawRectangle(new XSolidBrush(AltRow), x, y, width, rowHeight);

                _gfx.DrawRectangle(new XPen(GridLine, 0.5), x, y, width, rowHeight);

                _gfx.DrawString(rows[i].label, _labelFont, new XSolidBrush(BrandDark),
                    new XRect(x + 4, y, labelWidth - 8, rowHeight), XStringFormats.CenterLeft);
                _gfx.DrawString(rows[i].value ?? "", _valueFont, XBrushes.Black,
                    new XRect(x + labelWidth + 4, y, width - labelWidth - 8, rowHeight), XStringFormats.CenterLeft);

                y += rowHeight;
            }
        }

        private void DrawPostProcessingSection(string subHeader, List<PdfKeyValueRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            DrawSectionHeader(subHeader);

            double usableWidth = _page.Width.Point - Margin * 2;
            double rowHeight = 14;
            double labelWidth = usableWidth * 0.6;

            foreach (var row in rows)
            {
                EnsureSpace(rowHeight);

                _gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(233, 238, 247)), Margin, _y, labelWidth, rowHeight);
                _gfx.DrawRectangle(new XPen(GridLine, 0.5), Margin, _y, labelWidth, rowHeight);
                _gfx.DrawString(row.Label, _labelFont, new XSolidBrush(XColors.Black),
                    new XRect(Margin + 4, _y, labelWidth - 8, rowHeight), XStringFormats.CenterLeft);

                double valueX = Margin + labelWidth;
                double valueWidth = usableWidth - labelWidth;

                if (row.IsStatusCell)
                {
                    var bg = row.IsActive ? StatusActiveBg : StatusInactiveBg;
                    var fg = row.IsActive ? StatusActiveText : StatusInactiveText;
                    _gfx.DrawRectangle(new XSolidBrush(bg), valueX, _y, valueWidth, rowHeight);
                    _gfx.DrawString(row.Value, new XFont("Verdana", 8, row.IsActive ? XFontStyle.Bold : XFontStyle.Regular),
                        new XSolidBrush(fg), new XRect(valueX, _y, valueWidth, rowHeight), XStringFormats.Center);
                }
                else
                {
                    _gfx.DrawString(row.Value ?? "", _valueFont, XBrushes.Black,
                        new XRect(valueX + 4, _y, valueWidth - 8, rowHeight), XStringFormats.CenterLeft);
                }

                _gfx.DrawRectangle(new XPen(GridLine, 0.5), valueX, _y, valueWidth, rowHeight);

                _y += rowHeight;
            }

            _y += 8;
        }

        // ── Generic paginated data table (peaks / quan rows) ───────────────────

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
