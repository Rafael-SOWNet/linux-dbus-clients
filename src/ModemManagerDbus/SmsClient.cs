using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus;

namespace ModemManagerDbus;

public sealed class SmsClient
{
    private readonly ISms _proxy;
    private readonly ObjectPath _path;

    public SmsClient(Connection connection, ObjectPath objectPath)
    {
        _path = objectPath;
        _proxy = connection.CreateProxy<ISms>(MmConstants.ModemManagerInterface, objectPath);
    }

    public ObjectPath ObjectPath => _path;

    public Task SendAsync() => _proxy.SendAsync();

    public Task StoreAsync(MMSmsStorage storage) => _proxy.StoreAsync((uint)storage);

    public async Task<MMSmsState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("State").WaitAsync(cancellationToken);
        return (MMSmsState)val;
    }

    public async Task<MMSmsPduType> GetPduTypeAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("PduType").WaitAsync(cancellationToken);
        return (MMSmsPduType)val;
    }

    public Task<string> GetNumberAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Number").WaitAsync(cancellationToken);

    public Task<string> GetTextAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("Text").WaitAsync(cancellationToken);

    public Task<byte[]> GetDataAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<byte[]>("Data").WaitAsync(cancellationToken);

    public Task<string> GetSMSCAsync(CancellationToken cancellationToken = default)
        => _proxy.GetAsync<string>("SMSC").WaitAsync(cancellationToken);

    public async Task<IDictionary<MMSmsValidityType, object>> GetValidityAsync(CancellationToken cancellationToken = default)
    {
        var raw = await _proxy.GetAsync<object>("Validity").WaitAsync(cancellationToken);

        var dict = new Dictionary<MMSmsValidityType, object>();

        if (raw is object[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
                dict[(MMSmsValidityType)(uint)i] = arr[i]!;
            return dict;
        }

        if (raw is System.Collections.IEnumerable enumerable)
        {
            var index = 0u;
            foreach (var element in enumerable)
            {
                dict[(MMSmsValidityType)index] = element!;
                index++;
            }
            return dict;
        }

        throw new InvalidOperationException($"Unexpected D-Bus type for Sms.Validity: {raw?.GetType().FullName ?? "<null>"}");
    }

    public async Task<int> GetClassAsync(CancellationToken cancellationToken = default)
        => await _proxy.GetAsync<int>("Class").WaitAsync(cancellationToken);

    public async Task<MMSmsCdmaTeleserviceId> GetTeleserviceIdAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("TeleserviceId").WaitAsync(cancellationToken);
        return (MMSmsCdmaTeleserviceId)val;
    }

    public async Task<MMSmsCdmaServiceCategory> GetServiceCategoryAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("ServiceCategory").WaitAsync(cancellationToken);
        return (MMSmsCdmaServiceCategory)val;
    }

    public async Task<bool> GetDeliveryReportRequestAsync(CancellationToken cancellationToken = default)
        => await _proxy.GetAsync<bool>("DeliveryReportRequest").WaitAsync(cancellationToken);

    public async Task<MMSmsPduType> GetMessageReferenceAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("MessageReference").WaitAsync(cancellationToken);
        return (MMSmsPduType)val;
    }

    private static DateTime ParseMmIso8601(string s)
    {
        // ModemManager uses RFC3339Nano strings.
        var dto = DateTimeOffset.ParseExact(s, "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK", CultureInfo.InvariantCulture);
        return dto.DateTime;
    }

    public async Task<DateTime> GetTimestampAsync(CancellationToken cancellationToken = default)
    {
        var res = await _proxy.GetAsync<string>("Timestamp").WaitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(res))
            throw new InvalidOperationException("No timestamp available");
        return ParseMmIso8601(res);
    }

    public async Task<DateTime> GetDischargeTimestampAsync(CancellationToken cancellationToken = default)
    {
        var res = await _proxy.GetAsync<string>("DischargeTimestamp").WaitAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(res))
            throw new InvalidOperationException("No discharge timestamp available");
        return ParseMmIso8601(res);
    }

    public async Task<MMSmsDeliveryState> GetDeliveryStateAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("DeliveryState").WaitAsync(cancellationToken);
        return (MMSmsDeliveryState)val;
    }

    public async Task<MMSmsStorage> GetStorageAsync(CancellationToken cancellationToken = default)
    {
        var val = await _proxy.GetAsync<uint>("Storage").WaitAsync(cancellationToken);
        return (MMSmsStorage)val;
    }
}
