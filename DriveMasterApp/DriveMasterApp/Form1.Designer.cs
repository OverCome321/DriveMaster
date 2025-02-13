using System.Collections.ObjectModel;
using System.IO.Ports;
using DriveMasterApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using DriverMasterModels;
using DriveMasterApp.Utils;
namespace DriveMasterApp
{
    public partial class Form1 : Form
    {
        #region Fields
        private bool isMessageBoxShown = false;
        private bool isMotorOn = false;
        private readonly IComPortConnection _comPortConnectionService;
        private readonly IComPortSend _comPortSendService;
        private readonly IServiceProvider _serviceProvider;
        private string _selectedPort;

        private Label labelCommandStatus;
        private ComboBox comboBoxComPorts;
        private Button buttonConnect;
        private Label labelStatus;
        private Panel panelIndicator;
        private Button buttonSendCommand;
        private Button buttonToggleMotor;
        private Button buttonFwd;
        private Button buttonRew;
        private Button buttonStop;
        private Button buttonRun;
        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();
        #endregion
        public Form1(IComPortConnection comPortConnectionService, IComPortSend comPortSendService, IServiceProvider serviceProvider)
        {
            _comPortConnectionService = comPortConnectionService;
            _comPortSendService = comPortSendService;
            _serviceProvider = serviceProvider;
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += Form1_FormClosing;

            UpdateUI();
        }
        /// <summary>
        /// Метод для инициализации всех объектов на экране
        /// </summary>
        private void InitializeComponent()
        {
            comboBoxComPorts = new ComboBox();
            buttonConnect = new Button();
            labelStatus = new Label();
            panelIndicator = new Panel();
            buttonSendCommand = new Button();
            buttonToggleMotor = new Button();
            buttonFwd = new Button();
            buttonRew = new Button();
            buttonStop = new Button();
            buttonRun = new Button();
            labelCommandStatus = new Label();
            SuspendLayout();
            // 
            // comboBoxComPorts
            // 
            comboBoxComPorts.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxComPorts.Location = new Point(20, 20);
            comboBoxComPorts.Name = "comboBoxComPorts";
            comboBoxComPorts.Size = new Size(150, 28);
            comboBoxComPorts.TabIndex = 0;
            // 
            // buttonConnect
            // 
            buttonConnect.Location = new Point(180, 20);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(104, 28);
            buttonConnect.TabIndex = 1;
            buttonConnect.Text = "Подключить";
            // 
            // labelStatus
            // 
            labelStatus.Location = new Point(316, 24);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(129, 25);
            labelStatus.TabIndex = 3;
            labelStatus.Text = "Не подключено";
            // 
            // panelIndicator
            // 
            panelIndicator.BackColor = Color.Gray;
            panelIndicator.Location = new Point(290, 25);
            panelIndicator.Name = "panelIndicator";
            panelIndicator.Size = new Size(20, 20);
            panelIndicator.TabIndex = 2;
            // 
            // buttonSendCommand
            // 
            buttonSendCommand.Location = new Point(20, 60);
            buttonSendCommand.Name = "buttonSendCommand";
            buttonSendCommand.Size = new Size(172, 39);
            buttonSendCommand.TabIndex = 4;
            buttonSendCommand.Text = "Режим диагностики";
            // 
            // buttonToggleMotor
            // 
            buttonToggleMotor.Location = new Point(200, 60);
            buttonToggleMotor.Name = "buttonToggleMotor";
            buttonToggleMotor.Size = new Size(160, 39);
            buttonToggleMotor.TabIndex = 5;
            buttonToggleMotor.Text = "Включить мотор";
            // 
            // buttonFwd
            // 
            buttonFwd.Location = new Point(366, 60);
            buttonFwd.Name = "buttonFwd";
            buttonFwd.Size = new Size(235, 39);
            buttonFwd.TabIndex = 6;
            buttonFwd.Text = "Прямое вращение двигателя";
            // 
            // buttonRew
            // 
            buttonRew.Location = new Point(366, 105);
            buttonRew.Name = "buttonRew";
            buttonRew.Size = new Size(235, 37);
            buttonRew.TabIndex = 7;
            buttonRew.Text = "Обратное вращение двигателя";
            // 
            // buttonStop
            // 
            buttonStop.Location = new Point(20, 105);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(340, 37);
            buttonStop.TabIndex = 8;
            buttonStop.Text = "Остановить двигатель";
            // 
            // buttonRun
            // 
            buttonRun.Location = new Point(607, 60);
            buttonRun.Name = "buttonRun";
            buttonRun.Size = new Size(269, 39);
            buttonRun.TabIndex = 9;
            buttonRun.Text = "Работать в циклическом режиме";
            // 
            // labelCommandStatus
            // 
            labelCommandStatus.BackColor = Color.LightGray;
            labelCommandStatus.Dock = DockStyle.Bottom;
            labelCommandStatus.Location = new Point(0, 340);
            labelCommandStatus.Name = "labelCommandStatus";
            labelCommandStatus.Size = new Size(897, 40);
            labelCommandStatus.Font = new Font(labelCommandStatus.Font.FontFamily, 12);
            labelCommandStatus.TabIndex = 10;
            labelCommandStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            ClientSize = new Size(897, 370);
            Controls.Add(comboBoxComPorts);
            Controls.Add(buttonConnect);
            Controls.Add(panelIndicator);
            Controls.Add(labelStatus);
            Controls.Add(buttonSendCommand);
            Controls.Add(buttonToggleMotor);
            Controls.Add(buttonFwd);
            Controls.Add(buttonRew);
            Controls.Add(buttonStop);
            Controls.Add(buttonRun);
            Controls.Add(labelCommandStatus);
            Name = "Form1";
            Text = "DriveMaster";
            ResumeLayout(false);

            RefreshPorts();
            BindData();
        }
        #region Binding
        /// <summary>
        /// Метод привязывает события и данные 
        /// </summary>
        private void BindData()
        {
            comboBoxComPorts.DataSource = AvailablePorts;
            comboBoxComPorts.SelectedIndexChanged += (s, e) =>
            {
                if (_comPortConnectionService.IsConnected && !isMessageBoxShown)
                {
                    isMessageBoxShown = true; 

                    var result = MessageBox.Show(
                        "Вы уверены, что хотите сменить COM порт? Старое подключение будет закрыто.",
                        "Подтверждение смены порта",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Yes)
                    {
                        ToggleConnection();
                        _selectedPort = comboBoxComPorts.SelectedItem as string;
                    }
                    else
                    {
                        comboBoxComPorts.SelectedItem = _selectedPort;
                    }

                    isMessageBoxShown = false; 
                }
                else if (!_comPortConnectionService.IsConnected)
                {
                    _selectedPort = comboBoxComPorts.SelectedItem as string;
                }
            };

            // Изначально выбираем пустой элемент, если нет доступных портов
            comboBoxComPorts.SelectedIndex = -1;

            // Обработка клика по кнопке, вызов команды вручную
            buttonConnect.Click += async (s, e) =>
            {
                if (comboBoxComPorts.SelectedItem != null)
                {
                    _selectedPort = comboBoxComPorts.SelectedItem.ToString();
                    await ToggleConnection();
                }
            };
            buttonSendCommand.Click += ButtonSendCommand_Click;
            buttonRun.Click += ButtonRun_Click;
            buttonStop.Click += ButtonStop_Click;
            buttonRew.Click += ButtonRew_Click;
            buttonFwd.Click += ButtonFwd_Click;
            buttonToggleMotor.Click += ButtonToggleMotor_Click;
        }
        #endregion
        #region Port methods
        /// <summary>
        /// Метод получает список всех доступных портов
        /// </summary>
        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var port in SerialPort.GetPortNames())
            {
                AvailablePorts.Add(port);
            }
        }
        /// <summary>
        /// Метод для подключения к com порту
        /// </summary>
        /// <returns></returns>
        private async Task ToggleConnection()
        {
            if (_comPortConnectionService.IsConnected)
            {
                await _comPortConnectionService.Disconnect();
                labelCommandStatus.Text = string.Empty;
            }
            else if (!string.IsNullOrEmpty(_selectedPort))
            {
                await _comPortConnectionService.Connect(_selectedPort);
            }

            // Обновление состояния UI
            UpdateUI();
        }
        #endregion
        #region UI methods
        /// <summary>
        /// Метод для обновления UI в соответствии с изменением состояния подключения
        /// </summary>
        private void UpdateUI()
        {
            // Обновление индикатора и текста состояния
            panelIndicator.BackColor = _comPortConnectionService.IsConnected ? Color.Green : Color.Gray;
            labelStatus.Text = _comPortConnectionService.IsConnected ? "Подключено" : "Не подключено";
            buttonConnect.Text = _comPortConnectionService.IsConnected ? "Отключить" : "Подключить";
            buttonSendCommand.Enabled = _comPortConnectionService.IsConnected;
            buttonToggleMotor.Enabled = _comPortConnectionService.IsConnected;
            buttonFwd.Enabled = _comPortConnectionService.IsConnected;
            buttonRew.Enabled = _comPortConnectionService.IsConnected;
            buttonStop.Enabled = _comPortConnectionService.IsConnected;
            buttonRun.Enabled = _comPortConnectionService.IsConnected;
        }
        /// <summary>
        /// Анимация загрузки статуса доставления команды
        /// </summary>
        /// <param name="startColor"></param>
        /// <param name="endColor"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        private async Task AnimateLabelAsync(Color startColor, Color endColor, int duration)
        {
            int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float ratio = (float)i / steps;
                int r = (int)(startColor.R + (endColor.R - startColor.R) * ratio);
                int g = (int)(startColor.G + (endColor.G - startColor.G) * ratio);
                int b = (int)(startColor.B + (endColor.B - startColor.B) * ratio);
                labelCommandStatus.BackColor = Color.FromArgb(r, g, b);
                await Task.Delay(duration / steps);
            }
        }
        #endregion
        #region EventsArgs
        /// <summary>
        /// Отправка команды DM для диагностики
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSendCommand_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command dmCommand = Command.DM;
                labelCommandStatus.Text = $"Статус команды: {dmCommand} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                var port = _comPortConnectionService.GetPort();
                port.DataReceived += DataReceivedHandler;

                var formattedString = CommandsFormatting.GetCommandWithFormatting("DM");
                await _comPortSendService.SendMessage(formattedString);
                labelCommandStatus.Text = $"Статус команды: {dmCommand} отправлена";

                // Ожидание ответа
                await WaitForResponseAsync();
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TaskCompletionSource<bool> _responseReceived = new TaskCompletionSource<bool>();

        private async Task WaitForResponseAsync()
        {
            labelCommandStatus.Text = $"Получаем ответ от COM port... Ожидается ответ и отправка команды Y";
            await _responseReceived.Task;

            // Отправляем подтверждение "Y"
            await _comPortSendService.SendMessage("Y");

            // Показываем окно после получения ответа
            var plotForm = _serviceProvider.GetRequiredService<PlotForm>();
            plotForm.Show();

            labelCommandStatus.Text = "Команда выполнена успешно";
            await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
        }

        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            var port = (SerialPort)sender;
            string response = port.ReadExisting();

            if (!string.IsNullOrEmpty(response))
            {
                _responseReceived.TrySetResult(true);
            }
        }
        /// <summary>
        /// Событие клик для отправки команды MotorOn/MotorOff
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonToggleMotor_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {

                isMotorOn = !isMotorOn;
                Command command = isMotorOn ? Command.MotorOn : Command.MotorOff;
                string formattedComad = CommandsFormatting.GetCommandWithFormatting(command.ToString());

                labelCommandStatus.Text = $"Статус команды: {command} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                buttonToggleMotor.Text = isMotorOn ? "Отключить мотор" : "Включить мотор";
                await _comPortSendService.SendMessage(formattedComad);

                labelCommandStatus.Text = $"Статус команды: {command} доставлена";
                await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
                await Task.Delay(100);
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Событие клик для отправки команды Fwd
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonFwd_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command command = Command.Fwd;

                labelCommandStatus.Text = $"Статус команды: {command} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                string formattedComad = CommandsFormatting.GetCommandWithFormatting(command.ToString());
                await _comPortSendService.SendMessage(formattedComad);

                labelCommandStatus.Text = $"Статус команды: {command} доставлена";
                await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Событие клик для отправки команды Rew
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonRew_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command command = Command.Rew;

                labelCommandStatus.Text = $"Статус команды: {command} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                string formattedComad = CommandsFormatting.GetCommandWithFormatting(command.ToString());
                await _comPortSendService.SendMessage(formattedComad);

                labelCommandStatus.Text = $"Статус команды: {command} доставлена";
                await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Событие клик для отправки команды Stop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonStop_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command command = Command.Stop;

                labelCommandStatus.Text = $"Статус команды: {command} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                string formattedComad = CommandsFormatting.GetCommandWithFormatting(command.ToString());
                await _comPortSendService.SendMessage(formattedComad);

                labelCommandStatus.Text = $"Статус команды: {command} доставлена";
                await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Событие клик для отправки команды RUN
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonRun_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command command = Command.RUN;

                labelCommandStatus.Text = $"Статус команды: {command} отправляется...";
                await AnimateLabelAsync(Color.LightYellow, Color.Yellow, 300);

                string formattedComad = CommandsFormatting.GetCommandWithFormatting(command.ToString());
                await _comPortSendService.SendMessage(formattedComad);

                labelCommandStatus.Text = $"Статус команды: {command} доставлена";
                await AnimateLabelAsync(Color.Yellow, Color.LightGray, 300);
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Метод отключает соединения от com порта при закрытии программы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await _comPortConnectionService.Disconnect();
        }
        #endregion
    }
}
