using System.Reflection;
using System.Text.Json;
using Jurassic;
using Jurassic.Library;

namespace lwnsim.JsDecoder;

public static class JsDecoderExtensions
{
    private static readonly ScriptEngine _engine = new();
    
    public static ArrayInstance ToJsArray(this byte[] buffer)
    {
        var ju = _engine.Array.New();
        foreach (var b in buffer)
        {
            ju.Push((int)b);
        }

        return ju;
    }
    
    public static T? MapTo<T>(this ObjectInstance? objectInstance) where T : new()
    {
        if (objectInstance == null)
            return default;
        
        _engine.SetGlobalValue("instance", objectInstance);
        var json = _engine.Evaluate<string>("JSON.stringify(instance);");

        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true
        });
    }
    
    public static T? Decode<T>(this ScriptEngine engine, byte[] buffer, byte fport) where T : new()
    {
        var instance = engine.CallGlobalFunction("Decoder", buffer.ToJsArray(), (int)fport) as ObjectInstance;
        
        return instance.MapTo<T>() ?? default;
    }
}