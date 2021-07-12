using System.Collections.Generic;
using System.Data.SqlClient;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Group
    {
        public Group()
        {
            PlayerList = new List<PlayerOuting>();
        }

        public int GroupID { get; set; }
        public int OutingID { get; set; }
        public List<PlayerOuting> PlayerList { get; set; }

    }
}
