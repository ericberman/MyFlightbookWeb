using MyFlightbook.FlightStatistics;
using MyFlightbook.Histogram;
using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

/******************************************************
 * 
 * Copyright (c) 2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Web.Admin
{
    public class TotalUserStats
    {
        public int Users { get; private set; }
        public int EmailSubscriptions { get; private set; }
        public int PropertyBlacklists { get; private set; }
        public int DropboxUsers { get; private set; }
        public int GDriveUsers { get; private set; }
        public int OneDriveUsers { get; private set; }
        public int CloudStorageUsers { get; private set; }
        public int DefaultedCloudUsers { get; private set; }
        public int UsersMonthToDate { get; private set; }

        public void Refresh()
        {
            DBHelper dbh = new DBHelper(@"SELECT COUNT(username) AS 'Users', 
	SUM(emailsubscriptions<> 0) AS Subscriptions, 
    SUM(propertyblacklist <> '') AS blocklistcount,
    SUM(DropboxAccesstoken <> '') AS dropboxusers,
    SUM(GoogleDriveAccessToken <> '') AS googleusers,
    SUM(OnedriveaccessToken <> '') AS oneDriveUsers,
    SUM(IF(DropboxAccesstoken <> '', 1, 0) + IF(GoogleDriveAccessToken<>'', 1, 0) + IF(OneDriveAccessToken <> '', 1, 0) > 1) as multicloudusers,
    SUM(DefaultCloudDriveID <> 0) AS multiusers,
    SUM(month(creationdate)=month(now()) AND year(creationdate)=year(now())) AS 'New Users this month' 
FROM users");
            dbh.ReadRow((comm) => { },
                (dr) =>
                {
                    Users = Convert.ToInt32(dr["Users"], CultureInfo.InvariantCulture);
                    EmailSubscriptions = Convert.ToInt32(dr["Subscriptions"], CultureInfo.InvariantCulture);
                    PropertyBlacklists = Convert.ToInt32(dr["blocklistcount"], CultureInfo.InvariantCulture);
                    DropboxUsers = Convert.ToInt32(dr["dropboxusers"], CultureInfo.InvariantCulture);
                    GDriveUsers = Convert.ToInt32(dr["googleusers"], CultureInfo.InvariantCulture);
                    OneDriveUsers = Convert.ToInt32(dr["oneDriveUsers"], CultureInfo.InvariantCulture);
                    CloudStorageUsers = Convert.ToInt32(dr["multicloudusers"], CultureInfo.InvariantCulture);
                    DefaultedCloudUsers = Convert.ToInt32(dr["multiusers"], CultureInfo.InvariantCulture);
                    UsersMonthToDate = Convert.ToInt32(dr["New Users this month"], CultureInfo.InvariantCulture);
                });
        }
    }

    public class OAuthStats
    {
        public int NonceCount { get; private set; }
        public int OAuthAccounts { get; private set; }
        public int PasswordResets { get; private set; }

        public void Refresh()
        {
            DBHelper dbh = new DBHelper(@"SELECT 
            (SELECT count(*) FROM nonce) AS NonceCount,
            (SELECT count(*) FROM oauthclientauthorization) AS 'oAuthClient Auths',
            (SELECT count(*) FROM passwordresetrequests) AS PasswordResetCount;");
            dbh.ReadRow((comm) => { },
                (dr) =>
                {
                    NonceCount = Convert.ToInt32(dr["NonceCount"], CultureInfo.InvariantCulture);
                    OAuthAccounts = Convert.ToInt32(dr["oAuthClient Auths"], CultureInfo.InvariantCulture);
                    PasswordResets = Convert.ToInt32(dr["PasswordResetCount"], CultureInfo.InvariantCulture);
                });
        }
    }

    public class MiscStats
    {
        public int Students { get; private set; }
        public int PublicNotes { get; private set; }
        public int PrivateNotes { get; private set; }
        public int EmbeddedVideos { get; private set; }
        public int AWSVideos { get; private set; }
        public int Clubs { get; private set; }

        public void Refresh()
        {
            DBHelper dbh = new DBHelper(@"SELECT
                        (SELECT COUNT(*) FROM students) AS Students, 
                        (SELECT count(*) from aircraft where publicnotes <> '') AS publicnotescount,
                        (SELECT count(*) from useraircraft where privatenotes <> '') AS privatenotescount,
                        (SELECT count(*) from flightvideos) AS flightVideoCount,
                        (SELECT count(*) from images where virtpathid=0 and imagetype=3) AS AWSVideoCount,
                        (SELECT count(*) from clubs) AS clubcount");
            dbh.ReadRow((comm) => { },
                (dr) =>
                {
                    Students = Convert.ToInt32(dr["Students"], CultureInfo.InvariantCulture);
                    PublicNotes = Convert.ToInt32(dr["publicnotescount"], CultureInfo.InvariantCulture);
                    PrivateNotes = Convert.ToInt32(dr["privatenotescount"], CultureInfo.InvariantCulture);
                    EmbeddedVideos = Convert.ToInt32(dr["flightVideoCount"], CultureInfo.InvariantCulture);
                    AWSVideos = Convert.ToInt32(dr["AWSVideoCount"], CultureInfo.InvariantCulture);
                    Clubs = Convert.ToInt32(dr["clubcount"], CultureInfo.InvariantCulture);
                });
        }
    }

    public class AppSourceStats
    {
        public int NumUsers { get; private set; }
        public string SourceKey { get; private set; }

        public static IEnumerable<AppSourceStats> Refresh()
        {
            List<AppSourceStats> lst = new List<AppSourceStats>();
            DBHelper dbh = new DBHelper(@"SELECT 
    COUNT(eventID) AS 'Users',
    RIGHT(description,
        LENGTH(description) - LOCATE(' - ', description) - 2) AS Source
FROM
    wsevents
WHERE
    eventType = 6
        AND description LIKE '% - %'
GROUP BY Source
ORDER BY Users DESC");
            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new AppSourceStats() { NumUsers = Convert.ToInt32(dr["Users"], CultureInfo.InvariantCulture), SourceKey = dr["source"].ToString() }); });
            return lst;
        }
    }

    public class WebServiceEventStats
    {
        public string EventType { get; private set; }
        public int EventCount { get; private set; }

        public static IEnumerable<WebServiceEventStats> Refresh()
        {
            List<WebServiceEventStats> lst = new List<WebServiceEventStats>();
            DBHelper dbh = new DBHelper(@"SELECT 
    ELT(eventtype,
            'AuthUser',
            'GetAircraft',
            'FlightsByDate',
            'CommitFlightDEPRECATED',
            'CreateAircraft',
            'CreateUser',
            'CreateUserAttemptDEPRECATED',
            'CreateUserError',
            'ExpiredToken') AS 'Event Type',
    COUNT(*) AS 'Number of Events'
FROM
    wsevents
GROUP BY eventtype;");
            dbh.ReadRows((comm) => { },
                (dr) => { lst.Add(new WebServiceEventStats() { EventCount = Convert.ToInt32(dr["Number of Events"], CultureInfo.InvariantCulture), EventType = (string)dr["Event type"] }); });

            return lst;
        }
    }

    public class CommunityLogbookStats
    {
        public int FlightCount { get; private set; }
        public int TelemetryCount { get; private set; }
        public int ModelsCount { get; private set; }
        public int UserAirportCount { get; private set; }
        public int WSCommittedFlights { get; private set; }
        public int ImportedFlights { get; private set; }

        public void Refresh()
        {
            FlightStats fs = FlightStats.GetFlightStats();  // try to get cached flight count for speed - that's actually kinda slow
            if (fs != null)
            {
                FlightCount = fs.NumFlightsTotal;
            }

            DBHelper dbh = new DBHelper(@"SELECT
   (SELECT COUNT(*) from flighttelemetry) AS 'Telemetry Count',
   (SELECT COUNT(*) FROM models) AS 'Models',
   (SELECT COUNT(*) FROM airports where sourceusername <> '') AS UserAirports,
   (SELECT WSCommittedFlights FROM eventcounts WHERE id=1) AS 'WS Committed Flights',
   (SELECT ImportedFlights FROM eventcounts WHERE id=1) AS 'Imported Flights'");

            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    TelemetryCount = Convert.ToInt32(dr["Telemetry Count"], CultureInfo.InvariantCulture);
                    ModelsCount = Convert.ToInt32(dr["Models"], CultureInfo.InvariantCulture);
                    UserAirportCount = Convert.ToInt32(dr["UserAirports"], CultureInfo.InvariantCulture);
                    WSCommittedFlights = Convert.ToInt32(dr["WS Committed Flights"], CultureInfo.InvariantCulture);
                    ImportedFlights = Convert.ToInt32(dr["Imported Flights"], CultureInfo.InvariantCulture);
                });
        }
    }

    /// <summary>
    /// Flights by month-year; can be slow to compute
    /// </summary>
    public class FlightsByDateStats : SimpleDateHistogrammable
    {
        public static IEnumerable<FlightsByDateStats> Refresh()
        {
            List<FlightsByDateStats> lst = new List<FlightsByDateStats>();
            DBHelper dbh = new DBHelper(@"SELECT 
    COUNT(*) AS numflights,
    MONTH(f.date) AS fMonth,
    YEAR(f.date) AS fYear
FROM
    flights f
GROUP BY fYear, fMonth
ORDER BY fYear ASC, fMonth ASC");
            dbh.CommandArgs.Timeout = 300;  // use a long timeout
            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    DateTime dt = new DateTime(Convert.ToInt32(dr["fYear"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["fMonth"], CultureInfo.InvariantCulture), 1);
                    lst.Add(new FlightsByDateStats() { Date = dt, Count = Convert.ToInt32(dr["numFlights"], CultureInfo.InvariantCulture) });
                });

            return lst;
        }
    }

    public class UserActivityStats : SimpleDateHistogrammable
    {
        public static IEnumerable<UserActivityStats> Refresh()
        {
            List<UserActivityStats> lst = new List<UserActivityStats>();
            DBHelper dbh = new DBHelper(@"SELECT 
    YEAR(LastActivityDate) AS activityyear,
    MONTH(lastactivitydate) AS activitymonth,
    COUNT(DISTINCT (PKID)) AS UsersWithSessions
FROM
    users
GROUP BY activityyear , activitymonth
ORDER BY activityyear ASC , activitymonth ASC");
            dbh.ReadRows((comm) => { },
                (dr) =>
                {
                    DateTime dt = new DateTime(Convert.ToInt32(dr["activityyear"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["activitymonth"], CultureInfo.InvariantCulture), 1);
                    lst.Add(new UserActivityStats() { Date = dt, Count = Convert.ToInt32(dr["UsersWithSessions"], CultureInfo.InvariantCulture) });
                });

            return lst;
        }
    }

    public class FlightsPerUserStats : SimpleCountHistogrammable
    {
        public static IEnumerable<FlightsPerUserStats> Refresh(DateTime? creationDate)
        {
            List<FlightsPerUserStats> lstFlightsPerUser = new List<FlightsPerUserStats>();

            // For performance, MUCH faster to find the users without flights first, then find those with flights (use an inner join rather than the overall left join).
            DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT 
    COUNT(u.username) AS numusers, 0 AS numflights
FROM
    users u
        LEFT JOIN
    flights f ON u.username = f.username
WHERE {0}
    f.username IS NULL;", creationDate == null ? string.Empty : "u.creationdate > ?creationDate AND"));

            dbh.CommandArgs.Timeout = 300;  // use a long timeout
            dbh.ReadRow((comm) =>
            {
                if (creationDate.HasValue)
                    comm.Parameters.AddWithValue("creationDate", creationDate.Value);
            },
                (dr) => { lstFlightsPerUser.Add(new FlightsPerUserStats() { Count = Convert.ToInt32(dr["numusers"], CultureInfo.InvariantCulture), Range = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture) }); });

            // Now get the counts for those WITH flights using an inner join
            dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, @"SELECT 
    COUNT(f2.usercount) AS numusers, f2.numflights
FROM
    (SELECT 
        u.username AS usercount,
            FLOOR((COUNT(f.idflight) + 99) / 100) * 100 AS numflights
    FROM
        users u
    INNER JOIN flights f ON u.username = f.username
    {0}
    GROUP BY u.username
    ORDER BY numflights ASC) f2
GROUP BY f2.numflights
ORDER BY f2.numflights ASC;", creationDate == null ? string.Empty : "WHERE u.creationdate > ?creationDate"));
            dbh.CommandArgs.Timeout = 300;  // use a long timeout
            dbh.ReadRows((comm) =>
            {
                if (creationDate.HasValue)
                    comm.Parameters.AddWithValue("creationDate", creationDate.Value);
            },
                (dr) => { lstFlightsPerUser.Add(new FlightsPerUserStats() { Count = Convert.ToInt32(dr["numusers"], CultureInfo.InvariantCulture), Range = Convert.ToInt32(dr["numflights"], CultureInfo.InvariantCulture) }); });

            return lstFlightsPerUser;
        }
    }

    public class AdminStats
    {
        #region properties
        public TotalUserStats UserStats { get; private set; } = new TotalUserStats();
        public OAuthStats OAuth { get; private set; } = new OAuthStats();
        public MiscStats Misc { get; private set; } = new MiscStats();

        public IEnumerable<AppSourceStats> AppSources { get; private set; } = Array.Empty<AppSourceStats>();

        public IEnumerable<WebServiceEventStats> WSEvents { get; private set; } = Array.Empty<WebServiceEventStats>();

        public IEnumerable<UserTransactionSummary> UserTransactions { get; private set; } = Array.Empty<UserTransactionSummary>();

        public IEnumerable<AmountTransactionSummary> AmountTransactions { get; private set; } = Array.Empty<AmountTransactionSummary>();

        public CommunityLogbookStats LogStats { get; private set; } = new CommunityLogbookStats();

        public IEnumerable<AircraftInstance.AircraftInstanceTypeStat> AircraftInstances { get; private set; } = Array.Empty<AircraftInstance.AircraftInstanceTypeStat>();

        public IEnumerable<NewUserStats> NewUserStatsMonthly { get; private set; } = Array.Empty<NewUserStats>();

        public IEnumerable<NewUserStats> NewUserStatsDaily { get; private set; } = Array.Empty<NewUserStats>();

        public IEnumerable<FlightsByDateStats> FlightsByDate { get; private set; } = Array.Empty<FlightsByDateStats>();

        public IEnumerable<UserActivityStats> UserActivity { get; private set; } = Array.Empty<UserActivityStats>();

        public IEnumerable<FlightsPerUserStats> FlightsPerUser { get; private set; } = Array.Empty<FlightsPerUserStats>();
        #endregion

        public AdminStats() { }

        /// <summary>
        /// Refreshes site stats
        /// </summary>
        /// <param name="fIncludeSlowQueries">True to include flights by month (slowest query)</param>
        public async Task<bool> Refresh(bool fIncludeSlowQueries)
        {
            await Task.WhenAll(
                Task.Run(() => { UserStats.Refresh(); }),
                Task.Run(() => { OAuth.Refresh(); }),
                Task.Run(() => { Misc.Refresh(); }),
                Task.Run(() => { AppSources = AppSourceStats.Refresh(); }),
                Task.Run(() => { WSEvents = WebServiceEventStats.Refresh(); }),
                Task.Run(() => { UserTransactions = UserTransactionSummary.Refresh(); }),
                Task.Run(() => { AmountTransactions = AmountTransactionSummary.Refresh(); }),
                Task.Run(() => { LogStats.Refresh(); }),
                Task.Run(() => { AircraftInstances = AircraftInstance.AdminInstanceTypeCounts(); }),
                Task.Run(() => { NewUserStatsMonthly = ProfileAdmin.ADMINUserStatsByTime(); }),
                Task.Run(() => { NewUserStatsDaily = ProfileAdmin.ADMINDailyNewUsers(); }),
                Task.Run(() => { UserActivity =  UserActivityStats.Refresh(); }),
                Task.Run(() =>
                {
                    if (fIncludeSlowQueries)
                        FlightsPerUser = FlightsPerUserStats.Refresh(null);
                }),
                Task.Run(() =>
                {
                    if (fIncludeSlowQueries)
                        FlightsByDate = FlightsByDateStats.Refresh();
                })
                );
            return true;
        }

    }
}