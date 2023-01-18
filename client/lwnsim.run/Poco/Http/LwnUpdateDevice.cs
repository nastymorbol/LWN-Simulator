using System.Text.Json.Serialization;

namespace lwnsim.Poco.Http;

#pragma warning disable CS8618
public class LwnUpdateDevice
{
    [JsonPropertyName("id")] public int id { get; set; }
    [JsonPropertyName("info")] public Info info { get; set; }
}

