// ReSharper disable ClassNeverInstantiated.Global

using System.Text.Json;
using System.Text.Json.Serialization;

namespace lwnsim.Poco.Http;

#pragma warning disable CS8618
public class LwnDeviceResponse
{
    [JsonPropertyName("id")]   public int id { get; set; }
    [JsonPropertyName("info")] public Info info { get; set; }
}

public class Info
{
    [JsonPropertyName("devEUI")]   public string devEUI { get; set; }
    [JsonPropertyName("devAddr")]   public string devAddr { get; set; }
    [JsonPropertyName("nwkSKey")]   public string nwkSKey { get; set; }
    [JsonPropertyName("appSKey")]   public string appSKey { get; set; }
    [JsonPropertyName("appKey")]   public string appKey { get; set; }
    [JsonPropertyName("name")]   public string name { get; set; }
    [JsonPropertyName("status")]   public Status status { get; set; }
    [JsonPropertyName("configuration")]   public Configuration configuration { get; set; }
    [JsonPropertyName("location")]   public Location location { get; set; }
    [JsonPropertyName("rxs")]   public Rxs[] rxs { get; set; }
}

public class Status
{
    [JsonPropertyName("mtype")] public Mtype Mtype { get; set; }
    [JsonPropertyName("payload")] public string Payload { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("infoUplink")] public InfoUplink InfoUplink { get; set; }
    [JsonPropertyName("fcntDown")] public int FcntDown { get; set; }
}

public class InfoUplink
{
    [JsonPropertyName("fport")] public int fport { get; set; }
    [JsonPropertyName("fcnt")] public int fcnt { get; set; }
}

public class Configuration
{
    [JsonPropertyName("region")] public int region { get; set; }
    [JsonPropertyName("sendInterval")] public int sendInterval { get; set; }
    [JsonPropertyName("ackTimeout")] public int ackTimeout { get; set; }
    [JsonPropertyName("range")] public int range { get; set; }
    [JsonPropertyName("disableFCntDown")] public bool disableFCntDown { get; set; }
    [JsonPropertyName("supportedOtaa")] public bool supportedOtaa { get; set; }
    [JsonPropertyName("supportedADR")] public bool supportedADR { get; set; }
    [JsonPropertyName("supportedFragment")] public bool supportedFragment { get; set; }
    [JsonPropertyName("supportedClassB")] public bool supportedClassB { get; set; }
    [JsonPropertyName("supportedClassC")] public bool supportedClassC { get; set; }
    [JsonPropertyName("dataRate")] public int dataRate { get; set; }
    [JsonPropertyName("rx1DROffset")] public int rx1DROffset { get; set; }
    [JsonPropertyName("nbRetransmission")] public int nbRetransmission { get; set; }
}

public class Location
{
    [JsonPropertyName("latitude")] public double latitude { get; set; }
    [JsonPropertyName("longitude")] public double longitude { get; set; }
    [JsonPropertyName("altitude")] public int altitude { get; set; }
}

public class Rxs
{
    [JsonPropertyName("delay")] public int delay { get; set; }
    [JsonPropertyName("durationOpen")] public int durationOpen { get; set; }
    [JsonPropertyName("channel")] public Channel channel { get; set; }
    [JsonPropertyName("dataRate")] public int dataRate { get; set; }
}

public class Channel
{
    [JsonPropertyName("active")] public bool active { get; set; }
    [JsonPropertyName("freqDownlink")] public int freqDownlink { get; set; }
    [JsonPropertyName("enableUplink")] public bool? enableUplink { get; set; }
    [JsonPropertyName("freqUplink")] public int? freqUplink { get; set; }
    [JsonPropertyName("minDR")] public int? minDR { get; set; }
    [JsonPropertyName("maxDR")] public int? maxDR { get; set; }
}

[JsonConverter(typeof(MtypeJsonConverter))]
public enum Mtype
{
    UnConfirmedDataUp,
    ConfirmedDataUp
}

public class MtypeJsonConverter : JsonConverter<Mtype>
{
    public override Mtype Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Could not deserialize MTYPE from {reader.TokenType}");

        var value = reader.GetString();
        if(string.IsNullOrWhiteSpace(value))
            throw new JsonException($"Could not deserialize MTYPE from empty string");

        if (value.Contains("unconfirmed", StringComparison.InvariantCultureIgnoreCase))
            return Mtype.UnConfirmedDataUp;
        
        return Mtype.ConfirmedDataUp;
    }

    public override void Write(Utf8JsonWriter writer, Mtype value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case Mtype.UnConfirmedDataUp:
                writer.WriteStringValue("UnConfirmedDataUp");
                break;
            case Mtype.ConfirmedDataUp:
                writer.WriteStringValue("ConfirmedDataUp");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
        }
        
        
    }
}