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
            [MessageId.SortCardFinished] = OnSortCardFinished,
            [MessageId.PlayCard] = OnPlayCard,
            [MessageId.Operation] = OnOperation
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

    /// <summary>
    /// 玩家摸牌
    /// </summary>
    /// <param name="room">房间信息</param>
    /// <param name="dealerWind">摸牌玩家门风</param>
    private void PlayerDrawCard(RoomInfo room, byte dealerWind)
    {
        // 决定摸牌玩家
        PlayerInfo drawPlayer = room.players[0];
        foreach (PlayerInfo player in room.players)
        {
            if (player.dealerWind == dealerWind)
            {
                drawPlayer = player;
            }
        }

        // 摸牌并同步给摸牌玩家
        byte card = room.deck.Draw();
        drawPlayer.handCard.Add(card);
        drawPlayer.handCard.Sort();
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

        // 东家摸牌
        PlayerDrawCard(room, 1);
    }

    private void OnPlayCard(Request request)
    {
        PlayCardReq req = ProtoUtil.Deserialize<PlayCardReq>(request.json);
        RoomInfo room = _rooms[req.roomId];
        byte dealerWind = 1;
        foreach (PlayerInfo player in room.players)
        {
            if (player.userId == req.userId)
            {
                dealerWind = player.dealerWind;
                player.handCard.Remove(req.card);
            }
        }

        int playerCount = 0;
        // 通知其他玩家有人出牌
        foreach (PlayerInfo player in room.players)
        {
            if (player.userId != req.userId)
            {
                PlayCardEvent data = new() {card = req.card, dealerWind = dealerWind};
                // 检测玩家能否碰、杠、胡
                bool canPeng = CardDeck.CanPeng(player.handCard, req.card);
                bool canGang = CardDeck.CanGang(player.handCard, req.card) && room.deck.RemainCard > 0;
                bool canHu = CardDeck.CanHu(player.handCard, req.card);
                if (canPeng || canGang || canHu)
                    playerCount += 1;

                data.canPeng = canPeng;
                data.canGang = canGang;
                data.canHu = canHu;
                player.client?.Send(MessageId.PlayCardEvent, data);
            }
        }

        room.lastPlayCardDealer = dealerWind;
        // 启动监听线程，收集玩家操作结果

        // 如果没有人可以吃碰胡，通知下家摸牌
        if (playerCount == 0)
        {
            // 确定下家
            int playerDealerWind = dealerWind + 1 <= 4 ? dealerWind + 1 : 1;
            // 下家摸牌
            PlayerDrawCard(room, (byte) playerDealerWind);
        }
        else
        {
            _ = WaitingForOperationAsync(room, playerCount);
        }
    }

    private void OnOperation(Request request)
    {
        OperationReq req = ProtoUtil.Deserialize<OperationReq>(request.json);
        RoomInfo room = _rooms[req.roomId];
        room.operationList.Add(req);
    }

    private async Task WaitingForOperationAsync(RoomInfo room, int players)
    {
        for (int times = 0; times < 9; times++)
        {
            await Task.Delay(1000);
            if (room.operationList.Count == players)
                break;
        }

        OperationReq? topOperation = null;
        // 获取操作优先级
        foreach (OperationReq operation in room.operationList)
        {
            if (topOperation is null)
            {
                topOperation = operation;
            }
            else if (operation.operationCode == topOperation.operationCode)
            {
                if (operation.dealerWind + 4 - room.lastPlayCardDealer <
                    topOperation.dealerWind + 4 - room.lastPlayCardDealer)
                {
                    topOperation = operation;
                }
            }
            else if (operation.operationCode > topOperation.operationCode)
            {
                topOperation = operation;
            }
        }

        if (topOperation is null)
        {
            // 没有有效操作，下家摸牌
            int playerDealerWind = room.lastPlayCardDealer + 1 <= 4 ? room.lastPlayCardDealer + 1 : 1;
            // 下家摸牌
            PlayerDrawCard(room, (byte) playerDealerWind);
        }
        else
        {
            if (topOperation.operationCode == OperationCode.Hu)
            {
            }
            else if (topOperation.operationCode == OperationCode.Peng)
            {
                foreach (PlayerInfo player in room.players)
                {
                    player.client?.Send(MessageId.OperationEvent, new OperationEvnet()
                    {
                        dealerWind = topOperation.dealerWind,
                        operationCode = OperationCode.Peng,
                        operationCard = room.lastPlayCard
                    });
                }

                foreach (PlayerInfo player in room.players)
                {
                    if (player.dealerWind == topOperation.dealerWind)
                    {
                        player.handCard.Remove(room.lastPlayCard);
                        player.handCard.Remove(room.lastPlayCard);
                    }
                }
            }
            else if (topOperation.operationCode == OperationCode.Gang)
            {
                foreach (PlayerInfo player in room.players)
                {
                    player.client?.Send(MessageId.OperationEvent, new OperationEvnet()
                    {
                        dealerWind = topOperation.dealerWind,
                        operationCode = OperationCode.Gang,
                        operationCard = room.lastPlayCard
                    });
                }

                foreach (PlayerInfo player in room.players)
                {
                    if (player.dealerWind == topOperation.dealerWind)
                    {
                        player.handCard.Remove(room.lastPlayCard);
                        player.handCard.Remove(room.lastPlayCard);
                        player.handCard.Remove(room.lastPlayCard);
                        byte card = room.deck.DrawFromTail();
                        player.handCard.Add(card);
                        player.handCard.Sort();

                        DrawCardEvent data = new() {dealerWind = player.dealerWind, card = card};
                        player.client?.Send(MessageId.DrawCardEvent, data);
                        data.card = 0;
                        foreach (PlayerInfo playerInfo in room.players)
                        {
                            if (playerInfo != player)
                            {
                                playerInfo.client?.Send(MessageId.DrawCardEvent, data);
                            }
                        }
                    }
                }
            }
        }
    }
}