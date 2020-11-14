using MyFlightbook.Image;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminAircraft : AdminPage
    {
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
            Aircraft[] rgac = (new UserAircraft(Page.User.Identity.Name)).GetAircraftForUser(UserAircraft.AircraftRestriction.AllSims, -1);
            Array.Sort(rgac, (ac1, ac2) =>
            {
                if (ac1.ModelID == ac2.ModelID)
                    return ((int)ac1.InstanceType - (int)ac2.InstanceType);
                else
                    return String.Compare(ac1.ModelDescription, ac2.ModelDescription, StringComparison.CurrentCultureIgnoreCase);
            });
            gvSims.DataSource = rgac;
            gvSims.DataBind();
            lblSimsFound.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.SimsFoundTemplate, rgac.Length);
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
                Label l = (Label)e.Row.FindControl("lblTailnumber");

                GroupCollection gc = regexPseudoSim.Match(l.Text).Groups;
                if (gc != null && gc.Count > 1)
                {
                    string szTailnumFixed = String.Format(CultureInfo.InvariantCulture, "N{0}", gc[1].Value);
                    h.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.Admin.ViewRegistrationTemplate, szTailnumFixed));
                    h.NavigateUrl = Aircraft.LinkForTailnumberRegistry(szTailnumFixed);
                }
                else if (regexOOrI.IsMatch(l.Text))
                {
                    string szTailnumFixed = l.Text.ToUpper(CultureInfo.CurrentCulture).Replace('I', '1').Replace('O', '0');
                    h.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, Resources.Admin.ViewRegistrationTemplate, szTailnumFixed));
                    h.NavigateUrl = Aircraft.LinkForTailnumberRegistry(szTailnumFixed);
                }
                else
                    h.Visible = false;

                e.Row.FindControl("lnkRemoveLeadingN").Visible = l.Text.StartsWith("N0", StringComparison.CurrentCultureIgnoreCase) || l.Text.StartsWith("NN", StringComparison.CurrentCultureIgnoreCase);
                e.Row.FindControl("lnkConvertOandI").Visible = regexOOrI.IsMatch(l.Text);
            }
        }


        protected void gvPseudoGeneric_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            string szAircraftID = (string) e.CommandArgument;

            if (!int.TryParse(szAircraftID, out int idAircraft))
                throw new MyFlightbookValidationException("Missing tail");

            Aircraft ac = new Aircraft(idAircraft);

            if (String.IsNullOrWhiteSpace(ac.TailNumber) || ac.AircraftID <= 0)
                throw new MyFlightbookValidationException("No aircraft with ID " + szAircraftID);

            if (e.CommandName.CompareCurrentCultureIgnoreCase("TrimLeadingN") == 0)
                Aircraft.AdminRenameAircraft(ac, ac.TailNumber.Replace("-", string.Empty).Substring(1));
            else if (e.CommandName.CompareCurrentCultureIgnoreCase("ConvertOandI") == 0)
                Aircraft.AdminRenameAircraft(ac, ac.TailNumber.ToUpper(CultureInfo.CurrentCulture).Replace('O', '0').Replace('I', '1'));

            ((Control)e.CommandSource).Visible = false;
        }

        protected void btnOrphans_Click(object sender, EventArgs e)
        {
            mvAircraftIssues.SetActiveView(vwOrphans);
            gvOrphanedAircraft.DataSourceID = sqlOrphanedAircraft.ID;
            gvOrphanedAircraft.DataBind();
        }

        protected static void DeleteOrphanAircraft(int idAircraft)
        {
            Aircraft ac = new Aircraft(idAircraft);
            ac.PopulateImages();
            foreach (MFBImageInfo mfbii in ac.AircraftImages)
                mfbii.DeleteImage();

            ImageList il = new ImageList(MFBImageInfo.ImageClass.Aircraft, ac.AircraftID.ToString(CultureInfo.InvariantCulture));
            DirectoryInfo di = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(il.VirtPath));
            if (di.Exists)
                di.Delete(true);

            // Delete any tombstone that might point to *this*
            DBHelper dbh = new DBHelper("DELETE FROM aircraftTombstones WHERE idMappedAircraft=?idAc");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idAc", ac.AircraftID); });
        }

        protected void gvOrphanedAircraft_RowCommand(object sender, CommandEventArgs e)
        {
            if (e != null && e.CommandName.CompareCurrentCultureIgnoreCase("_Delete") == 0)
            {
                int idAircraft = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
                DeleteOrphanAircraft(idAircraft);
                DBHelper dbh = new DBHelper("DELETE FROM Aircraft WHERE idAircraft=?idaircraft");
                dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("idaircraft", idAircraft); });
            }
            btnOrphans_Click(sender, e);
        }

        protected void btnDeleteAllOrphans_Click(object sender, EventArgs e)
        {
            foreach (DataKey dk in gvOrphanedAircraft.DataKeys)
            {
                int idAircraft = Convert.ToInt32(dk.Value, CultureInfo.InvariantCulture);
                DeleteOrphanAircraft(Convert.ToInt32(idAircraft, CultureInfo.InvariantCulture));
                new DBHelper("DELETE FROM Aircraft WHERE idAircraft=?idaircraft").DoNonQuery((comm) => { comm.Parameters.AddWithValue("idaircraft", idAircraft); });
            }

            btnOrphans_Click(sender, e);
        }

        protected void gvDupeAircraft_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int rowClicked = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            GridViewRow gvr = gvDupeAircraft.Rows[rowClicked];
            string szTail = gvr.Cells[0].Text;
            List<Aircraft> lstDupes = Aircraft.AircraftMatchingTail(szTail);

            // Sort by instance type, then model ID.  We'll be grouping by these two.
            lstDupes.Sort((ac1, ac2) => { return (ac1.InstanceTypeID == ac2.InstanceTypeID) ? ac1.ModelID - ac2.ModelID : ac1.InstanceTypeID - ac2.InstanceTypeID; });

            // Now go through the list.  If adjacent aircraft are the same, merge them.
            // TODO: This actually has a bug where if we have 3 aircraft to merge, it does the 1st two then exits the loop.
            for (int i = 0; i < lstDupes.Count - 1; i++)
            {
                Aircraft acThis = lstDupes[i];
                Aircraft acNext = lstDupes[i + 1];

                if (acThis.InstanceTypeID == acNext.InstanceTypeID && acThis.ModelID == acNext.ModelID)
                {
                    AircraftUtility.AdminMergeDupeAircraft(acThis, acNext);
                    lstDupes.RemoveAt(i + 1);
                }
                if (acThis.Version != i)
                {
                    acThis.Version = i;
                    acThis.Commit();
                }
                if (acNext.Version != i + 1)
                {
                    acNext.Version = i + 1;
                    acNext.Commit();
                }
            }

            // Now hide each row that matched - just for speed; much quicker than rebinding the data
            string szNormalTail = Aircraft.NormalizeTail(szTail);
            foreach (GridViewRow gvrow in gvDupeAircraft.Rows)
            {
                if (String.Compare(Aircraft.NormalizeTail(gvrow.Cells[0].Text), szNormalTail, StringComparison.OrdinalIgnoreCase) == 0)
                    gvrow.Visible = false;
            }
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

            string szTailToMatch = Regex.Replace(txtTailToFind.Text, "[^a-zA-Z0-9#%]", string.Empty);

            DBHelper dbh = new DBHelper("SELECT * FROM aircraft WHERE REPLACE(UPPER(tailnumber), '-', '') LIKE ?tailNum");

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

            List<int> lst = new List<int>();
            DBHelper dbh = new DBHelper(szSQLMaintainedVirtualAircraft);
            dbh.ReadRows((comm) => { }, (dr) => { lst.Add(Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture)); });
            if (lst.Count == 0)
                return;
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
                DataRowView drv = (DataRowView)e.Row.DataItem;
                rbl.SelectedValue = drv["TemplateType"].ToString();
                rbl = (RadioButtonList)e.Row.FindControl("rblHyphenPref");
                rbl.SelectedValue = drv["hyphenpref"].ToString();
            }
            else if (e.Row.RowType == DataControlRowType.DataRow && (e.Row.RowState & DataControlRowState.Normal) == DataControlRowState.Normal)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                string szPrefix = drv["Prefix"].ToString();
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