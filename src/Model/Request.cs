using MahjongServer.Protocol;

namespace MahjongServer.Model;

public class Request
{
    public Client client;
    public MessageId messageId;
    public string json;

    public Request(Client client, MessageId id, string json)
    {
        this.client = client;
        messageId = id;
        this.json = json;
    }
}