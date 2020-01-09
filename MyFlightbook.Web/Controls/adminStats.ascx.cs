using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Collections;
using System.Globalization;
using System.Text;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_adminStats : System.Web.UI.UserControl
{
    protected class UserStats
    {
        public DateTime DisplayPeriod { get; set; }
        public int NewUsers { get; set; }
        public int RunningTotal { get; set; }
        public UserStats()
        {
        }

        public UserStats(DateTime dt, int numUsers, int total)
        {
            DisplayPeriod = dt;
            NewUsers = numUsers;
            RunningTotal = total;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            List<UserStats> lst = new List<UserStats>();

            DataView dv = (DataView)sqlUserData.Select(DataSourceSelectArguments.Empty);
            foreach (DataRowView dr in dv)
                lst.Add(new UserStats(new DateTime(Convert.ToInt32(dr["CreationYear"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["CreationMonth"], CultureInfo.InvariantCulture), 1), Convert.ToInt32(dr["NewUsers"], CultureInfo.InvariantCulture), Convert.ToInt32(dr["RunningTotal"], CultureInfo.InvariantCulture)));
            gvUserData.DataSource = lst;
            gvUserData.DataBind();

            foreach (UserStats us in lst)
            {
                gcNewUsers.XVals.Add(us.DisplayPeriod);
                gcNewUsers.YVals.Add(us.NewUsers);
                gcNewUsers.Y2Vals.Add(us.RunningTotal);
            }
        }
    }

    protected void TrimOldItems(EventRecorder.MFBEventID eventID)
    {
        // below is too slow
        // string szDelete = String.Format("DELETE w1 FROM wsevents w1 JOIN wsevents w2 ON (w1.eventType=w2.eventType AND w1.user=w2.user AND w1.eventID < w2.eventID) WHERE w1.eventType={0}", (int)eventID);

        Hashtable htUsers = new Hashtable();
        StringBuilder sbRowsToDelete = new StringBuilder();
        DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM wsevents w WHERE w.eventType={0} ORDER BY w.Date DESC", (int)eventID));
        if (!dbh.ReadRows(
            (comm) => { comm.CommandTimeout = 120; },
            (dr) =>
            {
                string szUser = dr["user"].ToString();
                DateTime dtLastEntered = Convert.ToDateTime(dr["Date"], CultureInfo.InvariantCulture);
                if (htUsers[szUser] == null)
                    htUsers[szUser] = dtLastEntered;
                else
                    sbRowsToDelete.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", sbRowsToDelete.Length > 0 ? "," : "", Convert.ToInt32(dr["eventID"], CultureInfo.InvariantCulture));
            }))
            lblTrimErr.Text = "Error trimming data: " + dbh.LastError;

        if (sbRowsToDelete.Length > 0)
        {
            dbh.CommandText = String.Format(CultureInfo.InvariantCulture, "DELETE FROM wsevents WHERE eventID IN ({0})", sbRowsToDelete.ToString());
            dbh.DoNonQuery();
        }
    }

    protected void btnTrimAuthenticate_Click(object sender, EventArgs e)
    {
        TrimOldItems(EventRecorder.MFBEventID.AuthUser);
        TrimOldItems(EventRecorder.MFBEventID.ExpiredToken);
    }

    protected void btnTrimOAuth_Click(object sender, EventArgs e)
    {
        // Clean up old Nonces and cryptokeys
        new DBHelper("DELETE FROM nonce WHERE timestampUtc < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
        new DBHelper("DELETE FROM cryptokeys WHERE ExpiresUtc < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
        new DBHelper("DELETE FROM passwordresetrequests WHERE expiration < DATE_ADD(Now(), INTERVAL -14 DAY)").DoNonQuery();
    }

    #region Set timeouts
    protected void setTimeout(object sender, SqlDataSourceCommandEventArgs e)
    {
        if (e != null)
            e.Command.CommandTimeout = 90;
    }
    #endregion
}