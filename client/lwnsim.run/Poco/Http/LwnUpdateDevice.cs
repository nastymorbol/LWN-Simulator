using System.Text.Json.Serialization;

namespace lwnsim.Poco.Http;

public class LwnUpdateDevice
{
    [JsonPropertyName("id")] public int id { get; set; }
    [JsonPropertyName("info")] public Info info { get; set; }
}

