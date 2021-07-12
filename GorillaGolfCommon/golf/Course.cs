using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using GorillaGolfCommon.core;

namespace GorillaGolfCommon.golf
{
    public class Course
    {
        public Course()
        {
            HoleList = new List<Hole>();
        }

        public int CourseID { get; set; }
        public string Name { get; set; }
        public int Slope { get; set; }
        public decimal Rating { get; set; }
        public List<Hole> HoleList { get; set; }

        public IEnumerable<Hole> ActiveHoles
        {
            get { return HoleList.Where(x => (x.Par != 0)); }
        }

        public static Course GetCourse(int courseID)
        {
            const string coursesql = @"select c.CourseID, Name, Slope, Rating
                from Course c
                where c.CourseID = @courseid";

            const string holesql = @"select HoleID, HoleNumber, Par, Handicap
            from Hole
            where CourseID = @courseid";

            Course course = null;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                using (SqlDataReader rdr = DB.ExecSQLQuery(coursesql, conn, "@courseid", courseID.ToString()))
                {
                    if (rdr.Read())
                    {
                        // Get common course fields
                        course = new Course
                        {
                            CourseID = rdr.GetInt32(0),
                            Name = rdr.GetString(1),
                            Slope = rdr.GetInt32(2),
                            Rating = rdr.GetDecimal(3),
                            HoleList = new List<Hole>()
                        };
                    }
                }
                if (course == null) return null;

                // Add hole info
                using (SqlDataReader holerdr = DB.ExecSQLQuery(holesql, conn, "@courseid", courseID.ToString()))
                {
                    while (holerdr.Read())
                    {
                        course.HoleList.Add(new Hole
                        {
                            CourseID = course.CourseID,
                            HoleID = holerdr.GetInt32(0),
                            HoleNumber = holerdr.GetInt32(1),
                            Par = holerdr.GetInt32(2),
                            Handicap = holerdr.IsDBNull(3) ? (int?)null : holerdr.GetInt32(3)
                        });
                    }
                }
            }

            return course;
        }

        public static int AddCourse(Course course)
        {
            const string coursesql = @"
                insert into Course
                (Name, Slope, Rating)
                values (@name, @slope, @rating)
                select convert(int, @@IDENTITY)
            ";

            const string holesql = @"
                insert into Hole
                (CourseID, HoleNumber, Par, Handicap)
                values (@courseid, @holenumber, @par, @handicap)
            ";

            int courseID;
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                courseID = (int) DB.ExecScalar(coursesql, conn,
                    "@name", course.Name,
                    "@slope", course.Slope.ToString(),
                    "@rating", course.Rating.ToString()
                );

                foreach (var hole in course.HoleList)
                {
                    DB.ExecSQL(holesql, conn, "@courseid", courseID.ToString(),
                        "@holenumber", hole.HoleNumber.ToString(),
                        "@par", hole.Par.ToString(),
                        "@handicap", hole.Handicap?.ToString());
                }
            }

            course.CourseID = courseID;
            return courseID;
        }

        public static void UpdateCourse(Course course)
        {
            const string coursesql = @"update Course
                set Name=@name, Slope=@slope, Rating=@rating
                where CourseID=@courseid";

            const string holesql = @"
                update Hole
                set Par=@par, Handicap = @handicap
                where CourseID = @courseid and HoleNumber = @holenumber
            ";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(coursesql, conn,
                    "@courseid", course.CourseID.ToString(),
                    "@name", course.Name,
                    "@slope", course.Slope.ToString(),
                    "@rating", course.Rating.ToString()
                    );

                foreach (var hole in course.HoleList)
                {
                    DB.ExecSQL(holesql, conn,
                        "@par", hole.Par.ToString(),
                        "@courseid", course.CourseID.ToString(),
                        "@holenumber", hole.HoleNumber.ToString(),
                        "@handicap", hole.Handicap?.ToString());
                }
            }
        }

        public static bool CanCourseBeRemoved(int courseID)
        {
            // See if there are any Outings that reference this course
            const string sql = @"
                select count(*) from Outing
                where CourseID  = @courseid";

            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                return (int)DB.ExecScalar(sql, conn, "@courseid", courseID.ToString()) == 0;
            }
        }

        public static void DeleteCourse(int courseID)
        {
            const string coursesql = "delete from Course where CourseID = @courseid";
            const string holesql = "delete from Hole where CourseID = @courseid";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            {
                DB.ExecSQL(coursesql, conn, "@courseid", courseID.ToString());
                DB.ExecSQL(holesql, conn, "@courseid", courseID.ToString());
            }
        }

        public static List<Course> GetCourses()
        {
            List<Course> courseList = new List<Course>();
            const string sql = @"select CourseID, Name, Slope, Rating
                from Course
                order by Name asc";
            using (SqlConnection conn = DB.GetConnection(Settings.GetDSN()))
            using (SqlDataReader rdr = DB.ExecSQLQuery(sql, conn))
            {
                while (rdr.Read())
                {
                    Course course = new Course
                    {
                        CourseID = rdr.GetInt32(0),
                        Name = rdr.GetString(1),
                        Slope = rdr.GetInt32(2),
                        Rating = rdr.GetDecimal(3)
                    };
                    courseList.Add(course);
                }
            }
            return courseList;
        }
    }

    
}
