using MyFlightbook;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbDownload : System.Web.UI.UserControl, IDownloadableAsData
{
    /// <summary>
    /// The username to download.  Caller must validate authorization.
    /// </summary>
    public string User { get; set;}

    private const string szVSQuery = "viewStateQuery";

    public FlightQuery Restriction
    {
        get { return (FlightQuery)ViewState[szVSQuery] ?? new FlightQuery(User); }
        set { ViewState[szVSQuery] = value; }
    }

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

    protected string FormatTimeSpan(object o1, object o2)
    {
        if (!(o1 is DateTime && o2 is DateTime))
            return string.Empty;

        DateTime dt1 = (DateTime)o1;
        DateTime dt2 = (DateTime)o2;

        if (dt1.HasValue() && dt2.HasValue())
        {
            double cHours = dt2.Subtract(dt1).TotalHours;
            return (cHours > 0) ? String.Format(CultureInfo.CurrentCulture, "{0:#.##}", cHours) : string.Empty;
        }
        else
            return string.Empty;
    }

    public void UpdateData()
    {
        if (User.Length > 0)
        {
            // See whether or not to show catclassoverride column
            bool fShowAltCatClass = false;
            DBHelper dbh = new DBHelper("SELECT COALESCE(MAX(idCatClassOverride), 0) AS useAltCatClass FROM flights f WHERE f.username=?user");
            dbh.ReadRow(
                (comm) => { comm.Parameters.AddWithValue("user", User); },
                (dr) => { fShowAltCatClass = Convert.ToInt32(dr["useAltCatClass"], CultureInfo.InvariantCulture) > 0; });

            if (!fShowAltCatClass)
            {
                foreach (DataControlField dcf in gvFlightLogs.Columns)
                    if (dcf.HeaderText.CompareCurrentCultureIgnoreCase("Alternate Cat/Class") == 0)
                    {
                        gvFlightLogs.Columns.Remove(dcf);
                        break;
                    }
            }

            using (MySqlCommand comm = new MySqlCommand())
            {
                DBHelper.InitCommandObject(comm, LogbookEntry.QueryCommand(Restriction));
                comm.CommandTimeout = 80; // use a longer timeout - this could be slow.  

                try
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter(comm))
                    {
                        using (DataSet dsFlights = new DataSet())
                        {
                            dsFlights.Locale = CultureInfo.CurrentCulture;
                            da.Fill(dsFlights);
                            gvFlightLogs.DataSource = dsFlights;

                            // Get the list of property types used by this user to create additional columns
                            comm.CommandText = "SELECT DISTINCT cpt.Title FROM custompropertytypes cpt INNER JOIN flightproperties fp ON fp.idPropType=cpt.idPropType INNER JOIN flights f ON f.idFlight=fp.idFlight WHERE f.username=?uName";
                            // parameters should still be valid

                            Hashtable htProps = new Hashtable(); // maps titles to the relevant column in the gridview
                            int cColumns = gvFlightLogs.Columns.Count;
                            using (DataSet dsProps = new DataSet())
                            {
                                dsProps.Locale = CultureInfo.CurrentCulture;
                                da.Fill(dsProps);

                                // add a new column for each property and store the column number in the hashtable (keyed by title)
                                foreach (DataRow dr in dsProps.Tables[0].Rows)
                                {
                                    htProps[dr.ItemArray[0]] = cColumns++;
                                    BoundField bf = new BoundField()
                                    {
                                        HeaderText = dr.ItemArray[0].ToString(),
                                        HtmlEncode = false,
                                        DataField = "",
                                        DataFormatString = ""
                                    };
                                    gvFlightLogs.Columns.Add(bf);
                                }
                            }

                            if (OrderString != null && OrderString.Length > 0)
                            {
                                char[] delimit = { ',' };
                                string[] rgszCols = OrderString.Split(delimit);
                                ArrayList alCols = new ArrayList();

                                // identify the requested front columns
                                foreach (string szcol in rgszCols)
                                {
                                    int col = 0;
                                    if (int.TryParse(szcol, NumberStyles.Integer, CultureInfo.InvariantCulture, out col))
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

                            gvFlightLogs.DataBind();

                            // now splice in all of the properties above
                            // ?localecode and ?shortDate are already in the parameters, from above.
                            comm.CommandText = "SELECT ELT(cpt.type + 1, cast(fdc.intValue as char), cast(FORMAT(fdc.decValue, 2, ?localecode) as char), if(fdc.intValue = 0, 'No', 'Yes'), DATE_FORMAT(fdc.DateValue, ?shortDate), cast(DateValue as char), StringValue, cast(fdc.decValue AS char))  AS PropVal, fdc.idFlight AS idFlight, cpt.title AS Title FROM flightproperties fdc INNER JOIN custompropertytypes cpt ON fdc.idPropType=cpt.idPropType INNER JOIN flights f ON f.idflight=fdc.idFlight WHERE username=?uName";

                            // and parameters should still be valid!
                            using (DataSet dsPropValues = new DataSet())
                            {
                                dsPropValues.Locale = CultureInfo.CurrentCulture;
                                da.Fill(dsPropValues);
                                foreach (GridViewRow gvr in gvFlightLogs.Rows)
                                {
                                    int idFlight = Convert.ToInt32(dsFlights.Tables[0].Rows[gvr.RowIndex]["idFlight"], CultureInfo.CurrentCulture);
                                    foreach (DataRow dr in dsPropValues.Tables[0].Rows)
                                    {
                                        if (idFlight == Convert.ToInt32(dr["idFlight"], CultureInfo.CurrentCulture))
                                            gvr.Cells[Convert.ToInt32(htProps[dr["Title"]], CultureInfo.CurrentCulture)].Text = dr["PropVal"].ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    if (comm.Connection != null && comm.Connection.State != ConnectionState.Closed)
                        comm.Connection.Close();
                }
            }
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

    public string CSVData()
    {
        return gvFlightLogs.CSVFromData();
    }

    public void gvFlightLogs_RowDataBound(Object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            int idFlight = Convert.ToInt32(DataBinder.Eval(e.Row.DataItem, "idflight"), CultureInfo.InvariantCulture);
            IEnumerable<CustomFlightProperty> rgProps = CustomFlightProperty.PropertiesFromJSONTuples((string) DataBinder.Eval(e.Row.DataItem, "CustomPropsJSON"), idFlight);
            string szProperties = CustomFlightProperty.PropListDisplay(rgProps, false, "; ");

            if (szProperties.Length > 0)
            {
                PlaceHolder plcProperties = (PlaceHolder)e.Row.FindControl("plcProperties");
                TableCell tc = (TableCell) plcProperties.Parent;
                tc.Text = szProperties;
            }
        }
    }

    protected string CSVInUSCulture()
    {
        // ALWAYS download in US conventions
        CultureInfo ciSave = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        UpdateData();

        StringBuilder sbCSV = new StringBuilder();
        sbCSV.Append('\uFEFF'); // UTF-8 BOM
        sbCSV.Append(CSVData());
        System.Threading.Thread.CurrentThread.CurrentCulture = ciSave;

        return sbCSV.ToString();
    }

    public byte[] RawData(string szUser)
    {
        if (String.IsNullOrEmpty(szUser))
            return new byte[0];
        else
        {
            User = szUser;
            UpdateData();
            UTF8Encoding enc = new UTF8Encoding(true);    // to include the BOM
            byte[] preamble = enc.GetPreamble();
            string body = CSVData();
            byte[] allBytes = new byte[preamble.Length + enc.GetByteCount(body)]; ;
            for (int i = 0; i < preamble.Length; i++)
                allBytes[i] = preamble[i];
            enc.GetBytes(body, 0, body.Length, allBytes, preamble.Length);
            return allBytes;
        }
    }
}
