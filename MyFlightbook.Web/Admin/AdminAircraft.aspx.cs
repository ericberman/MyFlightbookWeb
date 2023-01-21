using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminAircraft : AdminPage
    {
        #region WebMethods
        [WebMethod(EnableSession = true)]
        public static void ConvertOandI(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to ConvertOandI");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            Aircraft.AdminRenameAircraft(ac, ac.TailNumber.ToUpper(CultureInfo.CurrentCulture).Replace('O', '0').Replace('I', '1'));
        }

        [WebMethod(EnableSession = true)]
        public static void TrimLeadingN(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to TrimLeadingN");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            if (!ac.TailNumber.StartsWith("N", StringComparison.CurrentCultureIgnoreCase))
                return;

            Aircraft.AdminRenameAircraft(ac, ac.TailNumber.Replace("-", string.Empty).Substring(1));
        }

        [WebMethod(EnableSession = true)]
        public static void TrimN0(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to TrimN0");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            string szTail = ac.TailNumber.Replace("-", string.Empty);

            if (!szTail.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase) || szTail.Length <= 2)
                return;

            Aircraft.AdminRenameAircraft(ac, "N" + szTail.Substring(2));
        }

        [WebMethod(EnableSession = true)]
        public static void MigrateGeneric(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to MigrateGeneric");

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
        public static void MigrateSim(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to MigrateSim");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            if (AircraftUtility.MapToSim(ac) < 0)
                throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Unable to map aircraft {0}", ac.TailNumber));
        }

        [WebMethod(EnableSession = true)]
        public static string ViewFlights(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name))
                throw new MyFlightbookException("Unauthenticated call to ViewFlights");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "No aircraft with ID {0}", idAircraft));

            using (Table t = new Table())
            {
                return FormlessPage.RenderControlsToHTML(
                    (p) =>
                        {
                            p.Form.Controls.Add(t);

                            DBHelper dbh = new DBHelper("SELECT *, IF(SignatureState = 0, '', 'Yes') AS sigState FROM flights f WHERE idAircraft=?id");
                            TableRow tr = new TableRow() { VerticalAlign = VerticalAlign.Top };
                            t.Rows.Add(tr);
                            tr.Cells.Add(new TableCell() { Text = "Date" });
                            tr.Cells.Add(new TableCell() { Text = "User" });
                            tr.Cells.Add(new TableCell() { Text = "Grnd" });
                            tr.Cells.Add(new TableCell() { Text = "Total" });
                            tr.Cells.Add(new TableCell() { Text = "Signed?" });
                            tr.Style["font-weight"] = "bold";

                            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("id", idAircraft); },
                                (dr) =>
                                {
                                    TableRow tr2 = new TableRow() { VerticalAlign = VerticalAlign.Top };
                                    t.Rows.Add(tr2);
                                    TableCell tc = new TableCell();
                                    tr2.Cells.Add(tc);
                                    tc.Controls.Add(new HyperLink()
                                    {
                                        NavigateUrl = VirtualPathUtility.ToAbsolute(String.Format(CultureInfo.InvariantCulture, "~/Member/LogbookNew.aspx/{0}?a=1", dr["idFlight"])),
                                        Text = Convert.ToDateTime(dr["date"], CultureInfo.InvariantCulture).ToShortDateString(),
                                        Target = "_blank"
                                    });

                                    tr2.Cells.Add(new TableCell() { Text = (string)dr["username"] });
                                    tr2.Cells.Add(new TableCell() { Text = String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["groundSim"]) });
                                    tr2.Cells.Add(new TableCell() { Text = String.Format(CultureInfo.CurrentCulture, "{0:F2}", dr["totalFlightTime"]) });
                                    tr2.Cells.Add(new TableCell() { Text = (string)dr["sigState"] });
                                });
                        },
                    (tw) =>
                        {
                            t.RenderControl(tw);
                        });
            }
        }

        [WebMethod(EnableSession = true)]
        public static void IgnorePseudo(int idAircraft)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated || String.IsNullOrEmpty(HttpContext.Current.User.Identity.Name) || !Profile.GetUser(HttpContext.Current.User.Identity.Name).CanManageData)
                throw new MyFlightbookException("Unauthenticated call to IgnorePseudo");

            Aircraft ac = new Aircraft(idAircraft);
            ac.PublicNotes += '\u2006'; // same marker as in flightlint - a very thin piece of whitespace
            DBHelper dbh = new DBHelper("UPDATE aircraft SET publicnotes=?notes WHERE idaircraft=?id");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("notes", ac.PublicNotes);
                comm.Parameters.AddWithValue("id", idAircraft);
            });
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageData);
            Master.SelectedTab = tabID.admAircraft;

            ScriptManager.GetCurrent(this).AsyncPostBackTimeout = 1500;  // use a long timeout
        }

        protected void btnRefreshDupes_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwDupeAircraft);

            gvDupeAircraft.DataSourceID = sqlDupeAircraft.ID;
            gvDupeAircraft.DataBind();
        }

        protected void btnRefreshInvalid_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwInvalidAircraft);

            gvInvalidAircraft.DataSource = AircraftUtility.AdminAllInvalidAircraft();
            gvInvalidAircraft.DataBind();
        }

        protected void btnRefreshDupeSims_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwDupeSims);

            gvDupeSims.DataSourceID = sqlDupeSims.ID;
            gvDupeSims.DataBind();
        }

        protected void btnRefreshAllSims_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwAllSims);
            List<Aircraft> lst = new List<Aircraft>((new UserAircraft(Page.User.Identity.Name)).GetAircraftForUser(UserAircraft.AircraftRestriction.AllSims, -1));
            lst.Sort((ac1, ac2) =>
            {
                if (ac1.ModelID == ac2.ModelID)
                    return ((int)ac1.InstanceType - (int)ac2.InstanceType);
                else
                    return String.Compare(ac1.ModelDescription, ac2.ModelDescription, StringComparison.CurrentCultureIgnoreCase);
            });
            gvSims.DataSource = lst;
            gvSims.DataBind();
            lblSimsFound.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SimsFoundTemplate, lst.Count);
        }

        protected void btnPseudoGeneric_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwPseudoGeneric);
            gvPseudoGeneric.DataSourceID = sqlPseudoGeneric.ID;
            gvPseudoGeneric.DataBind();
        }

        static readonly Regex regexPseudoSim = new Regex("N[a-zA-Z-]+([0-9].*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex regexOOrI = new Regex("^N.*[oOiI].*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected void gvPseudoGeneric_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e != null && e.Row.RowType == DataControlRowType.DataRow)
            {
                HyperLink h = (HyperLink)e.Row.FindControl("lnkViewFixedTail");
                HyperLink l = (HyperLink)e.Row.FindControl("lblTailnumber");

                GroupCollection gc = regexPseudoSim.Match(l.Text).Groups;
                string szTailnumFixed = l.Text;
                if (gc != null && gc.Count > 1)
                    szTailnumFixed = String.Format(CultureInfo.InvariantCulture, "N{0}", gc[1].Value);
                else if (regexOOrI.IsMatch(l.Text))
                    szTailnumFixed = l.Text.ToUpper(CultureInfo.CurrentCulture).Replace('I', '1').Replace('O', '0');
                else if (szTailnumFixed.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase))
                    szTailnumFixed = "N" + szTailnumFixed.Substring(2);

                h.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.Admin.ViewRegistrationTemplate, szTailnumFixed));
                h.NavigateUrl = Aircraft.LinkForTailnumberRegistry(szTailnumFixed);
                h.Visible = !String.IsNullOrEmpty(h.NavigateUrl);

                int idAircraft = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "idaircraft"), CultureInfo.InvariantCulture);

                HyperLink hLeadingN = (HyperLink)e.Row.FindControl("lnkRemoveLeadingN");
                hLeadingN.Visible = l.Text.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase) || l.Text.StartsWith("NN", StringComparison.CurrentCultureIgnoreCase);
                hLeadingN.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:trimLeadingN('{0}',{1});", hLeadingN.ClientID, idAircraft);

                HyperLink hConvertOandI = (HyperLink)e.Row.FindControl("lnkConvertOandI");
                hConvertOandI.Visible = regexOOrI.IsMatch(l.Text);
                hConvertOandI.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:convertOandI('{0}',{1});", hConvertOandI.ClientID, idAircraft);

                HyperLink hMigrateGeneric = (HyperLink)e.Row.FindControl("lnkMigrateGeneric");
                hMigrateGeneric.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:migrateGeneric('{0}',{1});", hMigrateGeneric.ClientID, idAircraft);

                HyperLink hN0ToN = (HyperLink)e.Row.FindControl("lnkN0ToN");
                hN0ToN.Visible = l.Text.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase);
                hN0ToN.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:trimN0('{0}', {1});", hN0ToN.ClientID, idAircraft);

                HyperLink hMigSim = (HyperLink)e.Row.FindControl("lnkMigrateSim");
                hMigSim.Visible = AircraftUtility.PseudoSimTypeFromTail(l.Text) != AircraftInstanceTypes.RealAircraft;
                hMigSim.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:migrateSim('{0}', {1});", hMigSim.ClientID, idAircraft);

                HyperLink hViewFlights = (HyperLink)e.Row.FindControl("lnkFlights");
                hViewFlights.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:viewFlights({0}, '{1}');", idAircraft, l.Text);

                HyperLink hIgnore = (HyperLink)e.Row.FindControl("lnkIgnore");
                hIgnore.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "javascript:ignorePseudoGeneric('{0}', '{1}');", hIgnore.ClientID, idAircraft);
            }
        }

        protected async Task<bool> RefreshOrphans()
        {
            await Task.Run(() =>
            {
                mvAircraftIssues.SetActiveView(vwOrphans);
                gvOrphanedAircraft.DataSourceID = sqlOrphanedAircraft.ID;
                gvOrphanedAircraft.DataBind();
            });
            return true;
        }

        protected async void btnOrphans_Click(object sender, EventArgs e)
        {
            await RefreshOrphans();
        }

        protected void gvOrphanedAircraft_RowCommand(object sender, CommandEventArgs e)
        {
            if (e != null && e.CommandName.CompareCurrentCultureIgnoreCase("_Delete") == 0)
                AircraftUtility.DeleteOrphanAircraft(Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture));
            btnOrphans_Click(sender, e);
        }

        protected async void btnDeleteAllOrphans_Click(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                foreach (DataKey dk in gvOrphanedAircraft.DataKeys)
                    AircraftUtility.DeleteOrphanAircraft(Convert.ToInt32(dk.Value, CultureInfo.InvariantCulture));
            });
            await RefreshOrphans();
        }

        protected void gvSims_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int rowClicked = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            int idAircraft = Convert.ToInt32(gvSims.Rows[rowClicked].Cells[0].Text, CultureInfo.InvariantCulture);
            Aircraft ac = new Aircraft(idAircraft);
            if (e.CommandName == "Preview")
            {
                Label lb = (Label)gvSims.Rows[rowClicked].FindControl("lblProposedRename");
                lb.Text = HttpUtility.HtmlEncode(Aircraft.SuggestTail(ac.ModelID, ac.InstanceType).TailNumber);
            }
            else if (e.CommandName == "Rename")
            {
                ac.TailNumber = Aircraft.SuggestTail(ac.ModelID, ac.InstanceType).TailNumber;
                ac.Commit();
                btnRefreshAllSims_Click(sender, e);
            }
        }

        protected void gvDupeSims_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int rowClicked = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            GridViewRow gvr = gvDupeSims.Rows[rowClicked];
            int idInstanceTypeKeep = Convert.ToInt32(gvr.Cells[0].Text, CultureInfo.InvariantCulture);
            int idModelKeep = Convert.ToInt32(gvr.Cells[1].Text, CultureInfo.InvariantCulture);
            int idAircraftKeep = Convert.ToInt32(gvr.Cells[2].Text, CultureInfo.InvariantCulture);
            Aircraft acMaster = new Aircraft(idAircraftKeep);

            List<int> lstDupesToMerge = new List<int>();


            foreach (GridViewRow gvrow in gvDupeSims.Rows)
            {
                int idInstanceType = Convert.ToInt32(gvrow.Cells[0].Text, CultureInfo.InvariantCulture);
                int idModel = Convert.ToInt32(gvrow.Cells[1].Text, CultureInfo.InvariantCulture);
                int idAircraft = Convert.ToInt32(gvrow.Cells[2].Text, CultureInfo.InvariantCulture);
                if (idAircraft != idAircraftKeep && idInstanceType == idInstanceTypeKeep && idModel == idModelKeep)
                    lstDupesToMerge.Add(idAircraft);
            }

            try
            {
                // Merge each of the dupes to the one we want to keep
                foreach (int acID in lstDupesToMerge)
                {
                    Aircraft ac = new Aircraft(acID);
                    AircraftUtility.AdminMergeDupeAircraft(acMaster, ac);
                }

                // refresh the list.
                gvDupeSims.DataBind();
            }
            catch (MyFlightbookException ex)
            {
                Label lblErr = (Label)gvr.FindControl("lblError");
                lblErr.Text = ex.Message;
                return;
            }
        }

        protected void btnFindAircraftByTail_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtTailToFind.Text))
                return;

            string szTailToMatch = Regex.Replace(txtTailToFind.Text, "[^a-zA-Z0-9#?]", "*").ConvertToMySQLWildcards();

            DBHelper dbh = new DBHelper("SELECT * FROM aircraft WHERE tailnormal LIKE ?tailNum");

            List<Aircraft> lstAc = new List<Aircraft>();
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("tailNum", szTailToMatch); },
                (dr) => {
                    lstAc.Add(new Aircraft() { AircraftID = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture), TailNumber = (string)dr["tailnumber"] });
                });

            gvFoundAircraft.DataSource = lstAc;
            gvFoundAircraft.DataBind();
            mvAircraftIssues.SetActiveView(vwMatchingAircraft);
        }

        protected void btnCleanUpMaintenance_Click(object sender, EventArgs e)
        {
            const string szSQLMaintainedVirtualAircraft = @"SELECT ac.*, group_concat(ml.id), group_concat(ml.Description)
FROM aircraft ac 
INNER JOIN maintenancelog ml ON ac.idaircraft=ml.idaircraft
WHERE (ac.tailnumber LIKE 'SIM%' OR ac.tailnumber LIKE '#%' OR ac.InstanceType <> 1) AND ml.idAircraft IS NOT NULL
GROUP BY ac.idaircraft";
            const string szSQLDeleteVirtualMaintenanceDates = @"UPDATE aircraft
SET lastannual=null, lastPitotStatic=null, lastVOR=null, lastAltimeter=null, lasttransponder=null, registrationdue=null, glassupgradedate=null
WHERE (tailnumber LIKE 'SIM%' OR tailnumber LIKE '#%' OR InstanceType <> 1) ";

            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper(szSQLMaintainedVirtualAircraft);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture)); });
            if (lst.Any()) {
                IEnumerable<Aircraft> rgac = Aircraft.AircraftFromIDs(lst);
                DBHelper dbhDelMaintenance = new DBHelper("DELETE FROM maintenancelog WHERE idAircraft=?idac");
                foreach (Aircraft ac in rgac)
                {
                    // clean up the maintenance
                    ac.Last100 = ac.LastNewEngine = ac.LastOilChange = 0.0M;
                    ac.LastAltimeter = ac.LastAnnual = ac.LastELT = ac.LastStatic = ac.LastTransponder = ac.LastVOR = ac.RegistrationDue = DateTime.MinValue;
                    ac.Commit();

                    // and then delete any maintenance records for this.
                    dbhDelMaintenance.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idac", ac.AircraftID); });
                }
            }
            dbh.CommandText = szSQLDeleteVirtualMaintenanceDates;
            dbh.DoNonQuery();
            lblMaintenanceResult.Text = String.Format(CultureInfo.CurrentCulture, "Maintenance cleaned up, {0} maintenance logs cleaned, all virtual aircraft had dates nullified", lst.Count);
        }

        const string szKeyVSMapModels = "VSModelMapping";

        protected void btnMapModels_Click(object sender, EventArgs e)
        {
            try
            {
                if (!fuMapModels.HasFile)
                    throw new MyFlightbookValidationException("Need to upload a CSV file with aircraft to map.");

                mvAircraftIssues.SetActiveView(vwMapModels);

                List<AircraftAdminModelMapping> lst = new List<AircraftAdminModelMapping>(AircraftAdminModelMapping.MapModels(fuMapModels.FileContent));
                ViewState[szKeyVSMapModels] = lst;
                gvMapModels.DataSource = lst;
                gvMapModels.DataBind();
            }
            catch (MyFlightbookValidationException ex)
            {
                lblMapModelErr.Text = ex.Message;
            }
            catch (MyFlightbookException ex)
            {
                lblMapModelErr.Text = ex.Message;
            }
        }

        protected void gvMapModels_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareCurrentCultureIgnoreCase("_MapModel") == 0)
            {
                int idRow = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
                List<AircraftAdminModelMapping> lst = (List<AircraftAdminModelMapping>)ViewState[szKeyVSMapModels];

                AircraftAdminModelMapping amm = lst[idRow];

                amm.CommitChange();

                lst.Remove(amm);
                gvMapModels.DataSource = lst;
                gvMapModels.DataBind();
            }
        }

        #region country codes
        protected void btnManageCountryCodes_Click(object sender, EventArgs e)
        {
            gvCountryCodes.DataSourceID = "sqlDSCountryCode";
            gvCountryCodes.DataBind();
        }

        protected void gvCountryCodes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                RadioButtonList rbl = (RadioButtonList)e.Row.FindControl("rblTemplateType");
                rbl.SelectedValue = ((HiddenField)e.Row.FindControl("hdnTempType")).Value;
                rbl = (RadioButtonList)e.Row.FindControl("rblHyphenPref");
                rbl.SelectedValue = ((HiddenField)e.Row.FindControl("hdnHyphPref")).Value; ;
            }
            else if (e.Row.RowType == DataControlRowType.DataRow && (e.Row.RowState & DataControlRowState.Normal) == DataControlRowState.Normal)
            {
                string szPrefix = ((Label) e.Row.FindControl("lblPrefix")).Text;
                if (szPrefix.CompareCurrentCultureIgnoreCase(hdnLastCountryEdited.Value) == 0)
                {
                    Label l = (Label)e.Row.FindControl("lblHyphenResult");
                    l.Visible = true;
                    l.Text = HttpUtility.HtmlEncode(hdnLastCountryResult.Value);
                    hdnLastCountryResult.Value = hdnLastCountryEdited.Value = string.Empty;
                }
            }
        }

        protected void gvCountryCodes_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            GridView gv = (GridView)sender;
            RadioButtonList rbl = (RadioButtonList)gv.Rows[e.RowIndex].FindControl("rblTemplateType");
            sqlDSCountryCode.UpdateParameters["templateType"].DefaultValue = rbl.SelectedValue;
            rbl = (RadioButtonList)gv.Rows[e.RowIndex].FindControl("rblHyphenPref");
            sqlDSCountryCode.UpdateParameters["hyphenpref"].DefaultValue = rbl.SelectedValue;
            sqlDSCountryCode.Update();

            gv.EditIndex = -1;
            CountryCodePrefix.FlushCache();
            gv.DataBind();
        }

        protected void gvCountryCodes_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvCountryCodes.EditIndex = e.NewEditIndex;
            gvCountryCodes.DataBind();
        }

        protected void gvCountryCodes_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.CommandName.CompareCurrentCultureIgnoreCase("fixHyphens") == 0)
            {
                hdnLastCountryEdited.Value = e.CommandArgument.ToString();

                CountryCodePrefix ccp = new List<CountryCodePrefix>(CountryCodePrefix.CountryCodes()).Find(ccp1 => ccp1.Prefix.CompareCurrentCultureIgnoreCase(hdnLastCountryEdited.Value) == 0);
                if (ccp != null)
                {
                    hdnLastCountryResult.Value = String.Format(CultureInfo.CurrentCulture, "{0} aircraft updated", ccp.ADMINNormalizeMatchingAircraft());
                    gvCountryCodes.DataBind();
                }
            }
        }
        #endregion
    }
}