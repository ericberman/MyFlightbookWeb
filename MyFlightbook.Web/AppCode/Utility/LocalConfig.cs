﻿using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2008-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// A class to get config values from the database rather than a config file.  Useful for local settings that are not shared 
    /// in source control, for example (e.g., anything that contains a password or secret key)
    /// </summary>
    public static class LocalConfig
    {
        static readonly private Dictionary<string, string> dictConfig = new Dictionary<string, string>();

        public static string SettingForKey(string szKey)
        {
            lock (dictConfig)
            {
                if (dictConfig.Count == 0 || !dictConfig.ContainsKey(szKey))
                {
                    dictConfig.Clear();
                    DBHelper dbh = new DBHelper("SELECT * FROM localconfig");
                    dbh.ReadRows(
                        (comm) => { },
                        (dr) => { dictConfig[(string)dr["keyName"]] = (string)dr["keyValue"]; });
                }

                return dictConfig[szKey];
            }
        }
    }
}