// <auto-generated />
using System;
using MahjongServer.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MahjongServer.Migrations
{
    [DbContext(typeof(MahjongDbContext))]
    [Migration("20220923155454_add user table")]
    partial class addusertable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("MahjongServer.DB.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("user_id")
                        .HasComment("用户ID");

                    b.Property<int>("Coin")
                        .HasColumnType("int")
                        .HasColumnName("coin")
                        .HasComment("金币数量");

                    b.Property<DateTime>("CreateTime")
                        .HasColumnType("timestamp")
                        .HasColumnName("create_time")
                        .HasComment("创建时间");

                    b.Property<int>("Diamond")
                        .HasColumnType("int")
                        .HasColumnName("diamond")
                        .HasComment("钻石数量");

                    b.Property<sbyte>("Gender")
                        .HasColumnType("tinyint")
                        .HasColumnName("gender")
                        .HasComment("姓名，1为男，2为女");

                    b.Property<DateTime>("UpdateTime")
                        .HasColumnType("timestamp")
                        .HasColumnName("update_time")
                        .HasComment("上次更新时间");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("varchar(16)")
                        .HasColumnName("username")
                        .HasComment("用户名");

                    b.HasKey("UserId");

                    b.ToTable("user");
                });
#pragma warning restore 612, 618
        }
    }
}
