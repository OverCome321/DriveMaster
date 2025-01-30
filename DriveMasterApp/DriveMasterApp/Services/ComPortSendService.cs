using DriveMasterApp.Interfaces;
using DriveMasterApp.Utils;

namespace DriveMasterApp.Services
{
    public class ComPortSendService : IComPortSend
    {
        public IComPortConnection _port {  get; }

        public ComPortSendService(IComPortConnection comPortConnection) 
        {
            _port = comPortConnection;
        }
        #region Methods
        /// <summary>
        /// Метод реализует SendMessage интерфейса IComPortConnection для отправки команды по com порту
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendMessage(string message)
        {
            await Task.Run(() =>
            {
                var serialPort = _port.GetPort();
                if (serialPort != null && serialPort.IsOpen)
                {
                    var formattedMessage = CommandsFormatting.GetCommandWithFormatting(message);
                    serialPort.Write(formattedMessage);
                }
            });
        }
        #endregion
    }
}
