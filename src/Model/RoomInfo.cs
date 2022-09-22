namespace MahjongServer.Model;

public class RoomInfo
{
    public short roomId;
    public short currentCycle = 1;
    public short totalCycle = 4;
    public List<PlayerInfo> players = new();
}