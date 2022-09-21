using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MahjongServer.Protocol;

namespace MahjongServer;

public class ClientState
{
    public TcpClient client; // socket连接
    public const short BUFFER_SIZE = 1024; //接收缓冲区大小
    public byte[] readBuffer = new byte[BUFFER_SIZE]; //接收缓冲区
    public short bufferCount; //缓冲区有效数据大小

    public ClientState(TcpClient client)
    {
        this.client = client;
    }
}

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
    private Dictionary<MessageId, Action<ClientState>> _router = new();
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
                TcpClient client = await _listener.AcceptTcpClientAsync();
                _ = Receive(client);
            }
            catch (Exception e)
            {
                Console.WriteLine("服务器异常: " + e.Message);
                break;
            }
        }
    }


    private async Task Receive(TcpClient client)
    {
        ClientState state = new(client);

        while (true)
        {
            // 计算写入内存区域
            Memory<byte> buffer = new(state.readBuffer, state.bufferCount, ClientState.BUFFER_SIZE - state.bufferCount);
            NetworkStream stream = client.GetStream();
            int count = await stream.ReadAsync(buffer);
            // int count = await handler.ReceiveAsync(buffer, SocketFlags.None);
            state.bufferCount += Convert.ToInt16(count);
            // 连接断开
            if (count == 0)
            {
                stream.Close();
                client.Close();
                Console.WriteLine("Socket Close");
                break;
            }

            // 处理消息
            HandleMessage(state);
        }
    }

    private void HandleMessage(ClientState state)
    {
        while (true)
        {
            short length = ProtoUtil.DecodeLength(state.readBuffer);
            // 获取消息id
            // 当前buffer有效数据小于包体长度，属于半包，不处理
            if (state.bufferCount < length)
                break;

            // 执行业务逻辑
            try
            {
                MessageId id = ProtoUtil.DecodeId(state.readBuffer);
                _router[id].Invoke(state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // buffCount和消息长度相等，表示只有一条数据，退出消息处理循环，等待下一次receive
            if (state.bufferCount == length)
            {
                state.bufferCount = 0;
                break;
            }
            else
            {
                state.bufferCount -= length;
                // 位移数组
                Array.Copy(state.readBuffer, length, state.readBuffer, 0, state.bufferCount);
            }
        }
    }

    private void FillInRoomIdPool()
    {
        for (short id = 0; id < 10010; id++)
        {
            _roomIdPool.Enqueue(id);
        }
    }

    private void OnLogin(ClientState state)
    {
        LoginReq data = ProtoUtil.DecodeBody<LoginReq>(state.readBuffer);
        LoginAck ack = new() {errCode = 0, username = "frank", id = 10001, gender = 1, coin = 2000, diamond = 200};
        byte[] sendBytes = ProtoUtil.Encode(MessageId.Login, ack);
        state.client.GetStream().WriteAsync(sendBytes);
    }

    private void OnCreateRoom(ClientState state)
    {
        CreateRoomReq data = ProtoUtil.DecodeBody<CreateRoomReq>(state.readBuffer);
        short roomId;
        _roomIdPool.TryDequeue(out roomId);

        short userId = data.userId;

        // 创建房间，加入房间字典
        RoomInfo room = new() {roomId = roomId, currentCycle = 1, totalCycle = data.totalCycle};
        List<PlayerInfo> players = new();
        players.Add(new PlayerInfo() {id = 10001, username = "frank", coin = 3000, dealerWind = 1, state = state});
        room.players = players;
        _rooms.TryAdd(roomId, room);


        // 响应客户端
        CreateRoomAck ack = new()
        {
            errCode = 0,
            roomId = roomId,
            currentCycle = 1,
            totalCycle = room.totalCycle,
            dealerWind = 2,
            players = room.players
        };
        byte[] sendBytes = ProtoUtil.Encode(MessageId.CreateRoom, ack);
        state.client.GetStream().WriteAsync(sendBytes);
    }

    private void OnJoinRoom(ClientState state)
    {
    }
}