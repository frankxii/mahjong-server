using System.Net.Sockets;
using MahjongServer.Protocol;

namespace MahjongServer.Model;

public class Client
{
    public TcpClient client;
    public const short BUFFER_SIZE = 1024; //接收缓冲区大小
    public byte[] readBuffer = new byte[BUFFER_SIZE]; //接收缓冲区
    public short bufferCount; //缓冲区有效数据大小
    public NetworkStream stream;

    public Client(TcpClient client)
    {
        this.client = client;
        stream = client.GetStream();
    }

    public async Task<List<Request>> ReceiveRequest()
    {
        // 计算写入内存区域
        Memory<byte> buffer = new(readBuffer, bufferCount, BUFFER_SIZE - bufferCount);
        int count = await stream.ReadAsync(buffer);
        if (count == 0)
        {
            return new List<Request>();
        }

        // 增长缓冲区有效数据长度
        bufferCount += Convert.ToInt16(count);


        List<Request> requests = new();
        while (true)
        {
            short length = ProtoUtil.DecodeLength(readBuffer);
            // 当前buffer有效数据小于包体长度，属于半包，不处理
            if (bufferCount < length)
                break;

            MessageId id = ProtoUtil.DecodeId(readBuffer);
            // 解包json，装入requests
            string json = ProtoUtil.DecodeBody(readBuffer);
            requests.Add(new Request(this, id, json));

            // buffCount和消息长度相等，表示只有一条数据，退出消息处理循环，等待下一次receive
            if (bufferCount == length)
            {
                bufferCount = 0;
                break;
            }
            else
            {
                bufferCount -= length;
                // 位移数组
                Array.Copy(readBuffer, length, readBuffer, 0, bufferCount);
            }
        }

        return requests;
    }

    public void Send(MessageId id, object response)
    {
        byte[] sendBytes = ProtoUtil.Encode(id, response);
        _ = stream.WriteAsync(sendBytes);
    }

    public void Close()
    {
        stream.Close();
        client.Close();
        Console.WriteLine("Client Close");
    }
}