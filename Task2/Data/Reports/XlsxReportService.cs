using Domain;
using OfficeOpenXml;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Data.Reports
{
    public class XlsxReportService
    {
        public void GenerateReport(List<CityAirQuality> data, string filePath)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Illia");

            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Summary");

                ws.Cells[1, 1].Value = "City / Country";
                ws.Cells[1, 2].Value = "Average AQI";
                ws.Cells[1, 3].Value = "Min AQI";
                ws.Cells[1, 4].Value = "Max AQI";

                int row = 2;
                foreach (var city in data)
                {
                    ws.Cells[row, 1].Value = city.CityCountry;
                    ws.Cells[row, 2].Value = city.AverageAQI;
                    ws.Cells[row, 3].Value = city.MonthlyData.Min(m => m.Value);
                    ws.Cells[row, 4].Value = city.MonthlyData.Max(m => m.Value);
                    row++;
                }

                using (var rng = ws.Cells[1, 1, 1, 4])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                var avgRange = ws.Cells[2, 2, row - 1, 2];
                avgRange.ConditionalFormatting.AddTop().Rank = 3;

                File.WriteAllBytes(filePath, package.GetAsByteArray());
            }
        }
    }
}
