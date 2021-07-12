using System.Web;
using GorillaGolfCommon.golf;

namespace GorillaGolfCommon.core
{
    public class SessionVariables
    {

        public static int? PlayerID
        {
            get => Get<int?>("PlayerID");
            set => Set("PlayerID", value);
        }

        public static string PlayerName
        {
            get => Get<string>("PlayerName");
            set => Set("PlayerName", value);
        }

        public static bool IsAdmin
        {
            get => Get<bool>("IsAdmin");
            set => Set("IsAdmin", value);
        }

        public static int? PlayerSummaryID
        {
            get => Get<int?>("PlayerSummaryID");
            set => Set("PlayerSummaryID", value);
        }

        public static int? SeasonID
        {
            get => Get<int?>("SeasonID");
            set => Set("SeasonID", value);
        }

        private static T Get<T>(string key)
        {
            if (HttpContext.Current != null && HttpContext.Current.Session != null)
            {
                try
                {
                    return (T)HttpContext.Current.Session[key];
                }
                catch
                {
                    return default(T);
                }
            }
            return default(T);
        }

        private static void Set(string key, object o)
        {
            if (HttpContext.Current != null && HttpContext.Current.Session != null)
                HttpContext.Current.Session[key] = o;
        }
    }
}
