using lwnsim.Devices.Interfaces;
using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices.Milesight;

public abstract class Em300th : SimDeviceBase, IEncoder
{
    internal readonly ILogger<Em300th> _logger;
    internal readonly LwnConnectionService _connectionService;
    internal readonly Random _random = new (DateTime.Now.Millisecond);

    internal int _uplinkCounter;
    internal Em300Payload _instance = new();
    
    public Em300th(ILogger<Em300th> logger, LwnConnectionService connectionService)
    {
        _logger = logger;
        _connectionService = connectionService;
    }

    protected override bool CanHandleDeviceResponse()
    {
        return Name.Contains("EM300-TH", StringComparison.InvariantCultureIgnoreCase);
    }

    public override Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        const double rad = Math.PI / 180;
        _instance.Battery = MathF.Sin((float) (rad * DateTime.Now.Second * 1.5)) * 100;
        _instance.Temperature = MathF.Sin((float) (rad * DateTime.Now.Second * 1.5)) * 30;
        _instance.Humidity = MathF.Sin((float) (rad * DateTime.Now.Second * 1.5)) * 100;

        _uplinkCounter++;
        return base.ProcessAsync(deviceResponse);
    }

    public byte[] Encode()
    {
        return _instance.Encode();
    }
}

public class Em300Th_Oat : Em300th
{
    public Em300Th_Oat(ILogger<Em300Th_Oat> logger, LwnConnectionService connectionService) : base(logger, connectionService)
    {
    }

    protected override bool CanHandleDeviceResponse()
    {
        return base.CanHandleDeviceResponse() && Name.Contains("OAT");
    }
    
    public override async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        const double rad = Math.PI / 180;
        await base.ProcessAsync(deviceResponse);
        _instance.Temperature = MathF.Sin((float) (rad * DateTime.Now.Second * 1.5)) * 30 - 30;
        await _connectionService.EnqueuePayloadAsync(Id, _instance.Encode(), CancellationToken.None);
    }
}

public class Em300Th_Rot : Em300th
{
    public Em300Th_Rot(ILogger<Em300Th_Rot> logger, LwnConnectionService connectionService) : base(logger, connectionService)
    {
    }

    protected override bool CanHandleDeviceResponse()
    {
        return base.CanHandleDeviceResponse() && Name.Contains("ROT");
    }
    
    public override async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        //const double rad = Math.PI / 180;
        await base.ProcessAsync(deviceResponse);
        await _connectionService.EnqueuePayloadAsync(this, CancellationToken.None);
    }
}