using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Data.Reports
{
    public class DocxReportService
    {
        public void GenerateReport(string outputPath, List<CityAirQuality> data, string chartPath)
        {
            using (var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                main.Document = new Document(new Body());
                var body = main.Document.Body;

                body.Append(new Paragraph(new Run(new Text("ЗВІТ ПРО ЯКІСТЬ ПОВІТРЯ"))));
                body.Append(new Paragraph(new Run(new Text("Студент: Мілецький Ілля Валерійович"))));
                body.Append(new Paragraph(new Run(new Text("Варіант: 37"))));
                body.Append(new Paragraph(new Run(new Text("Джерело: https://www.kaggle.com/datasets/dnkumars/air-quality-index"))));
                body.Append(new Paragraph(new Run(new Text($"Дата: {DateTime.Now:dd.MM.yyyy}"))));
                body.Append(new Paragraph(new Run(new Break())));

                var table = new Table(new TableProperties(
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 6 },
                        new BottomBorder { Val = BorderValues.Single, Size = 6 },
                        new LeftBorder { Val = BorderValues.Single, Size = 6 },
                        new RightBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 6 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 6 }
                    )));

                var header = new TableRow(
                    new TableCell(new Paragraph(new Run(new Text("Rank")))),
                    new TableCell(new Paragraph(new Run(new Text("City / Country")))),
                    new TableCell(new Paragraph(new Run(new Text("Average AQI"))))
                );
                table.Append(header);

                foreach (var city in data.Take(20))
                {
                    if (city == null) continue;

                    table.Append(new TableRow(
                        new TableCell(new Paragraph(new Run(new Text(city.Rank.ToString())))),
                        new TableCell(new Paragraph(new Run(new Text(city.CityCountry ?? "")))),
                        new TableCell(new Paragraph(new Run(new Text(city.AverageAQI.ToString()))))
                    ));
                }

                body.Append(table);

                if (File.Exists(chartPath))
                {
                    var imgPart = main.AddImagePart(ImagePartType.Png);
                    using (var fs = new FileStream(chartPath, FileMode.Open))
                        imgPart.FeedData(fs);

                    string relId = main.GetIdOfPart(imgPart);

                    var drawing = new Drawing(
                        new DW.Inline(
                            new DW.Extent() { Cx = 6000000L, Cy = 3500000L },
                            new DW.EffectExtent(),
                            new DW.DocProperties() { Id = 1, Name = $"Chart" },
                            new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks() { NoChangeAspect = true }),
                            new A.Graphic(
                                new A.GraphicData(
                                    new PIC.Picture(
                                        new PIC.NonVisualPictureProperties(
                                            new PIC.NonVisualDrawingProperties() { Id = 0U, Name = "ChartImage.png" },
                                            new PIC.NonVisualPictureDrawingProperties()),
                                        new PIC.BlipFill(
                                            new A.Blip() { Embed = relId },
                                            new A.Stretch(new A.FillRectangle())),
                                        new PIC.ShapeProperties(
                                            new A.Transform2D(
                                                new A.Offset() { X = 0L, Y = 0L },
                                                new A.Extents() { Cx = 6000000L, Cy = 3500000L }),
                                            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
                                    )
                                )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                        )
                    );

                    body.Append(new Paragraph(new Run(drawing)));
                }
                body.Append(new Paragraph(new Run(new Break())));

                main.Document.Save();
            }
        }
    }
}