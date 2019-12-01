using MyFlightbook;
using MyFlightbook.Achievements;
using MyFlightbook.Image;
using MyFlightbook.Payments;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Admin : System.Web.UI.Page
{
    private const string szTemplateAircraftOfModelType = "SELECT * FROM aircraft WHERE idmodel={0}";
    private const string szTemplateModelsForManufacturer = "SELECT * FROM models WHERE idmanufacturer={0}";

    protected void RejectUser()
    {
        util.NotifyAdminEvent("Attempt to view admin page", String.Format(CultureInfo.CurrentCulture, "User {0} tried to hit the admin page.", Page.User.Identity.Name), ProfileRoles.maskSiteAdminOnly);
        Response.Redirect("~/HTTP403.htm");
    }

    private bool IsAuthorizedForTab(tabID sidebarTab, Profile pf)
    {
        switch (sidebarTab)
        {
            case tabID.admUsers:
                return pf.CanSupport;
            case tabID.admModels:
            case tabID.admManufacturers:
            case tabID.admAircraft:
            case tabID.admImages:
            case tabID.admAirports:
            case tabID.admProperties:
            case tabID.admEndorsements:
            case tabID.admAchievements:
            case tabID.admMisc:
                return pf.CanManageData;
            case tabID.admFAQ:
                return pf.CanSupport || pf.CanManageData;
            case tabID.admDonations:
                return pf.CanManageMoney;
            case tabID.admStats:
                return pf.CanReport;
            default:
                return false;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        if (!pf.CanDoSomeAdmin)
            RejectUser();

        tabID sidebarTab = tabID.admUsers;
        if (!IsPostBack)
        {
            string szPage = util.GetStringParam( Request, "t");

            if (!String.IsNullOrEmpty(szPage))
            {
                if (!Enum.TryParse<tabID>(szPage, out sidebarTab))
                    sidebarTab = tabID.admUsers;
            }

            if (!IsAuthorizedForTab(sidebarTab, pf))
            {
                RejectUser();
                return;
            }

            switch (sidebarTab)
            {
                default:
                case tabID.admUsers:
                    mvAdmin.SetActiveView(vwUsers);
                    mvMain.SetActiveView(vwMainUsers);
                    break;
                case tabID.admEndorsements:
                    mvAdmin.SetActiveView(vwEndorsementTemplates);
                    mvMain.SetActiveView(vwMainEndorsements);
                    break;
                case tabID.admImages:
                    mvAdmin.SetActiveView(vwImages);
                    mvMain.SetActiveView(vwMainImages);
                    btnDeleteS3Debug.Visible = AWSConfiguration.UseDebugBucket;
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
                    break;
                case tabID.admProperties:
                    mvAdmin.SetActiveView(vwProperties);
                    mvMain.SetActiveView(vwMainProperties);
                    break;
                case tabID.admAircraft:
                    ScriptManager.GetCurrent(this).AsyncPostBackTimeout = 1500;  // use a long timeout
                    mvAdmin.SetActiveView(vwAircraft);
                    mvMain.SetActiveView(vwMainAircraft);
                    break;
                case tabID.admDonations:
                    mvAdmin.SetActiveView(vwDonations);
                    mvMain.SetActiveView(vwMainDonations);
                    RefreshDonations();

                    util.SetValidationGroup(pnlTestTransaction, "valTestTransaction");
                    dateTestTransaction.Date = DateTime.Now;
                    break;
                case tabID.admAchievements:
                    mvAdmin.SetActiveView(vwAchievements);
                    mvMain.SetActiveView(vwMainAchievements);
                    break;
            }

            hdnActiveTab.Value = sidebarTab.ToString();
        }
        else
            sidebarTab = (tabID)Enum.Parse(typeof(tabID), hdnActiveTab.Value);

        Master.SelectedTab = sidebarTab;
    }

    #region models
    protected void btnPreview_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;

        StringBuilder sb = new StringBuilder();

        sb.Append(String.Format(CultureInfo.CurrentCulture, "Model {0} will be deleted<br />", cmbModelToDelete.SelectedItem.Text));
        sb.Append(String.Format(CultureInfo.CurrentCulture, "The following airplanes will be mapped from model {0} to {1}<br />", cmbModelToDelete.SelectedItem.Text, cmbModelToMergeInto.SelectedItem.Text));

        pnlPreview.Visible = true;

        sqlAirplanesToReMap.SelectCommand = String.Format(CultureInfo.InvariantCulture, szTemplateAircraftOfModelType, cmbModelToDelete.SelectedValue);
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
                Aircraft.AdminMergeDupeAircraft(acGenericTarget, acGenericSource);
            }
        }

        string szQ = "";
        sqlAirplanesToReMap.SelectCommand = String.Format(CultureInfo.InvariantCulture, szTemplateAircraftOfModelType, idModelToDelete);
        using (IDataReader idr = (IDataReader)sqlAirplanesToReMap.Select(DataSourceSelectArguments.Empty))
        {
            // migrate the aircraft on the old model to the new model
            while (idr.Read())
            {
                string szIdAircraft = idr["idaircraft"].ToString();
                Aircraft ac = new Aircraft(Convert.ToInt32(szIdAircraft, CultureInfo.InvariantCulture)) { ModelID = idModelToMergeInto };
                ac.Commit();
                sbAudit.Append(String.Format(CultureInfo.CurrentCulture, "Updated aircraft {0} to model {1}<br />", szIdAircraft, idModelToMergeInto));
            }
        }

        // Update any custom currency references to the old model
        DBHelper dbhCCR = new DBHelper("UPDATE CustCurrencyRef SET value=?newID WHERE value=?oldID AND type=?modelsType");
        dbhCCR.DoNonQuery((comm) =>
        {
            comm.Parameters.AddWithValue("newID", idModelToMergeInto);
            comm.Parameters.AddWithValue("oldID", idModelToDelete);
            comm.Parameters.AddWithValue("modelsType", (int)MyFlightbook.FlightCurrency.CustomCurrency.CurrencyRefType.Models);
        });

        // Then delete the old model
        szQ = String.Format(CultureInfo.InvariantCulture, "DELETE FROM models WHERE idmodel={0}", idModelToDelete);
        DBHelper dbh = new DBHelper(szQ);
        if (!dbh.DoNonQuery())
            throw new MyFlightbookException("Error deleting model: " + szQ + "\r\n" + dbh.LastError);
        sbAudit.Append(szQ + "<br />");
        lblPreview.Text = sbAudit.ToString();

        gvDupeModels.DataBind();
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
                comm.Parameters.AddWithValue("modelsType", (int)MyFlightbook.FlightCurrency.CustomCurrency.CurrencyRefType.Models);
            });
        }
    }

    private string NormalizeModelName(string sz)
    {
        return sz.Replace(" ", "").Replace("-", "");
    }

    protected void CustomValidator1_ServerValidate(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
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

        sb.Append(String.Format(CultureInfo.CurrentCulture, "Manufacturer {0} will be deleted<br />",  cmbManToKill.SelectedItem.Text));
        sb.Append(String.Format(CultureInfo.CurrentCulture, "The following models will be mapped from manufacturer {0} to {1}<br />", cmbManToKill.SelectedItem.Text, cmbManToKeep.SelectedItem.Text));

        pnlPreviewDupeMan.Visible = true;

        sqlModelsToRemap.SelectCommand = String.Format(CultureInfo.InvariantCulture, szTemplateModelsForManufacturer, cmbManToKill.SelectedValue);
        gvModelsToRemap.DataBind();
        lblPreviewDupeMan.Text = sb.ToString();
    }

    protected void btnDeleteDupeMan_Click(object sender, EventArgs e)
    {
        StringBuilder sbAudit = new StringBuilder("<br /><br /><b>Audit of changes made:</b><br />");

        DBHelper dbh = new DBHelper(String.Format(CultureInfo.CurrentCulture, "UPDATE models SET idManufacturer={0} WHERE idmanufacturer={1}", cmbManToKeep.SelectedValue, cmbManToKill.SelectedValue));
        sbAudit.AppendFormat(CultureInfo.CurrentCulture, "Executed this command: {0}<br />", dbh.CommandText);
        if (!dbh.DoNonQuery())
            throw new MyFlightbookException("Error remapping model: " + dbh.CommandText + "\r\n" + dbh.LastError);

        // Then delete the old manufacturer
        dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM manufacturers WHERE idmanufacturer={0}", cmbManToKill.SelectedValue);
        sbAudit.AppendFormat(CultureInfo.CurrentCulture, "Deleted this manufacturer: {0}<br />", dbh.CommandText);
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
            throw new ArgumentNullException("args");
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
            throw new ArgumentNullException("e");
        if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
        {
            RadioButtonList rbl = (RadioButtonList)e.Row.FindControl("rblDefaultSim");
            rbl.SelectedValue = DataBinder.Eval(e.Row.DataItem, "DefaultSim").ToString();
        }
    }

    protected void ManRowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (sender == null)
            throw new ArgumentNullException("sender");

        GridView gv = (GridView)sender;
        RadioButtonList rbl = (RadioButtonList)gv.Rows[e.RowIndex].FindControl("rblDefaultSim");
        sqlDSManufacturers.UpdateParameters["DefaultSim"].DefaultValue = rbl.SelectedValue;
        sqlDSManufacturers.Update();
        gv.EditIndex = -1;

        Manufacturer.FlushCache();
    }
    #endregion

    #region Properties
    protected void btnNewCustomProp_Click(object sender, EventArgs e)
    {
        DBHelper dbh = new DBHelper("INSERT INTO custompropertytypes SET Title=?Title, FormatString=?FormatString, Type=?Type, Flags=?Flags, Description=?Desc, SortKey=''");
        dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Title", txtCustomPropTitle.Text);
                comm.Parameters.AddWithValue("FormatString", txtCustomPropFormat.Text);
                comm.Parameters.AddWithValue("Desc", txtCustomPropDesc.Text);
                comm.Parameters.AddWithValue("Type", Convert.ToInt32(cmbCustomPropType.SelectedValue, CultureInfo.InvariantCulture));
                comm.Parameters.AddWithValue("Flags", Convert.ToInt32(lblFlags.Text, CultureInfo.InvariantCulture));
            });

        CustomPropertyType.FlushGlobalCache();
        FlushCache();
        gvCustomProps.DataBind();
        txtCustomPropTitle.Text = txtCustomPropFormat.Text = "";
        cmbCustomPropType.SelectedIndex = 0;
        lblFlags.Text = Convert.ToString(0, CultureInfo.CurrentCulture);
        foreach (ListItem li in CheckBoxList1.Items)
            li.Selected = false;
    }

    protected void CustPropsRowEdited(object sender, EventArgs e)
    {
        CustomPropertyType.FlushGlobalCache();
    }

    protected void CheckBoxList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        int i = 0;

        foreach (ListItem li in CheckBoxList1.Items)
        {
            if (li.Selected)
                i += Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
        }
        lblFlags.Text = i.ToString(CultureInfo.InvariantCulture);
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

    #region Aircraft Management
    protected void btnRefreshDupes_Click(object sender, EventArgs e)
    {
        mvAircraftIssues.SetActiveView(vwDupeAircraft);

        gvDupeAircraft.DataSourceID = sqlDupeAircraft.ID;
        gvDupeAircraft.DataBind();
    }

    protected void btnRefreshInvalid_Click(object sender, EventArgs e)
    {
        mvAircraftIssues.SetActiveView(vwInvalidAircraft);

        gvInvalidAircraft.DataSource = Aircraft.AdminAllInvalidAircraft();
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
                h.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.ViewRegistrationTemplate, szTailnumFixed);
                h.NavigateUrl = Aircraft.LinkForTailnumberRegistry(szTailnumFixed);
            }
            else
                h.Visible = false;
        }
    }

    protected void btnOrphans_Click(object sender, EventArgs e)
    {
        mvAircraftIssues.SetActiveView(vwOrphans);
        gvOrphanedAircraft.DataSourceID = sqlOrphanedAircraft.ID;
        gvOrphanedAircraft.DataBind();
    }

    protected void DeleteOrphanAircraft(int idAircraft)
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
            throw new ArgumentNullException("e");
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
                Aircraft.AdminMergeDupeAircraft(acThis, acNext);
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
            throw new ArgumentNullException("e");
        int rowClicked = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
        int idAircraft = Convert.ToInt32(gvSims.Rows[rowClicked].Cells[0].Text, CultureInfo.InvariantCulture);
        Aircraft ac = new Aircraft(idAircraft);
        if (e.CommandName == "Preview")
        {
            Label lb = (Label)gvSims.Rows[rowClicked].FindControl("lblProposedRename");
            lb.Text = Aircraft.SuggestTail(ac.ModelID, ac.InstanceType).TailNumber;
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
            throw new ArgumentNullException("e");
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
                Aircraft.AdminMergeDupeAircraft(acMaster, ac);
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
            (dr) => { lstAc.Add(new Aircraft() { AircraftID = Convert.ToInt32(dr["idaircraft"], CultureInfo.InvariantCulture), TailNumber = (string)dr["tailnumber"] });
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
        dbh.ReadRows((comm) => { }, (dr) => { lst.Add(Convert.ToInt32(dr["idaircraft"])); });
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
            throw new ArgumentNullException("e");

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
            throw new ArgumentNullException("e");
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
                l.Text = hdnLastCountryResult.Value;
                hdnLastCountryResult.Value = hdnLastCountryEdited.Value = string.Empty;
            }
        }
    }

    protected void gvCountryCodes_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (sender == null)
            throw new ArgumentNullException("sender");

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
            throw new ArgumentNullException("e");
        gvCountryCodes.EditIndex = e.NewEditIndex;
        gvCountryCodes.DataBind();
    }

    protected void gvCountryCodes_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

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
    #endregion

    #region Endorsement Management
    protected void btnAddTemplate_Click(object sender, EventArgs e)
    {
        DBHelper dbh = new DBHelper("INSERT INTO endorsementtemplates SET FARRef=?FARRef, Title=?Title, Text=?Text");
        dbh.DoNonQuery((comm) =>
        {
            comm.Parameters.AddWithValue("FARRef", txtEndorsementFAR.Text);
            comm.Parameters.AddWithValue("Title", txtEndorsementTitle.Text);
            comm.Parameters.AddWithValue("Text", txtEndorsementTemplate.Text);
        });

        gvEndorsementTemplate.DataBind();
        txtEndorsementFAR.Text = txtEndorsementTemplate.Text = txtEndorsementTitle.Text = String.Empty;
    }

    protected void gvEndorsementTemplate_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_mfbEditEndorsement ee = (Controls_mfbEditEndorsement)e.Row.FindControl("mfbEditEndorsement1");
            if (ee != null)
            {
                ee.EndorsementID = Convert.ToInt32(((DataRowView)e.Row.DataItem)["id"], CultureInfo.InvariantCulture);
                util.SetValidationGroup(ee, "no-validation");
            }
        }
    }
    #endregion

    #region Image Management
    protected void btnDeleteOrphans_Click(object sender, EventArgs e)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("Deleting orphaned flight images:\r\n<br />");
        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Flight));
        sb.Append("Deleting orphaned endorsement images:\r\n<br />");
        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Endorsement));
        sb.Append("Deleting orphaned Aircraft images:\r\n<br />");
        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Aircraft));
        sb.Append("Deleting orphaned BasicMed images:\r\n<br />");
        sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.BasicMed));
        lblDeleted.Text = sb.ToString();
    }

    protected void btnDeleteS3Debug_Click(object sender, EventArgs e)
    {
        AWSImageManagerAdmin.ADMINCleanUpDebug();
    }

    protected void SyncImagesToDB(MFBImageInfo.ImageClass ic)
    {
        LiteralControl lc = new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<iframe src=\"{0}\" width=\"600\" height=\"300\"></iframe>", String.Format(CultureInfo.InvariantCulture, "{0}?sync=1&r={1}{2}", ResolveClientUrl("AdminImages.aspx"), ic.ToString(), ckPreviewOnly.Checked ? "&preview=1" : string.Empty)));
        plcDBSync.Controls.Add(lc);
    }

    protected void btnSyncFlight_Click(object sender, EventArgs e)
    {
        SyncImagesToDB(MFBImageInfo.ImageClass.Flight);
    }
    protected void btnSyncAircraftImages_Click(object sender, EventArgs e)
    {
        SyncImagesToDB(MFBImageInfo.ImageClass.Aircraft);
    }
    protected void btnSyncEndorsements_Click(object sender, EventArgs e)
    {
        SyncImagesToDB(MFBImageInfo.ImageClass.Endorsement);
    }

    protected void btnSyncBasicMed_Click(object sender, EventArgs e)
    {
        SyncImagesToDB(MFBImageInfo.ImageClass.BasicMed);
    }

    protected void DeleteS3Orphans(MFBImageInfo.ImageClass ic)
    {
        LiteralControl lc = new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<iframe src=\"{0}\" width=\"600\" height=\"300\"></iframe>", String.Format(CultureInfo.InvariantCulture, "{0}?dels3orphan=1&r={1}{2}", ResolveClientUrl("AdminImages.aspx"), ic.ToString(), ckPreviewOnly.Checked ? "&preview=1" : string.Empty)));
        plcDBSync.Controls.Add(lc);
    }

    protected void btnDelS3FlightOrphans_Click(object sender, EventArgs e)
    {
        DeleteS3Orphans(MFBImageInfo.ImageClass.Flight);
    }
    protected void btnDelS3AircraftOrphans_Click(object sender, EventArgs e)
    {
        DeleteS3Orphans(MFBImageInfo.ImageClass.Aircraft);
    }
    protected void btnDelS3EndorsementOrphans_Click(object sender, EventArgs e)
    {
        DeleteS3Orphans(MFBImageInfo.ImageClass.Endorsement);
    }

    protected void btnDelS3BasicMedOrphans_Click(object sender, EventArgs e)
    {
        DeleteS3Orphans(MFBImageInfo.ImageClass.BasicMed);
    }

    #endregion

    #region Achievements and Badges
    protected void btnInvalidateUserAchievements_Click(object sender, EventArgs e)
    {
        MyFlightbook.Profile.InvalidateAllAchievements();
    }

    protected void btnAddAirportAchievement_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtAirportAchievementList.Text) && !String.IsNullOrEmpty(txtAirportAchievementName.Text))
        {
            AirportListBadgeData.Add(txtAirportAchievementName.Text, txtAirportAchievementList.Text, txtOverlay.Text, ckBinaryAchievement.Checked, mfbDecEditBronze.IntValue, mfbDecEditSilver.IntValue, mfbDecEditGold.IntValue, mfbDecEditPlatinum.IntValue);
            txtAirportAchievementList.Text = txtAirportAchievementName.Text = string.Empty;
            gvAirportAchievements.DataBind();
        }
    }
    #endregion

    #region Donations

    protected const string szDonationsTemplate = "SELECT *, (Amount - ABS(Fee)) AS Net FROM payments WHERE username LIKE ?user {0} ORDER BY date DESC";
    protected void RefreshDonations()
    {
        sqlDSDonations.SelectParameters.Clear();
        sqlDSDonations.SelectParameters.Add("user", String.IsNullOrEmpty(txtDonationUser.Text) ? "%" : txtDonationUser.Text);

        List<string> l = new List<string>();
        foreach (ListItem li in ckTransactionTypes.Items)
            if (li.Selected)
                l.Add(li.Value);

        sqlDSDonations.SelectCommand = String.Format(CultureInfo.InvariantCulture, szDonationsTemplate, (l.Count == 0) ? string.Empty : String.Format(CultureInfo.InvariantCulture, " AND TransactionType IN ({0}) ", String.Join(",", l.ToArray())));
        gvDonations.DataBind();

        using (IDataReader idr = (IDataReader) sqlDSTotalPayments.Select(DataSourceSelectArguments.Empty))
        {
            IEnumerable<YearlyPayments> rgyp = YearlyPayments.PaymentsByYearAndMonth(idr);
            YearlyPayments.ToTable(plcPayments, rgyp);
        }
    }

    protected void btnFindDonations_Click(object sender, EventArgs e)
    {
        RefreshDonations();
    }

    protected void btnComputeStats_Click(object sender, EventArgs e)
    {
        IEnumerable<Payment> lst = Payment.AllRecords();
        foreach (Payment p in lst)
        {
            try
            {
                System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(p.TransactionNotes);
                p.Fee = Math.Abs(Convert.ToDecimal(nvc["mc_fee"], CultureInfo.InvariantCulture));
                if (p.Type == Payment.TransactionType.Payment || p.Type == Payment.TransactionType.Refund)
                    p.Commit();
            }
            catch (InvalidOperationException)
            {
            }
            catch (MySqlException)
            {
                throw;
            }
        }
        RefreshDonations();
        gvDonations.DataBind();
    }

    protected void gvDonations_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            DataRowView drv = (DataRowView)e.Row.DataItem;
            Label l = (Label)e.Row.FindControl("lblTxNotes");
            Label lTransactionType = (Label)e.Row.FindControl("lblTransactionType");
            lTransactionType.Text = ((Payment.TransactionType)Convert.ToInt32(drv.Row["TransactionType"], CultureInfo.InvariantCulture)).ToString();
            PlaceHolder plc = (PlaceHolder)e.Row.FindControl("plcDecoded");
            l.Text = (string)drv.Row["TransactionData"];

            System.Collections.Specialized.NameValueCollection nvc = HttpUtility.ParseQueryString(l.Text);
            Table t = new Table();
            plc.Controls.Add(t);
            foreach (string szKey in nvc.AllKeys)
            {
                TableRow tr = new TableRow();
                t.Rows.Add(tr);

                TableCell tc = new TableCell();
                tr.Cells.Add(tc);
                tc.Text = szKey;
                tc.Style["font-weight"] = "bold";

                tc = new TableCell();
                tr.Cells.Add(tc);
                tc.Text = nvc[szKey];
            }
        }
    }

    protected void btnResetGratuities_Click(object sender, EventArgs e)
    {
        EarnedGrauity.UpdateEarnedGratuities(string.Empty, ckResetGratuityReminders.Checked);
    }

    protected void btnEnterTestTransaction_Click(object sender, EventArgs e)
    {
        Page.Validate("valTestTransaction");
        if (Page.IsValid)
        {
            Payment.TransactionType tt = (Payment.TransactionType)Convert.ToInt32(cmbTestTransactionType.SelectedValue, CultureInfo.InvariantCulture);
            decimal amount = decTestTransactionAmount.Value;
            switch (tt)
            {
                case Payment.TransactionType.Payment:
                    amount = Math.Abs(amount);
                    break;
                case Payment.TransactionType.Refund:
                    amount = -1 * Math.Abs(amount);
                    break;
                default:
                    break;
            }
            Payment p = new Payment(dateTestTransaction.Date, txtTestTransactionUsername.Text, amount, decTestTransactionFee.Value, tt , txtTestTransactionNotes.Text, "Manual Entry", string.Empty);
            p.Commit();
            EarnedGrauity.UpdateEarnedGratuities(txtTestTransactionUsername.Text, true);
            txtTestTransactionUsername.Text = txtTestTransactionNotes.Text = string.Empty;
            decTestTransactionAmount.Value = decTestTransactionFee.Value = 0;
            RefreshDonations();
        }
    }

    protected void gvDonations_PageIndexChanging(object sender, GridViewPageEventArgs e)
    {
        if (e != null)
        {
            gvDonations.PageIndex = e.NewPageIndex;
            RefreshDonations();
        }
    }

    protected class PeriodPaymentStat
    {
        public double Net { get; set; }
        public double Gross { get; set; }
        public double Fee { get; set; }
        public double YOYPercent { get; set; }
        public double YOYGross { get; set; }

        public void AddToContainer(Control c)
        {
            if (Net == 0)
                return;
            if (c == null)
                throw new ArgumentNullException("c");
            Label l = new Label();
            c.Controls.Add(l);
            l.Text = Net.ToString("C", CultureInfo.CurrentCulture);

            l.ToolTip = String.Format(CultureInfo.CurrentCulture, "Gross: {0:C}, Fee: {1:C}", Gross, Fee);

            if (YOYGross != 0)
            {
                l.ToolTip += String.Format(CultureInfo.CurrentCulture, ".\r\nYOY: {0:C} ({1:#,##0.0}%)", YOYGross, YOYPercent * 100);
                l.ForeColor = (YOYGross > 0) ? System.Drawing.Color.Green : System.Drawing.Color.Red;
            }
        }
    }

    protected class YearlyPayments : IComparable
    {
        public int Year { get; set; }
        public PeriodPaymentStat[] MonthlyPayments { get; set; }
        public PeriodPaymentStat AnnualPayment { get; set; }

        public YearlyPayments(int year)
        {
            Year = year;
            MonthlyPayments = new PeriodPaymentStat[12];
            for (int i = 0; i < 12; i++)
                MonthlyPayments[i] = new PeriodPaymentStat();
            AnnualPayment = new PeriodPaymentStat();
        }

        public static IEnumerable<YearlyPayments> PaymentsByYearAndMonth(IDataReader idr)
        {
            Dictionary<int, YearlyPayments> d = new Dictionary<int, YearlyPayments>();

            if (idr == null)
                throw new ArgumentNullException("idr");
            while (idr.Read())
            {
                string MonthPeriod = idr["MonthPeriod"].ToString();
                string[] rgsz = MonthPeriod.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (rgsz.Length != 2)
                    throw new MyFlightbookValidationException("Bogus month/year in donations");
                int year = Convert.ToInt32(rgsz[0], CultureInfo.InvariantCulture);
                int month = Convert.ToInt32(rgsz[1], CultureInfo.InvariantCulture);
                if (!d.ContainsKey(year))
                    d[year] = new YearlyPayments(year);
                if (month <= 0 || month > 12)
                    throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Invalid month in donations: {0}", month));
                d[year].MonthlyPayments[month - 1] = new PeriodPaymentStat() {
                    Net = Convert.ToDouble(idr["NetPaypal"], CultureInfo.InvariantCulture),
                    Gross = Convert.ToDouble(idr["Gross"], CultureInfo.InvariantCulture),
                    Fee = Convert.ToDouble(idr["Fee"], CultureInfo.InvariantCulture)
                };
            }

            List<YearlyPayments> lst = new List<YearlyPayments>(d.Values);
            lst.Sort();

            // Now go through and compute stats.
            for (int i = 1; i < lst.Count; i++)
            {
                if (lst[i - 1].Year != lst[i].Year - 1) // ignore non-congiguous years (shouldn't happen)
                    continue;

                double annual = 0;
                YearlyPayments lastYear = lst[i - 1];
                YearlyPayments thisYear = lst[i];

                for (int j = 0; j < 12; j++)
                {
                    PeriodPaymentStat lastYearMonth = lastYear.MonthlyPayments[j];
                    PeriodPaymentStat thisYearMonth = thisYear.MonthlyPayments[j];

                    annual += thisYearMonth.Net;
                    thisYearMonth.YOYGross = thisYearMonth.Net - lastYearMonth.Net;
                    thisYearMonth.YOYPercent = (lastYearMonth.Net == 0) ? 0 : thisYearMonth.YOYGross / lastYearMonth.Net;
                }

                thisYear.AnnualPayment.Net = annual;
                thisYear.AnnualPayment.YOYGross = thisYear.AnnualPayment.Net - lastYear.AnnualPayment.Net;
                thisYear.AnnualPayment.YOYPercent = (lastYear.AnnualPayment.Net == 0) ? 0 : thisYear.AnnualPayment.YOYGross / lastYear.AnnualPayment.Net;
            }

            return lst;
        }

        public int CompareTo(object obj)
        {
            return Year.CompareTo(((YearlyPayments)obj).Year);
        }

        private static TableCell[] NewRow()
        {
            TableCell[] rgc = new TableCell[14];
            for (int i = 0; i < rgc.Length; i++)
                rgc[i] = new TableCell();
            return rgc;
        }

        public static void ToTable(Control c, IEnumerable<YearlyPayments> rgyp)
        {
            if (rgyp == null)
                return;
            if (c == null)
                throw new ArgumentNullException("c");
            Table t = new Table();
            c.Controls.Add(t);
            t.CellPadding = 3;

            TableRow tr = new TableRow();
            t.Rows.Add(tr);
            tr.TableSection = TableRowSection.TableHeader;
            tr.Cells.AddRange(NewRow());

            tr.Cells[0].Text = Resources.LocalizedText.ChartTotalsGroupYear;
            for (int i = 1; i < 13; i++)
                tr.Cells[i].Text = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedMonthNames[i - 1];
            tr.Cells[13].Text = Resources.LocalizedText.ChartDataTotal;
            tr.Font.Bold = true;

            foreach(YearlyPayments yp in rgyp)
            {
                tr = new TableRow();
                t.Rows.Add(tr);
                tr.Cells.AddRange(NewRow());
                tr.Cells[0].Text = yp.Year.ToString(CultureInfo.CurrentCulture);
                tr.Cells[0].Font.Bold = true;
                for (int i = 1; i < 13; i++)
                    yp.MonthlyPayments[i - 1].AddToContainer(tr.Cells[i]);
                yp.AnnualPayment.AddToContainer(tr.Cells[13]);
                tr.Cells[13].Font.Bold = true;
            }
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
        List<LogbookEntry> lst = new List<LogbookEntry>();
        List<LogbookEntry> lstAutoFix = new List<LogbookEntry>();

        List<int> lstIDs = new List<int>();
        DBHelper dbh = new DBHelper("SELECT idFlight FROM Flights WHERE signatureState<>0 ORDER BY Username ASC, Date DESC");
        dbh.CommandArgs.Timeout = 300;  // up to 300 seconds.
        dbh.ReadRows((comm) => { }, (dr) => { lstIDs.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });

        int cTotalSigned = lstIDs.Count;

        lstIDs.ForEach((idFlight) =>
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
                        lst.Add(le);
                }
            });

        gvInvalidSignatures.DataSource = ViewState["InvalidSigs"] = lst;
        gvInvalidSignatures.DataBind();

        lblSigResults.Text = String.Format("Found {0} signed flights, {1} appear to have problems, {2} were autofixed (capitalization or leading/trailing whitespace)", cTotalSigned, lst.Count, lstAutoFix.Count);
    }

    protected void btnFixInvalidSigState_Click(object sender, EventArgs e)
    {
        List<LogbookEntry> lst = (List<LogbookEntry>) gvInvalidSignatures.DataSource;
        if (lst == null)
            return;

        lst.ForEach((le) =>
            {
                if (le.IsValidSignature())
                    le.FCommit(false, true);
            });

        UpdateInvalidSigs();
    }

    protected void gvInvalidSignatures_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        int idFlight = e.CommandArgument.ToString().SafeParseInt(LogbookEntry.idFlightNone);
        if (idFlight != LogbookEntry.idFlightNone)
        {
            LogbookEntry le = new LogbookEntry();
            le.FLoadFromDB(idFlight, string.Empty, LogbookEntry.LoadTelemetryOption.None, true);
            if (le.AdminSignatureSanityFix(e.CommandName.CompareTo("ForceValidity") == 0))
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

    protected void FlushCache()
    {
        foreach (System.Collections.DictionaryEntry entry in HttpRuntime.Cache)
            HttpRuntime.Cache.Remove((string)entry.Key);
    }

    protected void btnFlushCache_Click(object sender, EventArgs e)
    {
        FlushCache();
    }
    #endregion
}
