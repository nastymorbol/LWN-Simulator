using System.Net.Http.Json;
using System.Text.Json;
using lwnsim.Configuration;
using lwnsim.Devices;
using lwnsim.Devices.Factory;
using lwnsim.Devices.Interfaces;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketIOClient;
using SocketIOClient.JsonSerializer;

#pragma warning disable CS8618
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

    public LwnConnectionService(ILogger<LwnConnectionService> logger, IHostApplicationLifetime applicationLifetime, IOptions<LwnConnection> options, IHttpClientFactory factory, SimDeviceFactory deviceFactory)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _deviceFactory = deviceFactory;
        _connection = options.Value;
        _httpClient = factory.CreateClient(nameof(LwnConnectionService));
        
        _socketIo = new SocketIO(_connection.Connection, new SocketIOOptions
        {
            EIO = 3,
        });
        _socketIo.JsonSerializer = new CustomJsonSerializer(3);
        
        _socketIo.On( Events.EventDev, async response =>
        {
            _logger.LogTrace("[{Channel}] {Response}", Events.EventDev, response);
            var message = response.GetValue<ConsoleLog>();
            await _deviceFactory.ProcessAsync(message);
        });

        _socketIo.On(Events.EventLog, response =>
        {
            _logger.LogTrace("[{Channel}] {Response}", Events.EventLog, response);
        });
        
        _socketIo.On(Events.EventResponseCommand, response =>
        {
            _logger.LogTrace("[{Channel}] {Response}", Events.EventResponseCommand, response);
        });
        
        _socketIo.On(Events.EventReceivedDownlink, async response =>
        {
            _logger.LogTrace("[{Channel}] {Response}", Events.EventResponseCommand, response);
            var downlink = response.GetValue<ReceiveDownlink>();
            await _deviceFactory.ProcessAsync(downlink);
        });
        _socketIo.On(Events.EventReceivedUplink, async response =>
        {
            _logger.LogTrace("[{Channel}] {Response}", Events.EventSendUplink, response);
            var uplink = response.GetValue<ReceiveUplink>();
            await _deviceFactory.ProcessAsync(uplink);
        });
        
        _socketIo.OnConnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Connected");
        };
        _socketIo.OnDisconnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Disconnected");
            _logger.LogError("Socket.IO Disconnected. Restart application.");
            _applicationLifetime.StopApplication();
        };
        _socketIo.OnReconnected += (sender, e) =>
        {
            _logger.LogInformation("Socket.IO Reconnected");
        };
        
    }


    public async Task<IEnumerable<lwnsim.Poco.Http.LwnDeviceResponse>> GetDevicesAsync(CancellationToken cancellationToken)
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

    public async Task StartSimulatorAsync(CancellationToken cancellationToken)
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
    
    
    /// <summary>
    /// Sends the Payload on next Downlink Cycle defined in the Simulated device.
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
    /// Sends the Payload on next Downlink Cycle defined in the Simulated device.
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
    /// Sends the Payload on next Downlink Cycle defined in the Simulated device.
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
    public async Task ChangePayloadAsync(int id, string payload, CancellationToken cancellationToken)
    {
        if(_socketIo.Disconnected)
            await _socketIo.ConnectAsync();
        await _socketIo.EmitAsync(Events.EventChangePayload, cancellationToken, new NewPayload(){ Id = id, MType = "ConfirmedDataUp", Payload = payload});
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var devices = await GetDevicesAsync(cancellationToken);
            
        foreach (var device in devices)
        {
            await _deviceFactory.ProcessAsync(device);
        }

        await StartSimulatorAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        //await StopSimulatorAsync(cancellationToken);
        
        if(_socketIo.Connected)
            _socketIo.Dispose();

        return Task.CompletedTask;
    }
}