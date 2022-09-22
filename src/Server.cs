using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MahjongServer.Model;
using MahjongServer.Protocol;
using Newtonsoft.Json;

namespace MahjongServer;

public struct RoomInfo
{
    public short roomId;
    public short currentCycle;
    public short totalCycle;
    public List<PlayerInfo> players;
}

public class Server
{
    private TcpListener? _listener;
    private Dictionary<MessageId, Action<Request>> _router = new();
    private ConcurrentQueue<short> _roomIdPool = new();
    private ConcurrentDictionary<short, RoomInfo> _rooms = new();

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
                break;

            foreach (Request request in requests)
            {
                _router[request.messageId](request);
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
        LoginReq? data = JsonConvert.DeserializeObject<LoginReq>(request.json);
        Response<LoginAck> response = new()
            {data = new LoginAck() {username = "frank", id = 10001, gender = 1, coin = 2000, diamond = 200}};
        request.client.Send(MessageId.Login, response);
    }

    private void OnCreateRoom(Request request)
    {
        CreateRoomReq? data = JsonConvert.DeserializeObject<CreateRoomReq>(request.json);
        short roomId;
        _roomIdPool.TryDequeue(out roomId);

        short userId = data.userId;

        // 创建房间，加入房间字典
        RoomInfo room = new() {roomId = roomId, currentCycle = 1, totalCycle = data.totalCycle};
        List<PlayerInfo> players = new();
        players.Add(
            new PlayerInfo()
            {
                id = 10001,
                username = "frank",
                coin = 3000,
                dealerWind = 1,
                client = request.client
            }
        );
        room.players = players;
        _rooms.TryAdd(roomId, room);


        // 响应客户端
        Response<CreateRoomAck> response = new()
        {
            data = new CreateRoomAck()
            {
                roomId = roomId,
                currentCycle = 1,
                totalCycle = room.totalCycle,
                dealerWind = 2,
                players = room.players
            }
        };
        request.client.Send(MessageId.CreateRoom, response);
    }

    private void OnJoinRoom(Request request)
    {
    }
}