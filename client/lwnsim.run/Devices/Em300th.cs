using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using lwnsim.Devices.Interfaces;
using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices;

public sealed class Em300th : SimDeviceBase
{
    private readonly ILogger<Em300th> _logger;
    private readonly LwnConnectionService _connectionService;
    private readonly Random _random = new (DateTime.Now.Millisecond);

    private bool _uplinkReceived;

    
    public Em300th(ILogger<Em300th> logger, LwnConnectionService connectionService)
    {
        _logger = logger;
        _connectionService = connectionService;
    }


    public override bool CanHandle(LwnDeviceResponse device)
    {
        return device.info.name.Contains("sensative", StringComparison.InvariantCultureIgnoreCase);
    }

    public override async Task ProcessAsync(LwnDeviceResponse device)
    {

        if (!_uplinkReceived) return;
        _uplinkReceived = false;
        
        return;
    }

    public override Task ProcessAsync(ConsoleLog message)
    {
        return base.ProcessAsync(message);
    }
}