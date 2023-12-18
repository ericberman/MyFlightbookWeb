using MyFlightbook.Printing;
using System;
using System.Globalization;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Instruction
{
    public partial class StudentLogbook : Page
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
                hdnStudent.Value = Page.User.Identity.Name; //default
                string szStudent = util.GetStringParam(Request, "student");
                CFIStudentMap sm = new CFIStudentMap(Page.User.Identity.Name);
                InstructorStudent student = CFIStudentMap.GetInstructorStudent(sm.Students, szStudent);
                if (student == null)
                    lblErr.Text = Resources.SignOff.ViewStudentNoSuchStudent;
                else
                {
                    if (!student.CanViewLogbook)
                        lblErr.Text = Master.Title = Resources.SignOff.ViewStudentLogbookUnauthorized;
                    else
                    {
                        // If we're here, we are authorized
                        lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.SignOff.ViewStudentLogbookHeader, HttpUtility.HtmlEncode(student.UserFullName));
                        hdnStudent.Value = student.UserName;
                        Restriction = new FlightQuery(student.UserName);

                        if (mfbLogbook1.CanResignValidFlights = student.CanAddLogbook)
                        {
                            mfbEditFlight.FlightUser = student.UserName;
                            mfbEditFlight.SetUpNewOrEdit(LogbookEntryBase.idFlightNew);
                        }
                        else
                            apcNewFlight.Container.Style["display"] = "none";

                        if (!String.IsNullOrEmpty(hdnStudent.Value))
                            UpdateForUser(hdnStudent.Value);

                        mfbSearchForm.Username = student.UserName;
                        ResolvePrintLink();
                    }
                }

                pnlLogbook.Visible = (lblErr.Text.Length == 0);
            }

            if (pnlLogbook.Visible && mfbChartTotals.Visible)
                mfbChartTotals.HistogramManager = LogbookEntryDisplay.GetHistogramManager(mfbLogbook1.Data, hdnStudent.Value);   // do this every time, since charttotals doesn't persist its data.
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
            mfbChartTotals.HistogramManager = LogbookEntryDisplay.GetHistogramManager(mfbLogbook1.Data, hdnStudent.Value);
            mfbChartTotals.Refresh();
            ResolvePrintLink();
        }

        public void ClearForm(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void ShowResults(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClickedEventArgs fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
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
            mfbChartTotals.HistogramManager = LogbookEntryDisplay.GetHistogramManager(mfbLogbook1.Data, hdnStudent.Value);
            mfbChartTotals.Refresh();
        }

        protected void mfbEditFlight_FlightWillBeSaved(object sender, LogbookEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

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
            mfbEditFlight.SetUpNewOrEdit(LogbookEntryBase.idFlightNew);
            AccordionCtrl.SelectedIndex = -1;
            mfbLogbook1.RefreshData();
        }

        protected void ResolvePrintLink()
        {
            if (!ckEndorsements.Checked)
                ckIncludeEndorsementImages.Checked = false;

            lnkPrintView.NavigateUrl = PrintingOptions.PermaLink(Restriction, new PrintingOptions()
            {
                Sections = new PrintingSections()
                {
                    Endorsements = ckEndorsements.Checked ? (ckIncludeEndorsementImages.Checked ? PrintingSections.EndorsementsLevel.DigitalAndPhotos : PrintingSections.EndorsementsLevel.DigitalOnly) : PrintingSections.EndorsementsLevel.None,
                    IncludeCoverPage = ckIncludeCoverSheet.Checked,
                    IncludeFlights = true,
                    IncludeTotals = ckTotals.Checked
                }
            }, Request.Url.Host, Request.Url.Scheme, (nvc) =>
            {
                nvc["u"] = hdnStudent.Value;
            }).ToString();
        }

        protected void lnkDownloadCSV_Click(object sender, EventArgs e)
        {
            mfbDownload1.User = hdnStudent.Value;
            mfbDownload1.UpdateData();

            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, Profile.GetUser(hdnStudent.Value).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", RegexUtility.SafeFileChars.Replace(szFilename, string.Empty));
            Response.AddHeader("Content-Disposition", szDisposition);
            mfbDownload1.ToStream(Response.OutputStream);
            Response.End();
        }
    }
}