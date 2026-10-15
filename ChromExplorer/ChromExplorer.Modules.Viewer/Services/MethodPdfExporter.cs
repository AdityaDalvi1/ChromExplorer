using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.IO;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>
    /// Renders the Method Report (Sample / General / Ion Source / Acquisition Parameters,
    /// as recorded in the loaded .mtd file) directly to a PDF using PdfSharp/XGraphics —
    /// same approach and brand styling as ChromatogramPdfExporter, kept as its own small class
    /// since the two DTOs (MethodReportData vs. ChromatogramReportData) don't share a shape.
    /// </summary>
    public class MethodPdfExporter
    {
        private const double Margin = 36;
        private const double FooterReserve = 24;
        private const double RowHeight = 16;

        private static readonly XColor BrandDark = XColor.FromArgb(26, 74, 138);
        private static readonly XColor BrandLight = XColor.FromArgb(37, 99, 176);
        private static readonly XColor HeaderText = XColor.FromArgb(255, 255, 255);
        private static readonly XColor AltRow = XColor.FromArgb(240, 245, 251);
        private static readonly XColor GridLine = XColor.FromArgb(208, 216, 232);

        private readonly XFont _bannerFont = new XFont("Verdana", 15, XFontStyle.Bold);
        private readonly XFont _metaFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _sectionFont = new XFont("Verdana", 9.5, XFontStyle.Bold);
        private readonly XFont _labelFont = new XFont("Verdana", 8, XFontStyle.Bold);
        private readonly XFont _valueFont = new XFont("Verdana", 8, XFontStyle.Regular);
        private readonly XFont _tableHeaderFont = new XFont("Verdana", 8, XFontStyle.Bold);
        private readonly XFont _tableCellFont = new XFont("Verdana", 8, XFontStyle.Regular);

        private readonly PdfDocument _document = new PdfDocument();
        private PdfPage _page;
        private XGraphics _gfx;
        private double _y;

        public void Export(MethodReportData data, string filePath)
        {
            _document.Info.Title = "ChromExplorer — Method Report";

            BeginPage();

            DrawTitleBanner(data);

            foreach (var section in data.Sections)
            {
                if (section.Rows.Count > 0)
                    DrawSection(section.Title, section.Rows);
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
            _y = Margin;
        }

        private void FinalizePage()
        {
            _gfx.DrawLine(new XPen(XColors.Black, 0.75), Margin, _page.Height.Point - FooterReserve + 4,
                _page.Width.Point - Margin, _page.Height.Point - FooterReserve + 4);
            _gfx.DrawString("ChromExplorer Method Report", new XFont("Verdana", 7.5), XBrushes.Gray,
                new XRect(Margin, _page.Height.Point - FooterReserve + 6, _page.Width.Point - Margin * 2, FooterReserve),
                XStringFormats.TopCenter);
        }

        private void EnsureSpace(double neededHeight)
        {
            if (_y + neededHeight > _page.Height.Point - FooterReserve)
                BeginPage();
        }

        // ── Sections ────────────────────────────────────────────────────────────

        private void DrawTitleBanner(MethodReportData data)
        {
            double usableWidth = _page.Width.Point - Margin * 2;
            double bannerHeight = 34;
            EnsureSpace(bannerHeight + 10);

            _gfx.DrawRectangle(new XSolidBrush(BrandDark), Margin, _y, usableWidth, bannerHeight);
            _gfx.DrawString("ChromExplorer — Method Report", _bannerFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 4, usableWidth, 18), XStringFormats.TopCenter);
            _gfx.DrawString($"Generated: {data.GeneratedDate}   |   User: {data.UserName}", _metaFont, new XSolidBrush(HeaderText),
                new XRect(Margin, _y + 22, usableWidth, 10), XStringFormats.TopCenter);

            _y += bannerHeight + 10;
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

        private void DrawSection(string title, System.Collections.Generic.List<MethodReportRow> rows)
        {
            DrawSectionHeader(title);

            double usableWidth = _page.Width.Point - Margin * 2;
            double labelWidth = usableWidth * 0.4;

            for (int i = 0; i < rows.Count; i++)
            {
                EnsureSpace(RowHeight);

                if (i % 2 == 1)
                    _gfx.DrawRectangle(new XSolidBrush(AltRow), Margin, _y, usableWidth, RowHeight);

                _gfx.DrawRectangle(new XPen(GridLine, 0.5), Margin, _y, usableWidth, RowHeight);

                _gfx.DrawString(rows[i].Label ?? "", _labelFont, new XSolidBrush(BrandDark),
                    new XRect(Margin + 4, _y, labelWidth - 8, RowHeight), XStringFormats.CenterLeft);
                _gfx.DrawString(rows[i].Value ?? "", _valueFont, XBrushes.Black,
                    new XRect(Margin + labelWidth + 4, _y, usableWidth - labelWidth - 8, RowHeight), XStringFormats.CenterLeft);

                _y += RowHeight;
            }

            _y += 8;
        }

    }
}
