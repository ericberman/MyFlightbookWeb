using MyFlightbook;
using MyFlightbook.Airports;
using MyFlightbook.Clubs;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Instruction;
using MyFlightbook.Telemetry;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_ClubManage : System.Web.UI.Page
{
    /// <summary>
    /// The current club being viewed.  Actually delegates to the ViewClub control, since this saves it in its viewstate.
    /// </summary>
    protected Club CurrentClub
    {
        get { return ViewClub1.ActiveClub; }
        set { ViewClub1.ActiveClub = value; }
    }

    protected bool IsManager
    {
        get
        {
            ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);
            return cm != null && cm.IsManager;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.actMyClubs;

        try
        {
            if (Request.PathInfo.Length > 0 && Request.PathInfo.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsPostBack)
                {
                    CurrentClub = Club.ClubWithID(Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture));
                    if (CurrentClub == null)
                        throw new MyFlightbookException(Resources.Club.errNoSuchClub);

                    Master.Title = CurrentClub.Name;
                    lblClubHeader.Text = CurrentClub.Name;

                    ClubMember cm = CurrentClub.GetMember(Page.User.Identity.Name);

                    if (!IsManager)
                        throw new MyFlightbookException(Resources.Club.errNotAuthorizedToManage);

                    gvMembers.DataSource = CurrentClub.Members;
                    gvMembers.DataBind();
                    vcEdit.ShowDelete = (cm.RoleInClub == ClubMember.ClubMemberRole.Owner);

                    dateStart.Date = CurrentClub.CreationDate;
                    dateEnd.DefaultDate = dateEnd.Date = DateTime.Now;
                    RefreshAircraft();

                    lblManageheader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.LabelManageThisClub, CurrentClub.Name);
                    lnkReturnToClub.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/ClubDetails.aspx/{0}", CurrentClub.ID);

                    vcEdit.ActiveClub = CurrentClub;    // do this at the end so that all of the above (like ShowDelete) are captured
                }
            }
            else
                throw new MyFlightbookException(Resources.Club.errNoClubSpecified);
        }
        catch (MyFlightbookException ex)
        {
            lblErr.Text = ex.Message;
            lnkReturnToClub.Visible = tabManage.Visible = false;
        }
    }

    protected void StopManaging()
    {
        tabManage.ActiveTabIndex = 0;
    }

    protected void vcEdit_ClubChanged(object sender, EventArgs e)
    {
        CurrentClub = vcEdit.ActiveClub;
        StopManaging();
    }
    protected void vcEdit_ClubChangeCanceled(object sender, EventArgs e)
    {
        vcEdit.ActiveClub = CurrentClub;
        StopManaging();
    }

    protected void btnDeleteClub_Click(object sender, EventArgs e)
    {
        CurrentClub.FDelete();
        Response.Redirect("~/Default.aspx");
    }

    #region aircraft management
    protected void refreshAdminAircraft(int editIndex)
    {
        gvAircraft.EditIndex = editIndex;
        ClubAircraft.RefreshClubAircraftTimes(CurrentClub.ID, CurrentClub.MemberAircraft);  // update the "max" times
        gvAircraft.DataSource = CurrentClub.MemberAircraft;
        gvAircraft.DataBind();
    }

    protected void gvAircraft_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        refreshAdminAircraft(-1);
    }

    protected void gvAircraft_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (e != null)
            refreshAdminAircraft(e.NewEditIndex);
    }

    protected void btnAddAircraft_Click(object sender, EventArgs e)
    {
        if (String.IsNullOrEmpty(cmbAircraftToAdd.SelectedValue))
            return;

        ClubAircraft ca = new ClubAircraft()
        {
            AircraftID = Convert.ToInt32(cmbAircraftToAdd.SelectedValue, CultureInfo.InvariantCulture),
            ClubDescription = txtDescription.FixedHtml,
            ClubID = CurrentClub.ID
        };
        if (!ca.FSaveToClub())
            lblManageAircraftError.Text = ca.LastError;
        else
        {
            txtDescription.Text = string.Empty;
            CurrentClub.InvalidateMemberAircraft(); // force a reload
            RefreshAircraft();
        }
    }

    protected void RefreshAircraft()
    {
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        List<Aircraft> lst = new List<Aircraft>(ua.GetAircraftForUser());
        lst.RemoveAll(ac => ac.IsAnonymous || CurrentClub.MemberAircraft.FirstOrDefault(ca => ca.AircraftID == ac.AircraftID) != null); // remove all anonymous aircraft, or aircraft that are already in the list
        cmbAircraftToAdd.DataSource = lst;
        cmbAircraftToAdd.DataBind();

        gvAircraft.DataSource = CurrentClub.MemberAircraft;
        gvAircraft.DataBind();
    }

    protected void gvAircraft_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        ClubAircraft ca = CurrentClub.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == Convert.ToInt32(e.Keys[0]));
        if (ca != null)
        {
            GridViewRow row = ((GridView) sender).Rows[e.RowIndex];
            ca.ClubDescription = ((Controls_mfbHtmlEdit)row.FindControl("txtDescription")).FixedHtml;
            ca.HighWater = ((Controls_mfbDecimalEdit)row.FindControl("decEditTime")).Value;

            if (ca.FSaveToClub())
            {
                gvAircraft.EditIndex = -1;
                RefreshAircraft();
            }
            else
                lblManageAircraftError.Text = ca.LastError;
        }
    }

    protected void gvAircraft_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if ((e.Row.RowState & DataControlRowState.Edit) == DataControlRowState.Edit)
        {
            ClubAircraft ca = (ClubAircraft) e.Row.DataItem;
            Controls_mfbDecimalEdit decHighWater = (Controls_mfbDecimalEdit)e.Row.FindControl("decEditTime");
            decHighWater.Value = ca.HighWater;
            Label lnkHobbs = (Label)e.Row.FindControl("lnkCopyHobbs");
            Label lnkTach = (Label)e.Row.FindControl("lnkCopyTach");
            e.Row.FindControl("pnlHighHobbs").Visible = ca.HighestRecordedHobbs > 0;
            e.Row.FindControl("pnlHighTach").Visible = ca.HighestRecordedTach > 0;
            lnkHobbs.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubAircraftHighestHobbs, ca.HighestRecordedHobbs);
            lnkTach.Text = String.Format(CultureInfo.CurrentCulture, Resources.Club.ClubAircraftHighestTach, ca.HighestRecordedTach);
            ((Image)e.Row.FindControl("imgXFillHobbs")).Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:$find('{0}').set_text('{1}');", decHighWater.EditBoxWE.ClientID, ca.HighestRecordedHobbs.ToString(CultureInfo.CurrentCulture));
            ((Image)e.Row.FindControl("imgXFillTach")).Attributes["onclick"] = String.Format(CultureInfo.InvariantCulture, "javascript:$find('{0}').set_text('{1}');", decHighWater.EditBoxWE.ClientID, ca.HighestRecordedTach.ToString(CultureInfo.CurrentCulture));
        }
    }

    protected void gvAircraft_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName.CompareTo("_Delete") == 0)
        {
            ClubAircraft ca = CurrentClub.MemberAircraft.FirstOrDefault(ac => ac.AircraftID == Convert.ToInt32(e.CommandArgument));
            if (ca != null)
            {
                if (!ca.FDeleteFromClub())
                    lblManageAircraftError.Text = ca.LastError;
                else
                {
                    CurrentClub.InvalidateMemberAircraft();
                    RefreshAircraft();
                }
            }
        }
    }
    #endregion

    #region Member Management
    protected void gvMembers_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            RadioButtonList rbl = (RadioButtonList)e.Row.FindControl("rblRole");
            if (rbl != null)
            {
                ClubMember cm = (ClubMember)e.Row.DataItem;
                rbl.SelectedValue = cm.RoleInClub.ToString();
            }
        }
    }
    protected void gvMembers_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        ClubMember cm = CurrentClub.Members.FirstOrDefault(pf => pf.UserName.CompareTo(e.Keys[0].ToString()) == 0);
        if (cm != null)
        {
            RadioButtonList rbl = (RadioButtonList)gvMembers.Rows[e.RowIndex].FindControl("rblRole");
            ClubMember.ClubMemberRole requestedRole = (ClubMember.ClubMemberRole)Enum.Parse(typeof(ClubMember.ClubMemberRole), rbl.SelectedValue);

            bool fResult = true;
            try
            {
                if (requestedRole != cm.RoleInClub) // check to see if anything changed
                {
                    if (requestedRole == ClubMember.ClubMemberRole.Owner) // that's fine, but we need to un-make any other creators
                    {
                        ClubMember cmOldOwner = CurrentClub.Members.FirstOrDefault(pf => pf.RoleInClub == ClubMember.ClubMemberRole.Owner);
                        if (cmOldOwner != null) //should never happen!
                        {
                            cmOldOwner.RoleInClub = ClubMember.ClubMemberRole.Admin;
                            if (!cmOldOwner.FCommitClubMembership())
                                throw new MyFlightbookException(cmOldOwner.LastError);
                        }
                    }
                    else if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)    // if we're not requesting creator role, but this person currently is creator, then we are demoting - that's a no-no
                        throw new MyFlightbookException(Resources.Club.errCantDemoteOwner);

                    cm.RoleInClub = requestedRole;
                    if (!cm.FCommitClubMembership())
                        throw new MyFlightbookException(cm.LastError);
                }
            }
            catch (MyFlightbookException ex)
            {
                lblManageMemberError.Text = ex.Message;
                fResult = false;
            }

            if (fResult)
                refreshAdminMembers(-1);
        }
    }

    protected void refreshAdminMembers(int editRow)
    {
        gvMembers.EditIndex = editRow;
        gvMembers.DataSource = CurrentClub.Members;
        gvMembers.DataBind();
    }

    protected void gvMembers_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
    {
        refreshAdminMembers(-1);
    }

    protected void gvMembers_RowEditing(object sender, GridViewEditEventArgs e)
    {
        if (e != null)
            refreshAdminMembers(e.NewEditIndex);
    }

    protected void gvMembers_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e.CommandName.CompareTo("_Delete") == 0)
        {
            ClubMember cm = CurrentClub.Members.FirstOrDefault(pf => pf.UserName.CompareTo(e.CommandArgument) == 0);
            if (cm != null)
            {
                if (cm.RoleInClub == ClubMember.ClubMemberRole.Owner)
                    lblManageMemberError.Text = Resources.Club.errCannotDeleteOwner;
                else
                {
                    if (!cm.FDeleteClubMembership())
                        lblManageMemberError.Text = cm.LastError;
                    else
                    {
                        CurrentClub.InvalidateMembers();
                        gvMembers.DataSource = CurrentClub.Members;
                        gvMembers.DataBind();
                    }
                }
            }
        }
    }

    protected void btnAddMember_Click(object sender, EventArgs e)
    {
        if (!Page.IsValid)
            return;

        if (CurrentClub.Status == Club.ClubStatus.Inactive)
        {
            lblErr.Text = Branding.ReBrand(Resources.Club.errClubInactive);
            return;
        }
        if (CurrentClub.Status == Club.ClubStatus.Expired)
        {
            lblErr.Text = Branding.ReBrand(Resources.Club.errClubPromoExpired);
            return;
        }

        try
        {
            new CFIStudentMapRequest(Page.User.Identity.Name, txtMemberEmail.Text, CFIStudentMapRequest.RoleType.RoleInviteJoinClub, CurrentClub).Send();
            lblAddMemberSuccess.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileRequestHasBeenSent, txtMemberEmail.Text);
            lblAddMemberSuccess.CssClass = "success";
            txtMemberEmail.Text = "";
        }
        catch (MyFlightbookException ex)
        {
            lblAddMemberSuccess.Text = ex.Message;
            lblAddMemberSuccess.CssClass = "error";
        }
    }
    #endregion

    #region Reporting
    protected string FullName(string szFirst, string szLast, string szEmail)
    {
        Profile pf = new Profile()
        {
            FirstName = szFirst,
            LastName = szLast,
            Email = szEmail
        };
        return pf.UserFullName;
    }

    protected string ValueString(object o, decimal offSet = 0.0M)
    {
        if (o is DateTime)
        {
            DateTime dt = (DateTime)o;
            if (dt != null && dt.HasValue())
                return dt.ToShortDateString();
        }
        else if (o is decimal)
        {
            decimal d = (decimal)o;
            if (d > 0)
                return (d + offSet).ToString("#,##0.0#", CultureInfo.CurrentCulture);
        }
        return string.Empty;
    }

    protected string CSSForValue(decimal current, decimal due, int hoursWarning, int offSet = 0)
    {
        if (due > 0)
            due += offSet;

        if (current > due)
            return "currencyexpired";
        else if (current + hoursWarning > due)
            return "currencynearlydue";
        return "currencyok";
    }

    protected string CSSForDate(DateTime dt)
    {
        if (DateTime.Now.CompareTo(dt) > 0)
            return "currencyexpired";
        else if (DateTime.Now.AddDays(30).CompareTo(dt) > 0)
            return "currencynearlydue";
        return "currencyok";
    }

    protected string FormattedUTCDate(object o)
    {
        if (o == null)
            return string.Empty;
        if (o is DateTime)
            return ((DateTime)o).UTCFormattedStringOrEmpty(false);
        return string.Empty;
    }

    protected string MonthForDate(DateTime dt)
    {
        return String.Format(CultureInfo.InvariantCulture, "{0}-{1} ({2})", dt.Year, dt.Month.ToString("00", CultureInfo.InvariantCulture), dt.ToString("MMM", CultureInfo.CurrentCulture));
    }

    protected void btnUpdate_Click(object sender, EventArgs e)
    {
        sqlDSReports.SelectParameters.Clear();
        sqlDSReports.SelectParameters.Add(new Parameter("idclub", System.Data.DbType.Int32, CurrentClub.ID.ToString(CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("startDate", System.Data.DbType.Date, dateStart.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("endDate", System.Data.DbType.Date, dateEnd.Date.HasValue() ? dateEnd.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        gvClubReports.DataSourceID = sqlDSReports.ID;
        gvClubReports.DataBind();
        btnDownload.Visible = true;
    }
    protected void btnDownload_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, System.Text.RegularExpressions.Regex.Replace(CurrentClub.Name, "[^0-9a-zA-Z-]", string.Empty), DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", string.Empty));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write(gvClubReports.CSVFromData());
        Response.End();
    }

    protected void lnkViewKML_Click(object sender, EventArgs e)
    {
        DataSourceType dst = DataSourceType.DataSourceTypeFromFileType(DataSourceType.FileType.KML);
        Response.Clear();
        Response.ContentType = dst.Mimetype;
        Response.AddHeader("Content-Disposition", String.Format(CultureInfo.CurrentCulture, "attachment;filename={0}-AllFlights.{1}", Branding.CurrentBrand.AppName, dst.DefaultExtension));

        // Get the flight IDs that contribute to the report
        sqlDSReports.SelectParameters.Clear();
        sqlDSReports.SelectParameters.Add(new Parameter("idclub", System.Data.DbType.Int32, CurrentClub.ID.ToString(CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("startDate", System.Data.DbType.Date, dateStart.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
        sqlDSReports.SelectParameters.Add(new Parameter("endDate", System.Data.DbType.Date, dateEnd.Date.HasValue() ? dateEnd.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

        List<int> lstIds = new List<int>();
        using (DataView dv = (DataView) sqlDSReports.Select(DataSourceSelectArguments.Empty))
        {
            foreach (DataRowView dr in dv)
                lstIds.Add(Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture));
        }

        string szErr;
        VisitedAirport.AllFlightsAsKML(new FlightQuery(), Response.OutputStream, out szErr, lstIds);
        if (String.IsNullOrEmpty(szErr))
            lblErr.Text = szErr;
        Response.End();
    }

    protected void btnUpdateMaintenance_Click(object sender, EventArgs e)
    {
        // flush the cache to pick up any aircraft changes
        CurrentClub = Club.ClubWithID(CurrentClub.ID, true);
        gvMaintenance.DataSource = CurrentClub.MemberAircraft;
        gvMaintenance.DataBind();
        btnDownloadMaintenance.Visible = true;
    }

    protected void btnDownloadMaintenance_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-Maintenance-{2}", Branding.CurrentBrand.AppName, System.Text.RegularExpressions.Regex.Replace(CurrentClub.Name, "[^0-9a-zA-Z-]", string.Empty), DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "inline;filename={0}.csv", System.Text.RegularExpressions.Regex.Replace(szFilename, "[^0-9a-zA-Z-]", string.Empty));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write(gvMaintenance.CSVFromData());
        Response.End();
    }
    #endregion

    protected string CSSForItem(CurrencyState cs)
    {
        switch (cs)
        {
            case CurrencyState.GettingClose:
                return "currencynearlydue";
            case CurrencyState.NotCurrent:
                return "currencyexpired";
            case CurrencyState.OK:
                return "currencyok";
            case CurrencyState.NoDate:
                return "currencynodate";
        }
        return string.Empty;
    }

    protected void btnInsuranceReport_Click(object sender, EventArgs e)
    {
        gvInsuranceReport.DataSource = ClubInsuranceReportItem.ReportForClub(CurrentClub.ID, Convert.ToInt32(cmbMonthsInsurance.SelectedValue, CultureInfo.InvariantCulture));
        gvInsuranceReport.DataBind();
    }

    protected void gvInsuranceReport_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            ClubInsuranceReportItem ciri = (ClubInsuranceReportItem)e.Row.DataItem;
            GridView gvTimeInAircraft = (GridView) e.Row.FindControl("gvAircraftTime");
            gvTimeInAircraft.DataSource = ciri.TotalsByClubAircraft;
            gvTimeInAircraft.DataBind();
            Repeater rptPilotStatus = (Repeater)e.Row.FindControl("rptPilotStatus");
            rptPilotStatus.DataSource = ciri.PilotStatusItems;
            rptPilotStatus.DataBind();
        }
    }
}