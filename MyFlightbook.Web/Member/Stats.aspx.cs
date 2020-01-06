using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Stats : System.Web.UI.Page
{
    [Serializable]
    protected class xyvalue : IComparable
    {
        public int x { get; set; }
        public int y { get; set; }
        public int ordinal { get; set; }
        public string bucket { get; set; }

        public xyvalue()
        {
            x = 0;
            y = 0;
            ordinal = 0;
            bucket = "";
        }

        public int CompareTo(object obj)
        {
            return this.ordinal - ((xyvalue)obj).ordinal;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.admStats;
        if (!MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanReport)
        {
            util.NotifyAdminEvent("Attempt to view admin page", String.Format(CultureInfo.CurrentCulture, "User {0} tried to hit the admin page.", Page.User.Identity.Name), ProfileRoles.maskSiteAdminOnly);
            Response.Redirect("~/HTTP403.htm");
        }

        if (!IsPostBack)
        {
            Dictionary<string, xyvalue> dictValues = new Dictionary<string, xyvalue>();

            DataView dv = (DataView) sqlFlightsPerUser.Select(DataSourceSelectArguments.Empty);
            foreach (DataRowView dr in dv)
            {
                string szbucket = dr["NumFlightsBucket"].ToString();

                xyvalue xyv = dictValues.ContainsKey(szbucket) ? dictValues[szbucket] : null;
                if (xyv == null)
                {
                    xyv = new xyvalue();
                    dictValues.Add(szbucket, xyv);
                }
                xyv.ordinal = Convert.ToInt32(dr["ordinal"], CultureInfo.InvariantCulture);
                xyv.bucket = szbucket;

                if (String.Compare(dr["UserClass"].ToString(), "new", StringComparison.OrdinalIgnoreCase) == 0)
                    xyv.y = Convert.ToInt32(dr["NumUsers"], CultureInfo.InvariantCulture);
                else
                    xyv.x = Convert.ToInt32(dr["NumUsers"], CultureInfo.InvariantCulture);
            }

            List<xyvalue> lstVals = new List<xyvalue>();

            foreach (string key in dictValues.Keys)
                lstVals.Add(dictValues[key]);

            lstVals.Sort();

            foreach (xyvalue xyv in lstVals)
            {
                gcFlightsPerUser.YVals.Add(xyv.x + xyv.y);
                gcFlightsPerUser.Y2Vals.Add(xyv.y);
                gcFlightsPerUser.XVals.Add(xyv.bucket);
            }
        }
    }

    protected void btnUpdateFlights_Click(object sender, EventArgs e)
    {
        DataView dv = (DataView)sqlFlightsTrend.Select(DataSourceSelectArguments.Empty);
        gcFlightsOnSite.Clear();
        foreach (DataRowView dr in dv)
        {
            gcFlightsOnSite.XVals.Add((string)dr["DisplayPeriod"]);
            gcFlightsOnSite.YVals.Add(Convert.ToInt32(dr["NewFlights"], CultureInfo.InvariantCulture));
            gcFlightsOnSite.Y2Vals.Add(Convert.ToInt32(dr["RunningTotal"], CultureInfo.InvariantCulture));
        }
        gcFlightsOnSite.TickSpacing = Math.Max(1, gcFlightsOnSite.XVals.Count / 15);

        gvFlightsData.DataSource = dv;
        gvFlightsData.DataBind();

        pnlFlightsChart.Visible = true;
    }

    protected void btnUserActivity_Click(object sender, EventArgs e)
    {
        DataView dv = (DataView)sqlUserActivity.Select(DataSourceSelectArguments.Empty);
        gcUserActivity.Clear();
        foreach (DataRowView dr in dv)
        {
            gcUserActivity.XVals.Add(new DateTime(Convert.ToInt32(dr["ActivityYear"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["ActivityMonth"], CultureInfo.InvariantCulture), 1));
            gcUserActivity.YVals.Add(Convert.ToInt32(dr["UsersWithSessions"], CultureInfo.InvariantCulture));
        }
        pnlUserActivity.Visible = true;
    }
    protected void sqlUserActivity_Selecting(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        e.Command.CommandTimeout = 90;
    }
    protected void sqlFlightsPerUser_Selecting(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        e.Command.CommandTimeout = 90;
    }
    protected void sqlFlightsTrend_Selecting(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        e.Command.CommandTimeout = 90;
    }
}
