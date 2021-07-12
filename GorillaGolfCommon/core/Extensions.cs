using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web;

namespace GorillaGolfCommon.core
{
    public static class Extensions
    {
        private static readonly Regex emailregex = (new Regex(@"^[a-zA-Z0-9._%'+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$"));

        public static string Currency(this decimal amt)
        {
            if (amt < 0) return $"-{-amt:C}";
            else return $"{amt:C}";
        }

        public static string IfNullOrEmpty(this string str, string alternateValue)
        {
            if (string.IsNullOrEmpty(str) || str.Trim().Length == 0)
            {
                return alternateValue;
            }
            return str;
        }

        public static bool IsEmpty(this string str)
        {
            return String.IsNullOrEmpty(str);
        }

        public static bool IsNotEmpty(this string str)
        {
            return !String.IsNullOrEmpty(str);
        }

        public static string ConvertHL7EscapeChars(this string text)
        {
            // Need to make sure HL7 escape sequences are converted to 
            // their proper character...
            //
            //  \F\ Vertical Bar
            //  \S\ Caret
            //  \R\ Tilde
            //  \T\ Ampersand
            //  \E\ Backslash

            return
                text.Replace("\\F\\", "|")
                    .Replace("\\S\\", "^")
                    .Replace("\\R\\", "~")
                    .Replace("\\T\\", "&")
                    .Replace("\\E\\", "\\");
        }

        
        public static string NewLineEncode(this string text)
        {
            if (String.IsNullOrEmpty(text)) return text;

            // Need to deal with cr and lf.  
            // Replace \r\n with <br>
            // and Replace \n with <br>
            return text.Replace("\r\n", "<br>").Replace("\n", "<br>");
        }

        public static string HtmlEnc(this string text)
        {
            if (text == null) return null;

            // Html encode the string
            string ret = HttpUtility.HtmlEncode(text);

            // Need to deal with cr and lf.  
            // Replace \r\n with <br>
            ret = ret.Replace("\r\n", "<br>");
            // Replace \n with <br>
            ret = ret.Replace("\n", "<br>");

            // Replace single quotes
            ret = ret.Replace("'", "&#39;");
            return ret;
        }

        public static string UrlEnc(this string text)
        {
            return HttpUtility.UrlEncode(text);
        }

        public static string Truncate(this string text, int length)
        {
            if (text == null) return "";
            if (text.Length > length)
            {
                return text.Substring(0, length);
            }
            return text;
        }

        public static string TruncateWithEllipsis(this string text, int length)
        {
            if (text == null) return "";
            if (text.Length > length)
            {
                return text.Substring(0, length) + "...";
            }
            return text;
        }

        public static string HighlightSubText(this string text, string subtext)
        {
            return text.HighlightSubText(subtext, "#f8db42");
        }

        public static string HighlightSubText(this string text, string subtext, string color)
        {
            if (String.IsNullOrEmpty(subtext)) return text;
            string startspan = String.Format("<span style='background-color:{0}'>", color);
            const string endspan = "</span>";
            int index = text.IndexOf(subtext, StringComparison.OrdinalIgnoreCase);
            while (index >= 0)
            {
                text = text.Insert(index, startspan);
                text = text.Insert(index + startspan.Length + subtext.Length, endspan);
                index += startspan.Length + subtext.Length + endspan.Length;
                index = text.IndexOf(subtext, index, StringComparison.OrdinalIgnoreCase);
            }
            return text;
        }

        
        public static bool IsValidEmail(this string email)
        {
            return emailregex.IsMatch(email); 
        }

        public static DateTime ConvertUTCToTZ(this DateTime date, string tz)
        {
            TimeZoneInfo tzInfo;
            switch (tz)
            {
                case "ET":
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(date, tzInfo);
                case "CT":
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(date, tzInfo);
                case "MT":
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(date, tzInfo);
                case "PT":
                    tzInfo = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(date, tzInfo);
                default:
                    return date;
            }
        }

        
        public static string FormatDate(this DateTime date)
        {
            return date.ToString("M/d/yy");
            //return date.ToShortDateString();
        }

        public static string FormatDateTime(this DateTime date)
        {
            return date.ToString("M/d/yy h:mm tt");
            //return date.ToShortDateString() + " " + date.ToShortTimeString();
        }

        public static string GetStringOrDefault(this SqlDataReader sqlDataReader, int columnIndex, string defaultValue)
        {
            return sqlDataReader.IsDBNull(columnIndex) ? defaultValue : sqlDataReader.GetString(columnIndex);
        }

        public static string GetStringOrDefault(this SqlDataReader sqlDataReader, int columnIndex)
        {
            return GetStringOrDefault(sqlDataReader, columnIndex, string.Empty);
        }
    }
}
