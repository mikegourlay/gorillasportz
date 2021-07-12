using System.Collections.Generic;
using GorillaGolfCommon.golf;

namespace GorillaGolfWeb.Areas.Admin.Models
{
    public class OutingListModel
    {
        public List<Outing> OutingList { get; set; }
        public int Season { get; set; }
    }
}