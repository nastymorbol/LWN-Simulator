using lwnsim.Devices;
using lwnsim.Devices.Factory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LwnSimulatorClient : BackgroundService
{
    private readonly ILogger<LwnSimulatorClient> _logger;
    private readonly LwnConnectionService _lwnConnectionService;
    private readonly SimDeviceFactory _deviceFactory;

    public LwnSimulatorClient(ILogger<LwnSimulatorClient> logger, LwnConnectionService lwnConnectionService, SimDeviceFactory deviceFactory)
    {
        _logger = logger;
        _lwnConnectionService = lwnConnectionService;
        _deviceFactory = deviceFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Execute {Process}", nameof(LwnSimulatorClient));
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(1_000);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lwnConnectionService.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        //return _lwnConnectionService.StopAsync(cancellationToken);
        return base.StopAsync(cancellationToken);
    }
}