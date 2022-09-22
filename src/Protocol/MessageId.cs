namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001, //创建房间
    JoinRoom = 1002 // 加入房间
}

public class Response<T>
{
    public short code = 0;
    public string message = "OK";
    public T? data;
}

public class LoginReq
{
    public string username = "";
    public string password = "";
}

public class CreateRoomReq
{
    public short userId;
    public short currentCycle;
    public short totalCycle;
}