using MyFlightbook.AircraftControls;
using Resources;
using System;
using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Web;
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
    [System.Web.Script.Services.ScriptService]
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

    }
}