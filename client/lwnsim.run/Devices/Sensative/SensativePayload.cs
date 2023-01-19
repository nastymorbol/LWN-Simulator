using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using lwnsim.Devices.Interfaces;

namespace lwnsim.Devices.Sensative;

internal class SensativePayload : IEncoder
{
    #region Json Wrapper

       
    internal class DoubleValue : IEncoder
    {
        public double Value { get; set; }
        public byte[] Encode()
        {
            var result = (short) (Value * 10.0);
            return BitConverter.GetBytes(result).Reverse().ToArray();
        }
            
        public static implicit operator DoubleValue( double value ) => new() {Value = value};
    }
        
    internal class BoolValue :IEncoder
    {
        public bool Value { get; set; }
        public byte[] Encode()
        {
            return new []{ Value ? (byte)1 : (byte)0 } ;
        }
            
        public static implicit operator BoolValue( bool value ) => new() {Value = value};

        public static BoolValue operator !(BoolValue? lhs) => new () {Value = !lhs?.Value ?? true};
    }
        
    internal class ByteValue : IEncoder
    {
        public byte Value { get; set; }
        public byte[] Encode()
        {
            return new[] {Value};
        }
            
        public static implicit operator ByteValue( byte value ) => new() {Value = value};
    }
        
    internal class ByteStateValue : IEncoder
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
        
    internal class Int16Value : IEncoder
    {
        public short Value { get; set; }
        public byte[] Encode()
        {
            return BitConverter.GetBytes(Value).Reverse().ToArray();
        }
            
        public static implicit operator Int16Value( short value ) => new() {Value = value};
        public static Int16Value operator +( Int16Value? rhs, int lhs ) => new() {Value = (rhs?.Value ?? 0) + lhs > short.MaxValue ? (short)0 : (short)( (rhs?.Value ?? 0) + lhs)};
    }
        
    internal class Alarms : IEncoder
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
            if(value is IEncoder encode)
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