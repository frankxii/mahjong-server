using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MahjongServer.Migrations
{
    public partial class addusertable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false, comment: "用户ID")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    username = table.Column<string>(type: "varchar(16)", nullable: false, comment: "用户名")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    coin = table.Column<int>(type: "int", nullable: false, comment: "金币数量"),
                    diamond = table.Column<int>(type: "int", nullable: false, comment: "钻石数量"),
                    gender = table.Column<sbyte>(type: "tinyint", nullable: false, comment: "姓名，1为男，2为女"),
                    create_time = table.Column<DateTime>(type: "timestamp", nullable: false, comment: "创建时间"),
                    update_time = table.Column<DateTime>(type: "timestamp", nullable: false, comment: "上次更新时间")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.user_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
