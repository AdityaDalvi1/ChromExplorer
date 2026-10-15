using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>Builds the in-app WPF preview of the Method Report from MethodReportData — same
    /// separation of concerns as ReportPreviewDocumentBuilder: this is a native FlowDocument
    /// renderer, independent of MethodPdfExporter's PdfSharp output, both driven by the same
    /// DTO.</summary>
    public static class MethodReportDocumentBuilder
    {
        private static readonly Color BrandDark = Color.FromRgb(26, 74, 138);
        private static readonly Color BrandLight = Color.FromRgb(37, 99, 176);
        private static readonly Color AltRow = Color.FromRgb(240, 245, 251);
        private static readonly Color GridLine = Color.FromRgb(208, 216, 232);

        public static FlowDocument Build(MethodReportData data)
        {
            var doc = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5,
                PagePadding = new Thickness(36),
                ColumnWidth = 3000,
                TextAlignment = TextAlignment.Left
            };

            var banner = new Paragraph
            {
                Background = new SolidColorBrush(BrandDark),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 16),
                TextAlignment = TextAlignment.Center
            };
            banner.Inlines.Add(new Run("ChromExplorer — Method Report") { FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            banner.Inlines.Add(new LineBreak());
            banner.Inlines.Add(new Run($"Generated: {data.GeneratedDate}   |   User: {data.UserName}") { FontSize = 10.5, Foreground = Brushes.White });
            doc.Blocks.Add(banner);

            foreach (var section in data.Sections)
            {
                if (section.Rows.Count > 0)
                    AddSection(doc, section.Title, section.Rows);
            }

            return doc;
        }

        private static void AddSectionHeader(FlowDocument doc, string title)
        {
            doc.Blocks.Add(new Paragraph(new Run(title) { FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 12.5 })
            {
                Background = new SolidColorBrush(BrandLight),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 14, 0, 6)
            });
        }

        private static void AddSection(FlowDocument doc, string title, System.Collections.Generic.List<MethodReportRow> rows)
        {
            AddSectionHeader(doc, title);

            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 4) };
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.4, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = new TableRow { Background = i % 2 == 1 ? new SolidColorBrush(AltRow) : Brushes.Transparent };
                row.Cells.Add(LabelCell(rows[i].Label));
                row.Cells.Add(ValueCell(rows[i].Value));
                rowGroup.Rows.Add(row);
            }

            doc.Blocks.Add(table);
        }

        private static TableCell LabelCell(string text)
        {
            return new TableCell(new Paragraph(new Run(text ?? "") { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(BrandDark), FontSize = 10.5 }) { Margin = new Thickness(4, 2, 4, 2) })
            {
                BorderBrush = new SolidColorBrush(GridLine),
                BorderThickness = new Thickness(0.5)
            };
        }

        private static TableCell ValueCell(string text)
        {
            return new TableCell(new Paragraph(new Run(text ?? "")) { FontSize = 10.5, Margin = new Thickness(4, 2, 4, 2) })
            {
                BorderBrush = new SolidColorBrush(GridLine),
                BorderThickness = new Thickness(0.5)
            };
        }
    }
}
