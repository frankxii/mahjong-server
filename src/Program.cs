namespace MahjongServer;

class Program
{
    public static async Task Main(string[] args)
    {
        await new Server().Run();
    }
}