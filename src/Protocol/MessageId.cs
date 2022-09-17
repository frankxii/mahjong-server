namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001 //创建房间
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
    public int coin;
    public int diamond;
}