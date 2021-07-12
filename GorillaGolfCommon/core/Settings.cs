using System;
using System.Collections.Generic;
using System.Collections;
using System.Configuration;
using System.Resources;

namespace GorillaGolfCommon.core
{
    public class Settings
    {
        //-- This class implements a singleton; all functions are static.

        private static Dictionary<string, string> settings;

        public static void Load(string settingsFile)
        {
            settings = new Dictionary<string, string>();
            try
            {
                ResXResourceReader resrdr = new ResXResourceReader(settingsFile);
                foreach (DictionaryEntry entry in resrdr)
                {
                    settings[(string)entry.Key] = (string)entry.Value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not open settings file: " + settingsFile, ex);
            }

        }

        public static string Get(string key)
        {
            if (settings != null)
            {
                return settings.ContainsKey(key) ? settings[key] : "";
            }
            return ConfigurationManager.AppSettings[key] ?? "";
        }

        public static string GetDSN()
        {
            string[] dsnList = GetDSNList();
            if (dsnList.Length == 0)
            {
                throw new Exception("dbList not found in settings");
            }

            string dsn = dsnList[0];
            if (String.IsNullOrEmpty(dsn))
            {
                throw new Exception("DSN not found");
            }
            return dsn;
        }

        public static string GetDSN(int dbnum)
        {
            string[] dsnList = GetDSNList();
            if (dbnum >= dsnList.Length)
            {
                throw new Exception(String.Format("dbList not found or not enough entries, list={0}, dbnum={1}",
                    Get("dbList"), dbnum));
            }
            string dsn = dsnList[dbnum];
            if (String.IsNullOrEmpty(dsn))
            {
                throw new Exception(String.Format("DSN not found:, db #{0}", dbnum));
            }
            return dsn;
        }

        public static string[] GetDSNList()
        {
            string dbListStr = Get("dbList");
            string[] dblist = dbListStr.Split(',');
            for (int i = 0; i < dblist.Length; i++)
            {
                dblist[i] = Get(dblist[i]);
            }
            return dblist;
        }

        public static string[] GetDSNNames()
        {
            string dbListStr = Get("dbList");
            string[] dblist = dbListStr.Split(',');
            return dblist;
        }

        public static string GetAppURI()
        {
            return Get("AppURI");
        }

        public static string GetAppHost()
        {
            return Get("AppHost");
        }

        public static bool GetAllowUnsecure()
        {
            string allowUnsecure = Get("allowUnsecure");
            return (allowUnsecure != null && allowUnsecure.ToLower() == "true");
        }

        public static string GetErrorEmailList()
        {
            return Get("errorEmail");
        }

        public static string GetPDFConverterLicenseKey()
        {
            return Get("PDFConverterKey");
        }

    }
}
