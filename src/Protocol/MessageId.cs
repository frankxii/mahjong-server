namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001, //创建房间
    JoinRoom = 1002, // 玩家加入房间
    UpdatePlayer = 1003, // 更新玩家信息
    LeaveRoom = 1004, // 玩家离开房间
    Ready = 1005, // 玩家准备
    DealCard = 1006, // 发牌
    SortCardFinished = 1007, // 理牌
    DrawCardEvent = 1008, // 摸牌
    PlayCard = 1009, // 出牌
    PlayCardEvent = 1010, // 其他玩家出牌事件
    Operation = 1011, // 玩家操作，碰、杠、胡
    OperationEvent = 1012 // 玩家操作事件，碰、杠、胡
}

public enum OperationCode
{
    Pass, // 过牌
    Peng, // 碰牌
    Gang, // 杠牌
    Hu // 胡牌
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

public class SortCardReq
{
    public int userId;
    public int roomId;
}

public class DrawCardEvent
{
    public byte dealerWind;
    public byte card = 0;
    public int remainCards;
}

public class PlayCardReq
{
    public int roomId;
    public int userId;
    public byte card;
}

public class PlayCardEvent
{
    public byte dealerWind;
    public byte card;
    public int remainHandCard;
    public bool canPeng;
    public bool canGang;
    public bool canHu;
}

public class OperationReq
{
    public int roomId;
    public byte dealerWind;
    public OperationCode operationCode;
}

public class OperationEvnet
{
    public byte dealerWind;
    public OperationCode operationCode;
    public byte operationCard;
}