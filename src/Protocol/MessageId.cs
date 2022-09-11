namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001 //创建房间
}

public struct LoginReq
{
    public short userId;
    public string password;
}

public struct LoginAck
{
    public string username;
}