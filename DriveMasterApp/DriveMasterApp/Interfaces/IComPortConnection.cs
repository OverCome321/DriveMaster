using System.IO.Ports;

namespace DriveMasterApp.Interfaces
{
    public interface IComPortConnection
    {
        Task Connect(string portName);
        Task Disconnect();
        SerialPort GetPort();
        bool IsConnected { get; }
    }
}
