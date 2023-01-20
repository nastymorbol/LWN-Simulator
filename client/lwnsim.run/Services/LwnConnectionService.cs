using lwnsim.Configuration;
using lwnsim.Devices.Factory;
using lwnsim.Poco.Socket.Io;
using SocketIOClient;
using SocketIOClient.JsonSerializer;

namespace Services;

public class LwnConnectionService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly SimDeviceFactory _deviceFactory;
    private readonly LwnConnection _connection;
    private readonly HttpClient _httpClient;
    private readonly SocketIO _socketIo;
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        PropertyNameCaseInsensitive = true
    };

    private DateTime _lastReceivedSocketIo;
    private readonly Timer _socketIoTimer;

    #region Contructor

    public LwnConnectionService(ILogger<LwnConnectionService> logger, IHostApplicationLifetime applicationLifetime, IOptions<LwnConnection> options, IHttpClientFactory factory, SimDeviceFactory deviceFactory)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _deviceFactory = deviceFactory;
        _connection = options.Value;
        _httpClient = factory.CreateClient(nameof(LwnConnectionService));
        _lastReceivedSocketIo = DateTime.Now;
        _socketIoTimer = new Timer((state =>
        {
            var duration = DateTime.Now - _lastReceivedSocketIo;
            if (duration.TotalMinutes > 5)
            {
                _logger.LogError("No Socket.IO Messages for more then {Duration} minutes.", duration.TotalMinutes);
                _applicationLifetime.StopApplication();
            }

        }), null, 5_000, 30_000);
        
        
        _socketIo = new SocketIO(_connection.Connection, new SocketIOOptions
        {
            EIO = 3,
        });
        _socketIo.JsonSerializer = new CustomJsonSerializer(3);

        _socketIo.OnAny((name, response) =>
        {
            _lastReceivedSocketIo = DateTime.Now;
            switch (name)
            {
                case Events.EventError:
                case Events.EventToggleStateDevice:
                case Events.EventToggleStateGateway:
                    _logger.LogError("[{Channel}] {Response}", name, response);
                    break;
                default:
                    _logger.LogTrace("[{Channel}] {Response}", name, response);
                    break;
            }
        });
        
        _socketIo.On( Events.EventDev, async response =>
        {
            var message = response.GetValue<ConsoleLog>();
            await _deviceFactory.ProcessAsync(message);
        });

        _socketIo.On(Events.EventLog, response =>
        {
        });
        
        _socketIo.On(Events.EventResponseCommand, response =>
        {
        });
        
        _socketIo.On(Events.EventReceivedDownlink, async response =>
        {
            var downlink = response.GetValue<ReceiveDownlink>();
            await _deviceFactory.ProcessAsync(downlink);
        });
        _socketIo.On(Events.EventReceivedUplink, async response =>
        {
            var uplink = response.GetValue<ReceiveUplink>();
            await _deviceFactory.ProcessAsync(uplink);
        });
        
        _socketIo.OnConnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connected");
        };
        _socketIo.OnDisconnected += (sender, e) =>
        {
            _logger.LogError("Socket.IO Disconnected. Restart application.");
            _applicationLifetime.StopApplication();
        };
        _socketIo.OnReconnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Reconnected");
        };
        
    }

    #endregion
    
    /// <summary>
    /// Sends the Payload on next Uplink Cycle defined in the Simulated device.
    /// </summary>
    /// <param Name="id">Simulated Device ID</param>
    /// <param Name="payload">Payload</param>
    /// <param Name="cancellationToken">Propagates notification that operations should be canceled.</param>
    public async Task EnqueuePayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        // 42["send-uplink",{"id":0,"mtype":"ConfirmedDataUp","payload":"0xffff01630400c1"}]	1673651564.2782326
        _logger.LogInformation("Enqueue Payload {Payload}", payload);
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventSendUplink, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
    }
    
    /// <summary>
    /// Sends the Payload on next Uplink Cycle defined in the Simulated device.
    /// </summary>
    /// <param Name="id">Simulated Device ID</param>
    /// <param Name="payload">Payload</param>
    /// <param Name="cancellationToken">Propagates notification that operations should be canceled.</param>
    public async Task EnqueuePayloadAsync(int id, byte[] buffer, CancellationToken cancellationToken)
    {
        // 42["send-uplink",{"id":0,"mtype":"ConfirmedDataUp","payload":"0xffff01630400c1"}]	1673651564.2782326
        var payload = "0x" + Convert.ToHexString( buffer );
        _logger.LogInformation("Enqueue Payload {Payload}", payload);

        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventSendUplink, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
    }
    
    /// <summary>
    /// Sends the Payload on next Uplink Cycle defined in the Simulated device.
    /// </summary>
    /// <param Name="id">Simulated Device ID</param>
    /// <param Name="payload">Payload</param>
    /// <param Name="cancellationToken">Propagates notification that operations should be canceled.</param>
    public async Task EnqueuePayloadAsync<T>(T device, CancellationToken cancellationToken = default) where T : ISimuDevice, IEncoder
    {
        if(device is not IEncoder encoder) return;
        
        var payload = "0x" + Convert.ToHexString( encoder.Encode() );
        _logger.LogInformation("[{Id}:{Name}] Enqueue Payload {Payload}", device.Id, device.Name, payload);

        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventSendUplink, cancellationToken, new NewPayload(){ Id = device.Id, MType = "ConfirmedDataUp", Payload = payload});
    }
    
    /// <summary>
    /// Change the Upload Payload for Device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <param name="payload">Payload (0x... for binary data)</param>
    public async Task ChangePayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventChangePayload, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
    }

    #region Implement Hosted Service

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var devices = await GetDevicesAsync(cancellationToken);
            
        foreach (var device in devices)
        {
            await _deviceFactory.ProcessAsync(device);
        }

        await StartSimulatorAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        //await StopSimulatorAsync(cancellationToken);
        await _socketIoTimer.DisposeAsync();
        _socketIo.Dispose();
    }
    

    #endregion
    
    private async Task<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await _httpClient
            .GetFromJsonAsync<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>>("devices", _jsonOptions, cancellationToken: cancellationToken);

        return devices ?? Enumerable.Empty<lwnsim.Poco.Http.LwnDeviceResponse>();
    }

    public async Task StopSimulatorAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient
            .GetStringAsync("stop",cancellationToken: cancellationToken);
    }

    private async Task StartSimulatorAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient
            .GetStringAsync("start", cancellationToken: cancellationToken);
        
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
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

}