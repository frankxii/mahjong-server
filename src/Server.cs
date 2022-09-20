using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using MahjongServer.Protocol;

namespace MahjongServer;

public class ClientState
{
    public Socket socket; // socket连接
    public const short BUFFER_SIZE = 1024; //接收缓冲区大小
    public byte[] readBuffer = new byte[BUFFER_SIZE]; //接收缓冲区
    public short bufferCount; //缓冲区有效数据大小

    public ClientState(Socket socket)
    {
        this.socket = socket;
    }
}

public class Server
{
    private Socket? _listener;
    private ConcurrentDictionary<Socket, ClientState> _clients = new();
    private Dictionary<MessageId, Action<ClientState>> _router = new();

    public Server()
    {
        // 绑定消息处理视图函数
        _router.Add(MessageId.Login, OnLogin);
        _router.Add(MessageId.CreateRoom, OnCreateRoom);
        _router.Add(MessageId.JoinRoom, OnJoinRoom);
    }


    public async Task Run(string address = "127.0.0.1", int port = 8000)
    {
        _listener = Listen(address, port);
        while (true)
        {
            try
            {
                Socket handler = await _listener.AcceptAsync();
                _ = Receive(handler);
            }
            catch (Exception e)
            {
                Console.WriteLine("服务器异常: " + e.Message);
                break;
            }
        }
    }

    private Socket Listen(string address, int port)
    {
        Socket listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint endPoint = new(IPAddress.Parse(address), port);
        listener.Bind(endPoint);
        listener.Listen();
        Console.WriteLine("服务器启动成功");
        return listener;
    }

    private async Task Receive(Socket handler)
    {
        ClientState state = new(handler);
        state.socket = handler;
        _clients.TryAdd(handler, state);

        while (true)
        {
            // 计算写入内存区域
            Memory<byte> buffer = new(state.readBuffer, state.bufferCount, ClientState.BUFFER_SIZE - state.bufferCount);
            int count = await handler.ReceiveAsync(buffer, SocketFlags.None);
            state.bufferCount += Convert.ToInt16(count);
            // 连接断开
            if (count == 0)
            {
                handler.Close();
                _clients.TryRemove(handler, out _);
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


    private void OnLogin(ClientState state)
    {
        LoginReq data = ProtoUtil.DecodeBody<LoginReq>(state.readBuffer);
        LoginAck ack = new() {errCode = 0, username = "frank", id = 10001, gender = 1, coin = 2000, diamond = 200};
        byte[] sendBytes = ProtoUtil.Encode(MessageId.Login, ack);
        _ = state.socket.SendAsync(sendBytes, SocketFlags.None);
    }

    private void OnCreateRoom(ClientState state)
    {
        CreateRoomReq data = ProtoUtil.DecodeBody<CreateRoomReq>(state.readBuffer);
        short userId = data.userId;
        short totalCycle = data.totalCycle;
        CreateRoomAck ack = new() {errCode = 0, roomId = 10001, currentCycle = 1, totalCycle = 8, dealerWind = 2};
        byte[] sendBytes = ProtoUtil.Encode(MessageId.CreateRoom, ack);
        _ = state.socket.SendAsync(sendBytes, SocketFlags.None);
    }

    private void OnJoinRoom(ClientState state)
    {
    }
}