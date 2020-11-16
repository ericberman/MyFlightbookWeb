using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Web.Security;
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
            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
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

            StringBuilder sb = new StringBuilder();

            sb.Append(String.Format(CultureInfo.CurrentCulture, "Model {0} will be deleted<br />", System.Web.HttpUtility.HtmlEncode(cmbModelToDelete.SelectedItem.Text)));
            sb.Append(String.Format(CultureInfo.CurrentCulture, "The following airplanes will be mapped from model {0} to {1}<br />", System.Web.HttpUtility.HtmlEncode(cmbModelToDelete.SelectedItem.Text), System.Web.HttpUtility.HtmlEncode(cmbModelToMergeInto.SelectedItem.Text)));

            pnlPreview.Visible = true;

            gvAirplanesToRemap.DataBind();
            lblPreview.Text = sb.ToString();
        }

        protected void btnDeleteDupeMake_Click(object sender, EventArgs e)
        {
            StringBuilder sbAudit = new StringBuilder("<br /><br /><b>Audit of changes made:</b><br />");
            int idModelToDelete = Convert.ToInt32(cmbModelToDelete.SelectedValue, CultureInfo.InvariantCulture);
            int idModelToMergeInto = Convert.ToInt32(cmbModelToMergeInto.SelectedValue, CultureInfo.InvariantCulture);

            // Before we migrate old aircraft, see if there are generics.
            Aircraft acGenericSource = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToDelete));
            Aircraft acGenericTarget = new Aircraft(Aircraft.AnonymousTailnumberForModel(idModelToMergeInto));

            if (acGenericSource.AircraftID != Aircraft.idAircraftUnknown)
            {
                // if the generic for the target doesn't exist, then no problem - just rename it and remap it!
                if (acGenericTarget.AircraftID == Aircraft.idAircraftUnknown)
                {
                    acGenericSource.ModelID = idModelToMergeInto;
                    acGenericSource.TailNumber = Aircraft.AnonymousTailnumberForModel(idModelToMergeInto);
                    acGenericSource.Commit();
                }
                else
                {
                    // if the generic for the target also exists, need to merge the aircraft (creating a tombstone).
                    AircraftUtility.AdminMergeDupeAircraft(acGenericTarget, acGenericSource);
                }
            }

            using (IDataReader idr = (IDataReader)sqlAirplanesToReMap.Select(DataSourceSelectArguments.Empty))
            {
                // migrate the aircraft on the old model to the new model
                while (idr.Read())
                {
                    string szIdAircraft = idr["idaircraft"].ToString();
                    Aircraft ac = new Aircraft(Convert.ToInt32(szIdAircraft, CultureInfo.InvariantCulture)) { ModelID = idModelToMergeInto };
                    ac.Commit();
                    sbAudit.Append(String.Format(CultureInfo.CurrentCulture, "Updated aircraft {0} to model {1}<br />", System.Web.HttpUtility.HtmlEncode(szIdAircraft), idModelToMergeInto));
                }
            }

            // Update any custom currency references to the old model
            DBHelper dbhCCR = new DBHelper("UPDATE CustCurrencyRef SET value=?newID WHERE value=?oldID AND type=?modelsType");
            dbhCCR.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("newID", idModelToMergeInto);
                comm.Parameters.AddWithValue("oldID", idModelToDelete);
                comm.Parameters.AddWithValue("modelsType", (int)MyFlightbook.Currency.CustomCurrency.CurrencyRefType.Models);
            });

            // Then delete the old model
            string szQ = String.Format(CultureInfo.InvariantCulture, "DELETE FROM models WHERE idmodel={0}", idModelToDelete);
            DBHelper dbh = new DBHelper(szQ);
            if (!dbh.DoNonQuery())
                throw new MyFlightbookException("Error deleting model: " + szQ + "\r\n" + dbh.LastError);
            sbAudit.Append(System.Web.HttpUtility.HtmlEncode(szQ) + "<br />");
#pragma warning disable CA3002 // Review code for XSS vulnerabilities - dangerous bits already encoded above.
            lblPreview.Text = sbAudit.ToString();
#pragma warning restore CA3002 // Review code for XSS vulnerabilities

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
        protected void btnRefreshInvalidSigs_Click(object sender, EventArgs e)
        {
            UpdateInvalidSigs();
        }

        protected void UpdateInvalidSigs()
        {
            List<int> lstSigned = new List<int>();
            List<LogbookEntry> lstToFix = new List<LogbookEntry>();
            List<LogbookEntry> lstAutoFix = new List<LogbookEntry>();

            // Pick up where we left off.
            int offset = Convert.ToInt32(hdnSigOffset.Value, CultureInfo.InvariantCulture);

            const int chunkSize = 250;

            DBHelper dbh = new DBHelper("SELECT idFlight FROM Flights WHERE signatureState<>0 ORDER BY Username ASC, Date DESC LIMIT ?lim, ?chunk");
            dbh.CommandArgs.Timeout = 300;  // up to 300 seconds.
            dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("lim", offset); comm.Parameters.AddWithValue("chunk", chunkSize); },
                (dr) => { lstSigned.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });

            lstSigned.ForEach((idFlight) =>
                {
                    LogbookEntry le = new LogbookEntry();
                    le.FLoadFromDB(idFlight, string.Empty, LogbookEntry.LoadTelemetryOption.None, true);
                    if (le.AdminSignatureSanityCheckState != LogbookEntry.SignatureSanityCheckState.OK)
                    {
                        // see if we can auto-fix these.  Auto-fixed = decrypted hash matches case insenstive and trimmed.
                        if (le.DecryptedCurrentHash.Trim().CompareCurrentCultureIgnoreCase(le.DecryptedFlightHash.Trim()) == 0)
                        {
                            lstAutoFix.Add(le);
                            le.AdminSignatureSanityFix(true);
                        }
                        else
                            lstToFix.Add(le);
                    }
                });

            offset += lstSigned.Count;

            if (lstSigned.Any())
            {
                // we have more to go, so show the progress view that auto-clicks for the next chunk.
                mvCheckSigs.SetActiveView(vwSigProgress);
                hdnSigOffset.Value = offset.ToString(CultureInfo.InvariantCulture);
                lblSigResults.Text = String.Format(CultureInfo.CurrentCulture, "Scanned {0} flights so far...", offset);
            }
            else
            {
                mvCheckSigs.SetActiveView(vwInvalidSigs);   // stop pressing 
                hdnSigOffset.Value = 0.ToString(CultureInfo.InvariantCulture);  // and reset the offset so you can press it again.
                gvInvalidSignatures.DataSource = ViewState["InvalidSigs"] = lstToFix;
                gvInvalidSignatures.DataBind();

                lblSigResults.Text = String.Format(CultureInfo.CurrentCulture, "Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", offset, lstToFix.Count, lstAutoFix.Count);
            }
        }

        protected void gvInvalidSignatures_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            int idFlight = e.CommandArgument.ToString().SafeParseInt(LogbookEntry.idFlightNone);
            if (idFlight != LogbookEntry.idFlightNone)
            {
                LogbookEntry le = new LogbookEntry();
                le.FLoadFromDB(idFlight, string.Empty, LogbookEntry.LoadTelemetryOption.None, true);
                if (le.AdminSignatureSanityFix(e.CommandName.CompareOrdinalIgnoreCase("ForceValidity") == 0))
                {
                    List<LogbookEntry> lst = (List<LogbookEntry>)ViewState["InvalidSigs"];
                    lst.RemoveAll(l => l.FlightID == idFlight);
                    gvInvalidSignatures.DataSource = ViewState["InvalidSigs"] = lst;
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


        protected void btnFlushCache_Click(object sender, EventArgs e)
        {
            FlushCache();
        }
        #endregion
    }
}