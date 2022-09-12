using System.Net;
using System.Net.Sockets;
using MahjongServer.Protocol;

namespace MahjongServer;

internal class ClientState
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

// 协议处理方法
public delegate void View(string str);

public class Server
{
    private Socket? _listener;
    public const int MAX_CLIENT = 30;
    private Dictionary<Socket, ClientState> _clients = new();

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
        listener.Listen(MAX_CLIENT);
        Console.WriteLine("服务器启动成功");
        return listener;
    }

    private async Task Receive(Socket handler)
    {
        ClientState state = new(handler);
        state.socket = handler;
        _clients.Add(handler, state);

        while (true)
        {
            // 计算写入内存区域
            Memory<byte> buffer = new(state.readBuffer, state.bufferCount, ClientState.BUFFER_SIZE - state.bufferCount);
            int count = await handler.ReceiveAsync(buffer, SocketFlags.None);
            state.bufferCount = Convert.ToInt16(count);
            Console.WriteLine(count);
            // 连接断开
            if (count == 0)
            {
                handler.Close();
                _clients.Remove(handler);
                Console.WriteLine("Socket Close");
                break;
            }

            // 处理消息
            await HandleMessage(state);
        }
    }

    private async Task HandleMessage(ClientState state)
    {
        while (true)
        {
            short length = ProtoUtil.DecodeLength(state.readBuffer);
            // 获取消息id
            // 当前buffer有效数据小于包体长度，属于半包，不处理
            if (state.bufferCount < length)
                break;
            MessageId id = ProtoUtil.DecodeId(state.readBuffer);
            state.bufferCount -= length;
            // ==========业务逻辑=========
            // 读取消息
            LoginReq data = ProtoUtil.DecodeBody<LoginReq>(state.readBuffer);
            Console.WriteLine(data.password);
            LoginAck ack = new() {username = "frank"};

            byte[] sendBytes = ProtoUtil.Encode(id, ack);
            // byte[] sendBytes = Encoding.UTF8.GetBytes("LoginAck");
            _ = state.socket.SendAsync(sendBytes, SocketFlags.None);
            // ==========业务逻辑=========

            // buffCount为0表示所有数据已处理完成，退出消息处理循环，等待下一次receive
            if (state.bufferCount == 0)
                break;
            await Task.Delay(10);
        }
    }
}