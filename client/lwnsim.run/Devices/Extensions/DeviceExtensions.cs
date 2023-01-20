using System.Reflection;

namespace lwnsim.Devices.Extensions;

public static class DeviceExtensions
{
    private static readonly Dictionary<int, Dictionary<string, object?>> _deviceData = new();

    const string key_type = "$type";
    const string key_can_handle = "can-handle-device";
    const string key_name = "<Name>k__BackingField";
    const string key_id = "<Id>k__BackingField";
    
    public static void ApplyValuesFromStorage(this ISimuDevice device, IDictionary<string, object?> data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        foreach (var fieldInfo in device.GetDeviceFields())
        {
            if(data.TryGetValue(fieldInfo.Name, out var obj))
                fieldInfo.SetValue(device, obj);
        }
    }
    
    public static void StoreValuesInStorage(this ISimuDevice device)
    {
        if(!_deviceData.TryGetValue(device.Id, out var data))
            return;
        
        foreach (var fieldInfo in device.GetDeviceFields())
        {
            if (fieldInfo != null) 
                data[fieldInfo.Name] = fieldInfo.GetValue(device);
        }
    }

    public static FieldInfo[] GetDeviceFields(this ISimuDevice device)
    {
        var allFieldInfos = new List<FieldInfo>();
        var type = device.GetType();
        do
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic) ?? Enumerable.Empty<FieldInfo>();
            foreach (var fieldInfo in fields)
            {
                if(fieldInfo.IsInitOnly) continue;
                allFieldInfos.Add(fieldInfo);
            }
            type = type.BaseType;
        } while (type?.IsAssignableTo(typeof(ISimuDevice)) ?? false);

        return allFieldInfos.ToArray();
    }

    public static ISimuDevice? GetDevice(this IServiceProvider serviceProvider, IDictionary<string, object?>? data)
    {
        if (data == null)
            return null;
        
        if (!data.TryGetValue(key_type, out var value) || value is not Type type || !type.IsAssignableTo(typeof(ISimuDevice))) 
            return null;

        var device = serviceProvider.GetDevice(type);
        device?.ApplyValuesFromStorage(data);
        return device;
    }
    
    public static ISimuDevice GetDevice(this IServiceProvider serviceProvider, Type type)
    {
        var device = serviceProvider.GetServices<ISimuDevice>().First(t => t.GetType() == type);;
        ArgumentNullException.ThrowIfNull(device);
        return device;
    }
    
    
    public static ISimuDevice? GetDevice(this IServiceProvider serviceProvider, int id, string name)
    {
        if (!_deviceData.TryGetValue(id, out var data))
        {
            data = GetOrCreateDeviceData(id); 
            ArgumentNullException.ThrowIfNull(data);

            if (serviceProvider.GetServices<ISimuDevice>().Any(service => DeviceCanHandle(service, id, name, data)))
            {
                return serviceProvider.GetDevice(data);
            }
        }
        return null;
    }
    
    private static bool DeviceCanHandle(this ISimuDevice device, int id, string name, IDictionary<string, object?> data)
    {
        if (data == null) return false;
        
        if (data.TryGetValue(key_id, out var value))
            return true;


        var tempData = new Dictionary<string, object?>()
        {
            {key_id, id},
            {key_name, name}
        };
        
        device.ApplyValuesFromStorage(tempData);

        if (!device.CanHandle()) return false;
        
        data[key_id] = id;
        data[key_name] = name;
        data[key_type] = device.GetType();
        

        return true;
    }
    
    private static Dictionary<string, object?>? GetOrCreateDeviceData(int? deviceId)
    {
        if (deviceId == null)
            return null;
        
        if (_deviceData.TryGetValue(deviceId.Value, out var result))
            return result;
        
        _deviceData[deviceId.Value] = new(8);
        
        return _deviceData[deviceId.Value];
    }
    
    public static ISimuDevice? GetDevice(this IServiceProvider serviceProvider, string name)
    {
        foreach (var (_, data) in _deviceData)
        {
            if (data.TryGetValue(key_name, out var value) && value is string deviceName && deviceName == name )
                return serviceProvider.GetDevice(data);
        }
        return null;
    }
    
    public static IEnumerable<ISimuDevice> GetDevices(this IServiceProvider serviceProvider)
    {
        foreach (var data in _deviceData.Values)
        {
            if (data.TryGetValue(key_type, out var value) && value is Type type)
                yield return serviceProvider.GetDevice(type);
        }
    }
}