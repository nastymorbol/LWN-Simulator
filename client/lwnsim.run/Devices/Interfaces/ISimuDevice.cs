using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices.Interfaces;

public interface ISimuDevice
{
    int Id { get; }
    string Name { get; }
    
    bool CanHandle();

    Task ProcessAsync();
    Task ProcessAsync(LwnDeviceResponse device);
    Task ProcessAsync(ReceiveDownlink downlink);
    Task ProcessAsync(ConsoleLog message);
    Task ProcessAsync(ReceiveUplink deviceResponse);
}