using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices;

public interface ISimuDevice
{
    Task ProcessAsync(LwnDeviceResponse device, Dictionary<string, object>? data);
    Task ProcessAsync(ReceiveDownlink downlink, Dictionary<string, object>? data);
    Task ProcessAsync(ConsoleLog message, Dictionary<string, object> data);
}