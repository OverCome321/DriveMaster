namespace DriveMasterApp.Interfaces
{
    public interface IComPortSend
    {
        IComPortConnection _port { get; }
        Task SendMessage(string message);   
    }
}
