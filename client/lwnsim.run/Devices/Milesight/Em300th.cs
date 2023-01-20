using lwnsim.Poco.Socket.Io;

namespace lwnsim.Devices.Milesight;

public abstract class Em300th : SimDeviceBase, IEncoder
{
    internal readonly ILogger<Em300th> _logger;
    internal readonly LwnConnectionService _connectionService;
    internal readonly ModbusClientService _modbusClientService;
    internal readonly Random _random = new (DateTime.Now.Millisecond);

    internal int _uplinkCounter;
    internal Em300Payload _instance = new();

    protected Em300th(ILogger<Em300th> logger, LwnConnectionService connectionService, ModbusClientService modbusClientService)
    {
        _logger = logger;
        _connectionService = connectionService;
        _modbusClientService = modbusClientService;
    }

    protected override bool CanHandleDeviceResponse()
    {
        return Name.Contains("EM300-TH", StringComparison.InvariantCultureIgnoreCase);
    }

    public override Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        const double rad = Math.PI / 180;
        _instance.Battery = MathF.Sin((float) (rad * (90 - _uplinkCounter))) * 100;
        _instance.Temperature = MathF.Sin((float) (rad * _uplinkCounter)) * 30;
        _instance.Humidity = MathF.Sin((float) (rad * _uplinkCounter)) * 100;

        _uplinkCounter++;
        if (_uplinkCounter >= 90)
            _uplinkCounter = 0;
        return base.ProcessAsync(deviceResponse);
    }

    private async Task<Em300Payload> GetModbusInstance(ushort startAddress)
    {
        using var master = _modbusClientService.GetModbusMaster();
        var result = await master.ReadInputRegistersAsync(1, startAddress, 3);
        var batt = (short)result[0];
        var temp = (short)result[1];
        var humi = (short)result[2];

        var instance = new Em300Payload()
        {
            Temperature = temp / 10.0,
            Battery = batt,
            Humidity = humi / 2.0
        };

        return instance;
    }

    internal async Task<bool> ValidateModbusValues(ushort startAddress)
    {
        try
        {
            var modbusInstance = await GetModbusInstance(startAddress);
            var modbusData = modbusInstance.Encode();
            var loraData = _instance.Encode();
            if (modbusData.SequenceEqual(loraData)) return true;
        }
        catch (Exception e)
        {
            _logger.LogError("Modbus Error: {message}", e.Message);
        }

        return false;
    }
    public byte[] Encode()
    {
        return _instance.Encode();
    }
}

public class Em300Th_Oat : Em300th
{
    public Em300Th_Oat(ILogger<Em300Th_Oat> logger, 
        LwnConnectionService connectionService, 
        ModbusClientService modbusClientService) 
        : base(logger, connectionService, modbusClientService)
    {
    }

    protected override bool CanHandleDeviceResponse()
    {
        return base.CanHandleDeviceResponse() && Name.Contains("OAT");
    }
    
    public override async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        if (_instance.Temperature != null)
        {
            if (await ValidateModbusValues(135))
            {
                _logger.LogInformation("Modbus and Lora data are equal");
            }
            else
            {
                _logger.LogError("Modbus and Lora data are different");
            }
        }
        await base.ProcessAsync(deviceResponse);
        _instance.Temperature -= 30;
        _logger.LogInformation("Current Instance: {Instance}", _instance);
        await _connectionService.EnqueuePayloadAsync(this, CancellationToken.None);
    }
}

public class Em300Th_Rot : Em300th
{
    public Em300Th_Rot(ILogger<Em300Th_Rot> logger, 
        LwnConnectionService connectionService,
        ModbusClientService modbusClientService) 
        : base(logger, connectionService, modbusClientService)
    {
    }

    protected override bool CanHandleDeviceResponse()
    {
        return base.CanHandleDeviceResponse() && Name.Contains("ROT");
    }
    
    public override async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        if (_instance.Temperature != null)
        {
            if (await ValidateModbusValues(143))
            {
                _logger.LogInformation("Modbus and Lora data are equal");
            }
            else
            {
                _logger.LogError("Modbus and Lora data are different");
            }
        }
        await base.ProcessAsync(deviceResponse);
        _logger.LogInformation("Current Instance: {Instance}", _instance);
        await _connectionService.EnqueuePayloadAsync(this, CancellationToken.None);
    }
}