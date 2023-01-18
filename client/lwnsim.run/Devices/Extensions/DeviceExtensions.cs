using System.Reflection;
using lwnsim.Devices.Interfaces;

namespace lwnsim.Devices.Extensions;

public static class DeviceExtensions
{
    public static void ApplyValuesFromStorage(this ISimuDevice device, IReadOnlyDictionary<string, object> data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        var fields = device.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fieldInfo in fields)
        {
            if(data.TryGetValue(fieldInfo.Name, out var obj))
                fieldInfo.SetValue(device, obj);
        }
    }
    
    public static void StoreValuesInStorage(this ISimuDevice device, IDictionary<string, object>? data)
    {
        if(data == null)
            return;
        
        var fields = device.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var fieldInfo in fields)
        {
            if(fieldInfo.IsInitOnly) continue;
            if (fieldInfo != null) data[fieldInfo.Name] = fieldInfo.GetValue(device);
        }
    }

}