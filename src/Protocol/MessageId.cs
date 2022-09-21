using Newtonsoft.Json;

namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001, //创建房间
    JoinRoom = 1002, // 加入房间
}

public struct LoginReq
{
    public string username;
    public string password;
}

public struct LoginAck
{
    public short errCode;
    public string username;
    public short id;
    public short gender;
    public int coin;
    public int diamond;
}

public struct CreateRoomReq
{
    public short userId;
    public short currentCycle;
    public short totalCycle;
}

public struct PlayerInfo
{
    [JsonIgnore]
    public ClientState state;
    public string username;
    public short id;
    public short dealerWind;
    public int coin;
}

public struct CreateRoomAck
{
    public short errCode;
    public short roomId;
    public short currentCycle;
    public short totalCycle;
    public byte dealerWind;
    public List<PlayerInfo> players;
}