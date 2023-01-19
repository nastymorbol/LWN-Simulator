using lwnsim.Devices.Interfaces;

namespace lwnsim.Devices.Milesight;

class Em300Payload : IEncoder
{
    public double? Battery { get; set; }
    public double? Temperature { get; set; }
    public double? Humidity { get; set; }
        
    /*
     *  --------------------- Payload Definition ---------------------
                           [channel_id] [channel_type] [channel_value]
        01: battery      -> 0x01         0x75          [1byte ] Unit: %
        03: temperature  -> 0x03         0x67          [2bytes] Unit: °C (℉)
        04: humidity     -> 0x04         0x68          [1byte ] Unit: %RH
        ------------------------------------------ EM300-TH */
    public byte[] Encode()
    {
        var buffer = new byte[10];
        var index = 0;
        if (Battery.HasValue)
        {
            buffer[index++] = 0x01; buffer[index++] = 0x75;
            buffer[index++] = (byte)Battery.Value;
        }
        if (Temperature.HasValue)
        {
            buffer[index++] = 0x03; buffer[index++] = 0x67;
            BitConverter.GetBytes( (short)(Temperature * 10) ).CopyTo(buffer,  index);
            index += 2;
        }
        if (Humidity.HasValue)
        {
            buffer[index++] = 0x04; buffer[index++] = 0x68;
            buffer[index++] = (byte)(Humidity * 2);
        }

        Array.Resize(ref buffer, index);

        return buffer;
    }
}