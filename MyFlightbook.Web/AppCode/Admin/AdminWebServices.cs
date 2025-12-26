using MyFlightbook.Airports;
using MyFlightbook.Geography;
using MyFlightbook.Telemetry;
using Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.Script.Services;
using System.Web.Security;
using System.Web.Services;

/******************************************************
 * 
 * Copyright (c) 2022-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Ajax
{
    /// <summary>
    /// Provides AUTHENTICATED AJAX support for the Website's ADMIN tools.  NOT FOR EXTERNAL CONSUMPTION!!!  These APIs may change at any point.
    /// </summary>
    [WebService(Namespace = "http://myflightbook.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ServiceContract]
    [ScriptService]
    [System.ComponentModel.ToolboxItem(false)]
    public class AdminWebServices : System.Web.Services.WebService
    {
        public static string AjaxScriptLink
        {
            get { return "~/Public/Scripts/adminajax.js?v=6".ToAbsolute(); }
        }

        #region Aircraft WebMethods

        #endregion

        #region UserManagement WebMethods 
        [WebMethod(EnableSession = true)]
        public void UnlockUser(string szUser)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            ProfileAdmin.UnlockUser(szUser);
        }

        /// <summary>
        /// Resets the password for a user AND sends the email.  (no longer a separate button)
        /// </summary>
        /// <param name="szPKID"></param>
        /// <exception cref="UnauthorizedAccessException"></exception>
        [WebMethod(EnableSession = true)]
        public void ResetPasswordForUser(string szPKID)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null
            // Need to get the user offline
            mu = ProfileAdmin.ADMINUserFromName(mu.UserName);

            string szPass = mu.ResetPassword();

            if (String.IsNullOrEmpty(szPass))
                throw new InvalidOperationException("Failure resetting password for user " + mu.UserName);
            Profile.UncacheUser(mu.UserName);

            string szEmail = util.ApplyHtmlEmailTemplate(Resources.EmailTemplates.ChangePassEmail.Replace("<% Password %>", HttpUtility.HtmlEncode(szPass)), true);
            Profile pf = Profile.GetUser(mu.UserName);
            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ResetPasswordEmailSubject, Branding.CurrentBrand.AppName), szEmail, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, true);
        }

        [WebMethod(EnableSession = true)]
        public void DeleteFlightsForUser(string szPKID)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null

            if (String.Compare(mu.UserName, User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                throw new InvalidOperationException("Don't delete your own flights here, silly!");

            ProfileAdmin.DeleteForUser(mu, ProfileAdmin.DeleteLevel.OnlyFlights);
        }

        [WebMethod(EnableSession = true)]
        public void DeleteUserAccount(string szPKID)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null

            if (String.Compare(mu.UserName, User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
                throw new InvalidOperationException("Don't delete your own account here, silly!");

            ProfileAdmin.DeleteForUser(mu, ProfileAdmin.DeleteLevel.EntireUser);
        }

        [WebMethod(EnableSession = true)]
        public void Disable2FA(string szPKID)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null

            Profile pf = Profile.GetUser(mu.UserName);
            if (!pf.PreferenceExists(MFBConstants.keyTFASettings))
                throw new InvalidOperationException("2fa was not on for user " + mu.UserName);

            pf.SetPreferenceForKey(MFBConstants.keyTFASettings, null, true);
        }


        [WebMethod(EnableSession = true)]
        public void EndowClubCreation(string szPKID)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null

            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery("REPLACE INTO earnedgratuities SET idgratuitytype=3, username=?szUser, dateEarned=Now(), dateExpired=Date_Add(Now(), interval 30 day), reminderssent=0, dateLastReminder='0001-01-01 00:00:00'", (comm) => { comm.Parameters.AddWithValue("szUser", mu.UserName); });
        }

        [WebMethod(EnableSession = true)]
        public void SendUserMessage(string szPKID, string szSubject, string szBody)
        {
            if (!Profile.GetUser(User.Identity.Name).CanSupport)
                throw new UnauthorizedAccessException();

            MembershipUser mu = ProfileAdmin.UserFromPKID(szPKID);  // will throw an error if szPKID is empty or null

            Profile pf = Profile.GetUser(mu.UserName);
            util.NotifyUser(szSubject, szBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
        }
        #endregion

        #region Telemetry WebMethods
        [WebMethod(EnableSession = true)]
        public string MigrateDBToFiles(int cLimit, string szLimitUser)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return TelemetryReference.MigrateFROMDBToFiles(szLimitUser, cLimit);
        }

        [WebMethod(EnableSession = true)]
        public string MigrateFilesToDB(int cLimit, string szLimitUser)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return TelemetryReference.MigrateFROMFilesToDB(szLimitUser, cLimit);
        }

        [WebMethod(EnableSession = true)]
        private static string EnumerableToHtmlResult<T>(IEnumerable<T> lst)
        {
            if (lst.Any())
            {
                StringBuilder sb = new StringBuilder();
                foreach (T item in lst)
                    sb.AppendFormat(CultureInfo.CurrentCulture, "<div>{0}</div>", item.ToString());
                return sb.ToString();
            }
            else
                return "<p>No issues found!</p>";
        }

        [WebMethod(EnableSession = true)]
        public string FindDupeTelemetry()
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return EnumerableToHtmlResult(TelemetryReference.FindDuplicateTelemetry());
        }

        [WebMethod(EnableSession = true)]
        public string FindOrphanReferences()
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return EnumerableToHtmlResult(TelemetryReference.FindOrphanedRefs());
        }

        [WebMethod(EnableSession = true)]
        public string FindOrphanedFiles()
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            return EnumerableToHtmlResult(TelemetryReference.FindOrphanedFiles());
        }

        [WebMethod(EnableSession = true)]
        public string MigrateFlightToDisk(int idFlight)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, null, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);
            le.Telemetry.Compressed = 0;    // no longer know the compressed 
            le.MoveTelemetryFromFlightEntry();

            return "Flight moved from DB to disk";
        }

        [WebMethod(EnableSession = true)]
        public string MigrateFlightFromDisk(int idFlight)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, null, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);
            le.MoveTelemetryToFlightEntry();
            return "Flight moved from disk back to DB";
        }

        private static string AntipodesSessionKey(string filename)
        {
            return "ANTIPODES-FILE*:" + filename;
        }

        [WebMethod(EnableSession = true)]
        public void UploadAntipodes()
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            if (HttpContext.Current.Request.Files.Count == 0)
                throw new InvalidOperationException("No file uploaded");
            HttpPostedFile pf = HttpContext.Current.Request.Files[0];
            byte[] rgbytes = new byte[pf.ContentLength];
            _ = pf.InputStream.Read(rgbytes, 0, pf.ContentLength);
            pf.InputStream.Close();
            HttpContext.Current.Session[AntipodesSessionKey(HttpContext.Current.Request.Files[0].FileName)] = rgbytes;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public dynamic Antipodes(string fileName)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            string sessionKey = AntipodesSessionKey(fileName);
            byte[] rgbytes = (byte[])HttpContext.Current.Session[sessionKey] ?? throw new InvalidOperationException("Uploaded file not found");
            HttpContext.Current.Session[sessionKey] = null; // clear it out.

            LatLong[] rgStraight = null;
            LatLong[] rgAntipodes = null;

            using (Telemetry.FlightData fdOriginal = new Telemetry.FlightData())
            {
                if (fdOriginal.ParseFlightData(Encoding.UTF8.GetString(rgbytes)))
                {
                    rgStraight = fdOriginal.GetPath();
                    rgAntipodes = new LatLong[rgStraight.Length];
                    for (int i = 0; i < rgStraight.Length; i++)
                        rgAntipodes[i] = rgStraight[i].Antipode;
                }
            }

            if (rgStraight == null || rgAntipodes == null)
                throw new InvalidOperationException("No path found");

            LatLongBox boundingBoxOriginal = new LatLongBox(rgStraight[0]);
            LatLongBox boundingBoxAntipodes = new LatLongBox(rgAntipodes[0]);
            List<double[]> original = new List<double[]>();
            List<double[]> antipodes = new List<double[]>();

            foreach (LatLong ll in rgStraight)
            {
                original.Add(new double[] { ll.Latitude, ll.Longitude });
                boundingBoxOriginal.ExpandToInclude(ll);
            }

            foreach (LatLong ll in rgAntipodes)
            {
                antipodes.Add(new double[] { ll.Latitude, ll.Longitude });
                boundingBoxAntipodes.ExpandToInclude(ll);
            }

            return new { original, antipodes, boundingBoxOriginal, boundingBoxAntipodes };
        }

        [WebMethod(EnableSession = true)]
        public dynamic PathsForFlight(int idFlight)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            TelemetryReference ted = TelemetryReference.LoadForFlight(idFlight);
            if (ted == null)
                return null;
            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.LoadAll, true);

            IEnumerable<LatLong> pathReconstituded = ted.GoogleData.DecodedPath();
            IEnumerable<LatLong> pathStraight = Array.Empty<LatLong>();
            using (Telemetry.FlightData fd = new Telemetry.FlightData())
            {
                if (fd.ParseFlightData(le.FlightData))
                    pathStraight = fd.GetPath();
            }

            LatLongBox llb = new LatLongBox(pathReconstituded.First());
            List<double[]> lstReconstituded = new List<double[]>();
            List<double[]> lstStraight = new List<double[]>();

            foreach (LatLong ll in pathReconstituded)
            {
                lstReconstituded.Add(new double[] { ll.Latitude, ll.Longitude });
                llb.ExpandToInclude(ll);
            }

            foreach (LatLong ll in pathStraight)
            {
                lstStraight.Add(new double[] { ll.Latitude, ll.Longitude });
                llb.ExpandToInclude(ll);
            }

            return new { reconstituded = lstReconstituded, straight = lstStraight, boundingBox = llb };
        }
        #endregion

        #region Airports 
        [WebMethod(EnableSession = true)]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public VisitedRoute VisitedRoutesForRoute(string szRoute, int maxSegments = 10)
        {
            if (!Profile.GetUser(User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException();

            VisitedRoute vr = new VisitedRoute(szRoute);
            vr.Refresh(maxSegments);
            return vr;
        }

        #region Geocoding
        [WebMethod]
        [ScriptMethod]
        public string[] SuggestCountries(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Country from Airports where Country like ?prefix ORDER BY Country ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Country"]); });

            return lst.ToArray();
        }

        [WebMethod]
        [ScriptMethod]
        public string[] SuggestAdmin(string prefixText, int count)
        {
            if (String.IsNullOrEmpty(prefixText))
                return Array.Empty<string>();
            List<string> lst = new List<string>();
            DBHelper dbh = new DBHelper("SELECT distinct Admin1 from Airports where Admin1 like ?prefix ORDER BY Admin1 ASC LIMIT ?count");
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("prefix", "%" + prefixText + "%"); comm.Parameters.AddWithValue("count", count); },
                (dr) => { lst.Add((string)dr["Admin1"]); });

            return lst.ToArray();
        }
        #endregion

        private static void CheckAirportAdmin()
        {
            if (HttpContext.Current == null || HttpContext.Current.User == null || HttpContext.Current.User.Identity == null || !HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException("You must be authenticated to make this call");

            Profile pf = Profile.GetUser(HttpContext.Current.User.Identity.Name);
            if (!pf.CanManageData)
                throw new UnauthorizedAccessException("You must be an admin to make this call");
        }

        #region Airport Import Functions
        [WebMethod]
        [ScriptMethod]
        public void UseGuess(string szCode, string szTypeCode, string szCountry, string szAdmin)
        {
            CheckAirportAdmin();
            if (String.IsNullOrEmpty(szCode))
                throw new ArgumentNullException(nameof(szCode));
            List<airport> lst = new List<airport>();
            lst.AddRange(airport.AirportsMatchingCodes(new string[] { szCode }));
            lst.RemoveAll(ap1 => ap1.FacilityTypeCode.CompareOrdinalIgnoreCase(szTypeCode) != 0);
            if (lst.Count != 1)
                return;
            lst[0].SetLocale(szCountry, szAdmin);
        }

        /// <summary>
        /// Finds potentially duplicate airports for review that can be reviewed to coalesce or mark one as preferred
        /// </summary>
        /// <param name="start">Starting point</param>
        /// <param name="count">Number to return</param>
        /// <param name="dupeSeed">If provided, limits to dupes near this "seed" airport.</param>
        /// <returns>Array of airport clusters, where each cluster is a list of airports that looke like aliases of one another</returns>
        [WebMethod]
        [ScriptMethod]
        public IEnumerable<AdminAirport[]> GetDupeAirports(int start, int count, string dupeSeed)
        {
            CheckAirportAdmin();
            return AdminAirport.GetDupeAirports(start, count, dupeSeed);
        }

        [WebMethod]
        [ScriptMethod]
        public bool AirportImportCommand(airportImportCandidate aic, string source, string szCommand)
        {
            CheckAirportAdmin();

            if (aic == null)
                throw new ArgumentNullException(nameof(aic));
            if (szCommand == null)
                throw new ArgumentNullException(nameof(szCommand));

            AirportImportRowCommand airc = (AirportImportRowCommand)Enum.Parse(typeof(AirportImportRowCommand), szCommand);
            AirportImportSource ais = (AirportImportSource)Enum.Parse(typeof(AirportImportSource), source);

            airport ap = null;
            switch (ais)
            {
                case AirportImportSource.FAA:
                    ap = aic.FAAMatch;
                    break;
                case AirportImportSource.ICAO:
                    ap = aic.ICAOMatch;
                    break;
                case AirportImportSource.IATA:
                    ap = aic.IATAMatch;
                    break;
            }

            switch (airc)
            {
                case AirportImportRowCommand.FixLocation:
                    ap.LatLong = aic.LatLong;
                    ap.FCommit(true, false);
                    if (!String.IsNullOrWhiteSpace(aic.Country))
                        ap.SetLocale(aic.Country, aic.Admin1);
                    break;
                case AirportImportRowCommand.FixType:
                    ap.FDelete(true);   // delete the existing one before we update - otherwise REPLACE INTO will not succeed (because we are changing the REPLACE INTO primary key, which includes Type)
                    ap.FacilityTypeCode = aic.FacilityTypeCode;
                    ap.FCommit(true, true); // force this to be treated as a new airport
                    break;
                case AirportImportRowCommand.Overwrite:
                case AirportImportRowCommand.AddAirport:
                    if (airc == AirportImportRowCommand.Overwrite)
                        ap.FDelete(true);   // delete the existing airport

                    switch (ais)
                    {
                        case AirportImportSource.FAA:
                            aic.Code = aic.FAA;
                            break;
                        case AirportImportSource.ICAO:
                            aic.Code = aic.ICAO;
                            break;
                        case AirportImportSource.IATA:
                            aic.Code = aic.IATA;
                            break;
                    }
                    aic.Code = RegexUtility.NonAlphaNumeric.Replace(aic.Code, string.Empty);
                    aic.FCommit(true, true);
                    if (!String.IsNullOrWhiteSpace(aic.Country))
                        aic.SetLocale(aic.Country, aic.Admin1);
                    break;
            }
            return true;
        }
        #endregion

        #region Duplicate Management
        /// <summary>
        /// Deletes a user airport that matches a built-in airport
        /// </summary>
        /// <returns>0 if unknown.</returns>
        [WebMethod(EnableSession = true)]
        public void DeleteDupeUserAirport(string idDelete, string idMap, string szUser, string szType)
        {
            CheckAirportAdmin();
            AdminAirport.DeleteUserAirport(idDelete, idMap, szUser, szType);
        }

        /// <summary>
        /// Sets the preferred flag for an airport
        /// </summary>
        [WebMethod(EnableSession = true)]
        public void SetPreferred(string szCode, string szType, bool fPreferred)
        {
            CheckAirportAdmin();

            AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));
            ap.SetPreferred(fPreferred);
        }

        /// <summary>
        /// Makes a user-defined airport native (i.e., eliminates the source username; accepted as a "true" airport)
        /// </summary>
        [WebMethod(EnableSession = true)]
        public void MakeNative(string szCode, string szType)
        {
            CheckAirportAdmin();

            AdminAirport ap = AdminAirport.AirportWithCodeAndType(szCode, szType) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Airport {0} (type {1}) not found", szCode, szType));
            ap.MakeNative();
        }

        /// <summary>
        /// Copies the latitude/longitude from the source airport to the target airport.
        /// </summary>
        [WebMethod(EnableSession = true)]
        public void MergeWith(string szCodeTarget, string szTypeTarget, string szCodeSource)
        {
            CheckAirportAdmin();

            AdminAirport apTarget = AdminAirport.AirportWithCodeAndType(szCodeTarget, szTypeTarget) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Target Airport {0} (type {1}) not found", szCodeTarget, szTypeTarget));
            AdminAirport apSource = AdminAirport.AirportWithCodeAndType(szCodeSource, szTypeTarget) ?? throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Source Airport {0} (type {1}) not found", szCodeSource, szTypeTarget));
            apTarget.MergeFrom(apSource);
        }
        #endregion
        #endregion

        #region Flights webmethods
        private static void CheckFlightsAdmin()
        {
            if (!(HttpContext.Current?.User?.Identity?.IsAuthenticated ?? false) || String.IsNullOrEmpty(HttpContext.Current?.User?.Identity?.Name))
                throw new UnauthorizedAccessException("You must be authenticated to make this call");

            Profile pf = Profile.GetUser(HttpContext.Current.User.Identity.Name);
            if (!pf.CanSupport)
                throw new UnauthorizedAccessException("You must be an admin to make this call");
        }

        [WebMethod(EnableSession = true)]
        public void FixSignature(int idFlight, bool fForceValid)
        {
            CheckFlightsAdmin();
            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, User.Identity.Name, LogbookEntryCore.LoadTelemetryOption.None, true);
            le.AdminSignatureSanityFix(fForceValid);
        }
        #endregion
    }
}