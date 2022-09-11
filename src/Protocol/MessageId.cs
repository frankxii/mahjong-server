using System.Text;
using Newtonsoft.Json;

namespace MahjongServer.Protocol;

// 通信协议消息ID
public enum MessageId : short
{
    Login = 1000, // 登录
    CreateRoom = 1001 //创建房间
}

public static class ProtoUtil
{
    public static void Encode()
    {
    }

    /// <summary>
    /// 解析消息长度
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static short DecodeLength(byte[] message)
    {
        return BitConverter.ToInt16(message);
    }

    /// <summary>
    /// 解析消息ID
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static MessageId DecodeId(byte[] message)
    {
        return (MessageId) BitConverter.ToInt16(message, 2);
    }

    /// <summary>
    /// 解析协议消息参数
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T? DecodeBody<T>(byte[] message)
    {
        short length = BitConverter.ToInt16(message);
        byte[] jsonByte = message.Skip(4).Take(length - 4).ToArray();
        // 获取json字符串

        string json = Encoding.UTF8.GetString(jsonByte);
        Console.WriteLine(json);
        return JsonConvert.DeserializeObject<T>(json);
    }
}

public struct LoginReq
{
    public short userId;
    public string password;
}