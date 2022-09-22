using MahjongServer.Model;
using Newtonsoft.Json;

namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001, //创建房间
    JoinRoom = 1002 // 加入房间
}

public class LoginReq
{
    public string username = "";
    public string password = "";
}

public class LoginAck
{
    public short errCode;
    public string username = "";
    public short id;
    public short gender;
    public int coin;
    public int diamond;
}

public class CreateRoomReq
{
    public short userId;
    public short currentCycle;
    public short totalCycle;
}

public class PlayerInfo
{
    [JsonIgnore]
    public Client? client;
    public string username = "";
    public short id;
    public short dealerWind;
    public int coin;
}

public class CreateRoomAck
{
    public short errCode;
    public short roomId;
    public short currentCycle;
    public short totalCycle;
    public byte dealerWind;
    public List<PlayerInfo> players = new();
}