using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices.Interfaces;

public abstract class SimDeviceBase : ISimuDevice
{
    public int Id { get; private set; }

    public string Name { get; private set; } = String.Empty;

    public bool CanHandle()
    {
        return CanHandleDeviceResponse();
    }

    protected abstract bool CanHandleDeviceResponse();


    public virtual Task ProcessAsync() => Task.CompletedTask;
    public virtual Task ProcessAsync(LwnDeviceResponse device) => Task.CompletedTask;

    public virtual Task ProcessAsync(ReceiveDownlink downlink) => Task.CompletedTask;

    public virtual Task ProcessAsync(ConsoleLog message) => Task.CompletedTask;

    public virtual Task ProcessAsync(ReceiveUplink deviceResponse) => Task.CompletedTask;
}