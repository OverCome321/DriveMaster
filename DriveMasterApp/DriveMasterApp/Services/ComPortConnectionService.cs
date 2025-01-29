using DriveMasterApp.Interfaces;
using System.IO.Ports;
using System.Text;

namespace DriveMasterApp.Services
{
    public class ComPortConnectionService : IComPortConnection
    {
        private SerialPort _serialPort;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

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
        public SerialPort GetPort() => _serialPort;
    }
}
