using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMModemCdmaActivationState : uint
{
    Unknown = 0,
    NotActivated = 1,
    Activating = 2,
    PartiallyActivated = 3,
    Activated = 4,
}

public enum MMModemCdmaRegistrationState : uint
{
    Unknown = 0,
    Registered = 1,
    Home = 2,
    Roaming = 3,
}

public enum MMCdmaActivationError : uint
{
    None = 0,
    Unknown = 1,
    Roaming = 2,
    WrongRadioInterface = 3,
    CouldNotConnect = 4,
    SecurityAuthenticationFailed = 5,
    ProvisioningFailed = 6,
    NoSignal = 7,
    TimedOut = 8,
    StartFailed = 9,
}

[Dictionary]
public sealed class CdmaProperty
{
    public string spc = default!;
    public ushort sid;
    public string mdn = default!;
    public string min = default!;
    public string? mn_ha_key = null;
    public string? mn_aaa_key = null;
    public byte[]? prl = null;
}
