using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices;

public abstract class SimDeviceBase : ISimuDevice
{
    public virtual Task ProcessAsync(LwnDeviceResponse device, Dictionary<string, object>? data)
    { return Task.CompletedTask; }

    public virtual Task ProcessAsync(ReceiveDownlink downlink, Dictionary<string, object>? data)
    { return Task.CompletedTask; }

    public virtual Task ProcessAsync(ConsoleLog message, Dictionary<string, object> data)
    {
        return Task.CompletedTask;
    }
}