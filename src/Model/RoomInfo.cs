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
    public byte lastPlayCardDealer;
    [JsonIgnore]
    public byte lastPlayCard;
    [JsonIgnore]
    public List<OperationReq> operationList = new();
}