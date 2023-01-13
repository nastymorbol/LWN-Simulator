using System.Text.Json.Serialization;

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
