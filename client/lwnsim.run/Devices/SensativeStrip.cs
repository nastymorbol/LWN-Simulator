using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jurassic;
using Jurassic.Library;
using lwnsim.JsDecoder;
using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices;

public class SensativeStrip : SimDeviceBase
{
    private readonly ILogger<SensativeStrip> _logger;
    private readonly LwnConnectionService _connectionService;
    private readonly Random _random = new (DateTime.Now.Millisecond);

    enum SendState
    {
        AvgTemperatur,
        DoorClosed,
        DoorOpened
    }

    private SensativePayload _currentState = new SensativePayload();
    private bool _uplinkReceived;


    class SensativePayload : SensativePayload.IValueEncode
    {
        #region Json Wrapper

        interface IValueEncode
        {
            internal byte[] Encode();
        }
        
        internal class DoubleValue :IValueEncode
        {
            public double Value { get; set; }
            public byte[] Encode()
            {
                var result = (short) (Value * 10.0);
                return BitConverter.GetBytes(result).Reverse().ToArray();
            }
            
            public static implicit operator DoubleValue( double value ) => new() {Value = value};
        }
        
        internal class BoolValue :IValueEncode
        {
            public bool Value { get; set; }
            public byte[] Encode()
            {
                return new []{ Value ? (byte)1 : (byte)0 } ;
            }
            
            public static implicit operator BoolValue( bool value ) => new() {Value = value};

            public static BoolValue operator !(BoolValue? lhs) => new () {Value = !lhs?.Value ?? true};
        }
        
        internal class ByteValue : IValueEncode
        {
            public byte Value { get; set; }
            public byte[] Encode()
            {
                return new[] {Value};
            }
            
            public static implicit operator ByteValue( byte value ) => new() {Value = value};
        }
        
        internal class ByteStateValue : IValueEncode
        {
            public byte Value { get; set; }

            public string State => Value switch
            {
                0 => "dirty",
                1 => "occupied",
                2 => "cleaning",
                _ => "clean",
            };
            public byte[] Encode()
            {
                return new[] {Value};
            }
            
            public static implicit operator ByteStateValue( byte value ) => new() {Value = value};
        }
        
        internal class Int16Value : IValueEncode
        {
            public short Value { get; set; }
            public byte[] Encode()
            {
                return BitConverter.GetBytes(Value).Reverse().ToArray();
            }
            
            public static implicit operator Int16Value( short value ) => new() {Value = value};
            public static Int16Value operator +( Int16Value? rhs, int lhs ) => new() {Value = (rhs?.Value ?? 0) + lhs > short.MaxValue ? (short)0 : (short)( (rhs?.Value ?? 0) + lhs)};
        }
        
        internal class Alarms : IValueEncode
        {
            public bool HighAlarm { get; set; }
            public bool LowAlarm { get; set; }
            public byte[] Encode()
            {
                var buffer = (byte) 0;
                if (HighAlarm)
                    buffer |= 0x01;
                if (LowAlarm)
                    buffer |= 0x02;
                return new[] {buffer};
            }
            
            public static implicit operator Alarms((bool high, bool low)tuple) => new() { HighAlarm = tuple.high, LowAlarm = tuple.low};
        }

        private class IndexAttribute : Attribute
        {
            internal int Index { get; }

            public IndexAttribute(int index)
            {
                Index = index;
            }
        }
        
        #endregion
        
        [Index(1)] public byte? Battery { get; set; }
        [Index(2)] public DoubleValue? Temperature { get; set; }
        [Index(3)] public Alarms? TempAlarm { get; set; }
        [Index(4)] public DoubleValue? AverageTemperature { get; set; }
        [Index(5)] public Alarms? AvgTempAlarm { get; set; }
        [Index(6)] public DoubleValue? Humidity { get; set; }
        [Index(7)] public DoubleValue? Lux { get; set; }
        [Index(8)] public DoubleValue? Lux2 { get; set; }
        [Index(9)] public BoolValue? Door { get; set; }
        [Index(10)] public ByteValue? TamperReport { get; set; }
        [Index(11)] public ByteValue? TamperAlarm { get; set; }
        [Index(12)] public ByteValue? Flood { get; set; }
        [Index(13)] public ByteValue? FloodAlarm { get; set; }
        [Index(14)] public ByteValue? OilAlarm { get; set; }
        [Index(15)] public ByteValue? FoilAlarm { get; set; }
        [Index(16)] public ByteValue? UserSwitch1Alarm { get; set; }
        [Index(17)] public Int16Value? DoorCount { get; set; }
        [Index(18)] public ByteValue? Presence { get; set; }
        [Index(19)] public Int16Value? IRproximity { get; set; }
        [Index(20)] public Int16Value? IRcloseproximity { get; set; }
        [Index(21)] public ByteValue? CloseProximityAlarm { get; set; }
        [Index(22)] public ByteStateValue? DisinfectAlarm { get; set; }

        public byte[] Encode()
        {
            var buffer = new List<byte>(32);
            var properties = this.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);

            buffer.Add(0xff);
            buffer.Add(0xff);
            foreach (var property in properties)
            {
                var index = property.GetCustomAttribute<IndexAttribute>();
                if(index == null)
                    continue;
                var value = property.GetValue(this);
                if (value is byte b)
                {
                    buffer.Add((byte)index.Index);
                    buffer.Add(b);
                    continue;
                }
                if(value is IValueEncode encode)
                {
                    buffer.Add((byte)index.Index);
                    buffer.AddRange(encode.Encode());
                }
            }
            if(buffer.Count == 2)
                buffer.Add(0); // empty frame
            return buffer.ToArray();
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
    
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
    

    public override async Task ProcessAsync(LwnDeviceResponse device, Dictionary<string, object>? data)
    {
        if(!device.info.name.Contains("sensative", StringComparison.InvariantCultureIgnoreCase))
            return;
        // avg temp
        // await _lwnConnectionService.ChangePayloadAsync(deviceResponse.id, "0xffff01630400c1", stoppingToken);
        // await Task.Delay(30_000, stoppingToken);
        // door open
        //await _lwnConnectionService.SendPayloadAsync(deviceResponse.id, "0xffff01630900110000", stoppingToken);
        //await Task.Delay(30_000, stoppingToken);
        // door closed
        //await _lwnConnectionService.SendPayloadAsync(deviceResponse.id, "0xffff01630901110000", stoppingToken);
        //await Task.Delay(30_000, stoppingToken);

        
        if (!_uplinkReceived) return;
        _uplinkReceived = false;

        var payload = Convert.ToHexString( _currentState.Encode() );
        
        _logger.LogInformation("Send Payload {Payload}", payload);
        await _connectionService.SendPayloadAsync(device.id, payload, CancellationToken.None);
            

    }

    public override Task ProcessAsync(ReceiveDownlink downlink, Dictionary<string, object>? data)
    {
        return Task.CompletedTask;
    }

    public override Task ProcessAsync(ConsoleLog message, Dictionary<string, object> data)
    {
        if(!message.Name.Contains("sensative", StringComparison.InvariantCultureIgnoreCase))
            return base.ProcessAsync(message, data);;

        if (message.Message.Contains("Uplink sent", StringComparison.InvariantCultureIgnoreCase))
        {
            _uplinkReceived = true;

            const double rad = Math.PI / 180;
            _currentState ??= new();
            
            _currentState.Door = !_currentState.Door;
            _currentState.Temperature = Math.Sin(rad * DateTime.Now.Minute * 1.5) * 20 + 10;
            _currentState.AverageTemperature = Math.Sin(rad * DateTime.Now.Hour * 3.75) * 20 + 10;
            _currentState.TempAlarm = (_currentState.Temperature.Value > 25, _currentState.Temperature.Value < 15);
            _currentState.AvgTempAlarm = (_currentState.AverageTemperature.Value > 25, _currentState.AverageTemperature.Value < 15);
            _currentState.Battery = (byte)_random.Next(0, 100);
            
            if (_currentState.AvgTempAlarm is {LowAlarm: false, HighAlarm: false})
                _currentState.AvgTempAlarm = null;
            if (_currentState.TempAlarm is {LowAlarm: false, HighAlarm: false})
                _currentState.TempAlarm = null;
            if(_currentState.Door is {Value:false})
                _currentState.DoorCount += 1;

            _logger.LogInformation("Changed status to {State}", _currentState);
        }

        return base.ProcessAsync(message, data);
    }
}