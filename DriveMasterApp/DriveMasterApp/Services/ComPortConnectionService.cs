using DriveMasterApp.Interfaces;
using System.IO.Ports;
using System.Text;

namespace DriveMasterApp.Services
{
    public class ComPortConnectionService : IComPortConnection
    {
        private SerialPort _serialPort;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;


        #region Methods
        /// <summary>
        /// Метод реализует Connect интерфейса IComPortConnection для соединения с com портом
        /// </summary>
        /// <param name="portName"></param>
        /// <returns></returns>
        public async Task Connect(string portName)
        {
            await Task.Run(async () =>
            {
                bool isMessageReceived = false;
                while (!IsConnected)
                {
                    try
                    {
                        _serialPort = new SerialPort
                        {
                            PortName = portName,
                            BaudRate = 115200,
                            Parity = Parity.None,
                            DataBits = 8,
                            StopBits = StopBits.One,
                            Handshake = Handshake.None,
                            Encoding = Encoding.UTF8
                        };
                        _serialPort.Open();
                    }
                    catch (Exception ex)
                    {
                        if (!isMessageReceived)
                            MessageBox.Show($"Ошибка при подключении к порту {portName}: {ex.Message}", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        isMessageReceived = true;
                    }
                    finally
                    {
                        await Task.Delay(100);  
                    }
                }
            });
        }
        /// <summary>
        /// Метод реализует Disconnect интерфейса IComPortConnection для отсоединения от com порта
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            await Task.Run(async () =>
            {
                try
                {
                    if (_serialPort != null && _serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        _serialPort.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при закрытии порта {_serialPort.PortName}: {ex.Message}", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }
        /// <summary>
        /// Метод реализует GetPort интерфейса IComPortConnection для получения информации по текущему порту
        /// </summary>
        /// <returns></returns>
        public SerialPort GetPort() => _serialPort;
        #endregion
    }
}
