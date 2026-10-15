using System;
using System.Collections.Generic;
using System.Linq;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ChromExplorer.Modules.Auth.Services
{
    /// <summary>
    /// Renders a simple paginated table (title + header row + data rows) to a PDF file.
    /// Column widths are proportional to the given weights (e.g. the source DataGrid's
    /// actual column widths), scaled to fit an A4 landscape page. Cell text is word-wrapped
    /// in full (never truncated) — rows grow taller as needed to fit the complete text.
    /// </summary>
    internal class AuditPdfExporter
    {
        private const double Margin = 30;
        private const double HeaderRowHeight = 22;
        private const double LineHeight = 10;
        private const double CellVerticalPadding = 4;

        private readonly XFont _titleFont = new XFont("Verdana", 13, XFontStyle.Bold);
        private readonly XFont _subFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _headerFont = new XFont("Verdana", 8, XFontStyle.Bold);
        private readonly XFont _cellFont = new XFont("Verdana", 7, XFontStyle.Regular);

        private readonly XSolidBrush _headerBg = new XSolidBrush(XColor.FromArgb(45, 62, 80));
        private readonly XSolidBrush _altRowBg = new XSolidBrush(XColor.FromArgb(248, 249, 250));
        private readonly XPen _gridPen = new XPen(XColor.FromArgb(224, 224, 224), 0.5);

        private readonly PdfDocument _document = new PdfDocument();
        private PdfPage _page;
        private XGraphics _gfx;
        private double _usableWidth;
        private double[] _colWidths;
        private double _y;

        public void Export(string title, string[] headers, double[] colWeights, List<string[]> rows, string filePath)
        {
            _document.Info.Title = title;

            double totalWeight = colWeights.Sum();
            if (totalWeight <= 0) totalWeight = headers.Length;

            BeginPage(title, headers, colWeights, totalWeight);

            int rowIndex = 0;
            foreach (var row in rows)
            {
                var wrappedCells = WrapRow(row);
                int lineCount = wrappedCells.Count == 0 ? 1 : wrappedCells.Max(c => c.Count);
                double rowHeight = Math.Max(1, lineCount) * LineHeight + CellVerticalPadding * 2;

                if (_y + rowHeight > _page.Height.Point - Margin)
                {
                    BeginPage(title, headers, colWeights, totalWeight);
                    rowIndex = 0;
                }

                DrawDataRow(wrappedCells, rowHeight, rowIndex % 2 == 1);
                rowIndex++;
            }

            _document.Save(filePath);
        }

        private void BeginPage(string title, string[] headers, double[] colWeights, double totalWeight)
        {
            _page = _document.AddPage();
            _page.Size = PageSize.A4;
            _page.Orientation = PageOrientation.Landscape;
            _gfx = XGraphics.FromPdfPage(_page);

            _usableWidth = _page.Width.Point - Margin * 2;
            _colWidths = colWeights.Select(w => w / totalWeight * _usableWidth).ToArray();
            _y = Margin;

            _gfx.DrawString(title, _titleFont, XBrushes.Black, new XPoint(Margin, _y + 14));
            _y += 20;
            _gfx.DrawString("Generated: " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm"), _subFont, XBrushes.Black, new XPoint(Margin, _y + 10));
            _y += 18;

            DrawHeaderRow(headers);
        }

        private void DrawHeaderRow(string[] headers)
        {
            _gfx.DrawRectangle(_headerBg, Margin, _y, _usableWidth, HeaderRowHeight);

            double x = Margin;
            for (int i = 0; i < headers.Length; i++)
            {
                var rect = new XRect(x + 3, _y, _colWidths[i] - 6, HeaderRowHeight);
                _gfx.DrawString(headers[i] ?? "", _headerFont, XBrushes.White, rect, XStringFormats.CenterLeft);
                x += _colWidths[i];
            }

            _y += HeaderRowHeight;
        }

        // Wraps every cell in the row to its column's width, in full — nothing is ever truncated.
        private List<List<string>> WrapRow(string[] row)
        {
            var wrapped = new List<List<string>>();
            for (int i = 0; i < row.Length && i < _colWidths.Length; i++)
            {
                wrapped.Add(WrapToWidth(row[i] ?? "", _cellFont, _colWidths[i] - 6));
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

                    // A single word wider than the column on its own — hard-break it by character
                    // rather than overflowing, since it can't be split on spaces any further.
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

        private void DrawDataRow(List<List<string>> wrappedCells, double rowHeight, bool alternateShading)
        {
            if (alternateShading)
                _gfx.DrawRectangle(_altRowBg, Margin, _y, _usableWidth, rowHeight);

            double x = Margin;
            for (int i = 0; i < wrappedCells.Count && i < _colWidths.Length; i++)
            {
                double lineY = _y + CellVerticalPadding;
                foreach (var line in wrappedCells[i])
                {
                    var rect = new XRect(x + 3, lineY, _colWidths[i] - 6, LineHeight);
                    _gfx.DrawString(line, _cellFont, XBrushes.Black, rect, XStringFormats.CenterLeft);
                    lineY += LineHeight;
                }
                x += _colWidths[i];
            }

            _gfx.DrawLine(_gridPen, Margin, _y + rowHeight, Margin + _usableWidth, _y + rowHeight);
            _y += rowHeight;
        }
    }
}
