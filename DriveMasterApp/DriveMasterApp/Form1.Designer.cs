using System.Collections.ObjectModel;
using System.IO.Ports;
using DriveMasterApp.Interfaces;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.WindowsForms;
using OxyPlot.Axes;
using Microsoft.Extensions.DependencyInjection;
using DriverMasterModels;
using DriveMasterApp.Utils;
namespace DriveMasterApp
{
    public partial class Form1 : Form
    {
        private bool isMessageBoxShown = false;
        private readonly IComPortConnection _comPortConnectionService;
        private readonly IComPortSend _comPortSendService;
        private readonly IServiceProvider _serviceProvider;
        private string _selectedPort;

        private ComboBox comboBoxComPorts;
        private Button buttonConnect;
        private Label labelStatus;
        private Panel panelIndicator;
        private Button buttonSendCommand;

        public ObservableCollection<string> AvailablePorts { get; } = new ObservableCollection<string>();

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
        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await _comPortConnectionService.Disconnect();
        }

        private void InitializeComponent()
        {
            comboBoxComPorts = new ComboBox();
            buttonConnect = new Button();
            labelStatus = new Label();
            panelIndicator = new Panel();
            buttonSendCommand = new Button();
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
            buttonSendCommand.Size = new Size(172, 28);
            buttonSendCommand.TabIndex = 4;
            buttonSendCommand.Text = "Режим диагностики";
            buttonSendCommand.Click += ButtonSendCommand_Click;
            // 
            // Form1
            // 
            ClientSize = new Size(897, 370);
            Controls.Add(comboBoxComPorts);
            Controls.Add(buttonConnect);
            Controls.Add(panelIndicator);
            Controls.Add(labelStatus);
            Controls.Add(buttonSendCommand);
            Name = "Form1";
            Text = "DriveMaster";
            ResumeLayout(false);

            RefreshPorts();
            BindData();
        }

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
                        // Закрыть старое подключение
                        ToggleConnection();
                        _selectedPort = comboBoxComPorts.SelectedItem as string;
                    }
                    else
                    {
                        // Если пользователь отменяет смену порта, восстанавливаем старое значение
                        comboBoxComPorts.SelectedItem = _selectedPort;
                    }

                    isMessageBoxShown = false; // Reset flag after the message box has been dealt with
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
        }
        private async void ButtonSendCommand_Click(object sender, EventArgs e)
        {
            if (_comPortConnectionService.IsConnected)
            {
                Command dmCommand = Command.DM;
                var formattedString = CommandsFormatting.GetCommandWithFormatting("DM");
                await _comPortSendService.SendMessage(formattedString);

                var plotForm = _serviceProvider.GetRequiredService<PlotForm>();
                plotForm.Show();
            }
            else
            {
                MessageBox.Show("COM порт не подключен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var port in SerialPort.GetPortNames())
            {
                AvailablePorts.Add(port);
            }
        }

        private async Task ToggleConnection()
        {
            if (_comPortConnectionService.IsConnected)
            {
                await _comPortConnectionService.Disconnect();
            }
            else if (!string.IsNullOrEmpty(_selectedPort))
            {
                await _comPortConnectionService.Connect(_selectedPort);
            }

            // Обновление состояния UI
            UpdateUI();
        }

        private void UpdateUI()
        {
            // Обновление индикатора и текста состояния
            panelIndicator.BackColor = _comPortConnectionService.IsConnected ? Color.Green : Color.Gray;
            labelStatus.Text = _comPortConnectionService.IsConnected ? "Подключено" : "Не подключено";
            buttonConnect.Text = _comPortConnectionService.IsConnected ? "Отключить" : "Подключить";
            buttonSendCommand.Enabled = _comPortConnectionService.IsConnected;
        }
    }
}
