using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class ModemTimeZone
{
    public int Offset { get; set; }
    public int DstOffset { get; set; }
    public int LeapSeconds { get; set; }
}
