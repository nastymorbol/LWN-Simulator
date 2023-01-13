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
    public string Connection { get; set; }

    public Uri ApiUrl => new UriBuilder(Connection + "/api/").Uri;
    public Uri WsHttpUrl => new UriBuilder(Connection + "/socket.io/").Uri;
    public Uri WsUrl => new UriBuilder(Connection.Replace("http", "ws") + "/socket.io/").Uri;
} 

public class LwnConnectionService
{
    private readonly ILogger _logger;
    private readonly LwnConnection _connection;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly SocketIO _socketIo;


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
            _logger.LogInformation(response.ToString());

            var text = response.GetValue<ConsoleLog>();

            // The socket.io server code looks like this:
            // socket.emit('hi', 'hi client');
        });

        _socketIo.On(Events.EventLog, response =>
        {
            // You can print the returned data first to decide what to do next.
            // output: ["ok",{"id":1,"name":"tom"}]
            _logger.LogInformation(response.ToString());
    
            // Get the first data in the response
            var text = response.GetValue<ConsoleLog>();
        });
        
        _socketIo.On(Events.EventResponseCommand, response =>
        {
            // You can print the returned data first to decide what to do next.
            // output: ["ok",{"id":1,"name":"tom"}]
            _logger.LogInformation(response.ToString());
            // 42["response-command","Sensus Simulation: Payload changed"]
            var s = response.GetValue<string>();
        });

        _socketIo.OnConnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connects {arg}", e);
        };
        _socketIo.OnDisconnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connects {arg}", e);
        };
        
    }


    public async Task<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await _httpClient
            .GetFromJsonAsync<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>>("devices", _jsonOptions, cancellationToken: cancellationToken);

        return devices;
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
    
    public async Task ChangePayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventChangePayload, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = "0x1234"});
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
            Thread.Sleep(1000);
            var devices = await _lwnConnectionService.GetDevicesAsync(stoppingToken);
            var sensusDevices = devices.Where(d =>
                d.info.name.StartsWith("sensus", StringComparison.InvariantCultureIgnoreCase));
            foreach (var deviceResponse in sensusDevices)
            {
                await _lwnConnectionService.ChangePayloadAsync(deviceResponse.id, "0x121212", stoppingToken);
            }
        }
    }
} 
