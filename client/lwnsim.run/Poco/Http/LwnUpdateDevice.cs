using System.Text.Json.Serialization;

namespace lwnsim.Poco.Http;

#pragma warning disable CS8618
public class LwnUpdateDevice
{
    [JsonPropertyName("id")] public int id { get; set; }
    [JsonPropertyName("Info")] public Info info { get; set; }
}

