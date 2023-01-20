using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CP.IO.Ports;
using ModbusRx.Device;

namespace Services;

public class ModbusClientService
{
    private readonly ILogger _logger;
    private readonly ModbusClientOption _config;

    public ModbusClientService(ILogger<ModbusClientService> logger, IOptions<ModbusClientOption> options)
    {
        _logger = logger;
        _config = options.Value;

    }

    public IModbusMaster GetModbusMaster()
    {
        var masterClient = new TcpClientRx(_config.Ip, _config.Port); 
        var master = ModbusIpMaster.CreateIp(masterClient);
        //var result = master.ReadInputRegistersAsync(1, 135, 3).Result;
        return master;
    }
}

public class ModbusClientOption
{
    public string Ip { get; set; }
    public int Port { get; set; }
}