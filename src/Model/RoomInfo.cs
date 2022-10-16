using MahjongServer.Protocol;
using Newtonsoft.Json;

namespace MahjongServer.Model;

public class RoomInfo
{
    public int roomId;
    public short currentCycle = 1;
    public short totalCycle = 4;
    public List<PlayerInfo> players = new();

    [JsonIgnore]
    public CardDeck deck = new();
    [JsonIgnore]
    public byte lastPlayCardDealer; // 上次出牌玩家座位
    [JsonIgnore]
    public byte lastPlayCard; // 上次出牌卡牌
    [JsonIgnore]
    public List<OperationReq> operationList = new();
}