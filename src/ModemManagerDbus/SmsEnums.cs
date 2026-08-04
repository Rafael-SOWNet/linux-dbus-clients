using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public enum MMSmsPduType : uint
{
    Unknown = 0,
    Deliver = 1,
    Submit = 2,
    StatusReport = 3,
}

public enum MMSmsState : uint
{
    Unknown = 0,
    Stored = 1,
    Receiving = 2,
    Received = 3,
    Sending = 4,
    Sent = 5,
}

public enum MMSmsValidityType : uint
{
    Unknown = 0,
    Relative = 1,
    Absolute = 2,
    Enhanced = 3,
}

public enum MMSmsCdmaTeleserviceId : uint
{
    Unknown = 0x0000,
}

public enum MMSmsCdmaServiceCategory : uint
{
    Unknown = 0x0000,
}

public enum MMSmsDeliveryState : uint
{
    Unknown = 0x100,
    CompletedReceived = 0x00,
}

public enum MMSmsStorage : uint
{
    Unknown = 0,
    Sm = 1,
    Me = 2,
    Mt = 3,
    Sr = 4,
    Bm = 5,
    Ta = 6,
}
