using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMModem3gppUssdSessionState : uint
{
    Unknown = 0,
    Idle = 1,
    Active = 2,
    UserResponse = 3,
}

public class BearerIpConfig
{
    public MMBearerIpMethod Method { get; set; }
    public string Address { get; set; } = string.Empty;
    public uint Prefix { get; set; }
    public string Dns1 { get; set; } = string.Empty;
    public string Dns2 { get; set; } = string.Empty;
    public string Dns3 { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public uint Mtu { get; set; }
    public MMBearerIpFamily IpFamily { get; set; }
}

public readonly record struct BearerStats(ulong RxBytes, ulong TxBytes, uint Duration);

public readonly record struct AudioFormat(string Encoding, string Resolution, uint Rate);
