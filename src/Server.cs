using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MahjongServer.DB;
using MahjongServer.Exceptions;
using MahjongServer.Model;
using MahjongServer.Protocol;

namespace MahjongServer;

public class Server
{
    private TcpListener? _listener;
    private Dictionary<MessageId, Action<Request>> _router; // 回调路由表
    private ConcurrentQueue<int> _roomIdPool = new(); // 房间ID池
    private ConcurrentDictionary<int, RoomInfo> _rooms = new(); // 房间字典

    public Server()
    {
        // 绑定消息处理视图函数
        _router = new Dictionary<MessageId, Action<Request>>()
        {
            [MessageId.Login] = OnLogin,
            [MessageId.CreateRoom] = OnCreateRoom,
            [MessageId.JoinRoom] = OnJoinRoom,
            [MessageId.LeaveRoom] = OnLeaveRoom,
            [MessageId.Ready] = OnReady,
            [MessageId.SortCardFinished] = OnSortCardFinished
        };
    }


    public async Task Run(string address = "127.0.0.1", int port = 8000)
    {
        FillInRoomIdPool();
        _listener = new TcpListener(IPAddress.Parse(address), port);
        _listener.Start();
        while (true)
        {
            try
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
                _ = Serve(new Client(tcpClient));
            }
            catch (Exception e)
            {
                Console.WriteLine("服务器异常: " + e.Message);
                break;
            }
        }
    }


    private async Task Serve(Client client)
    {
        while (true)
        {
            List<Request> requests = await client.ReceiveRequest();
            // 客户端断开
            if (requests.Count == 0)
            {
                Console.WriteLine("客户端退出");
                break;
            }

            foreach (Request request in requests)
            {
                try
                {
                    _router[request.messageId](request);
                }
                catch (DeserializeFailException)
                {
                    request.client.Send(request.messageId, new Response<object>() {code = 1, message = "参数错误"});
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }


    private void FillInRoomIdPool()
    {
        for (short id = 10000; id < 10010; id++)
        {
            _roomIdPool.Enqueue(id);
        }
    }


    private void OnLogin(Request request)
    {
        LoginReq data = ProtoUtil.Deserialize<LoginReq>(request.json);
        // 参数校验
        if (data.username == "" || data.password == "")
        {
            request.client.Send(MessageId.Login, new Response<object>() {code = 1, message = "参数错误"});
            return;
        }

        using MahjongDbContext db = new();

        User user;
        // 用户名不存在时会抛异常
        try
        {
            user = db.User.Single(row => row.Username == data.username);
        }
        catch (Exception)
        {
            request.client.Send(MessageId.Login, new Response<object>() {code = 1, message = "用户不存在"});
            return;
        }

        // 校验密码
        if (!user.Password.Equals(data.password))
        {
            request.client.Send(MessageId.Login, new Response<object>() {code = 2, message = "密码错误"});
            return;
        }

        // 构造响应结果
        Response<UserInfo> response = new()
        {
            data = new UserInfo()
            {
                userId = user.UserId,
                username = user.Username,
                gender = user.Gender,
                coin = user.Coin,
                diamond = user.Diamond
            }
        };
        request.client.Send(MessageId.Login, response);
    }

    private void OnCreateRoom(Request request)
    {
        CreateRoomReq data = ProtoUtil.Deserialize<CreateRoomReq>(request.json);

        int roomId;
        _roomIdPool.TryDequeue(out roomId);

        // short userId = data.userId;
        using MahjongDbContext db = new();
        User user = db.User.Single(row => row.UserId == data.userId);

        List<PlayerInfo> players = new();
        players.Add(
            new PlayerInfo()
            {
                userId = user.UserId,
                username = user.Username,
                gender = user.Gender,
                coin = user.Coin,
                isReady = false,
                dealerWind = 1,
                client = request.client
            }
        );
        // 创建房间，加入房间字典
        RoomInfo roomInfo = new() {roomId = roomId, totalCycle = data.totalCycle, players = players};
        _rooms.TryAdd(roomId, roomInfo);

        // 响应客户端
        Response<RoomInfo> response = new() {data = roomInfo};
        request.client.Send(MessageId.CreateRoom, response);
    }

    private void OnJoinRoom(Request request)
    {
        JoinRoomReq data = ProtoUtil.Deserialize<JoinRoomReq>(request.json);
        if (!_rooms.ContainsKey(data.roomId))
            request.client.Send(MessageId.JoinRoom, new Response<object>() {code = 10, message = "房间不存在"});

        using MahjongDbContext db = new();
        User user = db.User.Single(row => row.UserId == data.userId);
        if (_rooms[data.roomId].players.Count >= 4)
            request.client.Send(MessageId.JoinRoom, new Response<object>() {code = 11, message = "房间人数已满"});
        // 安排玩家门风
        List<byte> winds = new() {1, 2, 3, 4};

        foreach (PlayerInfo playerInfo in _rooms[data.roomId].players)
        {
            if (winds.Contains(playerInfo.dealerWind))
            {
                winds.Remove(playerInfo.dealerWind);
            }
        }

        PlayerInfo player = new()
        {
            userId = user.UserId,
            username = user.Username,
            coin = user.Coin,
            gender = user.Gender,
            isReady = false,
            dealerWind = winds[0],
            client = request.client
        };
        _rooms[data.roomId].players.Add(player);
        // 响应玩家加入房间信息
        request.client.Send(MessageId.JoinRoom, new Response<RoomInfo>() {data = _rooms[data.roomId]});

        // 同步其他玩家，更新房间信息
        foreach (PlayerInfo playerInfo in _rooms[data.roomId].players)
        {
            if (playerInfo.userId != data.userId)
            {
                playerInfo.client?.Send(MessageId.UpdatePlayer, _rooms[data.roomId].players);
            }
        }
    }

    private void OnLeaveRoom(Request request)
    {
        LeaveRoomReq req = ProtoUtil.Deserialize<LeaveRoomReq>(request.json);

        RoomInfo? roomInfo = _rooms[req.roomId];

        // 遍历玩家列表，移除id相同的玩家
        foreach (PlayerInfo player in roomInfo.players)
        {
            if (player.userId == req.userId)
            {
                roomInfo.players.Remove(player);
                break;
            }
        }

        // 发送离开成功响应
        request.client.Send(MessageId.LeaveRoom, new Response<object>());

        // 所有人已离开房间
        if (roomInfo.players.Count == 0)
        {
            // 把房间id放回id池
            _roomIdPool.Enqueue(roomInfo.roomId);
            // 移除房间信息
            _rooms.TryRemove(roomInfo.roomId, out roomInfo);
        }
        else
        {
            // 通知其他玩家更新房间人员信息
            foreach (PlayerInfo player in roomInfo.players)
            {
                player.client?.Send(MessageId.UpdatePlayer, roomInfo.players);
            }
        }
    }

    private void OnReady(Request request)
    {
        ReadyReq req = ProtoUtil.Deserialize<ReadyReq>(request.json);
        RoomInfo room = _rooms[req.roomId];
        // 找到准备的玩家，更新玩家信息
        foreach (PlayerInfo player in room.players)
        {
            if (player.userId == req.userId)
                player.isReady = true;
        }

        // 响应准备玩家，已准备ok
        request.client.Send(MessageId.Ready, new Response<object>());

        // 同步其他玩家当前房间信息
        foreach (PlayerInfo player in room.players)
        {
            if (player.userId != req.userId)
            {
                player.client?.Send(MessageId.UpdatePlayer, room.players);
            }
        }

        // 玩家不足四人，不准备发牌
        if (room.players.Count != 4)
            return;

        byte readyPlayer = 0;
        foreach (PlayerInfo player in room.players)
        {
            if (!player.isReady)
                break;
            readyPlayer += 1;
        }

        // 已准备玩家不足四人，不发牌
        if (readyPlayer != 4)
            return;

        // 所有玩家已准备好，准备发牌
        room.deck.Shuffle();
        // 发牌
        foreach (PlayerInfo player in room.players)
        {
            // 取消准备状态
            player.isReady = false;
            player.handCard = room.deck.Deal();
            player.client?.Send(MessageId.DealCard, player.handCard);
            player.handCard.Sort();
        }
    }

    private void OnSortCardFinished(Request request)
    {
        SortCardReq req = ProtoUtil.Deserialize<SortCardReq>(request.json);
        RoomInfo room = _rooms[req.roomId];
        foreach (PlayerInfo player in room.players)
        {
            if (player.userId == req.userId)
            {
                player.isSorted = true;
                break;
            }
        }

        foreach (PlayerInfo player in room.players)
        {
            // 还有玩家没理完牌，暂时不摸牌
            if (!player.isSorted)
                return;
        }
        
        // 决定摸牌玩家
        PlayerInfo drawPlayer = room.players[0];

        // 摸牌并同步给摸牌玩家
        byte card = room.deck.Draw();
        drawPlayer.handCard.Add(card);
        DrawCardEvent data = new() {dealerWind = drawPlayer.dealerWind, card = card};
        room.players[0].client?.Send(MessageId.DrawCardEvent, data);

        // 隐藏摸牌数值，同步其他玩家
        data.card = 0;
        foreach (PlayerInfo player in room.players)
        {
            if (player != drawPlayer)
            {
                player.client?.Send(MessageId.DrawCardEvent, data);
            }
        }
    }
}