using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public enum TxTypes
    {
        HIC_DEBIT,
        HIC_CREDIT,
        SKIN_DEBIT,
        SKIN_CREDIT,
        LOW_NET_DEBIT,
        LOW_NET_CREDIT,
        KP_DEBIT,
        KP_CREDIT,
        DEPOSIT,
        WITHDRAWAL
    }

    public class Transaction
    {
        public int TransactionID { get; set; }
        public DateTime TxDateUTC { get; set; }
        public TxTypes TxType { get; set; }
        public string Description { get; set; }
        public int PlayerID { get; set; }
        public int AssociatedPlayerID { get; set; }
        public Player AssociatedPlayer { get; set; }
        public Player Player { get; set; }
        public int OutingID { get; set; }

        [Range(1, 100)]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        public string FormattedAmount => Amount.Currency();

        public static void AddHicTransaction(int outingID, int playerID, int associatedPlayerID, decimal amount)
        {
            Transaction tx = new Transaction
            {
                PlayerID = playerID,
                AssociatedPlayerID = associatedPlayerID,
                OutingID = outingID,
                Amount = amount,
                TxType = (amount > 0) ? TxTypes.HIC_CREDIT : TxTypes.HIC_DEBIT
            };
            AddTransaction(tx);
        }

        public static void AddKPTransaction(int outingID, int playerID, int associatedPlayerID, decimal amount)
        {
            Transaction tx = new Transaction
            {
                PlayerID = playerID,
                AssociatedPlayerID = associatedPlayerID,
                OutingID = outingID,
                Amount = amount,
                TxType = (amount > 0) ? TxTypes.KP_CREDIT : TxTypes.KP_DEBIT
            };
            AddTransaction(tx);
        }

        public static void AddLowNetTransaction(int outingID, int playerID, int associatedPlayerID, decimal amount)
        {
            Transaction tx = new Transaction
            {
                PlayerID = playerID,
                AssociatedPlayerID = associatedPlayerID,
                OutingID = outingID,
                Amount = amount,
                TxType = (amount > 0) ? TxTypes.LOW_NET_CREDIT : TxTypes.LOW_NET_DEBIT
            };
            AddTransaction(tx);
        }

        public static void AddSkinTransaction(int outingID, int playerID, int associatedPlayerID, decimal amount)
        {
            Transaction tx = new Transaction
            {
                PlayerID = playerID,
                AssociatedPlayerID = associatedPlayerID,
                OutingID = outingID,
                Amount = amount,
                TxType = (amount > 0) ? TxTypes.SKIN_CREDIT : TxTypes.SKIN_DEBIT
            };
            AddTransaction(tx);
        }

        public static int AddTransaction(Transaction tx)
        {
            const string sql = @"
                insert into Transact
                (TxDateUTC, TxType, Description, PlayerID, AssociatedPlayerID, Amount, OutingID)
                values(@txdateutc, @txtype, @description, @playerid, @associatedplayerid, @amount, @outingid)
                select convert(int, @@IDENTITY)
            ";
            int txid;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                txid = (int) DB.ExecScalar(sql, conn,
                    "@txdateutc", DB.SqlDateTime(DateTime.UtcNow),
                    "@txtype", tx.TxType.ToString("d"),
                    "@description", tx.Description ?? "",
                    "@playerid", tx.PlayerID.ToString(),
                    "@associatedplayerid", tx.AssociatedPlayerID.ToString(),
                    "@amount", tx.Amount.ToString(),
                    "@outingid", tx.OutingID.ToString());
            }
            tx.TransactionID = txid;
            return txid;
        }

        public static void DeleteTransaction(int txid)
        {
            const string sql = "delete from Transact where TransactionID = @txid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@txid", txid.ToString());
            }
        }

        public static void DeleteTransactionsForOuting(int outingid)
        {
            const string sql = "delete from Transact where OutingID = @outingid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@outingid", outingid.ToString());
            }
        }

        public static List<Transaction> GetPlayerTransactions(int playerID, int? outingid = null)
        {
            string outingfilter = "";
            if (outingid.HasValue)
            {
                outingfilter = " and OutingID = @outingid";
            }

            string sql = $@"
                select TransactionID, TxDateUTC, TxType, Description, t.PlayerID, AssociatedPlayerID, Amount, OutingID,
                p.LastName, p.FirstName
                from Transact t
                left join Player p on p.PlayerID = t.AssociatedPlayerID
                where t.PlayerID = @playerid {outingfilter}
                order by TxDateUTC asc";

            List<Transaction> txList = new List<Transaction>();
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@playerid", playerID.ToString(), "@outingid", outingid?.ToString() ?? ""))
            {
                while (rdr.Read())
                {
                    txList.Add(new Transaction
                    {
                        TransactionID = rdr.GetInt32(0),
                        TxDateUTC = rdr.GetDateTime(1),
                        TxType = (TxTypes)rdr.GetInt32(2),
                        Description = rdr.GetString(3),
                        PlayerID = rdr.GetInt32(4),
                        AssociatedPlayerID = rdr.GetInt32(5),
                        Amount = rdr.GetDecimal(6),
                        OutingID = rdr.GetInt32(7),
                        AssociatedPlayer = (rdr.IsDBNull(8)) ? null : new Player { LastName = rdr.GetString(8), FirstName = rdr.GetString(9) }
                    });
                }
            }
            return txList;
        }

        public static Dictionary<int, List<Transaction>> GetPlayerTransactionDictionary(int playerID)
        { 
            Dictionary<int, List<Transaction>> txDict = new Dictionary<int, List<Transaction>>();
            List<Transaction> txList = GetPlayerTransactions(playerID);

            foreach (Transaction tx in txList)
            {
                if (!txDict.ContainsKey(tx.OutingID)) txDict.Add(tx.OutingID, new List<Transaction>());
                txDict[tx.OutingID].Add(tx);
            }

            return txDict;
        }

        public class PlayerTransactSummary
        {
            public PlayerTransactSummary()
            {
                Skins = new List<Transaction>();
                Hics = new List<Transaction>();
                LowNet = new List<Transaction>();
                KPs = new List<Transaction>();
            }

            public Player Player { get; set; }
            public List<Transaction> Skins { get; set; }
            public List<Transaction> Hics { get; set; }
            public List<Transaction> LowNet { get; set; }
            public List<Transaction> KPs { get; set; }

            public decimal SkinAmount
            {
                get { return Skins.Sum(x => x.Amount); }
            }
            public decimal HicAmount
            {
                get { return Hics.Sum(x => x.Amount); }
            }
            public decimal LowNetAmount
            {
                get { return LowNet.Sum(x => x.Amount); }
            }
            public decimal KPAmount
            {
                get { return KPs.Sum(x => x.Amount); }
            }
           
            public decimal TotalAmount => SkinAmount + HicAmount + LowNetAmount + KPAmount;
        }

        public static Dictionary<int, PlayerTransactSummary> GetTransactionSummary(int outingid)
        {
            Dictionary<int, PlayerTransactSummary> summary = new Dictionary<int, PlayerTransactSummary>();

            string sql = $@"
                select TransactionID, TxDateUTC, TxType, Description, t.PlayerID, AssociatedPlayerID, Amount, OutingID,
                 p2.LastName, p2.FirstName, p1.LastName, p1.FirstName
                from Transact t
                left join Player p1 on p1.PlayerID = t.PlayerID
                left join Player p2 on p2.PlayerID = t.AssociatedPlayerID
                where t.OutingID = @outingid
                order by TxDateUTC asc";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@outingid", outingid.ToString()))
            {
                while (rdr.Read())
                {
                    Transaction tx = new Transaction
                    {
                        TransactionID = rdr.GetInt32(0),
                        TxDateUTC = rdr.GetDateTime(1),
                        TxType = (TxTypes)rdr.GetInt32(2),
                        Description = rdr.GetString(3),
                        PlayerID = rdr.GetInt32(4),
                        AssociatedPlayerID = rdr.GetInt32(5),
                        Amount = rdr.GetDecimal(6),
                        OutingID = rdr.GetInt32(7),
                        AssociatedPlayer = (rdr.IsDBNull(8)) ? null : new Player { LastName = rdr.GetString(8), FirstName = rdr.GetString(9) }
                    };

                    if (!summary.ContainsKey(tx.PlayerID))
                        summary.Add(tx.PlayerID, new PlayerTransactSummary{Player = new Player{LastName = rdr.GetStringOrDefault(10), FirstName = rdr.GetStringOrDefault(11)}});
                    switch (tx.TxType)
                    {
                        case TxTypes.HIC_CREDIT:
                        case TxTypes.HIC_DEBIT:
                            summary[tx.PlayerID].Hics.Add(tx);
                            break;
                        case TxTypes.KP_CREDIT:
                        case TxTypes.KP_DEBIT:
                            summary[tx.PlayerID].KPs.Add(tx);
                            break;
                        case TxTypes.LOW_NET_CREDIT:
                        case TxTypes.LOW_NET_DEBIT:
                            summary[tx.PlayerID].LowNet.Add(tx);
                            break;
                        case TxTypes.SKIN_CREDIT:
                        case TxTypes.SKIN_DEBIT:
                            summary[tx.PlayerID].Skins.Add(tx);
                            break;
                       }
                }
            }

            return summary;
        }

        public static List<Transaction> GetCreditDepositTransactions()
        {
            List <Transaction> txList = new List<Transaction>();

            const string sql = @"select TxDateUTC, TxType, t.PlayerID, Amount, p.LastName, p.FirstName
                from Transact t
                inner join Player p on p.PlayerID = t.PlayerID
                where TxType = 8 or TxType = 9
                order by t.TxDateUTC desc";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn))
            {
                while (rdr.Read())
                {
                    txList.Add(new Transaction
                    {
                        TxDateUTC = rdr.GetDateTime(0),
                        TxType = (TxTypes) rdr.GetInt32(1),
                        PlayerID = rdr.GetInt32(2),
                        Amount = rdr.GetDecimal(3),
                        Player = new Player() {LastName = rdr.GetString(4), FirstName = rdr.GetString(5)}
                    });
                }
            }

            return txList;
        }
    }
}
