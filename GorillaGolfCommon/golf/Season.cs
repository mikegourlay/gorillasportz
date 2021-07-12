using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorillaGolfCommon.golf
{
    public class Season
    {
        public const int CurrentSeason = 2021;

        public string SeasonName { get; set; }
        public int SeasonID { get; set; }

        public static List<Season> Seasons = new List<Season>
        {
            new Season
            {
                SeasonName = "2019",
                SeasonID = 2019
            },
            new Season
            {
                SeasonName = "2020", 
                SeasonID = 2020
            },
            new Season
            {
            SeasonName = "2021",
            SeasonID = 2021
        }
        };
    }

   
}
