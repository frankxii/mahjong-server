using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MahjongServer.Migrations
{
    public partial class AddPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "password",
                table: "user",
                type: "varchar(64)",
                nullable: false,
                defaultValue: "",
                comment: "密码")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password",
                table: "user");
        }
    }
}
