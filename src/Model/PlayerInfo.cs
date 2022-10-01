using Newtonsoft.Json;

namespace MahjongServer.Model;

public class PlayerInfo : UserInfo
{
    [JsonIgnore]
    public new int diamond;

    [JsonIgnore]
    public Client? client;

    public bool isReady;
    public byte dealerWind;
}