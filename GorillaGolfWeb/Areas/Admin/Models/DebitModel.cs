using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace GorillaGolfWeb.Areas.Admin.Models
{
    public class DebitModel
    {
        public enum WithdrawalTypes
        {
            POOL,
            PLAYER
        };

        public string PlayerName { get; set; }
        public WithdrawalTypes WithdrawalType { get; set; }

        [Range(1, 100)]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public int PlayerID { get; set; }
        public int CreditingPlayerID { get; set; }
        public List<SelectListItem> CreditingPlayerList { get; set; }
    }
}