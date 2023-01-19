using lwnsim.Devices.Interfaces;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices.Sensative;

public sealed class SensativeStrip : SimDeviceBase, IEncoder
{
    private readonly ILogger<SensativeStrip> _logger;
    private readonly LwnConnectionService _connectionService;
    private readonly Random _random = new (DateTime.Now.Millisecond);

    private SensativePayload _instance = new ();
    
    public SensativeStrip(ILogger<SensativeStrip> logger, LwnConnectionService connectionService)
    {
        _logger = logger;
        _connectionService = connectionService;

        //var engine = JavaScriptEngineSwitcher.Core.JsEngineSwitcher.Current.CreateDefaultEngine();
        //engine.ExecuteFile("./JsDecoder/strips-ttn-decoder.js");


        // var call1 = engine.Decode<SensativePayload>(Convert.FromHexString("ffff01630400c1"), 1);
        // var call2 = engine.Decode<SensativePayload>(Convert.FromHexString("ffff01630900110000"), 1);
        // var call3 = engine.Decode<SensativePayload>(Convert.FromHexString("ffff01630901110000"), 1);
        // var call4 = engine.Decode<SensativePayload>(encoded, 1);
    }


    protected override bool CanHandleDeviceResponse()
    {
        return Name.Contains("sensative", StringComparison.InvariantCultureIgnoreCase);
    }
    

    public override async Task ProcessAsync(ReceiveUplink message)
    {
        const double rad = Math.PI / 180;
        _instance ??= new();
            
        _instance.Door = !_instance.Door;
        _instance.Temperature = Math.Sin(rad * DateTime.Now.Minute * 1.5) * 20 + 10;
        _instance.AverageTemperature = Math.Sin(rad * DateTime.Now.Hour * 3.75) * 20 + 10;
        _instance.TempAlarm = (_instance.Temperature.Value > 25, _instance.Temperature.Value < 15);
        _instance.AvgTempAlarm = (_instance.AverageTemperature.Value > 25, _instance.AverageTemperature.Value < 15);
        _instance.Battery = (byte)_random.Next(0, 100);
            
        if (_instance.AvgTempAlarm is {LowAlarm: false, HighAlarm: false})
            _instance.AvgTempAlarm = null;
        if (_instance.TempAlarm is {LowAlarm: false, HighAlarm: false})
            _instance.TempAlarm = null;
        if(_instance.Door is {Value:false})
            _instance.DoorCount += 1;

        await _connectionService.EnqueuePayloadAsync(this, CancellationToken.None);
        _logger.LogInformation("Changed status to {State}", _instance);
    }

    public byte[] Encode() => _instance.Encode();
}