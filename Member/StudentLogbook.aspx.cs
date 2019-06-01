using MyFlightbook;
using MyFlightbook.Instruction;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_StudentLogbook : System.Web.UI.Page
{
    private const string keyVSRestriction = "vsCurrentRestriction";
    protected FlightQuery Restriction
    {
        get
        {
            if (ViewState[keyVSRestriction] == null)
                ViewState[keyVSRestriction] = new FlightQuery(Page.User.Identity.Name);
            return (FlightQuery)ViewState[keyVSRestriction];
        }
        set
        {
            ViewState[keyVSRestriction] = value;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabTraining;

        if (!IsPostBack)
        {
            string szStudent = util.GetStringParam(Request, "student");
            CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
            InstructorStudent student = sm.GetInstructorStudent(sm.Students, szStudent);
            if (student == null)
                lblErr.Text = Resources.SignOff.ViewStudentNoSuchStudent;
            else
            {
                if (!student.CanViewLogbook)
                    lblErr.Text = Master.Title = Resources.SignOff.ViewStudentLogbookUnauthorized;
                else
                {
                    // If we're here, we are authorized
                    lblHeader.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.SignOff.ViewStudentLogbookHeader, student.UserFullName);
                    hdnStudent.Value = student.UserName;
                    Restriction = new FlightQuery(student.UserName);

                    if (!String.IsNullOrEmpty(hdnStudent.Value))
                        UpdateForUser(hdnStudent.Value);

                    if (student.CanAddLogbook)
                        mfbEditFlight.SetUpNewOrEdit(LogbookEntry.idFlightNew);
                    else
                        apcNewFlight.Visible = false;
                }
            }

            pnlLogbook.Visible = (lblErr.Text.Length == 0);
        }

        if (pnlLogbook.Visible && mfbChartTotals.Visible)
            mfbChartTotals.SourceData = mfbLogbook1.Data;   // do this every time, since charttotals doesn't persist its data.
    }

    protected void UpdateForUser(string szUser)
    {
        FlightQuery r = Restriction;
        mfbTotalSummary.Username = mfbCurrency1.UserName = mfbLogbook1.User = szUser;
        mfbTotalSummary.CustomRestriction = mfbLogbook1.Restriction = r;
        mfbCurrency1.RefreshCurrencyTable();
        bool fRestrictionIsDefault = r.IsDefault;
        mfbQueryDescriptor.DataSource = fRestrictionIsDefault ? null : r;
        mfbQueryDescriptor.DataBind();
        apcFilter.LabelControl.Font.Bold = !fRestrictionIsDefault;
        apcFilter.IsEnhanced = !fRestrictionIsDefault;
        pnlFilter.Visible = !fRestrictionIsDefault;
        mfbLogbook1.RefreshData();
    }

    protected void UpdateQuery()
    {
        Restriction = mfbSearchForm.Restriction;
        UpdateForUser(hdnStudent.Value);
        AccordionCtrl.SelectedIndex = -1;
        apcAnalysis.LazyLoad = true;
        mfbChartTotals.Visible = false;
        int idx = mfbAccordionProxyExtender.IndexForProxyID(apcAnalysis.ID);
        if (idx == AccordionCtrl.SelectedIndex)
            AccordionCtrl.SelectedIndex = -1;
        mfbAccordionProxyExtender.SetJavascriptForControl(apcAnalysis, false, idx);
    }

    public void ClearForm(object sender, EventArgs e)
    {
        UpdateQuery();
    }

    protected void ShowResults(object sender, EventArgs e)
    {
        UpdateQuery();
    }

    protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");
        mfbSearchForm.Restriction = Restriction.ClearRestriction(fic.FilterItem);
        UpdateQuery();
    }

    protected void apcAnalysis_ControlClicked(object sender, EventArgs e)
    {
        apcAnalysis.LazyLoad = false;
        int idx = mfbAccordionProxyExtender.IndexForProxyID(apcAnalysis.ID);
        mfbAccordionProxyExtender.SetJavascriptForControl(apcAnalysis, true, idx);
        AccordionCtrl.SelectedIndex = idx;

        mfbChartTotals.Visible = true;
        mfbChartTotals.Refresh(mfbLogbook1.Data);
    }

    protected void mfbEditFlight_FlightWillBeSaved(object sender, LogbookEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        LogbookEntry le = e.Flight;
        if (le == null)
            throw new MyFlightbookValidationException("No flight to save!");

        // Fix up the flight for the user
        le.User = hdnStudent.Value;

        // ensure that the aircraft is in their profile
        UserAircraft ua = new UserAircraft(le.User);
        if (ua.GetUserAircraftByID(le.AircraftID) == null)
            ua.FAddAircraftForUser(new Aircraft(le.AircraftID));
    }

    protected void mfbEditFlight_FlightUpdated(object sender, LogbookEventArgs e)
    {
        mfbEditFlight.SetUpNewOrEdit(LogbookEntry.idFlightNew);
        AccordionCtrl.SelectedIndex = -1;
        mfbLogbook1.RefreshData();
    }
}