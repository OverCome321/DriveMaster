using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using DriveMasterApp.Interfaces;
using System.IO.Ports;
using OxyPlot.WindowsForms;
using System.Collections.Concurrent;
using Timer = System.Windows.Forms.Timer;
using DriveMasterApp.Utils;
using OxyPlot.Legends;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DriveMasterApp
{
    public partial class PlotForm : Form
    {
        #region Fields
        private readonly IComPortConnection _comPortConnectionService;
        private readonly IComPortSend _comPortSendService;
        private readonly ConcurrentBag<(double elapsedTime, double value)> dataBag = new ConcurrentBag<(double, double)>();
        private DateTime startTime;
        private Timer timer;
        private double elapsedTime;
        private Label timerLabel;
        private PlotView plotView;
        private PlotModel plotModel;
        private List<LineSeries> lineSeriesList;
        private long _countLines = 1;
        private Thread threadRead;
        private bool _isCanThreadStop = false;
        private bool isThreadStop = false;
        #endregion
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
        #region Init
        /// <summary>
        /// Метод инициализирует объекты и отображения их на форме
        /// </summary>
        private void InitializeComponent()
        {
            plotView = new OxyPlot.WindowsForms.PlotView();
            timerLabel = new Label();
            SuspendLayout();

            plotView.Dock = DockStyle.Fill;
            plotView.AutoSize = true;
            plotView.Name = "plotView";
            plotView.TabIndex = 0;

            timerLabel.AutoSize = true;
            timerLabel.ForeColor = System.Drawing.Color.Gray;
            timerLabel.Location = new System.Drawing.Point(10, 10);
            timerLabel.Font = new Font("Arial", 10, FontStyle.Regular);

            Controls.Add(plotView);
            Controls.Add(timerLabel);
            ClientSize = new System.Drawing.Size(800, 600);
            timerLabel.BringToFront();
            ResumeLayout(false);
            PerformLayout();
        }
        /// <summary>
        /// Метод инициализирует график на экране
        /// </summary>
        private void InitializeChart()
        {
            plotModel = new PlotModel
            {
                Title = "График диагностики"
            };

            // Создаем и настраиваем легенду
            var legend = new Legend
            {
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorder = OxyColors.Black,
                LegendBackground = OxyColor.FromArgb(200, 255, 255, 255)
            };
            plotModel.Legends.Add(legend);

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
        }
        /// <summary>
        /// Метод инициализирует процесс получения данных при открытии окна
        /// </summary>
        public void StartReceivingData()
        {
            elapsedTime = 0;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += TimerTick;
            timer.Start();
            startTime = DateTime.Now;
            UpdateTimerLabel();
            threadRead = new Thread(new ThreadStart(CheckPort));
            threadRead.Start();
        }
        #endregion
        #region Event
        /// <summary>
        /// Событие привязанное к получению ответа от com порт, при получении ответа он парсится и отображается на графике
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckPort()
        {
            try
            {
                string incomingData = _comPortConnectionService.GetPort().ReadExisting();
                if (incomingData == "") return;
                Debug.WriteLine(incomingData);

                // Разделяем строку по пробелам и запятым, а затем удаляем лишние пробелы
                string[] lines = incomingData.Split("\r");

                for(int i = 0; i < lines.Length-1; i++)
                {
                    _countLines++;
                    Debug.WriteLine("Количество интераций = " + _countLines.ToString());
                    string[] values = lines[i].Split(' ');

                    if (values.Length != 7) continue;

                    for (int j = 0; j < values.Length; j++)
                    {
                        // Пробуем распарсить каждое значение как число с плавающей точкой
                        if (int.TryParse(values[j], NumberStyles.Any, CultureInfo.InvariantCulture, out int value))
                        {
                            // Добавляем значение в dataBag (если нужно)
                            //dataBag.Add((_countLines, value));

                            // Обновляем график (если нужно)
                            UpdateChart(j, _countLines, value > 3000 ? 3000: value);
                        }
                        else
                        {
                            // Если значение не удалось распарсить, выводим сообщение в отладочную консоль
                            Debug.WriteLine($"Ошибка парсинга: {values[j]}");
                        }
                    }
                }


                //double timeElapsed = (DateTime.Now - startTime).TotalSeconds;


            }
            catch (Exception ex)
            {
                // Обрабатываем исключения и показываем сообщение об ошибке
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка получения данных", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                //this.Invoke(() =>
                //{
                //    if (isThreadStop)
                //        _isCanThreadStop = true;

                //});
                //if (!_isCanThreadStop)
                    CheckPort();
            }
        }
        /// <summary>
        /// Событие привязанное к тику времени, увеличивает время на экране на +1 секунду
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerTick(object sender, EventArgs e)
        {
            elapsedTime = Math.Round((DateTime.Now - startTime).TotalSeconds);
            UpdateTimerLabel();
        }
        #endregion
        #region Methods
        /// <summary>
        /// Метод для обновления времени на экране
        /// </summary>
        private void UpdateTimerLabel()
        {
            timerLabel.Text = $"Старт: {startTime:HH:mm:ss} | Прошло: {elapsedTime} сек.";
        }
        /// <summary>
        /// Метод обновляет информацию о значения на графике в момент получения данных от com порта 
        /// </summary>
        /// <param name="lineIndex"></param>
        /// <param name="elapsedTime"></param>
        /// <param name="value"></param>
        private void UpdateChart(int lineIndex, long elapsedTime, int value)
        {
            try
            {
                this.Invoke(() =>
                {
                    if (lineSeriesList.Count <= lineIndex)
                    {
                        var newLineSeries = new LineSeries
                        {
                            Title = $"Value {lineIndex + 1}",
                            MarkerType = MarkerType.Circle,
                            Color = GetLineColor(lineIndex),

                        };
                        //newLineSeries.Points.Add(new DataPoint(elapsedTime, value));
                        lineSeriesList.Add(newLineSeries);
                        plotModel.Series.Add(newLineSeries);
                    }

                    lineSeriesList[lineIndex].Points.Add(new DataPoint(elapsedTime, value));
                    plotView.InvalidatePlot(true);
                });
            }
            catch(Exception ex)
            {
                MessageBox.Show("Отрисовка графика = " + ex.Message);
            }
            finally
            {
                this.Invoke(() =>
                {
                    if (isThreadStop)
                        _isCanThreadStop = true;

                });
            }


        }
        /// <summary>
        /// Метод для получения цвета линий отобрадающихся на графике
        /// </summary>
        /// <param name="lineIndex"></param>
        /// <returns></returns>
        private OxyColor GetLineColor(int lineIndex)
        {
            OxyColor[] colors = new OxyColor[]
            {
                OxyColor.FromRgb(255, 0, 0),  // Red
                OxyColor.FromRgb(0, 255, 0),  // Green
                OxyColor.FromRgb(0, 0, 255),  // Blue
                OxyColor.FromRgb(255, 165, 0), // Orange
                OxyColor.FromRgb(255, 255, 0),  // Yellow
                OxyColor.FromRgb(255,255,255), // Black
                OxyColor.FromRgb(125,125,125) //Gray
            };
            return colors[lineIndex % colors.Length];
        }
        #endregion
        #region FormClosingEvent
        /// <summary>
        /// Метод отправляет команду Exit в момент закрытия формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                var port = _comPortConnectionService.GetPort();

                //port.DataReceived -= DataReceivedHandler;
                isThreadStop = true;
                threadRead.Join();
                port.DataReceived += DataReceivedHandlerForExit;

                var formattedString = CommandsFormatting.GetCommandWithFormatting("Exit");
                await _comPortSendService.SendMessage(formattedString);

                // Ожидание ответа
                await WaitForResponseAsync();

                // Отправляем подтверждение "Y"
                await _comPortSendService.SendMessage("Y");

                // Отписываемся от обработчика после завершения
                port.DataReceived -= DataReceivedHandlerForExit;
            }
            timer?.Stop();
            timer = null;
        }

        /// <summary>
        /// Ожидание ответа от устройства
        /// </summary>
        private TaskCompletionSource<bool> _responseReceived = new TaskCompletionSource<bool>();

        private async Task WaitForResponseAsync()
        {
            await _responseReceived.Task;
        }

        /// <summary>
        /// Обработчик данных для Exit
        /// </summary>
        private void DataReceivedHandlerForExit(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;
            string response = port.ReadExisting();

            if (!string.IsNullOrEmpty(response))
            {
                _responseReceived.TrySetResult(true);
            }
        }

        #endregion
    }
}
