using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ChromExplorer.Modules.Viewer.Services
{
    /// <summary>
    /// Builds an in-app, native WPF preview of the report from the same ChromatogramReportData
    /// that ChromatogramPdfExporter turns into a PDF. This is a separate, WPF-only renderer
    /// (not a rasterization of the PDF) because the installed PdfSharp version can only draw
    /// onto a PdfPage/XForm — it has no way to render onto an arbitrary bitmap for in-app
    /// display. Both renderers read the same DTO, so the content shown here is exactly what
    /// will be exported; only the rendering technology differs (FlowDocument vs. PdfSharp).
    /// </summary>
    public static class ReportPreviewDocumentBuilder
    {
        private static readonly Color BrandDark = Color.FromRgb(26, 74, 138);
        private static readonly Color BrandLight = Color.FromRgb(37, 99, 176);
        private static readonly Color AltRow = Color.FromRgb(240, 245, 251);
        private static readonly Color GridLine = Color.FromRgb(208, 216, 232);
        private static readonly Color StatusActiveText = Color.FromRgb(56, 87, 35);
        private static readonly Color StatusInactiveText = Color.FromRgb(166, 166, 166);

        /// <summary>Preview for a single file's report (not currently used by the multi-file
        /// "Add to Report" flow, kept for any single-shot use of the builder).</summary>
        public static FlowDocument Build(ChromatogramReportData data)
        {
            var doc = NewDocument();
            AddEntryContent(doc, data);
            AddSharedRsdSection(doc, data);
            AddSharedQuanSection(doc, data);
            return doc;
        }

        /// <summary>Preview for the queued multi-file report — one section per file, in the
        /// order they were added, each clearly divided so it's obvious how many files and which
        /// ones are included. The Quan calibration section (chart/equation/results table)
        /// reflects one shared calibration session, not any single file — every entry already
        /// carries an identical copy of it (see ChromatogramViewViewModel.BuildReportData), so
        /// repeating it per file was pure duplication. It's rendered exactly once, after every
        /// file, instead of once per file.</summary>
        public static FlowDocument BuildCombined(IList<ChromatogramReportData> entries)
        {
            var doc = NewDocument();

            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                    AddFileDivider(doc, i + 1, entries.Count, entries[i].FileName);

                AddEntryContent(doc, entries[i]);
            }

            if (entries.Count > 0)
            {
                AddSharedRsdSection(doc, entries[entries.Count - 1]);
                AddSharedQuanSection(doc, entries[entries.Count - 1]);
            }

            return doc;
        }

        /// <summary>Shown when nothing has been added to the report yet — deliberately blank
        /// rather than falling back to whatever file happens to be open in the viewer, since
        /// that file was never explicitly added and shouldn't leak into the report.</summary>
        public static FlowDocument BuildEmpty()
        {
            var doc = NewDocument();

            doc.Blocks.Add(new Paragraph(new Run("No files have been added to this report yet."))
            {
                FontSize = 14,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 80, 0, 6)
            });
            doc.Blocks.Add(new Paragraph(new Run("Process a file, then use \"Add to Report\" before generating."))
            {
                FontSize = 11.5,
                Foreground = Brushes.Gray,
                TextAlignment = TextAlignment.Center
            });

            return doc;
        }

        private static FlowDocument NewDocument()
        {
            return new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5,
                PagePadding = new Thickness(36),
                ColumnWidth = 3000, // effectively disables FlowDocument's newspaper-style columns
                TextAlignment = TextAlignment.Left
            };
        }

        private static void AddFileDivider(FlowDocument doc, int index, int total, string fileName)
        {
            doc.Blocks.Add(new Paragraph(new Run($"File {index} of {total} — {fileName}"))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(BrandDark),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 30, 0, 10),
                TextAlignment = TextAlignment.Center
            });
        }

        private static void AddEntryContent(FlowDocument doc, ChromatogramReportData data)
        {
            AddTitleBanner(doc, data);
            AddDescription(doc, data);

            AddImageSection(doc, "Chromatogram", data.ChromatogramImage);
            AddImageSection(doc, "Mass Spectrum", data.SpectrumImage);

            AddSideBySideKeyValueSection(doc,
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

            AddDataTable(doc, "Detected Chromatogram Peaks",
                new[] { "#", "Retention Time", "Label" },
                new[] { 0.5, 1.5, 3.0 },
                data.ChromPeakRows);

            AddDataTable(doc, "Spectrum Peak List",
                new[] { "#", "m/z", "Intensity" },
                new[] { 0.6, 1.5, 1.5 },
                data.SpectrumPeakRows);

            AddPostProcessingSection(doc, "Spectrum Post-Processing Operations", data.SpectrumPostProcessing);
            AddPostProcessingSection(doc, "EIC Post-Processing Operations", data.EicPostProcessing);

            AddSideBySideKeyValueSection(doc,
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

        /// <summary>%RSD-by-sample summary (same table as the RSD tab) — rendered once for the
        /// whole report, immediately before the Quan section, not once per file, since every
        /// entry already carries an identical copy of the same shared RSD queue.</summary>
        private static void AddSharedRsdSection(FlowDocument doc, ChromatogramReportData data)
        {
            AddDataTable(doc, "%RSD by Concentration",
                new[] { "Concentration", "N", "Mean Area", "Std Dev", "%RSD", "Note" },
                new[] { 1.6, 0.5, 1.0, 1.0, 0.8, 1.2 },
                data.RsdRows);

            AddDataTable(doc, "Queued Files",
                new[] { "#", "Sample ID", "Concentration", "Area", "File", "Added" },
                new[] { 0.3, 1.2, 1.0, 0.8, 2.6, 1.3 },
                data.RsdQueuedRows);
        }

        /// <summary>Quan calibration section (chart, equation, results table) — rendered once
        /// for the whole report (see BuildCombined), not once per file, since every entry
        /// already carries an identical copy of the same shared calibration session.</summary>
        private static void AddSharedQuanSection(FlowDocument doc, ChromatogramReportData data)
        {
            AddSectionHeader(doc, "Quantitation (Quan) — Calibration Curve");
            AddImage(doc, data.QuanChartImage, 260);
            doc.Blocks.Add(new Paragraph(new Run("Calibration Equation: " + (data.QuanEquation ?? "N/A")))
            {
                FontSize = 11.5,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 4, 0, 12)
            });

            AddDataTable(doc, "Quan Sample Results",
                new[] { "#", "Sample", "Sample Type", "Exp Amt", "Area", "Calc Amt", "% Recovery", "% Diff", "File" },
                new[] { 0.4, 1.2, 1.0, 0.9, 0.9, 0.9, 1.0, 0.9, 2.4 },
                data.QuanRows);
        }

        // ── Sections ────────────────────────────────────────────────────────────

        private static void AddTitleBanner(FlowDocument doc, ChromatogramReportData data)
        {
            var banner = new Paragraph
            {
                Background = new SolidColorBrush(BrandDark),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10),
                TextAlignment = TextAlignment.Center
            };
            banner.Inlines.Add(new Run("ChromExplorer — Analysis Report") { FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White });
            banner.Inlines.Add(new LineBreak());
            banner.Inlines.Add(new Run($"File: {data.FileName}   |   Generated: {data.GeneratedDate}") { FontSize = 10.5, Foreground = Brushes.White });
            banner.Inlines.Add(new LineBreak());
            banner.Inlines.Add(new Run($"Instrument: {data.InstrumentName}   |   User: {data.UserName}") { FontSize = 10.5, Foreground = Brushes.White });

            if (!string.IsNullOrWhiteSpace(data.ReviewedByName))
            {
                banner.Inlines.Add(new LineBreak());
                string reviewedLine = string.IsNullOrWhiteSpace(data.ReviewedDate)
                    ? $"Reviewed by: {data.ReviewedByName}"
                    : $"Reviewed by: {data.ReviewedByName}   |   Reviewed on: {data.ReviewedDate}";
                banner.Inlines.Add(new Run(reviewedLine) { FontSize = 10.5, Foreground = Brushes.White });
            }

            doc.Blocks.Add(banner);
        }

        private static void AddDescription(FlowDocument doc, ChromatogramReportData data)
        {
            doc.Blocks.Add(new Paragraph(new Run("Description: " + (data.Description ?? "")))
            {
                FontSize = 11.5,
                Foreground = Brushes.DimGray,
                Margin = new Thickness(0, 0, 0, 12)
            });
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

        private static void AddImageSection(FlowDocument doc, string title, byte[] imageBytes)
        {
            AddSectionHeader(doc, title);
            AddImage(doc, imageBytes, 220);
        }

        private static void AddImage(FlowDocument doc, byte[] imageBytes, double height)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.75),
                Background = Brushes.White,
                Height = height,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var image = FromBytes(imageBytes);
            if (image != null)
            {
                border.Child = new Image { Source = image, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
            }
            else
            {
                border.Child = new TextBlock
                {
                    Text = "Plot Not Available",
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            doc.Blocks.Add(new BlockUIContainer(border));
        }

        private static void AddSideBySideKeyValueSection(FlowDocument doc, string captionLeft, (string label, string value)[] rowsLeft,
            string captionRight, (string label, string value)[] rowsRight)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            for (int i = 0; i < 4; i++)
                table.Columns.Add(new TableColumn { Width = i % 2 == 0 ? new GridLength(1.1, GridUnitType.Star) : new GridLength(1.4, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var captionRow = new TableRow();
            captionRow.Cells.Add(CaptionCell(captionLeft, 2));
            captionRow.Cells.Add(CaptionCell(captionRight, 2));
            rowGroup.Rows.Add(captionRow);

            int rowCount = Math.Max(rowsLeft.Length, rowsRight.Length);
            for (int i = 0; i < rowCount; i++)
            {
                var row = new TableRow { Background = i % 2 == 1 ? new SolidColorBrush(AltRow) : Brushes.Transparent };

                (string label, string value) left = i < rowsLeft.Length ? rowsLeft[i] : ("", "");
                (string label, string value) right = i < rowsRight.Length ? rowsRight[i] : ("", "");

                row.Cells.Add(LabelCell(left.label));
                row.Cells.Add(ValueCell(left.value));
                row.Cells.Add(LabelCell(right.label));
                row.Cells.Add(ValueCell(right.value));

                rowGroup.Rows.Add(row);
            }

            doc.Blocks.Add(table);
        }

        private static void AddPostProcessingSection(FlowDocument doc, string subHeader, List<PdfKeyValueRow> rows)
        {
            if (rows == null || rows.Count == 0) return;

            AddSectionHeader(doc, subHeader);

            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            foreach (var pRow in rows)
            {
                var row = new TableRow();
                row.Cells.Add(LabelCell(pRow.Label, background: Color.FromRgb(233, 238, 247)));

                if (pRow.IsStatusCell)
                {
                    var run = new Run(pRow.Value) { FontWeight = pRow.IsActive ? FontWeights.Bold : FontWeights.Normal, Foreground = new SolidColorBrush(pRow.IsActive ? StatusActiveText : StatusInactiveText) };
                    var p = new Paragraph(run) { TextAlignment = TextAlignment.Center, Margin = new Thickness(4, 2, 4, 2) };
                    row.Cells.Add(new TableCell(p) { BorderBrush = new SolidColorBrush(GridLine), BorderThickness = new Thickness(0.5) });
                }
                else
                {
                    row.Cells.Add(ValueCell(pRow.Value));
                }

                rowGroup.Rows.Add(row);
            }

            doc.Blocks.Add(table);
        }

        private static void AddDataTable(FlowDocument doc, string title, string[] headers, double[] colWeights, List<string[]> rows)
        {
            AddSectionHeader(doc, title);

            if (rows == null || rows.Count == 0)
            {
                doc.Blocks.Add(new Paragraph(new Run("No data available") { FontStyle = FontStyles.Italic, Foreground = Brushes.Gray })
                {
                    Margin = new Thickness(0, 0, 0, 10)
                });
                return;
            }

            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var w in colWeights)
                table.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });

            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);

            var headerRow = new TableRow { Background = new SolidColorBrush(BrandDark) };
            foreach (var h in headers)
            {
                headerRow.Cells.Add(new TableCell(new Paragraph(new Run(h ?? "") { FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 11 }) { Margin = new Thickness(4, 3, 4, 3) }));
            }
            rowGroup.Rows.Add(headerRow);

            int idx = 0;
            foreach (var dataRow in rows)
            {
                var row = new TableRow { Background = idx % 2 == 1 ? new SolidColorBrush(AltRow) : Brushes.Transparent };
                for (int c = 0; c < headers.Length; c++)
                {
                    string cellText = c < dataRow.Length ? dataRow[c] ?? "" : "";
                    row.Cells.Add(new TableCell(new Paragraph(new Run(cellText)) { FontSize = 10.5, Margin = new Thickness(4, 2, 4, 2) })
                    {
                        BorderBrush = new SolidColorBrush(GridLine),
                        BorderThickness = new Thickness(0, 0, 0, 0.5)
                    });
                }
                rowGroup.Rows.Add(row);
                idx++;
            }

            doc.Blocks.Add(table);
        }

        // ── Cell helpers ────────────────────────────────────────────────────────

        private static TableCell CaptionCell(string text, int columnSpan)
        {
            return new TableCell(new Paragraph(new Run(text) { FontWeight = FontWeights.Bold, Foreground = Brushes.White, FontSize = 11.5 }) { Margin = new Thickness(6, 3, 6, 3) })
            {
                Background = new SolidColorBrush(BrandLight),
                ColumnSpan = columnSpan
            };
        }

        private static TableCell LabelCell(string text, Color? background = null)
        {
            return new TableCell(new Paragraph(new Run(text ?? "") { FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(BrandDark), FontSize = 10.5 }) { Margin = new Thickness(4, 2, 4, 2) })
            {
                BorderBrush = new SolidColorBrush(GridLine),
                BorderThickness = new Thickness(0.5),
                Background = background.HasValue ? new SolidColorBrush(background.Value) : Brushes.Transparent
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

        private static BitmapImage FromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                using (var ms = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
