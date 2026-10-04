using NetworkManagerDbus.Builders;
using NetworkManagerDbus.Dto;
using Xunit;

namespace NetworkManagerDbus.Tests;

/// <summary>
/// Freezes the exact shape <see cref="ConnectionSettingsBuilder"/> puts on the wire: which
/// sections exist, which keys are in each, and the CLR type of every value - because that type is
/// what decides the D-Bus signature the daemon receives.
///
/// <para><b>Why the key-set assertions are the important ones.</b> NetworkManager's
/// <c>Connection.Update()</c> replaces the entire settings dictionary rather than merging into it.
/// So a key the builder stops writing is not left unchanged - it is deleted from the stored
/// profile. AGENTS.md section 4 describes that invariant; these tests are the thing that notices
/// when it breaks. A diff here means a setting is about to disappear from every profile that gets
/// updated.</para>
///
/// <para><b>Why the types are asserted and not just the values.</b> Several keys are only correct
/// because of a cast that is easy to drop. <c>prefix</c> and <c>metric</c> are uint32; <c>ssid</c>
/// is a UTF-8 byte array and not a string; <c>channel</c>, <c>link-local</c> and <c>pmf</c> are
/// int32. Section-level properties are coerced by NetworkManager's GObject property path, but the
/// entries inside <c>address-data</c> and <c>route-data</c> are read with
/// <c>g_variant_lookup</c>, which is an exact type match - and for <c>metric</c> and
/// <c>next-hop</c> a mismatch is silently defaulted rather than rejected.</para>
/// </summary>
public class ConnectionSettingsSignatureTests
{
    private static IDictionary<string, object> Section(NetworkConfigurationDto dto, string name)
    {
        var settings = ConnectionSettingsBuilder.Build(dto);
        Assert.True(
            settings.ContainsKey(name),
            "expected section " + name + ", got: " + string.Join(", ", settings.Keys.Order()));
        return settings[name];
    }

    private static void AssertKeys(IDictionary<string, object> section, params string[] expected)
        => Assert.Equal(expected.Order().ToArray(), section.Keys.Order().ToArray());

    // ---- which sections exist ----------------------------------------------------------

    [Fact]
    public void BridgeStatic_emits_connection_ip_and_bridge_only()
        => Assert.Equal(
            new[] { "bridge", "connection", "ipv4", "ipv6" },
            ConnectionSettingsBuilder.Build(Profiles.BridgeStatic()).Keys.Order());

    [Fact]
    public void EthernetSlave_emits_no_bridge_or_wifi_section()
        => Assert.Equal(
            new[] { "connection", "ipv4", "ipv6" },
            ConnectionSettingsBuilder.Build(Profiles.EthernetSlave()).Keys.Order());

    [Fact]
    public void WifiClient_emits_the_security_section()
        => Assert.Equal(
            new[] { "802-11-wireless", "802-11-wireless-security", "connection", "ipv4", "ipv6" },
            ConnectionSettingsBuilder.Build(Profiles.WifiClient()).Keys.Order());

    [Fact]
    public void Gsm_emits_a_gsm_section()
        => Assert.Equal(
            new[] { "connection", "gsm", "ipv4", "ipv6" },
            ConnectionSettingsBuilder.Build(Profiles.Gsm()).Keys.Order());

    // ---- the highest-consequence behaviour, pinned deliberately -------------------------

    /// <summary>
    /// A blank pre-shared key drops the WHOLE security section, not just the psk. On an
    /// access-point profile that is the difference between a protected AP and an open one, and
    /// because <c>Update()</c> replaces everything it also deletes the security settings already
    /// stored. This is current, deliberate behaviour; the test exists so that changing it has to
    /// be a decision rather than an accident.
    /// </summary>
    [Fact]
    public void Blank_preshared_key_drops_the_entire_security_section()
    {
        var dto = Profiles.WifiAccessPointSae();
        dto.WifiSecurity = new WifiSecurityDto { KeyManagement = "sae", PreSharedKey = "" };

        var settings = ConnectionSettingsBuilder.Build(dto);

        Assert.DoesNotContain("802-11-wireless-security", settings.Keys);
        Assert.Contains("802-11-wireless", settings.Keys);
    }

    // ---- per-key signatures -------------------------------------------------------------

    [Fact]
    public void Connection_section_signature()
    {
        var section = Section(Profiles.EthernetSlave(), "connection");

        AssertKeys(section, "id", "type", "uuid", "autoconnect", "interface-name", "master", "slave-type");
        Assert.IsType<string>(section["id"]);
        Assert.IsType<string>(section["uuid"]);
        Assert.IsType<bool>(section["autoconnect"]);
        Assert.IsType<string>(section["master"]);
        Assert.IsType<string>(section["slave-type"]);
    }

    [Fact]
    public void Bridge_section_is_two_booleans()
    {
        var section = Section(Profiles.BridgeStatic(), "bridge");

        AssertKeys(section, "stp", "multicast-snooping");
        Assert.IsType<bool>(section["stp"]);
        Assert.IsType<bool>(section["multicast-snooping"]);
    }

    [Fact]
    public void Static_ipv4_section_signature()
    {
        var section = Section(Profiles.BridgeStatic(), "ipv4");

        AssertKeys(section, "method", "address-data", "gateway", "dns-data", "route-data", "link-local");
        Assert.Equal("manual", section["method"]);
        Assert.IsType<int>(section["link-local"]);
        Assert.IsType<string[]>(section["dns-data"]);
        Assert.IsType<IDictionary<string, object>[]>(section["address-data"]);
        Assert.IsType<IDictionary<string, object>[]>(section["route-data"]);
    }

    /// <summary>
    /// <c>prefix</c> must be uint32. NetworkManager parses address entries with
    /// <c>g_variant_lookup</c> against an exact type, so a plain int here is a parse failure
    /// rather than a coercion.
    /// </summary>
    [Fact]
    public void Address_data_entries_use_uint_prefix()
    {
        var addresses = (IDictionary<string, object>[])Section(Profiles.BridgeStatic(), "ipv4")["address-data"];

        var entry = Assert.Single(addresses);
        AssertKeys(entry, "address", "prefix");
        Assert.IsType<string>(entry["address"]);
        Assert.IsType<uint>(entry["prefix"]);
    }

    /// <summary>
    /// The two keys NetworkManager discards SILENTLY on a type mismatch: <c>metric</c> falls back
    /// to -1 and <c>next-hop</c> to NULL, with no error raised to the caller and nothing logged.
    /// Every other mistyped key in this dictionary produces a loud parse failure instead, which is
    /// why these two get their own test.
    ///
    /// <para>Note on <c>metric</c> specifically: the builder casts it to uint, but
    /// <c>IpRouteDto.Metric</c> is already <c>uint?</c>, so that cast is a no-op and dropping it
    /// changes nothing - verified by mutation. The assertion here therefore guards against the DTO
    /// being widened or re-typed later, not against the cast being removed. <c>prefix</c> is the
    /// opposite case and the one that genuinely depends on its cast: <c>IpRouteDto.Prefix</c> and
    /// <c>IpAddressDto.Prefix</c> are <c>int</c>, so removing the cast really does change the wire
    /// type, and both mutations are caught by the tests above.</para>
    /// </summary>
    [Fact]
    public void Route_data_metric_is_uint_because_a_mismatch_is_silently_discarded()
    {
        var routes = (IDictionary<string, object>[])Section(Profiles.BridgeStatic(), "ipv4")["route-data"];

        var entry = Assert.Single(routes);
        AssertKeys(entry, "dest", "prefix", "next-hop", "metric");
        Assert.IsType<uint>(entry["prefix"]);
        Assert.IsType<uint>(entry["metric"]);
        Assert.IsType<string>(entry["next-hop"]);
    }

    [Fact]
    public void Ipv6_addr_gen_mode_is_an_int()
    {
        var section = Section(Profiles.BridgeStatic(), "ipv6");

        Assert.Contains("addr-gen-mode", section.Keys);
        Assert.IsType<int>(section["addr-gen-mode"]);
    }

    /// <summary>
    /// <c>ssid</c> is a UTF-8 byte array. It is the one key where the wrong type is also the
    /// obvious one: a string compiles, serialises happily, and is rejected by the daemon.
    /// </summary>
    [Fact]
    public void Wifi_section_signature_with_ssid_as_bytes()
    {
        var section = Section(Profiles.WifiAccessPointSae(), "802-11-wireless");

        AssertKeys(section, "ssid", "mode", "hidden", "band", "channel");
        Assert.IsType<byte[]>(section["ssid"]);
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("example-ap"), (byte[])section["ssid"]);
        Assert.Equal("ap", section["mode"]);
        Assert.IsType<bool>(section["hidden"]);
        Assert.IsType<string>(section["band"]);
        Assert.IsType<int>(section["channel"]);
    }

    [Fact]
    public void Wpa_psk_security_section_carries_only_key_mgmt_and_psk()
    {
        var section = Section(Profiles.WifiClient(), "802-11-wireless-security");

        AssertKeys(section, "key-mgmt", "psk");
        Assert.Equal("wpa-psk", section["key-mgmt"]);
    }

    /// <summary>
    /// SAE needs proto/pairwise/group spelled out and pmf=3; the builder's own comments record why
    /// (the Settings API rejects pmf=2 outright for sae). These keys are written but never read
    /// back by the mapper, so nothing else in the library would notice them disappearing.
    /// </summary>
    [Fact]
    public void Sae_security_section_signature()
    {
        var section = Section(Profiles.WifiAccessPointSae(), "802-11-wireless-security");

        AssertKeys(section, "key-mgmt", "psk", "pmf", "proto", "pairwise", "group");
        Assert.Equal("sae", section["key-mgmt"]);
        Assert.Equal(3, section["pmf"]);
        Assert.IsType<int>(section["pmf"]);
        Assert.IsType<string[]>(section["proto"]);
        Assert.IsType<string[]>(section["pairwise"]);
        Assert.IsType<string[]>(section["group"]);
    }

    [Fact]
    public void Gsm_section_is_apn_only()
    {
        var section = Section(Profiles.Gsm(), "gsm");

        AssertKeys(section, "apn");
        Assert.IsType<string>(section["apn"]);
    }
}
