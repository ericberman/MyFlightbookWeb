using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Stats : System.Web.UI.Page
{
    [Serializable]
    protected class xyvalue : IComparable, IEquatable<xyvalue>
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

        #region IComparable
        public int CompareTo(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));
            return this.ordinal - ((xyvalue)obj).ordinal;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as xyvalue);
        }

        public bool Equals(xyvalue other)
        {
            return other != null &&
                   x == other.x &&
                   y == other.y &&
                   ordinal == other.ordinal &&
                   bucket == other.bucket;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = -720872820;
                hashCode = hashCode * -1521134295 + x.GetHashCode();
                hashCode = hashCode * -1521134295 + y.GetHashCode();
                hashCode = hashCode * -1521134295 + ordinal.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(bucket);
                return hashCode;
            }
        }

        public static bool operator ==(xyvalue left, xyvalue right)
        {
            return EqualityComparer<xyvalue>.Default.Equals(left, right);
        }

        public static bool operator !=(xyvalue left, xyvalue right)
        {
            return !(left == right);
        }

        public static bool operator <(xyvalue left, xyvalue right)
        {
            return left is null ? right is object : left.CompareTo(right) < 0;
        }

        public static bool operator <=(xyvalue left, xyvalue right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        public static bool operator >(xyvalue left, xyvalue right)
        {
            return left is object && left != null && left.CompareTo(right) > 0;
        }

        public static bool operator >=(xyvalue left, xyvalue right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }
        #endregion
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
            throw new ArgumentNullException(nameof(e));
        e.Command.CommandTimeout = 90;
    }
    protected void sqlFlightsPerUser_Selecting(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        e.Command.CommandTimeout = 90;
    }
    protected void sqlFlightsTrend_Selecting(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        e.Command.CommandTimeout = 90;
    }
}
