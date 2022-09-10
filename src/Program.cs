using System.Net;
using System.Net.Sockets;
using System.Text;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using Timer = System.Timers.Timer;

namespace MahjongServer;

internal class ClientState
{
    public Socket socket;
    public byte[] readBuff = new byte[1024];
}

class Program
{
    public static async Task Main(string[] args)
    {
        Socket listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 6666);
        listener.Bind(endPoint);
        listener.Listen();
        Console.WriteLine("服务器启动成功");

        Dictionary<Socket, ClientState> clients = new();


        while (true)
        {
            Socket handler = await listener.AcceptAsync();
            Task.Run(async() =>
            {
                Console.WriteLine(DateTime.Now);
                Console.WriteLine("accepted");

                ClientState state = new();
                state.socket = handler;
                clients.Add(handler, state);

                while (true)
                {
                    int count = await handler.ReceiveAsync(state.readBuff, SocketFlags.None);

                    // 连接断开
                    if (count == 0)
                    {
                        handler.Close();
                        clients.Remove(handler);
                        Console.WriteLine("Socket Close");
                        break;
                    }
                    else
                    {
                        string receivedStr = Encoding.UTF8.GetString(state.readBuff, 0, count);
                        Console.WriteLine(receivedStr);
                        byte[] sendBytes = Encoding.UTF8.GetBytes(receivedStr);
                        await handler.SendAsync(sendBytes, SocketFlags.None);
                    }
                }
            });
        }
    }
}