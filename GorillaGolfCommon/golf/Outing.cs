using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.SessionState;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Outing
    {
        public Outing()
        {
            GroupList = new List<Group>();
        }

        public int OutingID { get; set; }
        public int CourseID { get; set; }
        [DataType(DataType.Date)] public DateTime OutingDate { get; set; }
        public decimal SkinValue { get; set; }
        public decimal HicValue { get; set; }
        public decimal LowNetValue { get; set; }
        public decimal KPValue { get; set; }
        public bool Committed { get; set; }
        public bool Settled { get; set; }
        public bool NoFlipSkins { get; set; }
        public int Season { get; set; }
        public Course Course { get; set; }
        public List<Group> GroupList { get; set; }

        public List<string> KPWinners
        {
            get
            {
                List<PlayerOuting> pList = GroupList.SelectMany(x => x.PlayerList).Where(y => y.KPs > 0).ToList();
                return pList.Select(x => $"{x.Player.ShortName} ({x.KPs})").ToList();
            }
        }

        public List<string> SkinWinners
        {
            get
            {
                List<PlayerOuting> pList = GroupList.SelectMany(x => x.PlayerList).Where(y => y.Skins > 0).ToList();
                pList.Sort((a, b) => b.Skins.CompareTo(a.Skins));
                return pList.Select(x => $"{x.Player.ShortName} ({x.Skins})").ToList();
            }
        }

        public List<string> HicWinners
        {
            get
            {
                int maxhics = GroupList.SelectMany(x => x.PlayerList).Max(y => y.Hics);
                return GroupList.SelectMany(x => x.PlayerList).Where(y => y.Hics == maxhics)
                    .Select(z => $"{z.Player.ShortName} ({z.Hics})").ToList();
            }
        }

        public List<string> HicLosers
        {
            get
            {
                int minhics = GroupList.SelectMany(x => x.PlayerList).Min(y => y.Hics);
                return GroupList.SelectMany(x => x.PlayerList).Where(y => y.Hics == minhics)
                    .Select(z => $"{z.Player.ShortName} ({z.Hics})").ToList();
            }
        }

        public List<string> LowGrossers
        {
            get
            {
                int mingross = GroupList.SelectMany(x => x.PlayerList).Min(y => (int)(y.GrossScore??Int32.MaxValue));
                return GroupList.SelectMany(x => x.PlayerList).Where(y => y.GrossScore == mingross)
                    .Select(z => $"{z.Player.ShortName} ({z.GrossScore})").ToList();
            }
        }

        public List<string> LowNetWinners
        {
            get
            {
                return CalculateLowNetters().Select(x => $"{x.Value.Player.ShortName} ({x.Value.NetScore(Course)})").ToList();
            }
        }

        private Dictionary<int, PlayerOuting> CalculateLowNetters()
        {
            //  Determine list of low net winners
            int? lownet = null;
            Dictionary<int, PlayerOuting> lownetters = new Dictionary<int, PlayerOuting>();
            foreach (PlayerOuting po in GroupList.SelectMany(x => x.PlayerList))
            {
                int? playerNet = po.NetScore(Course);

                // Player might not have a low net score because of no handicap
                if (!playerNet.HasValue) continue;

                if (!lownet.HasValue)
                {
                    // First low netter
                    lownet = playerNet;
                    lownetters.Add(po.Player.PlayerID, po);
                }
                else if (playerNet == lownet)
                {
                    // multiple players with same low net score
                    lownetters.Add(po.Player.PlayerID, po);
                }
                else if (playerNet < lownet)
                {
                    // Sole low netter
                    lownet = playerNet;
                    lownetters.Clear();
                    lownetters.Add(po.Player.PlayerID, po);
                }
            }

            return lownetters;
        }

       public static int AddOuting(Outing outing)
        {
            const string sql = @"
                insert into Outing
                (CourseID, OutingDate, SkinValue, HicValue, LowNetValue, KPValue, Season, NoFlipSkins)
                values (@courseid, @outingdate, @skinvalue, @hicvalue, @lownetvalue, @kpvalue, @season, @noflipskins)
                select convert(int, @@IDENTITY)
            ";
            int outingID;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                outingID = (int) DB.ExecScalar(sql, conn,
                    "@courseid", outing.CourseID.ToString(),
                    "@outingdate", DB.SqlDate(outing.OutingDate),
                    "@skinvalue", outing.SkinValue.ToString(),
                    "@hicvalue", outing.HicValue.ToString(),
                    "@lownetvalue", outing.LowNetValue.ToString(),
                    "@kpvalue", outing.KPValue.ToString(),
                    "@season", outing.Season.ToString(),
                    "@noflipskins", outing.NoFlipSkins ? "1" : ""
                );
            }

            outing.OutingID = outingID;

            // Add the groups to the outing
            foreach (Group grp in outing.GroupList)
            {
                AddGroup(grp, outing.OutingID);
            }

            return outingID;
        }

        public static void UpdateOutingScore(Outing outing)
        {
            // The Outing is going to be sparsely populated with just what we need to update the scores

            // First, remove any scores for the players for this outing
            List<int> playerids = new List<int>();
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList)) playerids.Add(po.Player.PlayerID);
            string removesql = $@"delete from Score
                where OutingID = @outingid and PlayerID in ({string.Join(",", playerids.ToArray())})";

            string addsql = @"insert into Score
                (PlayerID, OutingID, HoleNumber, Strokes, HIndex, Hics, KP, SkinCarryOvers) values (@playerid, @outingid, @holenumber, @strokes, @hindex, @hics, @kp, @skincarryovers)";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(removesql, conn, "@outingid", outing.OutingID.ToString());

                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    foreach (Score s in po.Scores)
                    {
                        List<DB.SqlParam> paramList = new List<DB.SqlParam>
                        {
                            new DB.SqlParam("@playerid", po.Player.PlayerID, SqlDbType.Int),
                            new DB.SqlParam("@outingid", outing.OutingID, SqlDbType.Int),
                            new DB.SqlParam("@holenumber", s.HoleNumber, SqlDbType.Int),
                            new DB.SqlParam("@strokes", s.Strokes, SqlDbType.Int),
                            new DB.SqlParam("@hindex", po.Player.HIndex, SqlDbType.Decimal),
                            new DB.SqlParam("@hics", s.Hics, SqlDbType.Int),
                            new DB.SqlParam("@kp", s.KP ? "1" : "0"),
                            new DB.SqlParam("@skincarryovers", s.SkinCarryOvers ? "1" : "0")
                        };
                        DB.ExecSQL(addsql, conn, paramList);
                    }
                }
            }
        }

        public static void UpdateOuting(Outing outing)
        {
            const string sql = @"update Outing
                set CourseID=@courseid, OutingDate=@outingdate, SkinValue=@skinvalue, HicValue=@hicvalue, LowNetValue=@lownetvalue, KPValue=@kpvalue, NoFlipSkins=@noflipskins
                where OutingID=@outingid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@courseid", outing.CourseID.ToString(),
                    "@outingdate", DB.SqlDate(outing.OutingDate),
                    "@skinvalue", outing.SkinValue.ToString(),
                    "@hicvalue", outing.HicValue.ToString(),
                    "@lownetvalue", outing.LowNetValue.ToString(),
                    "@kpvalue", outing.KPValue.ToString(),
                    "@noflipskins", outing.NoFlipSkins ? "1" : "",
                    "@outingid", outing.OutingID.ToString());
            }

            // Remove all the existing groups for this outing
            DeleteGroups(outing.OutingID);

            // Add the groups to the outing
            foreach (Group grp in outing.GroupList)
            {
                AddGroup(grp, outing.OutingID);
            }

        }

        public static void AddGroup(Group grp, int outingid)
        {
            const string sql = @"
                insert into OutingGroup (outingid) values (@outingid)
                select convert(int, @@IDENTITY)";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                grp.GroupID = (int) DB.ExecScalar(sql, conn, "@outingid", outingid.ToString());
            }

            // Add the players to the group
            foreach (PlayerOuting player in grp.PlayerList)
            {
                AddPlayerToGroup(grp.GroupID, player.Player.PlayerID);
            }
        }

        public static void AddPlayerToGroup(int groupID, int playerID)
        {
            const string sql = @"
                insert into GroupPlayer
                (GroupID, PlayerID)
                values (@groupid, @playerid)
            ";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn,
                    "@groupid", groupID.ToString(),
                    "@playerid", playerID.ToString());
            }
        }

        public static void DeleteGroups(int outingid)
        {
            const string sql = @"
            delete from GroupPlayer where GroupID in
            (select GroupID from OutingGroup where OutingID = @outingid);
            delete from OutingGroup where OutingID = @outingid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@outingid", outingid.ToString());
            }
        }

        public static void DeleteOuting(int outingID)
        {
            // Do not allow delete if this outing is published
            if (IsCommitted(outingID)) return;

            const string sql = "delete from Outing where OutingID = @outingid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@outingid", outingID.ToString());
            }

            // delete all groups in this outing
            DeleteGroups(outingID);

            // Delete all transactions in this outing
            Transaction.DeleteTransactionsForOuting(outingID);
        }

        public static List<Outing> GetPlayerOutings(int season)
        {
            Dictionary<int, Outing> outDict = new Dictionary<int, Outing>();
            Dictionary<int, Dictionary<int, Group>> groupDict = new Dictionary<int, Dictionary<int, Group>>();

            const string sql = @"
                select OutingID, o.CourseID, OutingDate, SkinValue, HicValue, LowNetValue, KPValue, c.Name, Slope, Rating, HoleID, HoleNumber, Par, Committed, Settled, NoFlipSkins
                from Outing o
                inner join Course c on c.CourseID = o.CourseID
                inner join Hole h on h.CourseID = c.CourseID
                where o.season = @season and o.Committed = '1' and h.Par != 0
                order by OutingDate desc
            ";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@season", season.ToString()))
                {
                    while (rdr.Read())
                    {
                        int outingID = rdr.GetInt32(0);
                        // If this outing not found in dictionary, create a new outing and add it
                        if (!outDict.ContainsKey(outingID))
                        {
                            outDict[outingID] = new Outing
                            {
                                OutingID = outingID,
                                CourseID = rdr.GetInt32(1),
                                OutingDate = rdr.GetDateTime(2),
                                SkinValue = rdr.GetDecimal(3),
                                HicValue = rdr.GetDecimal(4),
                                LowNetValue = rdr.GetDecimal(5),
                                KPValue = rdr.GetDecimal(6),
                                Committed = rdr.GetStringOrDefault(13) == "1",
                                Settled = rdr.GetStringOrDefault(14) == "1",
                                NoFlipSkins = rdr.GetStringOrDefault(15) == "1",
                                Season = season,
                                Course = new Course
                                {
                                    CourseID = rdr.GetInt32(1),
                                    Name = rdr.GetString(7),
                                    Slope = rdr.GetInt32(8),
                                    Rating = rdr.GetDecimal(9)
                                }
                            };
                        }

                        Outing outing = outDict[outingID];

                        // Get hole info from all records
                        outing.Course.HoleList.Add(new Hole
                        {
                            HoleID = rdr.GetInt32(10),
                            HoleNumber = rdr.GetInt32(11),
                            Par = rdr.GetInt32(12),
                            CourseID = outing.CourseID
                        });

                    }
                }

                // If no outings, can return now
                if (outDict.Count == 0) return outDict.Values.ToList();

                const string groupsql = @"
                select g.GroupID, g.OutingID, p.PlayerID, p.LastName, p.FirstName, p.GHIN, p.HIndex, p.Email, p.TextNumber, s.HoleNumber, s.Strokes, s.HIndex, s.Hics, s.KP, s.SkinCarryOvers
                from OutingGroup g
                left join GroupPlayer pg on pg.GroupID = g.GroupID
                left join Player p on p.PlayerID = pg.PlayerID
                left join Score s on s.PlayerID = p.PlayerID and s.OutingID = g.OutingID
                left join Outing o on o.Outingid = g.OutingID
                where o.Committed = '1'";

                using (SqlDataReader rdr = DB.ExecSQLQuery(groupsql, conn))
                {
                    while (rdr.Read())
                    {
                        int groupID = rdr.GetInt32(0);
                        int outingID = rdr.GetInt32(1);
                        if (!groupDict.ContainsKey(outingID))
                        {
                            groupDict[outingID] = new Dictionary<int, Group>();
                        }
                        if (!groupDict[outingID].ContainsKey(groupID))
                        {
                            groupDict[outingID].Add(groupID, new Group
                            {
                                GroupID = groupID,
                                OutingID =outingID
                            });
                        }

                        int? playerID = rdr.IsDBNull(2) ? (int?)null : rdr.GetInt32(2);
                        if (playerID.HasValue)
                        {
                            // We may have already added this player since we are getting multiple rows in the query because
                            // of the join with the Scores table.
                            if (groupDict[outingID][groupID].PlayerList.All(x => x.Player.PlayerID != playerID))
                            {
                                Player p = new Player
                                {
                                    PlayerID = (int)playerID,
                                    LastName = rdr.GetString(3),
                                    FirstName = rdr.GetString(4),
                                    GHIN = rdr.GetString(5),
                                    // Set the HIndex in this player instance to that found in the Score table since it
                                    // is the HIndex for the player at the time the score was added whereas the HINdex
                                    // in the Player table is their current HIndex
                                    HIndex =rdr.IsDBNull(11) ? (decimal?)null : rdr.GetDecimal(11),
                                    Email = rdr.GetString(7),
                                    TextNumber = rdr.GetString(8)
                                };
                                groupDict[outingID][groupID].PlayerList.Add(new PlayerOuting { Player = p, Season = season });
                            }

                            // Get PlayerOuting
                            PlayerOuting po = groupDict[outingID][groupID].PlayerList.Find(x => x.Player.PlayerID == playerID);

                            // Add scores to player outing if found
                            if (!rdr.IsDBNull(9))
                            {
                                po.Scores.Add(new Score
                                {
                                    HoleNumber = rdr.GetInt32(9),
                                    Strokes = rdr.IsDBNull(10) ? 0 : rdr.GetInt32(10),
                                    HIndex = rdr.IsDBNull(11) ? (decimal?)null : rdr.GetDecimal(11),
                                    Hics = rdr.GetInt32(12),
                                    KP = rdr.GetStringOrDefault(13) == "1",
                                    SkinCarryOvers = rdr.GetStringOrDefault(14) == "1",
                                    PlayerID = po.Player.PlayerID,
                                    OutingID = outingID
                                });
                            }
                        }
                    }
                }
            }

            
            foreach (int outid in outDict.Keys)
            {
                // Add groups in dictionary to Outings' group list
                if (groupDict.ContainsKey(outid))
                {
                    outDict[outid].GroupList = groupDict[outid].Values.ToList();
                }

                // Get list of holes.
                List<int> holeNumList = outDict[outid].Course.HoleList.Select(x => x.HoleNumber).ToList();

                // Add Score entries for each hole for each player if not already present
                foreach (PlayerOuting po in outDict[outid].GroupList.SelectMany(x => x.PlayerList))  // This gets a list of all the players across the groups
                {
                    foreach (int holenum in holeNumList)
                    {
                        // Check to see if the player's score list already contains an entry for this hole
                        if (po.Scores.Any(x => x.HoleNumber == holenum)) continue;

                        // Hole not found in list of scores for player so add a blank score
                        po.Scores.Add(new Score
                        {
                            HIndex = po.Player.HIndex,
                            HoleNumber = holenum,
                            OutingID = outDict[outid].OutingID,
                            PlayerID = po.Player.PlayerID,
                            Strokes = 0,
                            Hics = 0
                        });
                    }

                    // Sort the player's scores by holenumber so they are in sequence in UI
                    po.Scores.Sort((x, y) => x.HoleNumber.CompareTo(y.HoleNumber));
                }

                // Calculate skins for each score
                CalculateSkins(outDict[outid]);
            }

            return outDict.Values.ToList();
        }

        public static Outing GetOuting(int outingID)
        {
            const string sql = @"
                select o.OutingID, o.CourseID, o.OutingDate, o.SkinValue, o.HicValue, o.LowNetValue, o.KPValue, c.Name, c.Slope, c.Rating, h.HoleID, h.HoleNumber, h.Par, o.Committed, o.season, o.Settled, o.NoFlipSkins
                from Outing o
                inner join Course c on c.CourseID = o.CourseID
                inner join Hole h on h.CourseID = c.CourseID
                where o.OutingID = @outingid and h.Par != 0
            ";

            Outing outing = null;
            Dictionary<int, Group> groupDict = new Dictionary<int, Group>();

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@outingid", outingID.ToString()))
                {
                    while (rdr.Read())
                    {
                        if (outing == null)
                        {
                            // get common outing fields and course fields from first record
                            outing = new Outing
                            {
                                OutingID = rdr.GetInt32(0),
                                CourseID = rdr.GetInt32(1),
                                OutingDate = rdr.GetDateTime(2),
                                SkinValue = rdr.GetDecimal(3),
                                HicValue = rdr.GetDecimal(4),
                                LowNetValue = rdr.GetDecimal(5),
                                KPValue = rdr.GetDecimal(6),
                                Committed = rdr.GetStringOrDefault(13) == "1",
                                Season = rdr.GetInt32(14),
                                Settled = rdr.GetStringOrDefault(15) == "1",
                                NoFlipSkins = rdr.GetStringOrDefault(16) == "1",
                                Course = new Course
                                {
                                    CourseID = rdr.GetInt32(1),
                                    Name = rdr.GetString(7),
                                    Slope = rdr.GetInt32(8),
                                    Rating = rdr.GetDecimal(9)
                                }
                            };
                        }

                        // Get hole info from all records
                        outing.Course.HoleList.Add(new Hole
                        {
                            HoleID = rdr.GetInt32(10),
                            HoleNumber = rdr.GetInt32(11),
                            Par = rdr.GetInt32(12),
                            CourseID = outing.CourseID
                        });
                    }
                }

                if (outing == null) return null;

                const string groupsql = @"
                select g.GroupID, g.OutingID, p.PlayerID, p.LastName, p.FirstName, p.GHIN, p.HIndex, p.Email, p.TextNumber, s.HoleNumber, s.Strokes, s.HIndex, s.Hics, s.KP, s.SkinCarryOvers
                from OutingGroup g
                left join GroupPlayer pg on pg.GroupID = g.GroupID
                left join Player p on p.PlayerID = pg.PlayerID
                left join Score s on s.PlayerID = p.PlayerID and s.OutingID = g.OutingID
                where g.OutingID = @outingid";

                
                using (SqlDataReader rdr = DB.ExecSQLQuery(groupsql, conn, "@outingid", outingID.ToString()))
                {
                    while (rdr.Read())
                    {
                        int groupID = rdr.GetInt32(0);
                        if (!groupDict.ContainsKey(groupID))
                        {
                            groupDict.Add(groupID, new Group
                            {
                                GroupID = groupID,
                                OutingID = rdr.GetInt32(1)
                            });
                        }

                        int? playerID = rdr.IsDBNull(2) ? (int?) null : rdr.GetInt32(2);
                        if (playerID.HasValue)
                        {
                            // We may have already added this player since we are getting multiple rows in the query because
                            // of the join with the Scores table.
                            if (groupDict[groupID].PlayerList.All(x => x.Player.PlayerID != playerID))
                            {
                                Player p = new Player
                                {
                                    PlayerID = (int) playerID,
                                    LastName = rdr.GetString(3),
                                    FirstName = rdr.GetString(4),
                                    GHIN = rdr.GetString(5),
                                    HIndex =rdr.IsDBNull(6) ? (decimal?) null : rdr.GetDecimal(6),
                                    Email = rdr.GetString(7),
                                    TextNumber = rdr.GetString(8)
                                };
                                groupDict[groupID].PlayerList.Add(new PlayerOuting {Player = p, Season = outing.Season});
                            }

                            // Get PlayerOuting
                            PlayerOuting po = groupDict[groupID].PlayerList.Find(x => x.Player.PlayerID == playerID);

                            // Add scores to player outing if found
                            if (!rdr.IsDBNull(9))
                            {
                                po.Scores.Add(new Score
                                {
                                    HoleNumber = rdr.GetInt32(9),
                                    Strokes = rdr.IsDBNull(10) ? 0 : rdr.GetInt32(10),
                                    HIndex = rdr.IsDBNull(11) ? (decimal?) null : rdr.GetDecimal(11),
                                    Hics = rdr.GetInt32(12),
                                    KP = rdr.GetStringOrDefault(13) == "1",
                                    SkinCarryOvers = rdr.GetStringOrDefault(14) == "1",
                                    PlayerID = po.Player.PlayerID,
                                    OutingID = groupDict[groupID].OutingID
                                });
                            }
                        }
                    }
                }
            }

            // Add groups in dictionary to Outing group list
            if (groupDict.Count > 0) outing.GroupList = groupDict.Values.ToList();

            // Get list of holes.
            List<int> holeNumList = outing.Course.HoleList.Select(x => x.HoleNumber).ToList();

            // Add Score entries for each hole for each player if not already present
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))  // This gets a list of all the players across the groups
            {
                foreach (int holenum in holeNumList)
                {
                    // Check to see if the player's score list already contains an entry for this hole
                    if (po.Scores.Any(x => x.HoleNumber == holenum)) continue;

                    // Hole not found in list of scores for player so add a blank score
                    po.Scores.Add(new Score
                    {
                        HIndex = po.Player.HIndex,
                        HoleNumber = holenum,
                        OutingID = outing.OutingID,
                        PlayerID = po.Player.PlayerID,
                        Strokes = 0,
                        Hics = 0
                    });
                }

                // Sort the player's scores by holenumber so they are in sequence in UI
                po.Scores.Sort((x,y) => x.HoleNumber.CompareTo(y.HoleNumber));
            }

            // Calculate skins for each score
           CalculateSkins(outing);

            return outing;
        }

        public static void CalculateSkins(Outing outing)
        {
            if (outing.NoFlipSkins) CalculateNoFlipSkins(outing);
            else CalculateSkinsWithFlip(outing);
        }

        public static void CalculateNoFlipSkins(Outing outing)
        {
            foreach (Hole h in outing.Course.ActiveHoles)
            {
                int? min = null;
                List<int> playerIDs = new List<int>();
                foreach (Score s in (outing.GroupList.SelectMany(x => x.PlayerList)).SelectMany(y => y.Scores)
                    .Where(z => z.HoleNumber == h.HoleNumber))
                {
                    // If no score yet then ignore
                    if (s.Strokes == 0) continue;

                    if (!min.HasValue)
                    {
                        min = s.Strokes;
                        playerIDs.Add(s.PlayerID);
                    }
                    else if (s.Strokes == (int) min)
                    {
                        playerIDs.Add(s.PlayerID);
                    }
                    else if (s.Strokes < (int) min)
                    {
                        min = s.Strokes;
                        playerIDs.Clear();
                        playerIDs.Add(s.PlayerID);
                    }
                }

                if (playerIDs.Count == 1)
                {
                    // The sole player on the list is the winner of the hole
                    // Get the player outing
                    PlayerOuting po = outing.GroupList.SelectMany(x => x.PlayerList)
                        .First(y => y.Player.PlayerID == playerIDs[0]);

                    // assign skin for current hole
                    po.Scores.First(x => x.HoleNumber == h.HoleNumber).Skin = true;
                }
            }
        }

        public static void CalculateSkinsWithFlip(Outing outing)
        {
            List<int> holeCarryovers = new List<int>();
            List<int> carryOverWinnerIDs = new List<int>();
            foreach (Hole h in outing.Course.ActiveHoles)
            {
                int? min = null;
                List<int> playerIDs = new List<int>();
                foreach (Score s in (outing.GroupList.SelectMany(x => x.PlayerList)).SelectMany(y => y.Scores).Where(z => z.HoleNumber == h.HoleNumber))
                {
                    // If no score yet then ignore
                    if (s.Strokes == 0) continue;

                    if (!min.HasValue)
                    {
                        min = s.Strokes;
                        playerIDs.Add(s.PlayerID);
                    }
                    else if (s.Strokes == (int)min)
                    {
                        playerIDs.Add(s.PlayerID);
                    }
                    else if (s.Strokes < (int)min)
                    {
                        min = s.Strokes;
                        playerIDs.Clear();
                        playerIDs.Add(s.PlayerID);
                    }

                    // Keep track of carryover winner if one selected
                    if (s.SkinCarryOvers && !carryOverWinnerIDs.Contains(s.PlayerID)) carryOverWinnerIDs.Add(s.PlayerID);
                }

                // If the playerID list has more than one entry, this is a carryover
                if (playerIDs.Count > 1)
                {
                    holeCarryovers.Add(h.HoleNumber);
                }
                else if (playerIDs.Count == 1)
                {
                    // The sole player on the list is the winner of the hole and also  gets the carryovers.
                    // Get the player outing
                    int playerID = playerIDs[0];
                    PlayerOuting po = outing.GroupList.SelectMany(x => x.PlayerList).First(y => y.Player.PlayerID == playerIDs[0]);

                    // assign skin for current hole
                    po.Scores.First(x => x.HoleNumber == h.HoleNumber).Skin = true;
                    foreach (int hn in holeCarryovers)
                    {
                        po.Scores.First(x => x.HoleNumber == hn).Skin = true;
                    }

                    holeCarryovers.Clear();
                }
            }

            // If there are still holes on the holeCarryOver list, need to give these skins
            // to whomever is indicated as the carry over flip winner.
            if (holeCarryovers.Count > 0 && carryOverWinnerIDs.Count == 1)
            {
                PlayerOuting po = outing.GroupList.SelectMany(x => x.PlayerList).First(y => y.Player.PlayerID == carryOverWinnerIDs[0]);
                foreach (int hn in holeCarryovers)
                {
                    Score s = po.Scores.First(x => x.HoleNumber == hn);
                    s.Skin = true;
                    s.FlipSkin = true;
                }
            }
        }

        public static List<string> CheckOuting(Outing outing)
        {
            // Check outing for scoring errors
            // 1. All Players have scores for all holes
            // 2. 3 hics per hole per group
            // 3. One Flip winner indicated if no skin on last hole
            // 4. No more than one KP winner per hole

            // Check for all holes scored
            List<string> errors = new List<string>();
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
            {
                foreach (Score s in po.Scores)
                {
                    if (s.Strokes == 0)
                    {
                        errors.Add($"{po.Player.ShortName} has no score on Hole: {s.HoleNumber}");
                    }
                }
            }

            // 3 hics per hole per group
            for (int gindex = 0; gindex < outing.GroupList.Count; gindex++)
            {
                foreach (Hole h in outing.Course.ActiveHoles)
                {
                    int hiccount = outing.GroupList[gindex].PlayerList.SelectMany(x => x.Scores).Where(y => y.HoleNumber == h.HoleNumber).Sum(z => z.Hics);
                    if (hiccount != 3)
                    {
                        errors.Add($"Group: {gindex+1}, Hole: {h.HoleNumber} -- incorrect Hic count");
                    }
                }
            }

            // If no skin on last hole and we are using with flip skin calculation, check for no flip winner entered.
            if (!outing.NoFlipSkins)
            {
                int maxholenum = outing.Course.ActiveHoles.Max(x => x.HoleNumber);
                bool lastskin = outing.GroupList.SelectMany(x => x.PlayerList).SelectMany(y => y.Scores)
                    .Where(z => z.HoleNumber == maxholenum).Any(a => a.Skin);
                int coCount = outing.GroupList.SelectMany(x => x.PlayerList).SelectMany(y => y.Scores)
                    .Where(z => z.HoleNumber == maxholenum).Count(a => a.SkinCarryOvers);
                if (!lastskin && coCount == 0)
                {
                    errors.Add($"No skin on last hole and no flip winner indicated.");
                }
                else if (!lastskin && coCount > 1)
                {
                    errors.Add($"No skin on last hole and more than one flip winner indicated.");
                }
            }

            // No more than one KP winner per par 3 hole
            foreach (Hole h in outing.Course.ActiveHoles.Where(x => x.Par == 3))
            {
                int kpcount = outing.GroupList.SelectMany(x => x.PlayerList).SelectMany(y => y.Scores)
                    .Count(z => z.HoleNumber == h.HoleNumber && z.KP);
                if (kpcount > 1) errors.Add($"Too many KPs indicated on Hole: {h.HoleNumber}");
            }

            return errors;
        }

        public static void GenerateTransactions(int outingid)
        {
            // Calculate transactions for this outing
            // 1. Skins
            // 2. KP 
            // 3. Low Net
            // 4. Hics

            Outing outing = GetOuting(outingid);
            if (outing == null) return;

            GenerateSkinTransactions(outing);
            GenerateKPTransactions(outing);
            GenerateLowNetTransactions(outing);
            GenerateHICTransactions(outing);
        }

        public static void GenerateLowNetTransactions(Outing outing)
        {
            // Generate Low Net transactions for each player

            // Get low netters
            Dictionary<int, PlayerOuting> lownetters = outing.CalculateLowNetters();

            // There may be more than one low netter so divide up the low net value and round
            // to 2 decimal places.
            decimal lownetvalue = (lownetters.Count > 1)
                ? Math.Round(outing.LowNetValue / lownetters.Count, 2)
                : outing.LowNetValue;

            foreach (PlayerOuting p1 in outing.GroupList.SelectMany(x => x.PlayerList))
            {
                foreach (PlayerOuting p2 in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    // If player 1 is on lownet list but player 2 is not, add transaction
                    if (lownetters.ContainsKey(p1.Player.PlayerID) && !lownetters.ContainsKey(p2.Player.PlayerID))
                    {
                        Transaction.AddLowNetTransaction(outing.OutingID, p1.Player.PlayerID, p2.Player.PlayerID, lownetvalue);
                        Transaction.AddLowNetTransaction(outing.OutingID, p2.Player.PlayerID, p1.Player.PlayerID, -lownetvalue);
                    }
                }
            }
        }

        public static void GenerateKPTransactions(Outing outing)
        {
            // Generate KP transactions for each player

            // Create a dictionary of KPs for each player
            Dictionary<int, int> kpcounts = new Dictionary<int, int>();
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
            {
                kpcounts.Add(po.Player.PlayerID, po.KPs);
            }

            foreach (int p1 in kpcounts.Keys)
            {
                foreach (int p2 in kpcounts.Keys)
                {
                    if (p1 == p2) continue;
                    if (kpcounts[p1] == kpcounts[p2]) continue;
                    if (kpcounts[p1] > kpcounts[p2])
                    {
                        Transaction.AddKPTransaction(outing.OutingID, p1, p2, (kpcounts[p1] - kpcounts[p2]) * outing.KPValue);
                        Transaction.AddKPTransaction(outing.OutingID, p2, p1, -(kpcounts[p1] - kpcounts[p2]) * outing.KPValue);
                    }
                }
            }
        }

        public static void GenerateSkinTransactions(Outing outing)
        {
            if (outing.NoFlipSkins) GenerateNoFlipSkinTransactions(outing);
            else GenerateSkinTransactionsWithFlip(outing);
        }

        public static void GenerateNoFlipSkinTransactions(Outing outing)
        {
            // Generate skin transactions for each player
            // No carryovers used in skin calculation so each skin winner gets 
            // a percentage of the skin based on their percentage of skins won.

            // Create a dictionary of skins for each player
            decimal totalskins = 0;
            Dictionary<int, int> skincounts = new Dictionary<int, int>();
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
            {
                skincounts.Add(po.Player.PlayerID, po.Skins);
                totalskins += po.Skins;
            }

            int holecount = outing.Course.ActiveHoles.Count();

            foreach (int p1 in skincounts.Keys)
            {
                foreach (int p2 in skincounts.Keys)
                {
                    if (p1 == p2) continue;
                    if (skincounts[p1] == skincounts[p2]) continue;
                    if (skincounts[p1] > skincounts[p2])
                    {
                        decimal amount = Math.Round((skincounts[p1] - skincounts[p2]) / totalskins * outing.SkinValue * holecount, 2);
                        Transaction.AddSkinTransaction(outing.OutingID, p1, p2, amount);
                        Transaction.AddSkinTransaction(outing.OutingID, p2, p1, -amount);
                    }
                }
            }
        }

        public static void GenerateSkinTransactionsWithFlip(Outing outing)
        {
            // Generate skin transactions for each player

            // Create a dictionary of skins for each player
            Dictionary<int, int> skincounts = new Dictionary<int, int>();
            foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
            {
                skincounts.Add(po.Player.PlayerID, po.Skins);
            }

            foreach (int p1 in skincounts.Keys)
            {
                foreach (int p2 in skincounts.Keys)
                {
                    if (p1 == p2) continue;
                    if (skincounts[p1] == skincounts[p2]) continue;
                    if (skincounts[p1] > skincounts[p2])
                    {
                        decimal amount = (skincounts[p1] - skincounts[p2]) * outing.SkinValue;
                        Transaction.AddSkinTransaction(outing.OutingID, p1, p2, amount);
                        Transaction.AddSkinTransaction(outing.OutingID, p2, p1, -amount);
                    }
                }
            }
        }

        public static void GenerateHICTransactions(Outing outing)
        {
            // Generate HIC transactions for each Group
            foreach (var grp in outing.GroupList)
            {
                // Create a dictionary of hiccounts for each player
                Dictionary<int, int> hiccounts = new Dictionary<int, int>();
                foreach (PlayerOuting p in grp.PlayerList)
                {
                    hiccounts.Add(p.Player.PlayerID, p.Hics);
                }

                // Compare HIC counts for each player against the other players in the group
                foreach (int p1 in hiccounts.Keys)
                {
                    foreach (int p2 in hiccounts.Keys)
                    {
                        if (p1 == p2) continue;
                        if (hiccounts[p1] == hiccounts[p2]) continue;
                        if (hiccounts[p1] > hiccounts[p2])
                        {
                            Transaction.AddHicTransaction(outing.OutingID, p1, p2, (hiccounts[p1] - hiccounts[p2]) * outing.HicValue);
                            Transaction.AddHicTransaction(outing.OutingID, p2, p1, -(hiccounts[p1] - hiccounts[p2]) * outing.HicValue);
                        }
                    }
                }
            }
        }

        public static void Settle(int outingid)
        {
            const string sql = @"update outing
                set Settled = '1'
                where OutingID = @outingid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQLQuery(sql, conn, "@outingid", outingid.ToString());
            }
        }

        public static void Unsettle(int outingid)
        {
            const string sql = @"update outing
                set Settled = '0'
                where OutingID = @outingid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQLQuery(sql, conn, "@outingid", outingid.ToString());
            }
        }

        public static void Commit(int outingid)
        {
            // Generate the transactions for this outing
            GenerateTransactions(outingid);

            const string sql = @"update outing
                set Committed = '1'
                where OutingID = @outingid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQLQuery(sql, conn, "@outingid", outingid.ToString());
            }
        }

        public static bool IsCommitted(int outingid)
        {
            const string sql = "select count(*) from Outing where outingid = @outingid and committed = '1'";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                int count = (int) DB.ExecScalar(sql, conn, "@outingid", outingid.ToString());
                return count > 0;
            }
        }

        public static void Uncommit(int outingid)
        {
            if (!IsCommitted(outingid)) return;

            const string sql = "update Outing set Committed = '0' where outingid = @outingid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@outingid", outingid.ToString());
            }

            // Remove all transactions associated with this outing
            Transaction.DeleteTransactionsForOuting(outingid);
        }

        public static List<Outing> GetOutings(int season)
        {
            Dictionary<int, Outing> outDict = new Dictionary<int, Outing>();

            const string sql = @"
                select OutingID, o.CourseID, OutingDate, SkinValue, HicValue, LowNetValue, KPValue, c.Name, Slope, Rating, HoleID, HoleNumber, Par, Committed, Season, Settled, NoFlipSkins
                from Outing o
                inner join Course c on c.CourseID = o.CourseID
                inner join Hole h on h.CourseID = c.CourseID
                where o.season = @season
                order by OutingDate desc
            ";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@season", season.ToString()))
                {
                    while (rdr.Read())
                    {
                        int outingID = rdr.GetInt32(0);
                        // If this outing not found in dictionary, create a new outing and add it
                        if (!outDict.ContainsKey(outingID))
                        {
                            outDict[outingID] = new Outing
                            {
                                OutingID = outingID,
                                CourseID = rdr.GetInt32(1),
                                OutingDate = rdr.GetDateTime(2),
                                SkinValue = rdr.GetDecimal(3),
                                HicValue = rdr.GetDecimal(4),
                                LowNetValue = rdr.GetDecimal(5),
                                KPValue = rdr.GetDecimal(6),
                                Committed = rdr.GetStringOrDefault(13) == "1",
                                Season = rdr.GetInt32(14),
                                Settled = rdr.GetStringOrDefault(15) == "1",
                                NoFlipSkins = rdr.GetStringOrDefault(16) == "1",
                                Course = new Course
                                {
                                    CourseID = rdr.GetInt32(1),
                                    Name = rdr.GetString(7),
                                    Slope = rdr.GetInt32(8),
                                    Rating = rdr.GetDecimal(9)
                                }
                            };
                        }

                        Outing outing = outDict[outingID];

                        // Get hole info from all records
                        outing.Course.HoleList.Add(new Hole
                        {
                            HoleID = rdr.GetInt32(10),
                            HoleNumber = rdr.GetInt32(11),
                            Par = rdr.GetInt32(12),
                            CourseID = outing.CourseID
                        });

                    }
                }

                return outDict.Values.ToList();
            }
        }

    }
}
