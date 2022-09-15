// using System;
// using MySql.Data.MySqlClient;
//
// namespace MojangServer
// {
//     public class UserInfo
//     {
//         public string Username { get; set; }
//         public int Coin { get; set; }
//         public int Diamond { get; set; }
//         public int Gender { get; set; }
//     }
//
//     public class MySqlManager
//     {
//         private MySqlConnection _conn;
//
//         public MySqlManager()
//         {
//             string host = "127.0.0.1";
//             int port = 3306;
//             string database = "Mojang";
//             string user = "root";
//             string password = "qa";
//
//             string connStr = $"server={host};port={port};database={database};user={user};password={password}";
//             _conn = new MySqlConnection(connStr);
//         }
//
//         public UserInfo QueryUserInfo(int id)
//         {
//             _conn.Open();
//             string sql = $"select username, coin, diamond, gender from user where id={id}";
//             MySqlCommand cmd = new MySqlCommand(sql, _conn);
//             MySqlDataReader reader = cmd.ExecuteReader();
//             reader.Read();
//
//             UserInfo info = new UserInfo();
//             info.Username = reader[0].ToString();
//             info.Coin = Convert.ToInt32(reader[1]);
//             info.Diamond = Convert.ToInt32(reader[2]);
//             info.Gender = Convert.ToInt32(reader[3]);
//
//             reader.Close();
//             _conn.Close();
//             return info;
//         }
//     }
// }