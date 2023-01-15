using System.Reflection;
using lwnsim.Poco.Http;
using lwnsim.Poco.Socket.Io;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace lwnsim.Devices;

public class SimuDeviceFactory
{
    private readonly ILogger<SimuDeviceFactory> _logger;
    private readonly IServiceProvider _serviceProvider;
    private static readonly Dictionary<int, Dictionary<string, object>> _deviceData = new();
    private static readonly Dictionary<int, LwnDeviceResponse> _deviceResponses = new();


    public SimuDeviceFactory(ILogger<SimuDeviceFactory> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    
    public void Process(LwnDeviceResponse device)
    {
        _deviceResponses[device.id] = device;
        foreach (var simuDevice in _serviceProvider.GetServices<ISimuDevice>())
        {
            var data = GetDeviceData(device.id);
            ApplyValuesFromStorage(simuDevice, data);
            simuDevice.ProcessAsync(device, data);
            StoreValuesInStorage(simuDevice, data);
        }
    }

    private void ApplyValuesFromStorage(ISimuDevice device, Dictionary<string,object>? data)
    {
        var fields = device.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fieldInfo in fields)
        {
            if(data.TryGetValue(fieldInfo.Name, out var obj))
                fieldInfo.SetValue(device, obj);
        }
    }
    
    private void StoreValuesInStorage(ISimuDevice device, Dictionary<string,object>? data)
    {
        var fields = device.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fieldInfo in fields)
        {
            if(fieldInfo.IsInitOnly) continue;
            data[fieldInfo.Name] = fieldInfo.GetValue(device);
        }
    }

    private static Dictionary<string, object>? GetDeviceData(int? deviceId)
    {
        if (deviceId == null)
            return null;
        
        if (_deviceData.TryGetValue(deviceId.Value, out var result))
            return result;
        _deviceData[deviceId.Value] = new();
        return _deviceData[deviceId.Value];
    }

    public void Process(ReceiveDownlink downlink)
    {
        var id = GetDeviceIdByName(downlink.Name);
        foreach (var simuDevice in _serviceProvider.GetServices<ISimuDevice>())
        {
            var data = GetDeviceData(id);
            ApplyValuesFromStorage(simuDevice, data);
            simuDevice.ProcessAsync(downlink, data);
            StoreValuesInStorage(simuDevice,data);
        }
    }
    
    public void Process(ConsoleLog message)
    {
        var id = GetDeviceIdByName(message.Name);
        foreach (var simuDevice in _serviceProvider.GetServices<ISimuDevice>())
        {
            var data = GetDeviceData(id);
            if(data==null)
                continue;
            ApplyValuesFromStorage(simuDevice, data);
            simuDevice.ProcessAsync(message, data);
            StoreValuesInStorage(simuDevice,data);
        }
    }

    private static int? GetDeviceIdByName(string downlinkName)
    {
        foreach (var (key, value) in _deviceResponses)
        {
            if (value.info.name == downlinkName)
                return key;
        }

        return null;
    }


}