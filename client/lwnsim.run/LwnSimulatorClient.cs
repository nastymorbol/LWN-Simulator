using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class LwnSimulatorClient : BackgroundService
{
    private readonly ILogger<LwnSimulatorClient> _logger;
    private readonly LwnConnectionService _lwnConnectionService;

    public LwnSimulatorClient(ILogger<LwnSimulatorClient> logger, LwnConnectionService lwnConnectionService)
    {
        _logger = logger;
        _lwnConnectionService = lwnConnectionService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start lwn-client");
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.Sleep(10_000);
            try
            {
                await _lwnConnectionService.StartSimulatorAsync(stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError("Error while starting simulation: {Message}", e.Message);
                continue;
            }
            
            var devices = await _lwnConnectionService.GetDevicesAsync(stoppingToken);
            var sensusDevices = devices.Where(d =>
                d.info.name.StartsWith("sensative", StringComparison.InvariantCultureIgnoreCase));
            foreach (var deviceResponse in sensusDevices)
            {
                // avg temp
                // await _lwnConnectionService.ChangePayloadAsync(deviceResponse.id, "0xffff01630400c1", stoppingToken);
                // await Task.Delay(30_000, stoppingToken);
                // door open
                //await _lwnConnectionService.SendPayloadAsync(deviceResponse.id, "0xffff01630900110000", stoppingToken);
                //await Task.Delay(30_000, stoppingToken);
                // door closed
                await _lwnConnectionService.SendPayloadAsync(deviceResponse.id, "0xffff01630901110000", stoppingToken);
                await Task.Delay(30_000, stoppingToken);
                
            }
        }
    }
}