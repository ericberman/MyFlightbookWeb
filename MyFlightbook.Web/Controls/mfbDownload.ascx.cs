using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    public partial class mfbDownload : System.Web.UI.UserControl
    {
        /// <summary>
        /// The username to download.  Caller must validate authorization.
        /// </summary>
        public string User { get; set; }

        private const string szVSQuery = "viewStateQuery";

        /// <summary>
        /// Controls whether or not the option to download a PDF file is offered
        /// </summary>
        public Boolean OfferPDFDownload
        {
            get { return pnlDownloadPDF.Visible; }
            set { pnlDownloadPDF.Visible = value; }
        }

        /// <summary>
        /// Controls whether or not the option to view the whole grid is offered
        /// </summary>
        public Boolean ShowLogbookData
        {
            get { return gvFlightLogs.Visible; }
            set { gvFlightLogs.Visible = value; }
        }

        /// <summary>
        /// String specifying the desired order of columns
        /// </summary>
        public string OrderString { get; set; }

        /// <summary>
        /// Returns the column to use for the specified integer-cast custompropertytype.
        /// </summary>
        protected Dictionary<int, int> PropertyColumnMap { get; } = new Dictionary<int, int>();

        protected Dictionary<int, Aircraft> AircraftForUser { get; } = new Dictionary<int, Aircraft>();

        public void UpdateData()
        {
            if (User.Length > 0)
            {
                IEnumerable<Aircraft> rgac = new UserAircraft(User).GetAircraftForUser();
                AircraftForUser.Clear();
                foreach (Aircraft ac in rgac)
                    AircraftForUser[ac.AircraftID] = ac;

                IEnumerable<LogbookEntryDisplay> rgle = LogbookEntryDisplay.GetFlightsForQuery(LogbookEntryBase.QueryCommand(new FlightQuery(User)), User, "Date", SortDirection.Descending, false, false);
                gvFlightLogs.DataSource = rgle;

                // See whether or not to show catclassoverride column
                bool fShowAltCatClass = false;
                foreach (LogbookEntryDisplay le in rgle)
                    fShowAltCatClass |= le.IsOverridden;

                if (!fShowAltCatClass)
                {
                    foreach (DataControlField dcf in gvFlightLogs.Columns)
                        if (dcf.HeaderText.CompareCurrentCultureIgnoreCase("Alternate Cat/Class") == 0)
                        {
                            gvFlightLogs.Columns.Remove(dcf);
                            break;
                        }
                }

                // Generate the set of properties used by the user
                int cColumns = gvFlightLogs.Columns.Count;
                PropertyColumnMap.Clear();
                HashSet<CustomPropertyType> hscpt = new HashSet<CustomPropertyType>();
                foreach (LogbookEntryBase le in rgle)
                {
                    foreach (CustomFlightProperty cfp in le.CustomProperties)
                    {
                        if (!hscpt.Contains(cfp.PropertyType))
                            hscpt.Add(cfp.PropertyType);
                    }
                }

                // Now sort that alphabetically and add them
                List<CustomPropertyType> lst = new List<CustomPropertyType>(hscpt);
                lst.Sort((cpt1, cpt2) => { return cpt1.Title.CompareCurrentCultureIgnoreCase(cpt2.Title); });
                foreach (CustomPropertyType cpt in lst)
                {
                    PropertyColumnMap[cpt.PropTypeID] = cColumns++;
                    BoundField bf = new BoundField()
                    {
                        HeaderText = cpt.Title,
                        HtmlEncode = false,
                        DataField = string.Empty,
                        DataFormatString = string.Empty
                    };
                    gvFlightLogs.Columns.Add(bf);
                }

                if (OrderString != null && OrderString.Length > 0)
                {
                    char[] delimit = { ',' };
                    string[] rgszCols = OrderString.Split(delimit);
                    ArrayList alCols = new ArrayList();

                    // identify the requested front columns
                    foreach (string szcol in rgszCols)
                    {
                        if (int.TryParse(szcol, NumberStyles.Integer, CultureInfo.InvariantCulture, out int col))
                        {
                            if (col < gvFlightLogs.Columns.Count)
                                alCols.Add(col);
                        }
                    }

                    int[] rgCols = (int[])alCols.ToArray(typeof(int));

                    // pull those columns to the left; this creates a duplicate column and shifts everything right by one...
                    int iCol = 0;
                    for (iCol = 0; iCol < rgCols.Length; iCol++)
                        gvFlightLogs.Columns.Insert(iCol, gvFlightLogs.Columns[rgCols[iCol] + iCol]);

                    // And then remove the duplicates, from right to left
                    Array.Sort(rgCols);
                    for (int i = rgCols.Length - 1; i >= 0; i--)
                        gvFlightLogs.Columns.RemoveAt(rgCols[i] + iCol);
                }

                // Ensure that we use adequate decimal settings for round-trip.
                Profile pf = Profile.GetUser(User);
                DecimalFormat df = pf.PreferenceExists(MFBConstants.keyDecimalSettings) ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings) : DecimalFormat.Adaptive;
                if (HttpContext.Current?.Session != null)
                    HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = DecimalFormat.Adaptive;
                gvFlightLogs.DataBind();
                if (HttpContext.Current?.Session != null)
                    HttpContext.Current.Session[MFBConstants.keyDecimalSettings] = df;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                User = User ?? string.Empty;
                OrderString = OrderString ?? string.Empty;
            }
        }

        public void gvFlightLogs_RowDataBound(Object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                if (!(e.Row.DataItem is LogbookEntryBase le))
                    throw new InvalidCastException("DataItem is not a logbook entry!");

                // Property summary
                string szProperties = CustomFlightProperty.PropListDisplay(le.CustomProperties, false, "; ");
                if (szProperties.Length > 0)
                {
                    PlaceHolder plcProperties = (PlaceHolder)e.Row.FindControl("plcProperties");
                    TableCell tc = (TableCell)plcProperties.Parent;
                    tc.Text = szProperties;
                }

                // Splice in the custom property types.
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                    e.Row.Cells[PropertyColumnMap[cfp.PropTypeID]].Text = HttpUtility.HtmlEncode(cfp.ValueString);

                // Add model attributes
                MakeModel m = MakeModel.GetModel(AircraftForUser[le.AircraftID].ModelID);
                Aircraft ac = AircraftForUser.ContainsKey(le.AircraftID) ? AircraftForUser[le.AircraftID] : null;
                ((Label)e.Row.FindControl("lblComplex")).Text = m.IsComplex.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblCSP")).Text = m.IsConstantProp.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblFlaps")).Text = m.HasFlaps.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblRetract")).Text = m.IsRetract.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblTailwheel")).Text = m.IsTailWheel.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblHP")).Text = m.IsHighPerf.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblTurbine")).Text = m.EngineType == MakeModel.TurbineLevel.Piston ? string.Empty : 1.FormatBooleanInt();
                ((Label)e.Row.FindControl("lblTAA")).Text = (m.AvionicsTechnology == MakeModel.AvionicsTechnologyType.TAA ||
                    (ac != null && (ac.AvionicsTechnologyUpgrade == MakeModel.AvionicsTechnologyType.TAA && (!ac.GlassUpgradeDate.HasValue || le.Date.CompareTo(ac.GlassUpgradeDate.Value) >= 0)))).FormatBooleanInt();
            }
        }

        public void ToStream(string szUser, System.IO.Stream s)
        {
            if (String.IsNullOrEmpty(szUser))
                return;
            else
            {
                if (s == null)
                    throw new ArgumentNullException(nameof(s));
                User = szUser;
                UpdateData();
                ToStream(s);
            }
        }

        public void ToStream(System.IO.Stream s)
        {
            gvFlightLogs.ToCSV(s);
        }
    }
}