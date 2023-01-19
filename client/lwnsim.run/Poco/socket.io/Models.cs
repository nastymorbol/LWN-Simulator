using System.Text;
using System.Text.Json.Serialization;
#pragma warning disable CS8618

namespace lwnsim.Poco.Socket.Io;

public class ConsoleLog {
	public string Name {get;set;}
	public string Message {get;set;}
}

public class NewStatusDev {
	public string DevEUI   {get;set;}
	public string DevAddr  {get;set;}
	public string NwkSKey  {get;set;}
	public string AppSKey  {get;set;}
	public string FCntDown {get;set;}
	public string FCnt     {get;set;}
}

public class ReceiveDownlink {
	[JsonPropertyName("time")]   public long   Time          {get;set;}
	[JsonPropertyName("Name")]   public string Name          {get;set;}
	[JsonPropertyName("fport")]  public byte   FPort         {get;set;}
	[JsonPropertyName("buffer")] public string Base64        {get;set;}
	[JsonIgnore] public byte[]? Buffer => string.IsNullOrWhiteSpace(Base64) ? null : Convert.FromBase64String(Base64);
	[JsonIgnore] public string? Utf8Buffer => Buffer == null ? null : Encoding.UTF8.GetString(Buffer);
}

public class ReceiveUplink {
	[JsonPropertyName("time")]   public long   Time          {get;set;}
	[JsonPropertyName("Name")]   public string Name          {get;set;} 
	[JsonPropertyName("buffer")] public string Base64        {get;set;}
	[JsonIgnore] public byte[]? Buffer => string.IsNullOrWhiteSpace(Base64) ? null : Convert.FromBase64String(Base64);
	[JsonIgnore] public string? Utf8Buffer => Buffer == null ? null : Encoding.UTF8.GetString(Buffer);
}

public class NewPayload {
	[JsonPropertyName("id")] public int Id      {get;set;}
	[JsonPropertyName("mtype")] public string MType   {get;set;}
	[JsonPropertyName("payload")] public string Payload {get;set;}
}

public class NewLocation {
	public string Id          {get;set;}
	public string Latitude    {get;set;}
	public string Longitude   {get;set;}
}

public class MacCommand {
	public string Altitude    {get;set;}
	public string Id            {get;set;}
	public string CID           {get;set;}
	public string Periodicity   {get;set;}
}
