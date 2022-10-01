namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001, //创建房间
    JoinRoom = 1002, // 玩家加入房间
    UpdatePlayer = 1003, // 更新玩家信息
    LeaveRoom = 1004, // 玩家离开房间
    Ready = 1005 // 玩家准备
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
    public int userId;
    public short canChi;
    public short currentCycle;
    public short totalCycle;
}

public class JoinRoomReq
{
    public int userId;
    public int roomId;
}

public class LeaveRoomReq
{
    public int userId;
    public int roomId;
}

public class ReadyReq
{
    public int userId;
    public int roomId;
}