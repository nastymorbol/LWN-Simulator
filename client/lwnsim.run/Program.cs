// See https://aka.ms/new-console-template for more information

using System.Net.Http.Json;
using System.Text.Json;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketIOClient;
using SocketIOClient.JsonSerializer;


var app = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(host =>
    {
    })
    .ConfigureLogging(options =>
    {
        options.ClearProviders();
        options.AddSimpleConsole(builder =>
        {
            builder.SingleLine = true;
        });
    })
    .ConfigureServices((host, services) =>
    {
        services.AddHostedService<LwnSimulator>();
        services.AddTransient<LwnConnectionService>();
        services.Configure<LwnConnection>(host.Configuration.GetSection("LwnSimulator"));
        services.AddHttpClient(nameof(LwnConnectionService), (provider, client) =>
        {
            client.BaseAddress = provider.GetRequiredService<IOptions<LwnConnection>>().Value.ApiUrl;
        });
    })
    .Build();

app.Run();


public class LwnConnection
{
#pragma warning disable CS8618
    public string Connection { get; set; }
#pragma warning restore CS8618

    public Uri ApiUrl => new UriBuilder(Connection + "/api/").Uri;
    public Uri WsHttpUrl => new UriBuilder(Connection + "/socket.io/").Uri;
    public Uri WsUrl => new UriBuilder(Connection.Replace("http", "ws") + "/socket.io/").Uri;
} 

public class LwnConnectionService
{
    private readonly ILogger _logger;
    private readonly LwnConnection _connection;
    private readonly HttpClient _httpClient;
    private readonly SocketIO _socketIo;
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };



    public LwnConnectionService(ILogger<LwnConnectionService> logger, IOptions<LwnConnection> options, IHttpClientFactory factory)
    {
        _logger = logger;
        _connection = options.Value;
        _httpClient = factory.CreateClient(nameof(LwnConnectionService));
        
        _socketIo = new SocketIO(_connection.Connection, new SocketIOOptions
        {
            EIO = 3,
        });
        _socketIo.JsonSerializer = new CustomJsonSerializer(3);
        _socketIo.On( Events.EventDev, response =>
        {
            _logger.LogInformation("[{Channel}] {Response}", Events.EventDev, response);
        });

        _socketIo.On(Events.EventLog, response =>
        {
            _logger.LogInformation("[{Channel}] {Response}", Events.EventLog, response);
        });
        
        _socketIo.On(Events.EventResponseCommand, response =>
        {
            _logger.LogInformation("[{Channel}] {Response}", Events.EventResponseCommand, response);
        });
        
        _socketIo.On(Events.EventReceivedDownlink, response =>
        {
            _logger.LogInformation("[{Channel}] {Response}", Events.EventResponseCommand, response);
            var downlink = response.GetValue<ReceiveDownlink>();
        });

        _socketIo.OnConnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connected");
        };
        _socketIo.OnDisconnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Disconnected");
        };
        
    }


    public async Task<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await _httpClient
            .GetFromJsonAsync<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>>("devices", _jsonOptions, cancellationToken: cancellationToken);

        return devices;
    }
    
    public async Task StopSimulatorAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient
            .GetStringAsync("stop",cancellationToken: cancellationToken);
    }

    public async Task StartSimulatorAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient
            .GetStringAsync("start",cancellationToken: cancellationToken);
    }

    class CustomJsonSerializer : SystemTextJsonSerializer
    {
        private readonly JsonSerializerOptions _jsonOptions  = new ()
        {
            PropertyNameCaseInsensitive = true
        };
        public CustomJsonSerializer(int eio) : base(eio)
        {
        }

        public override JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions( _jsonOptions );
        }
    }
    
    
    public async Task SendPayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        // 42["send-uplink",{"id":0,"mtype":"ConfirmedDataUp","payload":"0xffff01630400c1"}]	1673651564.2782326
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventSendUplink, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
        
    }
    public async Task ChangePayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventChangePayload, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
    }
}

public class LwnSimulator : BackgroundService
{
    private readonly ILogger<LwnSimulator> _logger;
    private readonly LwnConnectionService _lwnConnectionService;

    public LwnSimulator(ILogger<LwnSimulator> logger, LwnConnectionService lwnConnectionService)
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
