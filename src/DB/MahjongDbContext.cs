using Microsoft.EntityFrameworkCore;

namespace MahjongServer.DB;

public class MahjongDbContext : DbContext
{
    public DbSet<User> User { get; set; } = default!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        MySqlServerVersion version = new(new Version(8, 0, 27));
        options.UseMySql("server=localhost;database=Mahjong;user=root;password=qa;", version);
    }
}