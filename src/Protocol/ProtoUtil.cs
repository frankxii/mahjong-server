using System.Text;
using Newtonsoft.Json;

namespace MahjongServer.Protocol;

public static class ProtoUtil
{
    /// <summary>
    /// 按协议封装响应消息
    /// </summary>
    /// <param name="id">消息枚举ID</param>
    /// <param name="response">响应结果</param>
    /// <returns>封装字节流</returns>
    public static byte[] Encode<T>(MessageId id, Response<T> response)
    {
        // 默认长度为4，长度两个字节，协议ID两个字节
        short length = 4;
        byte[] messageIdBytes = BitConverter.GetBytes((short) id);

        // 结构体转字符串
        // string json = JsonUtility.ToJson(data);
        string json = JsonConvert.SerializeObject(response);
        // 字符串转字节
        byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
        // 获取发送消息字节长度
        length += Convert.ToInt16(bodyBytes.Length);

        byte[] lengthBytes = BitConverter.GetBytes(length);

        // 拼接消息体和长度
        return lengthBytes.Concat(messageIdBytes).Concat(bodyBytes).ToArray();
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
    /// <returns></returns>
    public static string DecodeBody(byte[] message)
    {
        short length = BitConverter.ToInt16(message);
        byte[] jsonByte = message.Skip(4).Take(length - 4).ToArray();
        // 返回json字符串
        return Encoding.UTF8.GetString(jsonByte);
    }
}