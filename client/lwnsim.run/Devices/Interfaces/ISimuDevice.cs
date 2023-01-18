using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices.Interfaces;

public interface ISimuDevice
{
    bool CanHandle(LwnDeviceResponse deviceResponse);

    Task ProcessAsync(LwnDeviceResponse device);
    Task ProcessAsync(ReceiveDownlink downlink);
    Task ProcessAsync(ConsoleLog message);
    Task ProcessAsync(ReceiveUplink deviceResponse);
}