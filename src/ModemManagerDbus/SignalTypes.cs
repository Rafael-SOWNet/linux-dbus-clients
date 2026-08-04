using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMSignalPropertyType : uint
{
    Cdma = 0,
    Evdo = 1,
    Gsm = 2,
    Umts = 3,
    Lte = 4,
}

public sealed class SignalProperty
{
    public MMSignalPropertyType Type { get; init; }
    public double Rssi { get; init; }
    public double Ecio { get; init; }
    public double Sinr { get; init; }
    public double Io { get; init; }
    public double Rscp { get; init; }
    public double Rsrq { get; init; }
    public double Rsrp { get; init; }
    public double Snr { get; init; }
}
