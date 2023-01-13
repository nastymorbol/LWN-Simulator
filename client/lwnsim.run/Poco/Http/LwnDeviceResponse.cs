namespace lwnsim.Poco.Http;

public class LwnDeviceResponse
{
    public int id { get; set; }
    public Info info { get; set; }
}

public class Info
{
    public string devEUI { get; set; }
    public string devAddr { get; set; }
    public string nwkSKey { get; set; }
    public string appSKey { get; set; }
    public string appKey { get; set; }
    public string name { get; set; }
    public Status status { get; set; }
    public Configuration configuration { get; set; }
    public Location location { get; set; }
    public Rxs[] rxs { get; set; }
}

public class Status
{
    public string mtype { get; set; }
    public string payload { get; set; }
    public bool active { get; set; }
    public InfoUplink infoUplink { get; set; }
    public int fcntDown { get; set; }
}

public class InfoUplink
{
    public int fport { get; set; }
    public int fcnt { get; set; }
}

public class Configuration
{
    public int region { get; set; }
    public int sendInterval { get; set; }
    public int ackTimeout { get; set; }
    public int range { get; set; }
    public bool disableFCntDown { get; set; }
    public bool supportedOtaa { get; set; }
    public bool supportedADR { get; set; }
    public bool supportedFragment { get; set; }
    public bool supportedClassB { get; set; }
    public bool supportedClassC { get; set; }
    public int dataRate { get; set; }
    public int rx1DROffset { get; set; }
    public int nbRetransmission { get; set; }
}

public class Location
{
    public double latitude { get; set; }
    public double longitude { get; set; }
    public int altitude { get; set; }
}

public class Rxs
{
    public int delay { get; set; }
    public int durationOpen { get; set; }
    public Channel channel { get; set; }
    public int dataRate { get; set; }
}

public class Channel
{
    public bool active { get; set; }
    public bool enableUplink { get; set; }
    public int freqUplink { get; set; }
    public int freqDownlink { get; set; }
    public int minDR { get; set; }
    public int maxDR { get; set; }
}

