using System.Net.Http.Json;
using System.Text.Json;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketIOClient;
using SocketIOClient.JsonSerializer;

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
        
        _socketIo.OnConnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connected");
        };
        _socketIo.OnDisconnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Disconnected");
        };
        _socketIo.OnReconnected += async (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Reconnected");
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