using lwnsim.Devices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LwnSimulatorClient : BackgroundService
{
    private readonly ILogger<LwnSimulatorClient> _logger;
    private readonly LwnConnectionService _lwnConnectionService;
    private readonly SimuDeviceFactory _deviceFactory;

    public LwnSimulatorClient(ILogger<LwnSimulatorClient> logger, LwnConnectionService lwnConnectionService, SimuDeviceFactory deviceFactory)
    {
        _logger = logger;
        _lwnConnectionService = lwnConnectionService;
        _deviceFactory = deviceFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start lwn-client");
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(1_000);
            try
            {
                //await _lwnConnectionService.StartSimulatorAsync(stoppingToken);
                var devices = await _lwnConnectionService.GetDevicesAsync(stoppingToken);
            
                foreach (var device in devices)
                {
                    _deviceFactory.Process(device);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Error while starting simulation: {Message}", e.Message);
                continue;
            }
            
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