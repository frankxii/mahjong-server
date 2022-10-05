using Newtonsoft.Json;

namespace MahjongServer.Model;

public class PlayerInfo : UserInfo
{
    [JsonIgnore]
    public new int diamond; // 钻石数量

    [JsonIgnore]
    public Client? client;

    public bool isReady; // 是否已准备
    public bool isSorted; // 是否已理牌
    public byte dealerWind; // 门风

    [JsonIgnore]
    public List<byte> handCard = new(); // 手牌
}