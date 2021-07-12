using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GorillaGolfCommon.core;
using GorillaGolfCommon.golf;

namespace GorillaGolfWeb.Areas.Main.Models
{
    public class PlayerSummaryModel
    {
        public int PlayerID { get; set; }
        public int Season { get; set; }
        public List<Outing> Outings { get; set; }
        public Dictionary<int, List<Transaction>> TransactionDictionary { get; set; }
        public decimal CurrentBalance { get; set; }

        public decimal? Winnings(int outingID)
        {
            if (!TransactionDictionary.ContainsKey(outingID)) return null;
            return TransactionDictionary[outingID].Sum(x => x.Amount);
        }

        public string FormattedWinnings(int outingID)
        {
            decimal? winnings = Winnings(outingID);
            if (winnings == null) return "";
            return ((decimal)winnings).Currency();
        }
    }
}
