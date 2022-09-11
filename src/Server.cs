using System.Net;
using System.Net.Sockets;
using System.Text;
using MahjongServer.Protocol;

namespace MahjongServer;

internal class ClientState
{
    public Socket? socket;
    public byte[] readBuff = new byte[1024];
}

// 协议处理方法
public delegate void View(string str);

public class Server
{
    private Socket? _listener;
    public const int MAX_CLIENT = 30;
    private Dictionary<Socket, ClientState> _clients = new();

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
        ClientState state = new();
        state.socket = handler;
        _clients.Add(handler, state);

        while (true)
        {
            int count = await handler.ReceiveAsync(state.readBuff, SocketFlags.None);
            // 连接断开
            if (count == 0)
            {
                handler.Close();
                _clients.Remove(handler);
                Console.WriteLine("Socket Close");
                break;
            }
            else
            {
                // 获取消息id
                MessageId id = ProtoUtil.DecodeId(state.readBuff);
                
                // 读取消息
                LoginReq data = ProtoUtil.DecodeBody<LoginReq>(state.readBuff);
                Console.WriteLine(data.userId);
                Console.WriteLine(data.password);
                byte[] sendBytes = Encoding.UTF8.GetBytes("LoginAck");
                _ = handler.SendAsync(sendBytes, SocketFlags.None);
            }
        }
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
}