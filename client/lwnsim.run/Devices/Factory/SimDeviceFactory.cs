using System.Text.Json;
using System.Text.Json.Serialization;
using lwnsim.Devices.Extensions;
using lwnsim.Devices.Interfaces;
using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices.Factory;

public class SimDeviceFactory
{
    const string key_can_handle = "can-handle-device";
    const string key_name = "<Name>k__BackingField";
    const string key_id = "<Id>k__BackingField";

    private readonly ILogger<SimDeviceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    //private static readonly Dictionary<int, LwnDeviceResponse> _deviceResponses = new();
    //private const string DataDirectory = "./persistence";

    public SimDeviceFactory(ILogger<SimDeviceFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task ProcessAsync()
    {
        foreach (var device in _serviceProvider.GetDevices())
        {
            await device.ProcessAsync();
            device.StoreValuesInStorage();
        }
    }

    public async Task ProcessAsync(LwnDeviceResponse deviceResponse)
    {
        // Device not handled yet
        var device = _serviceProvider.GetDevice(deviceResponse.id, deviceResponse.Info.Name);
        if (device == null) return;

        await device.ProcessAsync(deviceResponse);
        device.StoreValuesInStorage();
    }


    public async Task ProcessAsync(ReceiveDownlink downlink)
    {
        var device = _serviceProvider.GetDevice(downlink.Name);
        if (device == null) return;

        await device.ProcessAsync(downlink);
        device.StoreValuesInStorage();
    }
    
    public async Task ProcessAsync(ConsoleLog message)
    {
        var device = _serviceProvider.GetDevice(message.Name);
        if (device == null) return;

        await device.ProcessAsync(message);
        device.StoreValuesInStorage();
    }

    public async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        var device = _serviceProvider.GetDevice(deviceResponse.Name);
        if (device == null) return;

        await device.ProcessAsync(deviceResponse);
        device.StoreValuesInStorage();
    }
    




    


}