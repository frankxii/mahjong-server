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
    private Dictionary<MessageId, Action<Request>> _router = new(); // 回调路由表
    private ConcurrentQueue<short> _roomIdPool = new(); // 房间ID池
    private ConcurrentDictionary<short, RoomInfo> _rooms = new(); // 房间字典

    public Server()
    {
        // 绑定消息处理视图函数
        _router.Add(MessageId.Login, OnLogin);
        _router.Add(MessageId.CreateRoom, OnCreateRoom);
        _router.Add(MessageId.JoinRoom, OnJoinRoom);
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

        short roomId;
        _roomIdPool.TryDequeue(out roomId);

        // short userId = data.userId;


        List<PlayerInfo> players = new();
        players.Add(
            new PlayerInfo()
            {
                userId = 10001,
                username = "frank",
                coin = 3000,
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
    }
}