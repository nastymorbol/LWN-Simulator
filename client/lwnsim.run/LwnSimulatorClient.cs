using lwnsim.Devices.Factory;

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
            await _deviceFactory.ProcessAsync();
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _lwnConnectionService.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }
}