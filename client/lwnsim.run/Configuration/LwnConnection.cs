public class LwnConnection
{
#pragma warning disable CS8618
    public string Connection { get; set; }
#pragma warning restore CS8618

    public Uri ApiUrl => new UriBuilder(Connection + "/api/").Uri;
    public Uri WsHttpUrl => new UriBuilder(Connection + "/socket.io/").Uri;
    public Uri WsUrl => new UriBuilder(Connection.Replace("http", "ws") + "/socket.io/").Uri;
}