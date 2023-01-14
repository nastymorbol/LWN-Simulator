
namespace lwnsim.Poco.Socket.Io;

public class Events {
	public const string EventLog                = "console-sim";
	public const string EventError              = "console-error";
	public const string EventDev                = "log-dev";
	public const string EventGw                 = "log-gw";
	public const string EventToggleStateDevice  = "toggleState-dev";
	public const string EventToggleStateGateway = "toggleState-gw";
	public const string EventSaveStatus         = "save-status";
	public const string EventMacCommand         = "send-MACCommand";
	public const string EventResponseCommand    = "response-command";
	public const string EventChangePayload      = "change-payload";
	public const string EventSendUplink         = "send-uplink";
	public const string EventReceivedDownlink   = "rec-down";
	public const string EventChangeLocation     = "change-location";
	public const string EventGetParameters      = "get-regional-parameters";
}
