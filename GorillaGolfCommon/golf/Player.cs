using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public enum UserTypes
    {
        Normal,
        Admin
    }

    public enum UserStatus
    {
        Active,
        Inactive
    }

    public class DuplicatePlayerUserNameException : Exception
    {
        public DuplicatePlayerUserNameException(string message) : base(message)
        {
        }
    }

    public class Player
    {
        public int PlayerID { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string GHIN { get; set; }
        public decimal? HIndex { get; set; }
        public DateTime? HIndexLastUpdateUTC { get; set; }
        public DateTime CreateDateUTC { get; set; }
        public UserStatus Status { get; set; }
        public UserTypes UserType { get; set; }
        public string Email { get; set; }
        public string TextNumber { get; set; }
        public List<Transaction> Transactions { get; set; }
        public decimal? Balance { get; set; }
        public string FormattedBalance => Balance.HasValue ? $"{((decimal) Balance).Currency()}" : "";

        public string DisplayName
        {
            get
            {
                string dname = LastName;
                if (FirstName.IsNotEmpty()) dname += $", {FirstName}";
                return dname;
            }
        }

        public string ShortName
        {
            get
            {
                string name = LastName;
                if (FirstName.IsNotEmpty()) name = $"{FirstName.Substring(0, 1)}. {name}";
                return name;
            }
        }

        public string FullName => $"{FirstName} {LastName}";


        public string FormattedPhone
        {
            get
            {
                if (TextNumber == null || TextNumber.Length != 10) return TextNumber;
                var areacode = TextNumber.Substring(0, 3);
                var prefix = TextNumber.Substring(3, 3);
                var main = TextNumber.Substring(6, 4);
                return $"({areacode}) {prefix}-{main}";
            }
        }

        public Player()
        {
            Status = UserStatus.Active;
            UserType = UserTypes.Normal;
            CreateDateUTC = DateTime.UtcNow;
        }

        public static Player GetPlayer(int playerID, bool full = false)
        {
            const string sql = @"
                select LastName, FirstName, NickName, UserName, GHIN, HIndex, CreateDateUTC, Status, UserType, Email, TextNumber, PlayerID, HIndexLastUpdateUTC
                from Player
                where PlayerID = @playerID
            ";

            Player player = null;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@playerID", playerID.ToString()))
            {
                if (rdr.Read())
                {
                    player = new Player
                    {
                        LastName = rdr.GetString(0),
                        FirstName = rdr.GetString(1),
                        NickName = rdr.GetString(2),
                        UserName = rdr.GetString(3),
                        GHIN = rdr.GetString(4),
                        HIndex = rdr.IsDBNull(5) ? (decimal?) null : rdr.GetDecimal(5),
                        CreateDateUTC = rdr.GetDateTime(6),
                        Status = (UserStatus) rdr.GetInt32(7),
                        UserType = (UserTypes) rdr.GetInt32(8),
                        Email = rdr.GetString(9),
                        TextNumber = rdr.GetString(10),
                        PlayerID = rdr.GetInt32(11),
                        HIndexLastUpdateUTC = rdr.IsDBNull(12) ? (DateTime?)null : rdr.GetDateTime(12)
                    };
                }
            }

            if (player == null) return null;

            if (full)
            {
                // Get the player's deposit/withdrawal transactions and calculate player balance
                player.Transactions = Transaction.GetPlayerTransactions(player.PlayerID);
                player.Balance = player.Transactions.Sum(x => x.Amount);
            }

            return player;
        }

        public static Player CheckLogin(string username, string password)
        {
            Player player = null;
            const string sql =
                @"select LastName, FirstName, NickName, UserName, GHIN, HIndex, CreateDateUTC, Status, UserType, Email, TextNumber, PlayerID, HIndexLastUpdateUTC 
                from Player 
                where UserName=@userName and Password=@password and Status=@status";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@userName", username, "@password",
                HashUtil.HashPassword(password), "@status", UserStatus.Active.ToString("d")))
            {
                // Get all the Player info and put in a Player instance
                if (rdr.Read())
                {
                    player = new Player
                    {
                        LastName = rdr.GetString(0),
                        FirstName = rdr.GetString(1),
                        NickName = rdr.GetString(2),
                        UserName = rdr.GetString(3),
                        GHIN = rdr.GetString(4),
                        HIndex = rdr.IsDBNull(5) ? (decimal?) null : rdr.GetDecimal(5),
                        CreateDateUTC = rdr.GetDateTime(6),
                        Status = (UserStatus) rdr.GetInt32(7),
                        UserType = (UserTypes) rdr.GetInt32(8),
                        Email = rdr.GetString(9),
                        TextNumber = rdr.GetString(10),
                        PlayerID = rdr.GetInt32(11),
                        HIndexLastUpdateUTC = rdr.IsDBNull(12) ? (DateTime?)null : rdr.GetDateTime(12)
                    };
                }
            }

            return player;
        }

        public static List<Player> GetPlayers(bool activeOnly = false)
        {
            List<Player> playerList = new List<Player>();
            string sql = string.Format(
                @"select LastName, FirstName, NickName, UserName, GHIN, HIndex, CreateDateUTC, Status, UserType, Email, TextNumber, PlayerID, HIndexLastUpdateUTC 
                from Player 
                {0}
                order by LastName asc", activeOnly ? "where Status = @status" : "");
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@status", UserStatus.Active.ToString("d")))
            {
                while (rdr.Read())
                {
                    Player player = new Player
                    {
                        LastName = rdr.GetString(0),
                        FirstName = rdr.GetString(1),
                        NickName = rdr.GetString(2),
                        UserName = rdr.GetString(3),
                        GHIN = rdr.GetString(4),
                        HIndex = rdr.IsDBNull(5) ? (decimal?) null : rdr.GetDecimal(5),
                        CreateDateUTC = rdr.GetDateTime(6),
                        Status = (UserStatus) rdr.GetInt32(7),
                        UserType = (UserTypes) rdr.GetInt32(8),
                        Email = rdr.GetString(9),
                        TextNumber = rdr.GetString(10),
                        PlayerID = rdr.GetInt32(11),
                        HIndexLastUpdateUTC = rdr.IsDBNull(12) ? (DateTime?)null : rdr.GetDateTime(12)
                    };
                    playerList.Add(player);
                }
            }

            return playerList;
        }

        public static Dictionary<int, Player> GetPlayersDictionary(bool activeOnly = false)
        {
            Dictionary<int, Player> pList = new Dictionary<int, Player>();
            foreach (Player p in GetPlayers(activeOnly))
            {
                pList.Add(p.PlayerID, p);
            }

            return pList;
        }

        public static Dictionary<int, int> GetPlayerOutingCounts(int season)
        {
            Dictionary<int, int> pList = new Dictionary<int, int>();
            const string sql = @"select playerid, count(*)
                from (
                select distinct s.playerid, s.OutingID from score s
                inner join outing o on o.outingid = s.outingid
                where o.Committed = '1' and o.season = @season
                group by s.playerid, s.OutingID
                ) as foo
                group by playerid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
                using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@season", season.ToString()))
            {
                while (rdr.Read())
                {
                    pList.Add(rdr.GetInt32(0), rdr.GetInt32(1));
                }
            }

            return pList;
        }

        public static int AddPlayer(Player player)
        {
            const string sql = @"
                insert into Player
                (LastName, FirstName, GHIN, HIndex, CreateDateUTC, Status, UserName, Password, UserType, Email, TextNumber, NickName)
                values (@lastname, @firstname, @ghin, @hindex, @createdtutc, @status, @username, @password, @usertype, @email, @textnumber, @nickname)
                select convert(int, @@IDENTITY)
            ";
            int playerID;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                playerID = (int) DB.ExecScalar(sql, conn,
                    "@lastname", player.LastName ?? "",
                    "@firstname", player.FirstName ?? "",
                    "@ghin", player.GHIN ?? "",
                    "@hindex", player.HIndex.HasValue ? player.HIndex.ToString() : null,
                    "@createdtutc", DB.SqlDate(DateTime.UtcNow),
                    "@status", player.Status.ToString("d"),
                    "@username", player.UserName ?? "",
                    "@password", HashUtil.HashPassword(player.Password ?? ""),
                    "@usertype", player.UserType.ToString("d"),
                    "@email", player.Email ?? "",
                    "@textnumber", player.TextNumber ?? "",
                    "@nickname", player.NickName ?? "");
            }

            player.PlayerID = playerID;
            return playerID;
        }

        public static void UpdatePlayer(Player player)
        {
            const string dupSql = "select PlayerID from Player where UserName = @username";

            string sql = string.Format(@"update Player
                set LastName=@lastname, FirstName=@firstname, NickName=@nickname,
                UserName=@username, GHIN=@ghin, HIndex=@hindex, Status=@status,
                UserType=@usertype, Email=@email, TextNumber=@textnumber {0}
                where PlayerID=@playerid", player.Password.IsNotEmpty() ? ", Password=@password" : "");

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                // See if Player's username was changed to one already being used by another player
                object playerID = DB.ExecScalar(dupSql, conn, "@username", player.UserName);
                if (playerID != null && player.PlayerID != (int) playerID)
                {
                    throw new DuplicatePlayerUserNameException(
                        $"The User Name '{player.UserName}' is being used by another player.");
                }

                DB.ExecSQL(sql, conn,
                    "@playerid", player.PlayerID.ToString(),
                    "@lastname", player.LastName ?? "",
                    "@firstname", player.FirstName ?? "",
                    "@nickname", player.NickName ?? "",
                    "@username", player.UserName ?? "",
                    "@ghin", player.GHIN ?? "",
                    "@hindex", player.HIndex.HasValue ? player.HIndex.ToString() : null,
                    "@status", player.Status.ToString("d"),
                    "@usertype", player.UserType.ToString("d"),
                    "@email", player.Email ?? "",
                    "@textnumber", player.TextNumber ?? "",
                    "@password", HashUtil.HashPassword(player.Password ?? "")
                );
            }
        }

        public static void DeletePlayer(int playerID)
        {
            const string sql = "delete from Player where PlayerID = @playerid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@playerid", playerID.ToString());
            }
        }

        public static bool CanBeRemoved(int playerID)
        {
            // Can't remove a player that is referenced in another table
            const string scoresql = "select count(*) from Score where PlayerID = @playerid";
            const string gpsql = "select count(*) from GroupPlayer where PlayerID = @playerid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                int count = (int) DB.ExecScalar(scoresql, conn, "@playerid", playerID.ToString());
                if (count > 0) return false;

                count = (int) DB.ExecScalar(gpsql, conn, "@playerid", playerID.ToString());
                if (count > 0) return false;

            }

            return true;
        }

        public static bool UpdateHIndex(Player player, out string msg)
        {
            msg = "";

            // Make a call to the GHIN.com widget to get latest HIndex for player
            if (player.GHIN.IsEmpty())
            {
                msg = "No GHIN specified.";
                return false;
            }

            // If the HIndex has already been updated today, don't do it again
            if (player.HIndexLastUpdateUTC.HasValue &&
                ((DateTime) player.HIndexLastUpdateUTC).Date == DateTime.UtcNow.Date)
            {
                msg = "The Handicap Index has already been updated today.";
                return true;
            }

            // Make a call to the ghin API in a browser client
            string json;
            try
            {
                string url = $"https://api2.ghin.com/api/v1/public/login.json?ghinNumber={player.GHIN}&lastName={player.LastName}";
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    json = client.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return false;
            }

            // Parse out HIndex
            // JSON we are looking for looks like this. And we want to grab the value of the HiValue
            // "HiValue":"21.6"

            int startindex = json.IndexOf("\"HiValue\"");
            if (startindex < 0)
            {
                msg = "Could not find beginning Handicap Indicator string";
                return false;
            }

            int endindex = json.IndexOf(",", startindex);
            if (endindex < 0)
            {
                msg = "Could not find ending Handicap Indicator string";
                return false;
            }

            Regex regex = new Regex(@"[0123456789.]+");
           Match match = regex.Match(json.Substring(startindex, endindex  - startindex));
            if (!match.Success)
            {
                msg = $"Could not find Handicap number";
                return false;
            }
            
            Decimal hindex;
            if (!Decimal.TryParse(match.Value, out hindex))
            {
                msg = $"Could not parse Handicap string: {match.Value}";
                return false;
            }

            // Sanity check on Handicap Index value
            if (hindex < 0 || hindex > 40)
            {
                msg = $"Handicap Index out of appropriate range: {hindex}";
                return false;
            }

            // Update the player handicap
            Player.UpdateHIndex(player.PlayerID, hindex);

            msg = $"Handicap Index updated: {hindex}";
            return true;
        }

        public static void UpdateHIndex(int playerID, decimal? hindex)
        {
            const string sql = "update Player set HIndex = @hindex, HIndexLastUpdateUTC = @nowutc where playerID = @playerid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@playerid", playerID.ToString(), "@hindex", hindex?.ToString(), "@nowutc", DB.SqlDate(DateTime.UtcNow));
            }
        }

        public static void UpdateHIndexes(List<Player> playerList)
        {
            foreach (Player p in playerList)
            {
               UpdateHIndex(p.PlayerID, p.HIndex);
            }
        }
    }
}
