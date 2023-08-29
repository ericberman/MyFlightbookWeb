using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class Member_Admin : AdminPage
    {
        private static bool IsAuthorizedForTab(tabID sidebarTab, Profile pf)
        {
            switch (sidebarTab)
            {
                case tabID.admUsers:
                    return pf.CanSupport;
                case tabID.admModels:
                case tabID.admManufacturers:
                case tabID.admAirports:
                case tabID.admMisc:
                    return pf.CanManageData;
                default:
                    return false;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            CheckAdmin(pf.CanDoSomeAdmin);

            tabID sidebarTab = tabID.admUsers;
            if (!IsPostBack)
            {
                string szPage = util.GetStringParam(Request, "t");

                if (!String.IsNullOrEmpty(szPage))
                {
                    if (!Enum.TryParse<tabID>(szPage, out sidebarTab))
                        sidebarTab = tabID.admUsers;
                }

                CheckAdmin(IsAuthorizedForTab(sidebarTab, pf));

                switch (sidebarTab)
                {
                    default:
                    case tabID.admUsers:
                        mvAdmin.SetActiveView(vwUsers);
                        mvMain.SetActiveView(vwMainUsers);
                        break;
                    case tabID.admManufacturers:
                        mvAdmin.SetActiveView(vwManufacturers);
                        mvMain.SetActiveView(vwMainManufacturers);
                        break;
                    case tabID.admMisc:
                        mvAdmin.SetActiveView(vwMisc);
                        mvMain.SetActiveView(vwMainMisc);
                        DisplayMemStats();
                        break;
                    case tabID.admModels:
                        mvAdmin.SetActiveView(vwModels);
                        mvMain.SetActiveView(vwMainModels);
                        RefreshDupeModels();
                        break;
                }

                hdnActiveTab.Value = sidebarTab.ToString();
            }
            else
                sidebarTab = (tabID)Enum.Parse(typeof(tabID), hdnActiveTab.Value);

            Master.SelectedTab = sidebarTab;
        }

        #region models
        protected void RefreshDupeModels()
        {
            SqlDataSourceDupeModels.SelectCommand = String.Format(CultureInfo.InvariantCulture, @"SELECT man.manufacturer, 
                    cc.CatClass,
                    CAST(CONCAT(model, CONCAT(' ''', modelname, ''' '), IF(typename='', '', CONCAT('TYPE=', typename)), CONCAT(' - ', idmodel)) AS CHAR) AS 'DisplayName',
                    m.*
                FROM models m
                    INNER JOIN manufacturers man ON m.idmanufacturer = man.idManufacturer
                    INNER JOIN categoryclass cc ON cc.idCatClass=m.idcategoryclass
                WHERE UPPER(REPLACE(REPLACE(CONCAT(m.model,CONVERT(m.idcategoryclass, char),m.typename), '-', ''), ' ', '')) IN
                    (SELECT modelandtype FROM (SELECT model, COUNT(model) AS cModel, UPPER(REPLACE(REPLACE(CONCAT(m2.model,CONVERT(m2.idcategoryclass, char),m2.typename), '-', ''), ' ', '')) AS modelandtype FROM models m2 GROUP BY modelandtype HAVING cModel > 1) as dupes)
                {0}
                ORDER BY m.model", ckExcludeSims.Checked ? "HAVING m.fSimOnly = 0" : string.Empty);

            gvDupeModels.DataBind();
        }

        protected void ckExcludeSims_CheckedChanged(object sender, EventArgs e)
        {
            RefreshDupeModels();
        }

        protected void btnPreview_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            int idModelToDelete = Convert.ToInt32(cmbModelToDelete.SelectedValue, CultureInfo.InvariantCulture);
            lblPreview.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, "Model {0} will be deleted\r\nThe following airplanes will be mapped from model {1} to {2}", idModelToDelete, cmbModelToDelete.SelectedValue, cmbModelToMergeInto.SelectedValue));

            pnlPreview.Visible = true;

            gvAirplanesToRemap.DataSource = new UserAircraft(null).GetAircraftForUser(UserAircraft.AircraftRestriction.AllMakeModel, idModelToDelete);
            gvAirplanesToRemap.DataBind();
        }

        protected void btnDeleteDupeMake_Click(object sender, EventArgs e)
        {
            int idModelToDelete = Convert.ToInt32(cmbModelToDelete.SelectedValue, CultureInfo.InvariantCulture);
            int idModelToMergeInto = Convert.ToInt32(cmbModelToMergeInto.SelectedValue, CultureInfo.InvariantCulture);

            lblPreview.Text = String.Join("\r\n", MakeModel.AdminMergeDuplicateModels(idModelToDelete, idModelToMergeInto));

            RefreshDupeModels();
            gvOrphanMakes.DataBind();
            cmbModelToDelete.DataBind();
            cmbModelToMergeInto.DataBind();
            pnlPreview.Visible = false;
        }

        protected void gvOrphanMakes_RowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            if (e != null)
            {
                int idModel = Convert.ToInt32(e.Keys[0], CultureInfo.InvariantCulture); // first key is idmodel
                DBHelper dbh = new DBHelper("DELETE FROM CustCurrencyRef WHERE value=?idmodel AND type=?modelsType");
                dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("idmodel", idModel);
                    comm.Parameters.AddWithValue("modelsType", (int)MyFlightbook.Currency.CustomCurrency.CurrencyRefType.Models);
                });
            }
        }

        private static string NormalizeModelName(string sz)
        {
            return sz.Replace(" ", "").Replace("-", "");
        }

        protected void CustomValidator1_ServerValidate(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (cmbModelToMergeInto.SelectedValue == cmbModelToDelete.SelectedValue)
                args.IsValid = false;
            MakeModel mmToDelete = new MakeModel(Convert.ToInt32(cmbModelToDelete.SelectedValue, CultureInfo.InvariantCulture));
            MakeModel mmToKeep = new MakeModel(Convert.ToInt32(cmbModelToMergeInto.SelectedValue, CultureInfo.InvariantCulture));
            if (String.Compare(NormalizeModelName(mmToDelete.Model), NormalizeModelName(mmToKeep.Model), StringComparison.OrdinalIgnoreCase) != 0)
                args.IsValid = false;
        }

        protected void btnRefreshReview_Click(object sender, EventArgs e)
        {
            gvReviewTypes.DataSourceID = "sqlDSReviewTypes";
            gvReviewTypes.DataBind();
        }

        #endregion

        #region Manufacturers
        protected void btnPreviewDupeMans_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid)
                return;

            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(CultureInfo.CurrentCulture, "Manufacturer {0} will be deleted<br />", System.Web.HttpUtility.HtmlEncode(cmbManToKill.SelectedItem.Text)));
            sb.Append(String.Format(CultureInfo.CurrentCulture, "The following models will be mapped from manufacturer {0} to {1}<br />", System.Web.HttpUtility.HtmlEncode(cmbManToKill.SelectedItem.Text), System.Web.HttpUtility.HtmlEncode(cmbManToKeep.SelectedItem.Text)));

            pnlPreviewDupeMan.Visible = true;

            gvModelsToRemap.DataBind();
            lblPreviewDupeMan.Text = sb.ToString();
        }

        protected void btnDeleteDupeMan_Click(object sender, EventArgs e)
        {
            StringBuilder sbAudit = new StringBuilder("<br /><br /><b>Audit of changes made:</b><br />");

            DBHelper dbh = new DBHelper(String.Format(CultureInfo.CurrentCulture, "UPDATE models SET idManufacturer={0} WHERE idmanufacturer={1}", cmbManToKeep.SelectedValue, cmbManToKill.SelectedValue));
            sbAudit.AppendFormat(CultureInfo.CurrentCulture, "Executed this command: {0}<br />", System.Web.HttpUtility.HtmlEncode(dbh.CommandText));
            if (!dbh.DoNonQuery())
                throw new MyFlightbookException("Error remapping model: " + dbh.CommandText + "\r\n" + dbh.LastError);

            // Then delete the old manufacturer
            dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM manufacturers WHERE idmanufacturer={0}", cmbManToKill.SelectedValue);
            sbAudit.AppendFormat(CultureInfo.CurrentCulture, "Deleted this manufacturer: {0}<br />", System.Web.HttpUtility.HtmlEncode(dbh.CommandText));
            if (!dbh.DoNonQuery())
                throw new MyFlightbookException("Error deleting manufacturer: " + dbh.CommandText + "\r\n" + dbh.LastError);

            lblPreviewDupeMan.Text = sbAudit.ToString();

            gvModelsToRemap.DataBind();
            gvDupeMan.DataBind();
            gvOrphanMakes.DataBind();
            cmbManToKeep.DataBind();
            cmbManToKill.DataBind();
            pnlPreviewDupeMan.Visible = false;
        }

        protected void ValidateDupeMans(object source, ServerValidateEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (cmbManToKeep.SelectedValue == cmbManToKill.SelectedValue ||
                cmbManToKeep.SelectedItem == null || cmbManToKill.SelectedItem == null ||
                cmbManToKill.SelectedItem.Text.Length == 0 || cmbManToKeep.SelectedItem.Text.Length == 0)
            {
                args.IsValid = false;
                return;
            }

            string szMan1 = cmbManToKeep.SelectedItem.Text.Substring(cmbManToKeep.SelectedItem.Text.IndexOf(" - ", StringComparison.OrdinalIgnoreCase) + 3);
            string szMan2 = cmbManToKill.SelectedItem.Text.Substring(cmbManToKill.SelectedItem.Text.IndexOf(" - ", StringComparison.OrdinalIgnoreCase) + 3);

            if (String.Compare(szMan1, szMan2, StringComparison.CurrentCultureIgnoreCase) != 0)
                args.IsValid = false;
        }

        protected void btnManAdd_Click(object sender, EventArgs e)
        {
            DBHelper dbh = new DBHelper("INSERT INTO manufacturers SET Manufacturer = ?Manufacturer");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("Manufacturer", txtNewManufacturer.Text); });
            if (dbh.LastError.Length > 0)
                lblError.Text = dbh.LastError;
            gvManufacturers.DataBind();
        }

        protected void ManRowDeleting(object sender, GridViewDeleteEventArgs e)
        {
            if (e != null && Convert.ToInt32(e.Values["Number of Models"], CultureInfo.InvariantCulture) != 0)
                e.Cancel = true;
            Manufacturer.FlushCache();
        }

        protected void gvManufacturers_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
            {
                RadioButtonList rbl = (RadioButtonList)e.Row.FindControl("rblDefaultSim");
                rbl.SelectedValue = DataBinder.Eval(e.Row.DataItem, "DefaultSim").ToString();
            }
        }

        protected void ManRowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            GridView gv = (GridView)sender;
            RadioButtonList rbl = (RadioButtonList)gv.Rows[e.RowIndex].FindControl("rblDefaultSim");
            sqlDSManufacturers.UpdateParameters["DefaultSim"].DefaultValue = rbl.SelectedValue;
            sqlDSManufacturers.Update();
            gv.EditIndex = -1;

            Manufacturer.FlushCache();
        }
        #endregion

        #region User Management
        protected void UnlockUser(object sender, CommandEventArgs e)
        {
            if (e != null && String.Compare(e.CommandName, "Unlock", StringComparison.OrdinalIgnoreCase) == 0)
            {
                int row = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
                string szUser = gvLockedUsers.Rows[row].Cells[1].Text;
                MembershipUser u = Membership.GetUser(szUser);
                u.UnlockUser();
                sqlDSLockedUsers.DataBind();
                gvLockedUsers.DataBind();

                // Now send an email to the user
                util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Profile.AccountUnlockedSubject, Branding.CurrentBrand.AppName), Resources.EmailTemplates.AccountUnlocked, new MailAddress(u.Email, MyFlightbook.Profile.GetUser(szUser).UserFullName), true, false);
            }
        }
        #endregion

        #region Misc
        protected void DisplayMemStats()
        {
            Dictionary<string, int> d = new Dictionary<string, int>();
            foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            {
                string szClass = entry.Value.GetType().ToString();
                if (d.ContainsKey(szClass))
                    d[szClass]++;
                else
                    d[szClass] = 1;
            }
            gvCacheData.DataSource = d;
            gvCacheData.DataBind();
            lblMemStats.Text = String.Format(CultureInfo.CurrentCulture, "Cache has {0:#,##0} items", Cache.Count);
        }

        protected async void btnRefreshInvalidSigs_Click(object sender, EventArgs e)
        {
            await Task.Run(() => { UpdateInvalidSigs(); }).ConfigureAwait(true);
        }

        private const string szVSAutoFixed = "autoFixed";
        private const string szVSFlightsToFix = "InvalidSigs";

        private List<LogbookEntryBase> lstToFix
        {
            get
            {
                if (ViewState[szVSFlightsToFix] == null)
                    ViewState[szVSFlightsToFix] = new List<LogbookEntryBase>();
                return (List<LogbookEntryBase>)ViewState[szVSFlightsToFix];
            }
            set { ViewState[szVSFlightsToFix] = value; }
        }

        private List<LogbookEntryBase> lstAutoFix
        {
            get
            {
                if (ViewState[szVSAutoFixed] == null)
                    ViewState[szVSAutoFixed] = new List<LogbookEntryBase>();
                return (List<LogbookEntryBase>)ViewState[szVSAutoFixed];
            }
            set { ViewState[szVSAutoFixed] = value; }
        }

        protected void UpdateInvalidSigs()
        {
            // Pick up where we left off.
            int offset = Convert.ToInt32(hdnSigOffset.Value, CultureInfo.InvariantCulture);

            int additionalFlights = LogbookEntryBase.AdminGetProblemSignedFlights(offset, lstToFix, lstAutoFix);
            offset += additionalFlights;

            lblSigResults.Text = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", offset, lstToFix.Count, lstAutoFix.Count);

            if (additionalFlights > 0)
            {
                // we have more to go, so show the progress view that auto-clicks for the next chunk.
                mvCheckSigs.SetActiveView(vwSigProgress);
                hdnSigOffset.Value = offset.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                mvCheckSigs.SetActiveView(vwInvalidSigs);   // stop pressing 
                hdnSigOffset.Value = 0.ToString(CultureInfo.InvariantCulture);  // and reset the offset so you can press it again.
                gvInvalidSignatures.DataSource = lstToFix;
                gvInvalidSignatures.DataBind();
                gvAutoFixed.DataSource = lstAutoFix;
                gvAutoFixed.DataBind();
            }
        }

        protected void gvInvalidSignatures_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int idFlight = e.CommandArgument.ToString().SafeParseInt(LogbookEntryCore.idFlightNone);
            if (idFlight != LogbookEntryCore.idFlightNone)
            {
                LogbookEntryBase le = new LogbookEntry();
                le.FLoadFromDB(idFlight, string.Empty, LogbookEntryCore.LoadTelemetryOption.None, true);
                if (le.AdminSignatureSanityFix(e.CommandName.CompareOrdinalIgnoreCase("ForceValidity") == 0))
                {
                    List<LogbookEntryBase> lst = lstToFix;
                    lst.RemoveAll(l => l.FlightID == idFlight);
                    gvInvalidSignatures.DataSource = lstToFix = lst;
                    gvInvalidSignatures.DataBind();
                }
            }
        }

        protected void btnRefreshProps_Click(object sender, EventArgs e)
        {
            gvEmptyProps.DataSourceID = "sqlDSEmptyProps";
            gvEmptyProps.DataBind();
            gvDupeProps.DataSourceID = "sqlDSDupeProps";
            gvDupeProps.DataBind();
        }

        protected void sql_SelectingLongTimeout(object sender, SqlDataSourceSelectingEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            e.Command.CommandTimeout = 300;
        }

        protected void btnFlushCache_Click(object sender, EventArgs e)
        {
            lblCacheFlushResults.Text = String.Format(CultureInfo.CurrentCulture, "Cache flushed, {0:#,##0} items removed.", FlushCache());
        }

        protected void btnNightlyRun_Click(object sender, EventArgs e)
        {
            string szURL = String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/TotalsAndcurrencyEmail.aspx"));
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    byte[] rgdata = wc.DownloadData(szURL);
                    string szContent = Encoding.UTF8.GetString(rgdata);
                    if (szContent.Contains("-- SuccessToken --"))
                    {
                        lblNightlyRunResult.Text = "Started";
                        lblNightlyRunResult.CssClass = "success";
                        btnNightlyRun.Enabled = false;
                    }
                }
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                lblNightlyRunResult.Text = ex.Message;
                lblNightlyRunResult.CssClass = "error";
            }
        }
        #endregion
    }
}