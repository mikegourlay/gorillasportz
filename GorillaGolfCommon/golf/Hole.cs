using System.Data.SqlClient;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Hole
    {
        public int HoleID { get; set; }
        public int CourseID { get; set; }
        public int HoleNumber { get; set; }
        public int Par { get; set; }
        public int? Handicap { get; set; }

        public static Hole GetHole(int holeID)
        {
            const string sql = @"select HoleID, CourseID, HoleNumber, Par, Handicap
                from Hole
                where HoleID = @holeid";
            Hole hole = null;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn, "@holeid", holeID.ToString()))
            {
                if (rdr.Read())
                {
                    hole = new Hole
                    {
                        HoleID = rdr.GetInt32(0),
                        CourseID = rdr.GetInt32(1),
                        HoleNumber = rdr.GetInt32(2),
                        Par = rdr.GetInt32(3),
                        Handicap = rdr.IsDBNull(4) ? (int?)null : rdr.GetInt32(4)
                    };
                }
            }
            return hole;
        }

        public static int AddHole(Hole hole)
        {
            const string sql = @"
                insert into Hole
                (CourseID, HoleNumber, Par, Handicap)
                values (@courseid, @holenumber, @par, @handicap)
                select convert(int, @@IDENTITY)
            ";
            int holeID;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                holeID = (int)DB.ExecScalar(sql, conn,
                    "@courseID", hole.CourseID.ToString(),
                    "@holenumber", hole.HoleNumber.ToString(),
                    "@par", hole.Par.ToString(),
                    "@handicap", hole.Handicap?.ToString()
                );
            }

            hole.HoleID = holeID;
            return holeID;
        }

        public static void DeleteHole(int holeID)
        {
            const string sql = "delete from Hole where HoleID = @holeid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(sql, conn, "@holeid", holeID.ToString());
            }
        }
    }
}
