using System;
using System.Collections.Generic;
using System.Linq;
using Tmds.DBus;

namespace ModemManagerDbus;

public readonly record struct RawPcoData(uint SessionId, bool Complete, byte[] RawData);

public readonly record struct Network3Gpp(
    MMModem3gppNetworkAvailability Status,
    string OperatorLong,
    string OperatorShort,
    string OperatorCode,
    string Mcc,
    string Mnc,
    MMModemAccessTechnology AccessTechnology);

public sealed class NetworkScanResult
{
    public IReadOnlyList<Network3Gpp> Networks { get; init; } = Array.Empty<Network3Gpp>();
    public DateTimeOffset LastScan { get; init; }
    public double ScanDurationSeconds { get; init; }
    public bool Recent { get; init; }
}
