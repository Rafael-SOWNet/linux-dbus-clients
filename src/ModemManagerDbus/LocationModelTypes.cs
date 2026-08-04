using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class CurrentLocation
{
    public ThreeGppLacCiLocation ThreeGppLacCi { get; set; } = new();
    public GpsRawLocation GpsRaw { get; set; } = new();
    public GpsNmeaLocation GpsNmea { get; set; } = new();
    public CdmaBsLocation CdmaBs { get; set; } = new();
}

public sealed class ThreeGppLacCiLocation
{
    public string Mcc { get; set; } = string.Empty;
    public string Mnc { get; set; } = string.Empty;
    public string Lac { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Tac { get; set; } = string.Empty;
}

public sealed class GpsRawLocation
{
    public DateTimeOffset UtcTime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
}

public sealed class GpsNmeaLocation
{
    public IReadOnlyList<string> NmeaSentences { get; set; } = Array.Empty<string>();
}

public sealed class CdmaBsLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
