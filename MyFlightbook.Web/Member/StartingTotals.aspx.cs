using MyFlightbook;
using MyFlightbook.StartingFlights;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web.UI;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class StartingTotals : Web.WizardPage.MFBWizardPage
    {
        Collection<StartingFlight> m_lstStartingFlights;
        const string szKeyStartingFlights = "startingFlightsViewState";

        protected enum StartingFlightColumn { PIC, SIC, CFI, Total };

        protected RepresentativeAircraft.RepresentativeTypeMode CurrentMode
        {
            get
            {
                if (rbSimple.Checked)
                    return RepresentativeAircraft.RepresentativeTypeMode.CatClassType;
                else if (rbMedium.Checked)
                    return RepresentativeAircraft.RepresentativeTypeMode.CatClassCapabilities;
                else
                    return RepresentativeAircraft.RepresentativeTypeMode.ByModel;
            }
            set
            {
                rbSimple.Checked = (value == RepresentativeAircraft.RepresentativeTypeMode.CatClassType);
                rbMedium.Checked = (value == RepresentativeAircraft.RepresentativeTypeMode.CatClassCapabilities);
                rbModels.Checked = (value == RepresentativeAircraft.RepresentativeTypeMode.ByModel);
            }
        }

        protected Collection<StartingFlight> StartingFlights
        {
            get
            {
                if (m_lstStartingFlights != null)
                    return m_lstStartingFlights;
                else
                    return (Collection<StartingFlight>)ViewState[szKeyStartingFlights];
            }
        }

        protected void InitStartingFlights(Collection<StartingFlight> c)
        {
            ViewState[szKeyStartingFlights] = m_lstStartingFlights = c;
        }

        #region To/From Form
        protected static string ColumnTitleFromColumn(StartingFlightColumn sfc)
        {
            switch (sfc)
            {
                case StartingFlightColumn.CFI:
                    return Resources.LogbookEntry.FieldCFI;
                case StartingFlightColumn.SIC:
                    return Resources.LogbookEntry.FieldSIC;
                case StartingFlightColumn.PIC:
                    return Resources.LogbookEntry.FieldPIC;
                case StartingFlightColumn.Total:
                    return Resources.LogbookEntry.FieldTotal;
            }
            return "";
        }

        /// <summary>
        /// Gets the value from the starting flight in the specified column
        /// </summary>
        /// <param name="sfc">the column to read</param>
        /// <param name="sf">The starting flight</param>
        /// <returns>The appropriate column value</returns>
        protected static Decimal GetValueForColumn(StartingFlightColumn sfc, LogbookEntryCore sf)
        {
            if (sf == null)
                throw new ArgumentNullException(nameof(sf));
            switch (sfc)
            {
                case StartingFlightColumn.CFI:
                    return sf.CFI;
                case StartingFlightColumn.SIC:
                    return sf.SIC;
                case StartingFlightColumn.PIC:
                    return sf.PIC;
                case StartingFlightColumn.Total:
                    return sf.TotalFlightTime;
            }
            return 0.0M;
        }

        /// <summary>
        /// Updates the starting flight with the specified value in the specified column
        /// </summary>
        /// <param name="sfc">The column to read</param>
        /// <param name="sf">The target starting flight</param>
        /// <param name="value">The value</param>
        protected static void SetValueForColumn(StartingFlightColumn sfc, LogbookEntryCore sf, Decimal value)
        {
            if (sf == null)
                throw new ArgumentNullException(nameof(sf));
            switch (sfc)
            {
                case StartingFlightColumn.CFI:
                    sf.CFI = value;
                    break;
                case StartingFlightColumn.SIC:
                    sf.SIC = value;
                    break;
                case StartingFlightColumn.PIC:
                    sf.PIC = value;
                    break;
                case StartingFlightColumn.Total:
                    sf.TotalFlightTime = value;
                    break;
            }
        }

        /// <summary>
        /// Initializes a row in the table with the values from the named starting flight.
        /// </summary>
        /// <param name="sf">The starting flight from which to initialize</param>
        /// <param name="iRow">The row</param>
        protected void ToRow(LogbookEntryCore sf, int iRow)
        {
            int iCol = 0;
            foreach (StartingFlightColumn sfc in Enum.GetValues(typeof(StartingFlightColumn)))
            {
                Controls_mfbDecimalEdit de = (Controls_mfbDecimalEdit)tblStartingFlights.Rows[iRow].FindControl(IDForCell(iRow, iCol));
                de.Value = GetValueForColumn(sfc, sf);
                iCol++;
            }
        }

        /// <summary>
        /// Initializes the specified starting flight with the values of the decimal edits in the row
        /// </summary>
        /// <param name="sf">The starting flight</param>
        /// <param name="iRow">The row</param>
        protected void FromRow(LogbookEntryCore sf, int iRow)
        {
            int iCol = 0;
            foreach (StartingFlightColumn sfc in Enum.GetValues(typeof(StartingFlightColumn)))
            {
                Controls_mfbDecimalEdit de = (Controls_mfbDecimalEdit)tblStartingFlights.Rows[iRow].FindControl(IDForCell(iRow, iCol));
                SetValueForColumn(sfc, sf, de.Value);
                iCol++;
            }
        }
        #endregion

        protected static string IDForCell(int row, int column)
        {
            return String.Format(CultureInfo.InvariantCulture, "decR{0}C{1}", row, column);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.lbtStartingTotals;
            InitWizard(wizStartingTotals);

            if (!IsPostBack)
            {
                this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName);
                mfbTypeInDate1.Date = DateTime.Now.AddYears(-1);
                InitFlights(rbSimple, e);
                Aircraft[] rgac = (new UserAircraft(User.Identity.Name)).GetAircraftForUser();
                gvAircraft.DataSource = rgac;
                gvAircraft.DataBind();
                if (rgac.Length == 0)
                    cpeExistingAircraft.Collapsed = false;

                locWhyStartingFlights.Text = Branding.ReBrand(Resources.LocalizedText.WhyStartingFlights);
            }
            else
                UpdateFlightTable();
        }

        protected void InitFlights(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            if (sender.GetType() == typeof(RadioButton) && !((RadioButton)sender).Checked)
                return;

            if (!Page.User.Identity.IsAuthenticated || String.IsNullOrEmpty(Page.User.Identity.Name))
                return;

            InitStartingFlights(StartingFlight.StartingFlightsForUser(Page.User.Identity.Name, CurrentMode));
            UpdateFlightTable();
        }

        protected static void AddAircraftDescription(string szTitle, string szSubTitle, Control tc)
        {
            if (tc == null)
                throw new ArgumentNullException(nameof(tc));
            Panel pTitle = new Panel();
            tc.Controls.Add(pTitle);
            Label lTitle = new Label();
            pTitle.Controls.Add(lTitle);
            lTitle.Text = HttpUtility.HtmlEncode(szTitle);

            Panel pSubTitle = new Panel();
            tc.Controls.Add(pSubTitle);
            Label lSubTitle = new Label();
            pSubTitle.Controls.Add(lSubTitle);
            lSubTitle.Text = HttpUtility.HtmlEncode(szSubTitle);
        }

        protected void UpdateFlightTable()
        {
            if (StartingFlights == null)
                return;

            tblStartingFlights.Rows.Clear();

            if (StartingFlights.Count == 0)
            {
                lblNoAircraft.Visible = true;
                return;
            }

            TableRow trHeader = new TableRow();
            tblStartingFlights.Rows.Add(trHeader);
            trHeader.Font.Bold = true;
            // table header
            // Description...
            trHeader.Cells.Add(new TableCell());

            // ...Then the editable cells
            foreach (StartingFlightColumn sfc in Enum.GetValues(typeof(StartingFlightColumn)))
                trHeader.Cells.Add(new TableCell() { Text = ColumnTitleFromColumn(sfc) });

            bool fHHMM = Profile.GetUser(Page.User.Identity.Name).UsesHHMM;

            // Now the individual rows.
            int iRow = 0;
            foreach (StartingFlight sf in StartingFlights)
            {
                TableRow tr = new TableRow();
                tblStartingFlights.Rows.Add(tr);
                TableCell tc = new TableCell();
                tr.Cells.Add(tc);

                switch (CurrentMode)
                {
                    case RepresentativeAircraft.RepresentativeTypeMode.ByModel:
                        AddAircraftDescription(sf.RepresentativeAircraft.ExampleAircraft.ModelCommonName, sf.RepresentativeAircraft.ExampleAircraft.ModelDescription, tc);
                        break;
                    case RepresentativeAircraft.RepresentativeTypeMode.CatClassCapabilities:
                        {
                            string szDesc = sf.RepresentativeAircraft.Descriptor;
                            AddAircraftDescription(sf.RepresentativeAircraft.Name + (szDesc.Length > 0 ? String.Format(CultureInfo.InvariantCulture, Resources.LocalizedText.LocalizedParentheticalWithSpace, szDesc) : string.Empty), String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.StartingFlightAircraftExample, sf.RepresentativeAircraft.ExampleAircraft.ModelCommonName), tc);
                        }
                        break;
                    case RepresentativeAircraft.RepresentativeTypeMode.CatClassType:
                        AddAircraftDescription(sf.RepresentativeAircraft.Name, String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.StartingFlightAircraftExample, sf.RepresentativeAircraft.ExampleAircraft.ModelDescription), tc);
                        break;
                }

                int iCol = 0;
                foreach (StartingFlightColumn sfc in Enum.GetValues(typeof(StartingFlightColumn)))
                {
                    TableCell tc2 = new TableCell() { ID = IDForCell(iRow, iCol) + sfc.ToString() };
                    tr.Cells.Add(tc2);
                    Controls_mfbDecimalEdit dc = (Controls_mfbDecimalEdit)LoadControl("~/Controls/mfbDecimalEdit.ascx");
                    dc.ID = IDForCell(iRow, iCol);
                    tc2.Controls.Add(dc);
                    dc.EditingMode = fHHMM ? Controls_mfbDecimalEdit.EditMode.HHMMFormat : Controls_mfbDecimalEdit.EditMode.Decimal;
                    iCol++;
                }

                ToRow(sf, iRow);
                iRow++;
            }
        }

        protected void CommitFlights()
        {
            int iRow = 0;
            foreach (StartingFlight sf in StartingFlights)
            {
                FromRow(sf, iRow++);
                sf.Date = mfbTypeInDate1.Date;
                // Commit it, if it has data.
                if (sf.PIC + sf.SIC + sf.CFI + sf.TotalFlightTime > 0)
                    sf.FCommit();
            }
        }

        protected void Preview()
        {
            if (StartingFlights == null)
                return;

            FlightQuery fq = new FlightQuery(User.Identity.Name);

            // Force a refresh on "before"
            mfbTotalsBefore.CustomRestriction = fq;

            // Commit the flights
            CommitFlights();

            // force a refresh on "after"
            mfbTotalsAfter.CustomRestriction = fq;

            // now delete those starting flights.
            foreach (StartingFlight sf in StartingFlights)
            {
                if (!sf.IsNewFlight)
                    LogbookEntry.FDeleteEntry(sf.FlightID, User.Identity.Name);
                sf.FlightID = LogbookEntry.idFlightNew;
            }
        }

        protected void wizStartingTotals_ActiveStepChanged(object sender, EventArgs e)
        {
            if (wizStartingTotals.ActiveStep == stepPreview)
                Preview();
        }

        protected void wizStartingTotals_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            CommitFlights();
        }
    }
}