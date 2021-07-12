using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Reports
    {

        public static List<KeyValuePair< int, string>> AverageParScore(int par, int season)
        {
            Dictionary<int, List<int>> scoreDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                // Don't include outings on courses that are all par 3s
                if (!outing.Course.HoleList.Exists(x => x.Par != 3)) continue;

                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!scoreDict.ContainsKey(po.Player.PlayerID)) scoreDict.Add(po.Player.PlayerID, new List<int>());
                    foreach (Score score in po.Scores)
                    {
                        // Skip scores that are for holes not having the specified par
                        if (outing.Course.HoleList.Find(x => x.HoleNumber == score.HoleNumber).Par != par) continue;
                        scoreDict[po.Player.PlayerID].Add(score.Strokes);
                    }
                }
            }
            
            // Average all the scores and put in return dictionary
            SortedDictionary<int, double> parDict = new SortedDictionary<int, double>();
            foreach (int playerID in scoreDict.Keys)
            {
                parDict.Add(playerID, scoreDict[playerID].Count > 0 ? Math.Round(scoreDict[playerID].Average(x => x), 2) : 0);
            }

            List<KeyValuePair< int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(parDict);
            data.Sort((x, y) => x.Value.CompareTo(y.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value > 0 ? x.Value.ToString("N2") : "--")).ToList();
        }
       
        public static List<KeyValuePair< int, string>> AverageGrossPerOuting(int season)
        {
            Dictionary<int, List<int>> scoreDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                // Don't include outings on courses that are all par 3s
                if (!outing.Course.HoleList.Exists(x => x.Par != 3)) continue;

                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!scoreDict.ContainsKey(po.Player.PlayerID)) scoreDict.Add(po.Player.PlayerID, new List<int>());
                    int? grossScore = po.GrossScore;
                    if (!grossScore.HasValue) continue;
                    scoreDict[po.Player.PlayerID].Add((int)grossScore);
                }
            }
            
            // Average all the gross scores and put in return dictionary
            SortedDictionary<int, double> grossDict = new SortedDictionary<int, double>();
            foreach (int playerID in scoreDict.Keys)
            {
                grossDict.Add(playerID, Math.Round(scoreDict[playerID].Average(x => x), 2));
            }

            List<KeyValuePair< int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(grossDict);
            data.Sort((x, y) => x.Value.CompareTo(y.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.ToString("N1"))).ToList();
        }

        public static List<KeyValuePair<int, string>> AverageNetPerOuting(int season)
        {
            Dictionary<int, List<int>> scoreDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                // Don't include outings on courses that are all par 3s
                if (!outing.Course.HoleList.Exists(x => x.Par != 3)) continue;

                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!scoreDict.ContainsKey(po.Player.PlayerID)) scoreDict.Add(po.Player.PlayerID, new List<int>());
                    int? netScore = po.NetScore(outing.Course);
                    if (!netScore.HasValue) continue;
                    scoreDict[po.Player.PlayerID].Add((int)netScore);
                }
            }

            // Average all the net scores and put in return dictionary
            SortedDictionary<int, double> netDict = new SortedDictionary<int, double>();
            foreach (int playerID in scoreDict.Keys)
            {
                if (scoreDict[playerID].Count == 0) continue;
                netDict.Add(playerID, Math.Round(scoreDict[playerID].Average(x => x), 2));
            }

            List<KeyValuePair<int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(netDict);
            data.Sort((x, y) => x.Value.CompareTo(y.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.ToString("N1"))).ToList();
        }

        public static List<KeyValuePair<int, string>> TotalSkins(int season)
        {
            Dictionary<int, List<int>> skinDict = new Dictionary<int, List<int>>();
            Dictionary<int, List<int>> flipskinDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!skinDict.ContainsKey(po.Player.PlayerID)) skinDict.Add(po.Player.PlayerID, new List<int>());
                    skinDict[po.Player.PlayerID].Add(po.Skins);

                    if (!flipskinDict.ContainsKey(po.Player.PlayerID)) flipskinDict.Add(po.Player.PlayerID, new List<int>());
                    flipskinDict[po.Player.PlayerID].Add(po.FlipSkins);
                }
            }

            // Sum all the skins and put in return dictionary
            SortedDictionary<int, double> skinSumDict = new SortedDictionary<int, double>();
            foreach (int playerID in skinDict.Keys)
            {
                if (skinDict[playerID].Count == 0) skinSumDict.Add(playerID, 0);
               skinSumDict.Add(playerID, skinDict[playerID].Sum(x => x));
            }

            // Sum all flipskins
            Dictionary<int, int> flipsumDict = new Dictionary<int, int>();
            foreach (int playerID in flipskinDict.Keys)
            {
                if (flipskinDict[playerID].Count == 0) flipsumDict.Add(playerID, 0);
                flipsumDict.Add(playerID, flipskinDict[playerID].Sum(x => x));
            }

            List<KeyValuePair<int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(skinSumDict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, $"{x.Value.ToString()} [{flipsumDict[x.Key]}]")).ToList();
        }

        public static List<KeyValuePair< int, string>> AverageSkinsPerOuting(int season)
        {
            Dictionary<int, List<int>> skinDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!skinDict.ContainsKey(po.Player.PlayerID)) skinDict.Add(po.Player.PlayerID, new List<int>());
                    skinDict[po.Player.PlayerID].Add(po.Skins);
                }
            }
            
            // Average all the skins and put in return dictionary
            SortedDictionary<int, double> avgskinDict = new SortedDictionary<int, double>();
            foreach (int playerID in skinDict.Keys)
            {
                avgskinDict.Add(playerID, Math.Round(skinDict[playerID].Average(x => x), 2));
            }

            List<KeyValuePair< int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(avgskinDict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, $"{x.Value:N2}")).ToList();
        }

        public static List<KeyValuePair<int, string>> TotalHICS(int season)
        {
            Dictionary<int, List<int>> scoreDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!scoreDict.ContainsKey(po.Player.PlayerID)) scoreDict.Add(po.Player.PlayerID, new List<int>());
                    scoreDict[po.Player.PlayerID].Add(po.Hics);
                }
            }

            // Sum all the HICS and put in return dictionary
            SortedDictionary<int, double> dict = new SortedDictionary<int, double>();
            foreach (int playerID in scoreDict.Keys)
            {
                if (scoreDict[playerID].Count == 0) dict.Add(playerID, 0);
                dict.Add(playerID, scoreDict[playerID].Sum(x => x));
            }

            List<KeyValuePair<int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(dict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.ToString())).ToList();
        }

        public static List<KeyValuePair< int, string>> AverageHICSPerOuting(int season)
        {
            Dictionary<int, List<int>> hicDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!hicDict.ContainsKey(po.Player.PlayerID)) hicDict.Add(po.Player.PlayerID, new List<int>());
                    hicDict[po.Player.PlayerID].Add(po.Hics);
                }
            }
            
            // Average all the HICs and put in return dictionary
            SortedDictionary<int, double> avgHICDict = new SortedDictionary<int, double>();
            foreach (int playerID in hicDict.Keys)
            {
                avgHICDict.Add(playerID, Math.Round(hicDict[playerID].Average(x => x), 2));
            }

            List<KeyValuePair< int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(avgHICDict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, $"{x.Value:N2}")).ToList();
        }

        public static List<KeyValuePair<int, string>> TotalKPs(int season)
        {
            Dictionary<int, List<int>> scoreDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!scoreDict.ContainsKey(po.Player.PlayerID)) scoreDict.Add(po.Player.PlayerID, new List<int>());
                    scoreDict[po.Player.PlayerID].Add(po.KPs);
                }
            }

            // Sum all the KPs and put in return dictionary
            SortedDictionary<int, double> dict = new SortedDictionary<int, double>();
            foreach (int playerID in scoreDict.Keys)
            {
                if (scoreDict[playerID].Count == 0) dict.Add(playerID, 0);
                dict.Add(playerID, scoreDict[playerID].Sum(x => x));
            }

            List<KeyValuePair<int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(dict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.ToString())).ToList();
        }

        public static List<KeyValuePair< int, string>> AverageKPsPerOuting(int season)
        {
            Dictionary<int, List<int>> kpDict = new Dictionary<int, List<int>>();
            List<Outing> OutingList = Outing.GetPlayerOutings(season); // Published outings only
            foreach (Outing outing in OutingList)
            {
                foreach (PlayerOuting po in outing.GroupList.SelectMany(x => x.PlayerList))
                {
                    if (!kpDict.ContainsKey(po.Player.PlayerID)) kpDict.Add(po.Player.PlayerID, new List<int>());
                    kpDict[po.Player.PlayerID].Add(po.KPs);
                }
            }
            
            // Average all the KPs and put in return dictionary
            SortedDictionary<int, double> avgKPDict = new SortedDictionary<int, double>();
            foreach (int playerID in kpDict.Keys)
            {
                avgKPDict.Add(playerID, Math.Round(kpDict[playerID].Average(x => x), 2));
            }

            List<KeyValuePair< int, double>> data = new List<KeyValuePair<int, double>>();
            data.AddRange(avgKPDict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, $"{x.Value:N2}")).ToList();
        }

        public static List<KeyValuePair<int, string>> TotalTransactions(bool unsettledOnly, int season)
        {
            // Get all outing related transactions for the particular season
            string seasonFilter = $"and TxDateUTC > '{season}-01-01' and TxDateUTC < '{season + 1}-01-01'";
           string sql;

            if (!unsettledOnly)
            {
                sql = $@"select PlayerID, Amount
                from Transact
               where OutingID != 0 {seasonFilter}";
            }
            else
            {
                sql = $@"select t.PlayerID, t.Amount
                from Transact t
                inner join Outing o on o.OutingID = t.OutingID
               where o.OutingID != 0 and ISNULL(o.Settled, '0') != '1' {seasonFilter}";
            }

            Dictionary<int, List<decimal>> winDict = new Dictionary<int, List<decimal>>();
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn))
            {
                while (rdr.Read())
                {
                    int playerID = rdr.GetInt32(0);
                    if (!winDict.ContainsKey(playerID)) winDict.Add(playerID, new List<decimal>());
                    winDict[playerID].Add(rdr.GetDecimal(1));
                }
            }
           
            // Sum all the winnings and put in return dictionary
            SortedDictionary<int, decimal> dict = new SortedDictionary<int, decimal>();
            foreach (int playerID in winDict.Keys)
            {
                if (winDict[playerID].Count == 0) dict.Add(playerID, 0);
                dict.Add(playerID, winDict[playerID].Sum(x => x));
            }

            List<KeyValuePair<int, decimal>> data = new List<KeyValuePair<int, decimal>>();
            data.AddRange(dict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.Currency())).ToList();
        }

        public static List<KeyValuePair<int, string>> TotalTransactionsPerOuting(int season)
        {
            string seasonFilter = $" and TxDateUTC > '{season}-01-01' and TxDateUTC < '{season + 1}-01-01'";

            // Get all outing related transactions
            string sql = $@"select PlayerID, OutingID, Amount
                from Transact
                where OutingID != 0 {seasonFilter}";

            Dictionary<int, Dictionary<int, List<decimal>>> winDict = new Dictionary<int, Dictionary<int, List<decimal>>>();
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn))
            {
                while (rdr.Read())
                {
                    int playerID = rdr.GetInt32(0);
                    int outingID = rdr.GetInt32(1);
                    if (!winDict.ContainsKey(playerID)) winDict.Add(playerID, new Dictionary<int, List<decimal>>());
                    if (!winDict[playerID].ContainsKey(outingID)) winDict[playerID].Add(outingID, new List<decimal>());
                    winDict[playerID][outingID].Add(rdr.GetDecimal(2));
                }
            }
           
            // Sum all the winnings and put in return dictionary
            SortedDictionary<int, decimal> dict = new SortedDictionary<int, decimal>();
            foreach (int playerID in winDict.Keys)
            {
                if (winDict[playerID].Count == 0) dict.Add(playerID, 0);
                dict.Add(playerID, winDict[playerID].Average(x => x.Value.Sum()));
            }

            List<KeyValuePair<int, decimal>> data = new List<KeyValuePair<int, decimal>>();
            data.AddRange(dict);
            data.Sort((x, y) => y.Value.CompareTo(x.Value));
            return data.Select(x => new KeyValuePair<int, string>(x.Key, x.Value.Currency())).ToList();
        }
    }
}
