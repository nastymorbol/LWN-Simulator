using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices.Interfaces;

public abstract class SimDeviceBase : ISimuDevice
{
    public abstract bool CanHandle(LwnDeviceResponse deviceResponse);

    public virtual Task ProcessAsync(LwnDeviceResponse device)
    { return Task.CompletedTask; }

    public virtual Task ProcessAsync(ReceiveDownlink downlink)
    { return Task.CompletedTask; }

    public virtual Task ProcessAsync(ConsoleLog message)
    {
        return Task.CompletedTask;
    }

    public virtual Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        return Task.CompletedTask;
    }
}