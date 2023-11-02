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
 * Copyright (c) 2022-2023 MyFlightbook LLC
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
            get
            {
                return "~/public/Scripts/adminajax.js?v=1";
            }
        }

        public AdminWebServices()
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new UnauthorizedAccessException();
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanDoSomeAdmin)
                throw new UnauthorizedAccessException();
        }

        #region Aircraft WebMethods
        [WebMethod(EnableSession = true)]
        public void ConvertOandI(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to ConvertOandI");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            Aircraft.AdminRenameAircraft(ac, ac.TailNumber.ToUpper(CultureInfo.CurrentCulture).Replace('O', '0').Replace('I', '1'));
        }

        [WebMethod(EnableSession = true)]
        public void TrimLeadingN(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to TrimLeadingN");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            if (!ac.TailNumber.StartsWith("N", StringComparison.CurrentCultureIgnoreCase))
                return;

            Aircraft.AdminRenameAircraft(ac, ac.TailNumber.Replace("-", string.Empty).Substring(1));
        }

        [WebMethod(EnableSession = true)]
        public void TrimN0(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to TrimN0");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            string szTail = ac.TailNumber.Replace("-", string.Empty);

            if (!szTail.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase) || szTail.Length <= 2)
                return;

            Aircraft.AdminRenameAircraft(ac, "N" + szTail.Substring(2));
        }

        [WebMethod(EnableSession = true)]
        public void MigrateGeneric(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to MigrateGeneric");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            Aircraft acOriginal = new Aircraft(ac.AircraftID);

            // See if there is a generic for the model
            string szTailNumGeneric = Aircraft.AnonymousTailnumberForModel(acOriginal.ModelID);
            Aircraft acGeneric = new Aircraft(szTailNumGeneric);
            if (acGeneric.IsNew)
            {
                acGeneric.TailNumber = szTailNumGeneric;
                acGeneric.ModelID = acOriginal.ModelID;
                acGeneric.InstanceType = AircraftInstanceTypes.RealAircraft;
                acGeneric.Commit();
            }

            AircraftUtility.AdminMergeDupeAircraft(acGeneric, acOriginal);
        }

        [WebMethod(EnableSession = true)]
        public void MigrateSim(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to MigrateSim");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            if (AircraftUtility.MapToSim(ac) < 0)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to map aircraft {0}", ac.TailNumber));
        }

        [WebMethod(EnableSession = true)]
        public string ViewFlights(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to ViewFlights");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            StringBuilder sb = new StringBuilder("<table><tr style=\"vertical-align: top; font-weight: bold\"><td>Date</td><td>User</td><td>Grnd</tD><td>Total</td><td>Signed?</td></tr>");

                        DBHelper dbh = new DBHelper("SELECT *, IF(SignatureState = 0, '', 'Yes') AS sigState FROM flights f WHERE idAircraft=?id");
                        sb.AppendLine(@"");

                        dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("id", idAircraft); },
                            (dr) =>
                            {
                                sb.AppendFormat(CultureInfo.CurrentCulture, @"<tr style=""vertical-align: top;""><td><a target=""_blank"" href=""{0}"">{1}</a></td>", VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}?a=1", dr["idFlight"])), Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture).ToShortDateString());
                                sb.AppendFormat(CultureInfo.CurrentCulture, @"<td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td>", (string)dr["username"], String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["groundSim"]), String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["totalFlightTime"]), (string)dr["sigState"]);
                                sb.AppendLine("</tr>");
                            });
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        [WebMethod(EnableSession = true)]
        public void IgnorePseudo(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to IgnorePseudo");

            Aircraft ac = new Aircraft(idAircraft);
            ac.PublicNotes += '\u2006'; // same marker as in flightlint - a very thin piece of whitespace
            DBHelper dbh = new DBHelper("UPDATE aircraft SET publicnotes=?notes WHERE idaircraft=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("notes", ac.PublicNotes);
                comm.Parameters.AddWithValue("id", idAircraft);
            });
        }

        [WebMethod(EnableSession = true)]
        public bool ToggleLock(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to ToggleLock");

            DBHelper dbh = new DBHelper("UPDATE aircraft SET isLocked = IF(isLocked = 0, 1, 0) WHERE idaircraft=?id");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("id", idAircraft); });

            bool result = false;

            dbh.CommandText = "SELECT isLocked FROM aircraft WHERE idaircraft=?id";
            dbh.ReadRow((comm) => { comm.Parameters.AddWithValue("id", idAircraft); },
                (dr) => { result = Convert.ToInt32(dr["isLocked"], CultureInfo.InvariantCulture) != 0; });
            return result;
        }

        [WebMethod(EnableSession = true)]
        public void MergeAircraft(int idAircraftToMerge, int idTargetAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to MergeAircraft");

            if (idAircraftToMerge <= 0)
                throw new ArgumentOutOfRangeException(nameof(idAircraftToMerge), "Invalid id for aircraft to merge");
            if (idTargetAircraft <= 0)
                throw new ArgumentOutOfRangeException(nameof(idTargetAircraft), "Invalid target aircraft for merge");

            Aircraft acMaster = new Aircraft(idTargetAircraft);
            Aircraft acClone = new Aircraft(idAircraftToMerge);

            if (!acMaster.IsValid())
                throw new InvalidOperationException("Invalid target aircraft for merge");
            if (!acClone.IsValid())
                throw new InvalidOperationException("Invalid source aircraft for merge");

            AircraftUtility.AdminMergeDupeAircraft(acMaster, acClone);
        }

        [WebMethod(EnableSession = true)]
        public void MakeDefault(int idAircraft)
        {
            if (!Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new UnauthorizedAccessException("Unauthenticated call to MakeDefault");

            Aircraft ac = new Aircraft(idAircraft);
            if (ac.IsValid())
                ac.MakeDefault();
            else
                throw new InvalidOperationException(ac.ErrorString);
        }
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
            pf.InputStream.Read(rgbytes, 0, pf.ContentLength);
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
        #endregion
    }
}