using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

[Dictionary]
public sealed class SimpleProperties
{
    public string? pin = null;
    public string? operator_id = null;
    public string? apn = null;
    public MMBearerIpFamily? ip_type = null;
    public MMBearerAllowedAuth? allowed_auth = null;
    public string? user = null;
    public string? password = null;
    public string? number = null;
    public bool? allow_roaming = null;
    public MMModemCdmaRmProtocol? rm_protocol = null;
}

public sealed class SimpleStatus
{
    public MMModemState State { get; set; }
    public uint SignalQuality { get; set; }
    public IReadOnlyList<MMModemBand> CurrentBands { get; set; } = Array.Empty<MMModemBand>();
    public MMModemAccessTechnology AccessTechnology { get; set; }
    public MMModem3gppRegistrationState M3GppRegistrationState { get; set; }
    public string M3GppOperatorCode { get; set; } = string.Empty;
    public string M3GppOperatorName { get; set; } = string.Empty;
    public MMModemCdmaRegistrationState CdmaCdma1xRegistrationState { get; set; }
    public MMModemCdmaRegistrationState CdmaEvdoRegistrationState { get; set; }
    public uint CdmaSid { get; set; }
    public uint CdmaNid { get; set; }
}
