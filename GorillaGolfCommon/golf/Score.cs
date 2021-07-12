using System.Data.SqlClient;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Score
    {
        public int ScoreID { get; set; }
        public int PlayerID { get; set; }
        public int OutingID { get; set; }
        public int HoleNumber { get; set; }
        public int Strokes { get; set; }
        public decimal? HIndex { get; set; }
        public int Hics { get; set; }
        public bool KP { get; set; }
        public bool Skin { get; set; }
        public bool FlipSkin { get; set; }
        public bool SkinCarryOvers { get; set; }
    }
    
}
