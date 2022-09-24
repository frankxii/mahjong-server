using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MahjongServer.DB;

[Table("user")]
public class User
{
    [Key]
    [Comment("用户ID")]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Comment("用户名")]
    [Column("username", TypeName = "varchar(16)")]
    public string Username { get; set; } = "";

    [Required]
    [Comment("密码")]
    [Column("password", TypeName = "varchar(64)")]
    public string Password { get; set; } = "";

    [Required]
    [Comment("金币数量")]
    [Column("coin")]
    public int Coin { get; set; }

    [Required]
    [Comment("钻石数量")]
    [Column("diamond")]
    public int Diamond { get; set; }

    [Range(1, 3)]
    [Comment("姓名，1为男，2为女")]
    [Column("gender", TypeName = "tinyint")]
    public byte Gender { get; set; } = 1;

    [Comment("创建时间")]
    [Column("create_time", TypeName = "timestamp")]
    public DateTime CreateTime { get; set; } = DateTime.Now;

    [Comment("上次更新时间")]
    [Column("update_time", TypeName = "timestamp")]
    public DateTime UpdateTime { get; set; } = DateTime.Now;
}