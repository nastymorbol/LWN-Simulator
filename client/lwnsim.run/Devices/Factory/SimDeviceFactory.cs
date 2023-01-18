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
    private readonly ILogger<SimDeviceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly Dictionary<int, Dictionary<string, object>> _deviceData = new();
    private static readonly Dictionary<int, LwnDeviceResponse> _deviceResponses = new();
    private const string DataDirectory = "./persistence";

    public SimDeviceFactory(ILogger<SimDeviceFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public async Task ProcessAsync(LwnDeviceResponse deviceResponse)
    {
        _deviceResponses[deviceResponse.id] = deviceResponse;
        foreach (var device in _serviceProvider.GetServices<ISimuDevice>())
        {
            if(!DeviceCanHandle(device, deviceResponse, out var data)) continue;
            
            device.ApplyValuesFromStorage(data);
            await device.ProcessAsync(deviceResponse);
            device.StoreValuesInStorage(data);
        }
    }


    public async Task ProcessAsync(ReceiveDownlink downlink)
    {
        var id = GetDeviceIdByName(downlink.Name);
        foreach (var device in _serviceProvider.GetServices<ISimuDevice>())
        {
            if (!DeviceCanHandle(id, out var data)) continue;
            device.ApplyValuesFromStorage(data);
            await device.ProcessAsync(downlink);
            device.StoreValuesInStorage(data);
        }
    }
    
    public async Task ProcessAsync(ConsoleLog message)
    {
        var id = GetDeviceIdByName(message.Name);
        foreach (var device in _serviceProvider.GetServices<ISimuDevice>())
        {
            if (!DeviceCanHandle(id, out var data)) continue;
            device.ApplyValuesFromStorage(data);
            await device.ProcessAsync(message);
            device.StoreValuesInStorage(data);
        }
    }

    public async Task ProcessAsync(ReceiveUplink deviceResponse)
    {
        var id = GetDeviceIdByName(deviceResponse.Name);
        foreach (var device in _serviceProvider.GetServices<ISimuDevice>())
        {
            if (!DeviceCanHandle(id, out var data)) continue;
            device.ApplyValuesFromStorage(data);
            await device.ProcessAsync(deviceResponse);
            device.StoreValuesInStorage(data);
        }
    }
    
    private static int? GetDeviceIdByName(string name)
    {
        foreach (var (key, value) in _deviceResponses)
        {
            if (value.info.name == name)
                return key;
        }
        return null;
    }
    
    private static bool DeviceCanHandle(ISimuDevice device, LwnDeviceResponse response, out Dictionary<string, object> data)
    {
        const string can_handle_key = "can-handle-device";

        data = GetDeviceData(response.id);
        if (data == null) return false;
        
        if (data.TryGetValue(can_handle_key, out var value) && value is bool canHandle)
            return canHandle;

        canHandle = device.CanHandle(response);
        data[can_handle_key] = canHandle;
        return canHandle;
    }
    
    private static bool DeviceCanHandle(int? id, out Dictionary<string, object> data)
    {
        const string can_handle_key = "can-handle-device";

        data = GetDeviceData(id);

        if (data == null)
            return false;
        
        if (data.TryGetValue(can_handle_key, out var value) && value is bool canHandle)
            return canHandle;
        
        return false;
    }


    private static Dictionary<string, object>? GetDeviceData(int? deviceId)
    {
        if (deviceId == null)
            return null;
        
        if (_deviceData.TryGetValue(deviceId.Value, out var result))
            return result;
        
        _deviceData[deviceId.Value] = new()
        {
            {"id", deviceId.Value}
        };
        
        return _deviceData[deviceId.Value];
    }

}