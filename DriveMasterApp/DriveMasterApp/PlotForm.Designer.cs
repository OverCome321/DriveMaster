using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using DriveMasterApp.Interfaces;
using System.IO.Ports;
using OxyPlot.WindowsForms;
using System.Collections.Concurrent;
using Timer = System.Windows.Forms.Timer;
using DriveMasterApp.Services;
using DriverMasterModels;
using DriveMasterApp.Utils;

namespace DriveMasterApp
{
    public partial class PlotForm : Form
    {
        private readonly IComPortConnection _comPortConnectionService;
        private readonly IComPortSend _comPortSendService;
        private readonly ConcurrentBag<(double elapsedTime, double value)> dataBag = new ConcurrentBag<(double, double)>();
        private DateTime startTime;
        private Timer timer;
        private double elapsedTime;
        private Label timerLabel;
        private FlowLayoutPanel legendPanel;

        public PlotForm(IComPortConnection comPortConnectionService, IComPortSend comPortSendService)
        {
            _comPortSendService = comPortSendService;
            _comPortConnectionService = comPortConnectionService;
            InitializeComponent();
            InitializeChart(); 
            StartReceivingData();
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += Form1_FormClosing;
        }

        private void InitializeComponent()
        {
            plotView = new OxyPlot.WindowsForms.PlotView();
            timerLabel = new Label();
            legendPanel = new FlowLayoutPanel();
            SuspendLayout();

            plotView.Dock = DockStyle.Fill;
            plotView.AutoSize = true;
            plotView.Name = "plotView";
            plotView.TabIndex = 0;

            timerLabel.AutoSize = true;
            timerLabel.ForeColor = System.Drawing.Color.Gray;
            timerLabel.Location = new System.Drawing.Point(10, 10);
            timerLabel.Font = new Font("Arial", 10, FontStyle.Regular);

            legendPanel.FlowDirection = FlowDirection.TopDown;
            legendPanel.AutoSize = true;
            legendPanel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            legendPanel.Location = new System.Drawing.Point(750, 78);
            legendPanel.Width = 3;
            legendPanel.BackColor = Color.Transparent;


            Controls.Add(plotView);
            Controls.Add(timerLabel);
            Controls.Add(legendPanel);
            Text = "График диагностики";
            ClientSize = new System.Drawing.Size(800, 600);
            timerLabel.BringToFront();
            legendPanel.BringToFront();
            ResumeLayout(false);
            PerformLayout();
        }
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                var formattedString = CommandsFormatting.GetCommandWithFormatting("Exit");
                await _comPortSendService.SendMessage(formattedString);
            }
            timer?.Stop();
            timer = null;
            _comPortConnectionService.GetPort().DataReceived -= DataReceivedHandler;
        }

        private PlotView plotView;
        private PlotModel plotModel;
        private List<LineSeries> lineSeriesList;

        private void InitializeChart()
        {
            // Сначала сбрасываем старые данные
            plotModel = new PlotModel
            {
                Title = "График диагностики",
                IsLegendVisible = false
            };

            plotModel.Axes.Clear(); // Убедитесь, что оси очищены
            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Время (секунды)",
                Minimum = 0,
            });

            plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Значение",
                Minimum = 0,
            });

            // Инициализируем список линейных серий
            lineSeriesList = new List<LineSeries>();
            plotView.Model = plotModel;

            // Добавляем легенду
            AddLegend();
        }

        private void AddLegend()
        {
            string[] labels = { "Value1", "Value2", "Value3", "Value4", "Value5" };
            for (int i = 0; i < labels.Length; i++)
            {
                Panel legendItem = new Panel { Width = 80, Height = 20, Padding = new Padding(7), Margin = new Padding(2), BackColor = Color.Transparent };
                PictureBox colorBox = new PictureBox { Width = 15, Height = 15, BackColor = Color.FromArgb(GetLineColor(i).R, GetLineColor(i).G, GetLineColor(i).B), Margin = new Padding(0, 3, 5, 3) };
                Label legendLabel = new Label { Text = labels[i], AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.Black, Location = new System.Drawing.Point(25, 0) };

                legendItem.Controls.Add(colorBox);
                legendItem.Controls.Add(legendLabel);
                legendPanel.Controls.Add(legendItem);
            }
        }

        public void StartReceivingData()
        {
            _comPortConnectionService.GetPort().DataReceived += DataReceivedHandler;
            elapsedTime = 0;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += TimerTick;
            timer.Start();
            startTime = DateTime.Now;
            UpdateTimerLabel();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            elapsedTime = Math.Round((DateTime.Now - startTime).TotalSeconds);
            UpdateTimerLabel();
        }

        private void UpdateTimerLabel()
        {
            timerLabel.Text = $"Старт: {startTime:HH:mm:ss} | Прошло: {elapsedTime} сек.";
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string incomingData = _comPortConnectionService.GetPort().ReadExisting();
                string[] values = incomingData.Split(',');

                double timeElapsed = (DateTime.Now - startTime).TotalSeconds;

                for (int i = 0; i < values.Length; i++)
                {
                    if (double.TryParse(values[i], out double value))
                    {
                        dataBag.Add((timeElapsed, value));
                        UpdateChart(i, timeElapsed, value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка получения данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateChart(int lineIndex, double elapsedTime, double value)
        {
            if (lineSeriesList.Count <= lineIndex)
            {
                var newLineSeries = new LineSeries
                {
                    Title = $"Value {lineIndex + 1}",
                    MarkerType = MarkerType.Circle,
                    Color = GetLineColor(lineIndex)
                };
                lineSeriesList.Add(newLineSeries);
                plotModel.Series.Add(newLineSeries);
            }

            lineSeriesList[lineIndex].Points.Add(new DataPoint(elapsedTime, value));
            plotView.InvalidatePlot(true);
        }

        private OxyColor GetLineColor(int lineIndex)
        {
            OxyColor[] colors = new OxyColor[]
            {
                OxyColor.FromRgb(255, 0, 0),  // Red
                OxyColor.FromRgb(0, 255, 0),  // Green
                OxyColor.FromRgb(0, 0, 255),  // Blue
                OxyColor.FromRgb(255, 165, 0), // Orange
                OxyColor.FromRgb(255, 255, 0)  // Yellow
            };
            return colors[lineIndex % colors.Length];
        }
    }
}
