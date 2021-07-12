using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GorillaGolfCommon.golf
{
    public class PlayerOuting
    {
        public Player Player { get; set; }
        public List<Score> Scores { get; set; }
        public int Season { get; set; }

       public int? GrossScore
        {
            get
            {
                // If not all holes have been scored, return null
                if (Scores.Any(x => x.Strokes == 0)) return null;
                return Scores.Sum(x => x.Strokes);
            }
        }

        public int? NetScore(Course course)
        {
                int? ch = CourseHandicap(course);
                if (!ch.HasValue) return null;
                return GrossScore - (int) ch;
        }

        public decimal? CourseHIndex(Course course)
        {
            // Get get the player's HIndex at the time of the outing.
            // This is stored in the scores.
            Score s = Scores.FirstOrDefault(x => x.HIndex.HasValue);
            return s?.HIndex;
        }

        public int? CourseHandicap(Course course)
        {
            return (Season >= 2020) 
                ? CourseHandicapPost2020(course)
                : CourseHandicapPre2020(course);
        }

        public int? CourseHandicapPre2020(Course course)
        {
            // Get the HIndex stored in the scores and not from the Player.
            // This is the HIndex of the player at the time of the scoring and
            // not the current HIndex of the player.
            Score s = Scores.FirstOrDefault(x => x.HIndex.HasValue);
            if (s?.HIndex == null || course.Slope == 0) return null;
            decimal hindex = (decimal) s.HIndex;
            int holecount = course.ActiveHoles.Count();
            if (holecount != 9 && holecount != 18) return null;
            int divisor = (course.ActiveHoles.Count() == 9) ? 2 : 1;
            return Convert.ToInt32(Math.Round(hindex/divisor * course.Slope / 113, MidpointRounding.AwayFromZero));
        }

        public int? CourseHandicapPost2020(Course course)
        {
            // Get the HIndex stored in the scores and not from the Player.
            // This is the HIndex of the player at the time of the scoring and
            // not the current HIndex of the player.
            Score s = Scores.FirstOrDefault(x => x.HIndex.HasValue);
            if (s?.HIndex == null || course.Slope == 0 || course.Rating == 0) return null;
            decimal hindex = (decimal) s.HIndex;
            
            int holecount = course.ActiveHoles.Count();
            if (holecount != 9 && holecount != 18) return null;

            int par = course.ActiveHoles.Sum(x => x.Par);
            int divisor = (course.ActiveHoles.Count() == 9) ? 2 : 1;
            return Convert.ToInt32(Math.Round(((hindex/divisor * course.Slope / 113) + (course.Rating - par)), MidpointRounding.AwayFromZero));
        }

        public int Hics
        {
            get { return Scores.Sum(x => x.Hics); }
        }

        public int Skins
        {
            get { return Scores.Count(x => x.Skin); }
        }

        public int FlipSkins
        {
            get { return Scores.Count(x => x.FlipSkin); }
        }

        public int KPs
        {
            get { return Scores.Count(x => x.KP); }
        }

        public PlayerOuting()
        {
            Scores = new List<Score>();
        }
    }
}
