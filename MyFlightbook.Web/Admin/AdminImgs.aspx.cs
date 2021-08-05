using AWSNotifications;
using MyFlightbook.Image;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2009-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminImgs : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageData);
            Master.SelectedTab = tabID.admImages;
            btnDeleteS3Debug.Visible = AWSConfiguration.UseDebugBucket;
        }

        protected void btnDeleteOrphans_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Deleting orphaned flight images:\r\n<br />");
            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Flight));
            sb.Append("Deleting orphaned endorsement images:\r\n<br />");
            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Endorsement));
            sb.Append("Deleting orphaned Aircraft images:\r\n<br />");
            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.Aircraft));
            sb.Append("Deleting orphaned BasicMed images:\r\n<br />");
            sb.AppendFormat(CultureInfo.CurrentCulture, "{0}\r\n<br />", MFBImageInfo.ADMINDeleteOrphans(MFBImageInfo.ImageClass.BasicMed));
            lblDeleted.Text = sb.ToString();
        }

        protected void btnDeleteS3Debug_Click(object sender, EventArgs e)
        {
            AWSImageManagerAdmin.ADMINCleanUpDebug();
        }

        protected void SyncImagesToDB(MFBImageInfo.ImageClass ic)
        {
            LiteralControl lc = new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<iframe src=\"{0}\" width=\"600\" height=\"300\"></iframe>", String.Format(CultureInfo.InvariantCulture, "{0}?sync=1&r={1}{2}", ResolveClientUrl("AdminImages.aspx"), ic.ToString(), ckPreviewOnly.Checked ? "&preview=1" : string.Empty)));
            plcDBSync.Controls.Add(lc);
        }

        protected void btnSyncFlight_Click(object sender, EventArgs e)
        {
            SyncImagesToDB(MFBImageInfo.ImageClass.Flight);
        }
        protected void btnSyncAircraftImages_Click(object sender, EventArgs e)
        {
            SyncImagesToDB(MFBImageInfo.ImageClass.Aircraft);
        }
        protected void btnSyncEndorsements_Click(object sender, EventArgs e)
        {
            SyncImagesToDB(MFBImageInfo.ImageClass.Endorsement);
        }

        protected void btnSyncOfflineEndorsements_Click(object sender, EventArgs e)
        {
            SyncImagesToDB(MFBImageInfo.ImageClass.OfflineEndorsement);
        }

        protected void btnSyncBasicMed_Click(object sender, EventArgs e)
        {
            SyncImagesToDB(MFBImageInfo.ImageClass.BasicMed);
        }

        protected void DeleteS3Orphans(MFBImageInfo.ImageClass ic)
        {
            LiteralControl lc = new LiteralControl(String.Format(CultureInfo.InvariantCulture, "<iframe src=\"{0}\" width=\"600\" height=\"300\"></iframe>", String.Format(CultureInfo.InvariantCulture, "{0}?dels3orphan=1&r={1}{2}", ResolveClientUrl("AdminImages.aspx"), ic.ToString(), ckPreviewOnly.Checked ? "&preview=1" : string.Empty)));
            plcDBSync.Controls.Add(lc);
        }

        protected void btnDelS3FlightOrphans_Click(object sender, EventArgs e)
        {
            DeleteS3Orphans(MFBImageInfo.ImageClass.Flight);
        }
        protected void btnDelS3AircraftOrphans_Click(object sender, EventArgs e)
        {
            DeleteS3Orphans(MFBImageInfo.ImageClass.Aircraft);
        }
        protected void btnDelS3EndorsementOrphans_Click(object sender, EventArgs e)
        {
            DeleteS3Orphans(MFBImageInfo.ImageClass.Endorsement);
        }

        protected void btnDelS3OfflineEndorsementOrphans_Click(object sender, EventArgs e)
        {
            DeleteS3Orphans(MFBImageInfo.ImageClass.OfflineEndorsement);
        }

        protected void btnDelS3BasicMedOrphans_Click(object sender, EventArgs e)
        {
            DeleteS3Orphans(MFBImageInfo.ImageClass.BasicMed);
        }

        protected void btnCleanPendingVideos_Click(object sender, EventArgs e)
        {
            List<SNSNotification> lstPending = new List<SNSNotification>();
            List<int> lstFlights = new List<int>();

            // Get all pending videos that are more than an hour old and create synthetic SNS notifications for them.
            DBHelper dbh = new DBHelper("SELECT * FROM pendingvideos pv WHERE submitted < DATE_ADD(Now(), INTERVAL -1 HOUR)");
            dbh.ReadRows((comm) => { },
            (dr) =>
            {
                AWSETSStateMessage etsNotification = new AWSETSStateMessage() { JobId = (string)dr["jobID"], State = "COMPLETED" };
                SNSNotification sns = new SNSNotification() { Message = JsonConvert.SerializeObject(etsNotification) };
                lstPending.Add(sns);
                lstFlights.Add(Convert.ToInt32(dr["imagekey"], CultureInfo.InvariantCulture));
            });

            int cPending = lstPending.Count;
            gvVideos.DataSource = lstFlights;
            gvVideos.DataBind();

            // Now, go through them and create each one.  Should clean up as part of the process.
            // simply creating the object will do all that is necessary.
            foreach (SNSNotification sns in lstPending)
                _ = new MFBImageInfo(sns);

            int cRemaining = 0;
            dbh.CommandText = "SELECT count(*) AS numRemaining FROM pendingvideos pv WHERE submitted < DATE_ADD(Now(), INTERVAL -1 HOUR)";
            dbh.ReadRow((comm) => { }, (dr) => { cRemaining = Convert.ToInt32(dr["numRemaining"], CultureInfo.InvariantCulture); });

            lblPVResults.Text = String.Format(CultureInfo.CurrentCulture, "Found {0} videos orphaned, {1} now remain", cPending, cRemaining);
        }
    }
}