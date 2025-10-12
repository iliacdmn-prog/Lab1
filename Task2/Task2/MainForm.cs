using Data;
using Data.Providers;
using Data.Reports;
using DocumentFormat.OpenXml.Bibliography;
using Domain;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows.Forms;
using System.Xml.Linq;
using UI;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lab1._2
{
    public partial class MainForm : Form
    {
        DocxReportService docxReportService = new DocxReportService();
        CityAirQualityManager cityAirQualityManager = new CityAirQualityManager();
        private IProvider _m = new CsvProvider();
        XlsxReportService xlsxReportService = new XlsxReportService();
        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        private string _path = "";

        public MainForm()
        {
            InitializeComponent();
            LogMessage("�������� ��������");
        }

        private void LogMessage(string message)
        {
            if (textBoxLogs.InvokeRequired)
            {
                textBoxLogs.Invoke(new Action(() => LogMessage(message)));
            }
            else
            {
                textBoxLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                textBoxLogs.SelectionStart = textBoxLogs.Text.Length;
                textBoxLogs.ScrollToCaret();
            }
        }

        private void updateDataGridViewData(List<CityAirQuality> data)
        {
            LogMessage("��������� ������� �����...");
            dataGridViewData.Columns.Clear();
            dataGridViewData.Rows.Clear();

            dataGridViewData.Columns.Add("Rank", "Rank");
            dataGridViewData.Columns.Add("CityCountry", "City / Country");
            dataGridViewData.Columns.Add("AverageAQI", "Average AQI");

            foreach (var month in months)
            {
                dataGridViewData.Columns.Add(month, month);
            }

            foreach (var city in data)
            {
                var row = new List<object>
                {
                    city.Rank,
                    city.CityCountry,
                    city.AverageAQI
                };

                foreach (var month in months)
                {
                    var record = city.MonthlyData.FirstOrDefault(m => m.Month == month);
                    row.Add(record != null ? record.Value : (object)"-");
                }

                dataGridViewData.Rows.Add(row.ToArray());
            }

            dataGridViewData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dataGridViewData.AllowUserToAddRows = false;
            dataGridViewData.ReadOnly = false;

            LogMessage($"������� ��������: {data.Count} �����");
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            LogMessage("���������� ���������� ������");
            List<CityAirQuality> data = cityAirQualityManager.cityAirQualities;
            if (data == null || data.Count == 0)
            {
                LogMessage("������ ����������� ������ ��� �����");
                return;
            }

            string field = comboBoxField.SelectedItem?.ToString();
            string condition = comboBoxCondition.SelectedItem?.ToString();
            string input = textBoxValue.Text.Trim();

            if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(input))
            {
                MessageBox.Show("���� �����, ������� ���� �� ������ ��������.");
                LogMessage("������ ����������� ������ ��� �������� �����");
                return;
            }

            bool isNumber = int.TryParse(input, out int numericValue);

            if (field == "City")
            {
                data = data.Where(c => c.CityCountry.Split(", ")[0] == input).ToList();
            }
            else if (field == "Country")
            {
                data = data.Where(c => c.CityCountry.Split(", ")[1] == input).ToList();
            }
            else
            {
                if (!isNumber)
                {
                    MessageBox.Show("��� ����� ���� ������� ������ ������� ��������.");
                    LogMessage("�������: ���������� ������� �������� � ������");
                    return;
                }
                if (string.IsNullOrEmpty(condition))
                {
                    MessageBox.Show("�� ������ �����!");
                    LogMessage("�������: �������� ����� � ������");
                    return;
                }
                switch (field)
                {
                    case "Rank":
                        data = applyCondition(condition, data, numericValue, c => c.Rank);
                        break;
                    case "AverageAQI":
                        data = applyCondition(condition, data, numericValue, c => c.AverageAQI);
                        break;
                }
            }

            updateDataGridViewData(data.ToList());
            LogMessage($"Գ���� �����������: ����={field}, �����={condition}, ��������={input}. ����� ���� ����������: {data.Count}");
        }

        private List<CityAirQuality> applyCondition(string condition, List<CityAirQuality> data, int value, Func<CityAirQuality, int> selector)
        {
            return condition switch
            {
                ">" => data.Where(c => selector(c) > value).ToList(),
                "<" => data.Where(c => selector(c) < value).ToList(),
                "=" => data.Where(c => selector(c) == value).ToList(),
                "!=" => data.Where(c => selector(c) != value).ToList(),
                ">=" => data.Where(c => selector(c) >= value).ToList(),
                "<=" => data.Where(c => selector(c) <= value).ToList(),
                _ => data,
            };
        }

        private void comboBoxField_SelectedIndexChanged(object sender, EventArgs e)
        {
            string field = comboBoxField.SelectedItem?.ToString();
            comboBoxCondition.Enabled = field != "City" & field != "Country";
            LogMessage($"���������� ������ ���� ��� ����������: {field}");
        }

        private void �������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMessage("���������� ������ ����� ������ �����");
            var dialog = new OpenFileDialog();
            dialog.Filter = $"CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|XML files (*.xml)|*.xml|XLSX files (*.xlsx)|*.xlsx";

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                _path = dialog.FileName;
                string extension = Path.GetExtension(_path).ToLower().TrimStart('.');
                MessageBox.Show(_path);
                List<CityAirQuality> data = _m.Read(_path);

                if (data == null || data.Count == 0)
                {
                    MessageBox.Show("���� ������ ��� ��� �� ����������.");
                    LogMessage("������ �������� ������� ��� ������ �����");
                    return;
                }

                setManager(extension);
                ImportPreview importPreview = new ImportPreview(data);
                DialogResult dialog1 = importPreview.ShowDialog();
                if (dialog1 == DialogResult.OK)
                {
                    cityAirQualityManager.cityAirQualities = data;
                    updateDataGridViewData(cityAirQualityManager.cityAirQualities);
                    LogMessage($"���� �����������: {_path} ({extension.ToUpper()})");
                }
                else
                {
                    LogMessage($"���� �� ���� �����������: {_path} ({extension.ToUpper()})");
                }
            }
            else
            {
                LogMessage("���� ����� ��������� ������������");
            }
        }

        private void �����ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LogMessage("���������� ���������� ����� � ��������");
            var result = MessageBox.Show("�� �������, �� ������ �����?", "ϳ�����������", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                LogMessage("�������� ��������� ������");
                Application.Exit();
            }
            else
            {
                LogMessage("����� � �������� ��������� ������������");
            }
        }

        private void buttonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var extension = (sender as ToolStripMenuItem).Tag.ToString();
            LogMessage($"���������� ������ ���� � ������ {extension.ToUpper()}");

            var dialog = new SaveFileDialog();
            dialog.Filter = $"{char.ToUpper(extension[0]) + extension.Substring(1).ToLower()} files (*.{extension})|*.{extension}";

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                string path = dialog.FileName;
                setManager(extension);
                _m.Write(path, cityAirQualityManager.cityAirQualities);
                LogMessage($"���� ���������: {path} ({extension.ToUpper()})");
            }
            else
            {
                LogMessage($"���������� ����� � ������ {extension.ToUpper()} ��������� ������������");
            }
        }

        private void setManager(string extension)
        {
            switch (extension)
            {
                case "csv":
                    _m = new CsvProvider();
                    break;
                case "json":
                    _m = new JsonProvider();
                    break;
                case "xml":
                    _m = new XmlProvider();
                    break;
                case "xlsx":
                    _m = new XlsxProvider();
                    break;
            }
            LogMessage($"�������� ����� �����������: {extension.ToUpper()}");
        }

        private void editCity(object sender, DataGridViewCellEventArgs e)
        {
            LogMessage("���������� ������ ���� � ����");
            
                foreach (DataGridViewRow row in dataGridViewData.Rows)
                {
                    if (row.IsNewRow) continue;
                    try
                    {
                        var city = new CityAirQuality
                        {
                            Rank = int.TryParse(row.Cells["Rank"].Value?.ToString(), out int rank) ? rank : 0,
                            CityCountry = $"{row.Cells["CityCountry"].Value?.ToString()}",
                            AverageAQI = int.TryParse(row.Cells["AverageAQI"].Value?.ToString(), out int avg) ? avg : 0
                        };

                        foreach (var m in months)
                        {
                            if (dataGridViewData.Columns.Contains(m))
                            {
                                int val = int.TryParse(row.Cells[m].Value?.ToString(), out int v) ? v : 0;
                                city.MonthlyData.Add(new MonthlyAQI { Month = m, Value = val });
                            }
                        }

                        cityAirQualityManager.editCity(city);
                    }
                    catch (Exception ex)
                    {
                        LogMessage("������� ��� ���������� �����: " + ex.Message);
                        throw new Exception($"Error reading data from table: {ex.Message}");
                    }
                }
        }


        private void buttonApplyChart_Click(object sender, EventArgs e)
        {
            LogMessage("���������� ���� �������");
            int chartType = cmbChartType.SelectedIndex;
            string searchedText = textBoxCityCountry.Text?.Trim();
            int chartVolume = comboBoxVolume.SelectedIndex;
            if (chartType == -1)
            {
                MessageBox.Show("������� ��� �������.");
                LogMessage("�������: �� ������� ��� �������");
                return;
            }
            if (chartVolume == -1)
            {
                MessageBox.Show("������� ��'�� �������.");
                LogMessage("�������: �� ������� ��'�� �������");
                return;
            }

            PlotModel model = null;

            var city = cityAirQualityManager.cityAirQualities.FirstOrDefault(c => c.CityCountry == searchedText);

            switch (chartType)
            {
                case 0:
                    if (city == null)
                    {
                        MessageBox.Show($"̳��� '{searchedText}' �� ��������.");
                        LogMessage("�������: ���� �� ��������");
                        return;
                    }
                    model = CreateLineChart(city);
                    break;
                case 1:
                    if (city == null)
                    {
                        MessageBox.Show($"̳��� '{searchedText}' �� ��������.");
                        LogMessage("�������: ���� �� ��������");
                        return;
                    }
                    model = CreateBarChart(city);
                    break;
                case 2:
                    if (chartVolume == 0)
                    {
                        model = CreatePieChart("");
                    }
                    if (chartVolume == 1)
                    {
                        model = CreatePieChart(searchedText);
                    }
                    break;
            }

            plotView1.Model = model;
            LogMessage($"ĳ������ ����������. ���={chartType}");
        }

        private PlotModel CreateLineChart(CityAirQuality city)
        {
            LogMessage($"��������� ����� ������� ��� {city.CityCountry}");
            var model = new PlotModel();

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom };
            categoryAxis.Labels.AddRange(city.MonthlyData.Select(m => m.Month));
            model.Axes.Add(categoryAxis);

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "AQI" });

            var series = new LineSeries { MarkerType = MarkerType.Circle };
            foreach (var m in city.MonthlyData)
                series.Points.Add(new DataPoint(categoryAxis.Labels.IndexOf(m.Month), m.Value));

            model.Series.Add(series);
            return model;
        }

        private PlotModel CreateBarChart(CityAirQuality city)
        {
            LogMessage($"��������� ����������� ������� ��� {city.CityCountry}");
            var model = new PlotModel();

            var categoryAxis = new CategoryAxis { Position = AxisPosition.Left };
            categoryAxis.Labels.AddRange(city.MonthlyData.Select(m => m.Month));
            model.Axes.Add(categoryAxis);

            model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "AQI" });

            var series = new BarSeries
            {
                LabelPlacement = LabelPlacement.Inside,
                LabelFormatString = "{0}"
            };

            foreach (var m in city.MonthlyData)
            {
                series.Items.Add(new BarItem { Value = m.Value });
            }

            model.Series.Add(series);
            return model;
        }

        private PlotModel CreatePieChart(string city)
        {
            LogMessage("��������� ������� ������� ��� �����");
            var model = new PlotModel();

            var pieSeries = new PieSeries
            {
                StrokeThickness = 0.5,
                InsideLabelPosition = 0.5,
                AngleSpan = 360,
                StartAngle = 0,
                OutsideLabelFormat = "{0}: {1} ({2:0.0}%)",
                InsideLabelFormat = ""
            };
            Dictionary<string, double> AirQualities;

            if (city == "")
            {
                AirQualities = countAverageAQIForCountries(cityAirQualityManager.cityAirQualities);
            }
            else
            {
                AirQualities = countAverageAQIForCountry(cityAirQualityManager.cityAirQualities, city);
            }


            foreach (var keyValue in AirQualities)
            {
                pieSeries.Slices.Add(new PieSlice(keyValue.Key, keyValue.Value));
            }

            model.Series.Add(pieSeries);
            return model;
        }

        private Dictionary<string, double> countAverageAQIForCountries(List<CityAirQuality> data)
        {
            Dictionary<string, double> countriesAirQualities = new Dictionary<string, double>();
            Dictionary<string, int> countriesCounts = new Dictionary<string, int>();

            foreach (var cityAirQuality in data)
            {
                string[] parts = cityAirQuality.CityCountry.Split(", ");
                string country = parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();


                if (countriesAirQualities.ContainsKey(country))
                {
                    countriesAirQualities[country] += cityAirQuality.AverageAQI;
                    countriesCounts[country]++;
                }
                else
                {
                    countriesAirQualities[country] = cityAirQuality.AverageAQI;
                    countriesCounts[country] = 1;
                }
            }

            foreach (var country in countriesAirQualities.Keys.ToList())
            {
                countriesAirQualities[country] /= countriesCounts[country];
            }
            return countriesAirQualities;
        }

        private Dictionary<string, double> countAverageAQIForCountry(List<CityAirQuality> data, string countryName)
        {
            Dictionary<string, double> countryAirQualities = new Dictionary<string, double>();
            Dictionary<string, int> countryCounts = new Dictionary<string, int>();

            foreach (var cityAirQuality in data)
            {
                string[] parts = cityAirQuality.CityCountry.Split(", ");
                string country = parts[1].Trim();
                string city = parts[0].Trim();

                if (country == countryName & countryAirQualities.ContainsKey(country))
                {
                    countryAirQualities[city] += cityAirQuality.AverageAQI;
                    countryCounts[city]++;
                }
                else if (country == countryName)
                {
                    countryAirQualities[city] = cityAirQuality.AverageAQI;
                    countryCounts[city] = 1;
                }
            }

            foreach (var city in countryAirQualities.Keys.ToList())
            {
                countryAirQualities[city] /= countryCounts[city];
            }
            return countryAirQualities;
        }

        private void cmbChartType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbChartType.SelectedIndex == 2)
            {
                textBoxCityCountry.Enabled = false;
                comboBoxVolume.Enabled = true;
            }
            else
            {
                textBoxCityCountry.Enabled = true;
                textBoxCityCountry.PlaceholderText = "̳���, �����";
                comboBoxVolume.Enabled = false;
            }
            LogMessage($"������� ��� �������: {cmbChartType.SelectedIndex}");
        }


        private void comboBoxVolume_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxVolume.SelectedIndex == 1)
            {
                textBoxCityCountry.PlaceholderText = "�����";
                textBoxCityCountry.Enabled = true;
            }
            else
            {
                textBoxCityCountry.Enabled = false;
            }
        }
        private void buttonChartExport_Click(object sender, EventArgs e)
        {
            LogMessage("���������� �������� �������");
            if (plotView1.Model == null)
            {
                MessageBox.Show("���� ������� ��� ��������.");
                LogMessage("�������: ������ �������� ��� �������");
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "PNG Image (*.png)|*.png";
                dialog.Title = "�������� ������� �� PNG";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = dialog.FileName;
                        plotView1.Model.Background = OxyColors.White;
                        var pngExporter = new PngExporter { Width = 800, Height = 600 };
                        pngExporter.ExportToFile(plotView1.Model, filePath);

                        MessageBox.Show($"ĳ������ ���������: {filePath}");
                        LogMessage($"ĳ������ ������������ � PNG: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������� ��� ���������: " + ex.Message);
                        LogMessage("������� �������� �������: " + ex.Message);
                    }
                }
                else
                {
                    LogMessage("������� ������� ��������� ������������");
                }
            }
        }

        private void ������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cityAirQualityManager.cityAirQualities == null || cityAirQualityManager.cityAirQualities.Count == 0)
            {
                MessageBox.Show("��� ������. ���������� ���� ����� ���������� ����.");
                LogMessage("������ ����������� XLSX-��� ��� �����");
                return;
            }

            LogMessage("���������� ����� ��������� XLSX-����");

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel ����� (*.xlsx)|*.xlsx";
                dialog.Title = "�������� XLSX ���";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = dialog.FileName;

                        xlsxReportService.GenerateReport(
                            cityAirQualityManager.cityAirQualities,
                            filePath
                        );

                        MessageBox.Show($"XLSX-��� ������ ���������: {filePath}");
                        LogMessage($"XLSX ��� �����������: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������� ��� ��������� XLSX ����: " + ex.Message);
                        LogMessage("������� ��������� XLSX ����: " + ex.Message);
                    }
                }
                else
                {
                    LogMessage("��������� XLSX-���� ��������� ������������");
                }
            }
        }

        private void �����������DOCX���ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cityAirQualityManager.cityAirQualities == null || cityAirQualityManager.cityAirQualities.Count == 0)
            {
                MessageBox.Show("��� ������. ���������� ���� ����� ���������� ����.");
                LogMessage("������ ����������� DOCX-��� ��� �����");
                return;
            }

            LogMessage("���������� ����� ��������� DOCX-����");

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Word ��������� (*.docx)|*.docx";
                dialog.Title = "�������� DOCX ���";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = dialog.FileName;

                        plotView1.Model = CreatePieChart("");
                        string chartPath = $"./Chart_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.png";
                        plotView1.Model.Background = OxyColors.White;
                        var pngExporter = new PngExporter { Width = 800, Height = 600 };
                        pngExporter.ExportToFile(plotView1.Model, chartPath);
                        plotView1.Model = null;

                        docxReportService.GenerateReport(
                            filePath,
                            cityAirQualityManager.cityAirQualities,
                            chartPath
                        );

                        MessageBox.Show($"DOCX-��� ������ ���������: {filePath}");
                        LogMessage($"DOCX ��� �����������: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������� ��� ��������� DOCX ����: " + ex.Message);
                        LogMessage("������� ��������� DOCX ����: " + ex.Message);
                    }
                }
                else
                {
                    LogMessage("��������� DOCX-���� ��������� ������������");
                }
            }
        }

        private void buttonRemove_Click(object sender, EventArgs e)
        {
            LogMessage("���������� ���������� �������� ����");
            if (dataGridViewData.SelectedRows.Count == 0)
            {
                MessageBox.Show("������ ���� ��� ���������!");
                LogMessage("�������: ������ �������� ���� ��� ������");
                return;
            }
            string cityCountry = dataGridViewData.SelectedRows[0].Cells[1].Value.ToString();
            cityAirQualityManager.removeCity(cityCountry);
            updateDataGridViewData(cityAirQualityManager.cityAirQualities);
            string extension = Path.GetExtension(_path).ToLower().TrimStart('.');
            setManager(extension);
            _m.Write(_path, cityAirQualityManager.cityAirQualities);
            MessageBox.Show("̳��� ���� ��������!");
            LogMessage($"{cityCountry} ��������");
        }

        private void buttonLogExport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxLogs.Text))
            {
                MessageBox.Show("���� ������, ����� ������������.");
                LogMessage("������ ������������ ���� ����");
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt";
                dialog.Title = "�������� ���� � TXT";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string filePath = dialog.FileName;
                        var textProvider = new TextProvider();
                        textProvider.Write(filePath, textBoxLogs.Text);

                        MessageBox.Show($"���� ������ ���������: {filePath}");
                        LogMessage($"���� ������������ � ����: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("������� ��� ������� ����: " + ex.Message);
                        LogMessage("������� �������� ����: " + ex.Message);
                    }
                }
                else
                {
                    LogMessage("������� ���� ��������� ������������");
                }
            }
        }

    }
}

