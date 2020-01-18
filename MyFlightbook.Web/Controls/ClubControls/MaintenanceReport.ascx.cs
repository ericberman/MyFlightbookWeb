using MyFlightbook;
using MyFlightbook.Clubs;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/
public partial class Controls_ClubControls_MaintenanceReport : System.Web.UI.UserControl, IReportable
{

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

    protected string CSSForDate(DateTime dt)
    {
        if (DateTime.Now.CompareTo(dt) > 0)
            return "currencyexpired";
        else if (DateTime.Now.AddDays(30).CompareTo(dt) > 0)
            return "currencynearlydue";
        return "currencyok";
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

    public string CSVData
    {
        get { return gvMaintenance.CSVFromData(); }
    }

    public void Refresh(int ClubID)
    {
        // flush the cache to pick up any aircraft changes
        gvMaintenance.DataSource = Club.ClubWithID(ClubID, true).MemberAircraft;
        gvMaintenance.DataBind();
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}