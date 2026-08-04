namespace NetworkManagerDbus.Dto;

public sealed class WifiModeRequestDto
{
    public WifiProfileMode Mode { get; set; }

    public WifiClientProfileRequestDto? Client { get; set; }
    public WifiAccessPointProfileRequestDto? AccessPoint { get; set; }
}
