using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Telemetry;
using MyFlightbook.Geography;

/******************************************************
 * 
 * Copyright (c) 2015-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_AdminTelemetry : System.Web.UI.Page
{
    private const string szVSTED = "VSTEDList";

    protected Collection<TelemetryReference> BoundData
    {
        get
        {
            if (ViewState[szVSTED] == null)
                ViewState[szVSTED] = new List<TelemetryReference>();
            return new Collection<TelemetryReference>((List<TelemetryReference>)ViewState[szVSTED]);
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData)
        {
            util.NotifyAdminEvent("Attempt to view admin page", String.Format(System.Globalization.CultureInfo.CurrentCulture, "User {0} tried to hit the admin page.", Page.User.Identity.Name), ProfileRoles.maskSiteAdminOnly);
            Response.Redirect("~/HTTP403.htm");
        }
        this.Master.SelectedTab = tabID.admTelemetry;
        if (!IsPostBack)
            txtUser.Text = Page.User.Identity.Name;
    }

    protected void btnRefresh_Click(object sender, EventArgs e)
    {
        Refresh();
    }

    protected void Refresh()
    {
        if (String.IsNullOrEmpty(txtUser.Text))
            return;
        DBHelper dbh = new DBHelper(@"SELECT 
	f.idflight, 
    Length(telemetry) AS compressed, 
    length(uncompress(telemetry)) AS uncompressed, 
    CAST(UNCOMPRESS(telemetry) AS CHAR) AS telemetry,
    ft.*,
    IF(ft.idflight IS NULL, 0 , 1) AS HasFT
FROM flights f 
	LEFT JOIN FlightTelemetry ft ON f.idflight=ft.idflight
WHERE username=?szUser AND Coalesce(f.telemetry, ft.idflight) IS NOT NULL 
ORDER BY f.idFlight DESC;");
        Collection<TelemetryReference> lst = BoundData;
        lst.Clear();
        dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("szUser", txtUser.Text); },
            (dr) =>
            {
                TelemetryReference tr = (Convert.ToInt32(dr["HasFT"], CultureInfo.InvariantCulture) == 0) ?
                    new TelemetryReference(dr["telemetry"].ToString(), Convert.ToInt32(dr["idflight"], CultureInfo.InvariantCulture)) { Compressed = Convert.ToInt32(dr["compressed"], CultureInfo.InvariantCulture) } :
                    new TelemetryReference(dr) { Compressed = 0, RawData = dr["telemetry"].ToString() };
                lst.Add(tr);
            });
        gvData.DataSource = lst;
        gvData.DataBind();
        lblErr.Text = dbh.LastError;
    }

    protected void UpdateElement(int index, TelemetryReference tr)
    {
        BoundData[index] = tr;
        gvData.DataSource = BoundData;
        gvData.DataBind();
    }

    protected void gvData_RowCommand(object sender, CommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        int index = Convert.ToInt32(e.CommandArgument, System.Globalization.CultureInfo.InvariantCulture);
        TelemetryReference ted = BoundData[index];
        LogbookEntry le = new LogbookEntry();
        le.FLoadFromDB(ted.FlightID.Value, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll, true);

        if (e.CommandName == "MapEm")
        {
            using (FlightData fd = new FlightData())
            {
                if (fd.ParseFlightData(le.FlightData))
                {
                    mfbGMapStraight.Map.Path = fd.GetPath();
                    mfbGMapReconstituded.Map.Path = ted.GoogleData.DecodedPath();
                    pnlMaps.Visible = true;
                }
            }
        }
        else if (e.CommandName == "FromFlights")
        {
            le.Telemetry.Compressed = 0;    // no longer know the compressed 
            le.MoveTelemetryFromFlightEntry();
            UpdateElement(index, le.Telemetry);
        }
        else if (e.CommandName == "ToFlights")
        {
            le.MoveTelemetryToFlightEntry();
            UpdateElement(index, le.Telemetry);
        }
    }
    protected void btnMigrateFromDB_Click(object sender, EventArgs e)
    {
        int limit;
        if (!Int32.TryParse(txtMaxFiles.Text, out limit))
            limit = 50;

        DateTime dtStart = DateTime.Now;

        // for memory, just get the ID's up front, then we'll do flights one at a time.
        List<int> lst = new List<int>();
        DBHelper dbh = new DBHelper(String.Format(CultureInfo.InvariantCulture, "SELECT idFlight FROM flights WHERE {0} telemetry IS NOT NULL LIMIT {1}", ckLimitUsername.Checked ? "username=?szuser AND " : string.Empty, limit.ToString()));
        dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("szuser", txtUser.Text); }, (dr) => { lst.Add(Convert.ToInt32(dr["idFlight"], CultureInfo.InvariantCulture)); });

        int cFlightsMigrated = 0;
        lst.ForEach((idFlight) =>
        {
            try
            {
                new LogbookEntry(idFlight, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll, true).MoveTelemetryFromFlightEntry();
                cFlightsMigrated++;
                if ((cFlightsMigrated % 20) == 0)
                    System.Threading.Thread.Sleep(0);
            }
            catch (Exception ex)
            {
                throw new MyFlightbookException(String.Format("Exception migrating flight {0}: {1}", idFlight, ex.Message), ex);
            }
        });

        DateTime dtEnd = DateTime.Now;

        lblMigrateStatus.Text = String.Format(CultureInfo.InvariantCulture, "{0} flights migrated, elapsed: {1} seconds", cFlightsMigrated, dtEnd.Subtract(dtStart).TotalSeconds);
    }

    protected void btnMigrateFromFiles_Click(object sender, EventArgs e)
    {
        int limit;
        if (!Int32.TryParse(txtMaxFiles.Text, out limit))
            limit = 50;

        DateTime dtStart = DateTime.Now;

        // for memory, just get the ID's up front, then we'll do flights one at a time.
        List<TelemetryReference> lst = new List<TelemetryReference>();
        DBHelper dbh = new DBHelper((ckLimitUsername.Checked ? "SELECT ft.* FROM FlightTelemetry ft INNER JOIN flights f ON ft.idflight=f.idflight WHERE f.username=?szuser" : "SELECT * from FlightTelemetry") + " LIMIT " + limit.ToString());
        dbh.ReadRows((comm) => { comm.Parameters.AddWithValue("szuser", txtUser.Text); }, (dr) => { lst.Add(new TelemetryReference(dr)); });

        int cFlightsMigrated = 0;
        lst.ForEach((tr) =>
        {
            if (!tr.FlightID.HasValue)
            {
                lblMigrateStatus.Text += "Flight ID without a value!!! ";
            }
            else
            {
                new LogbookEntry(tr.FlightID.Value, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.LoadAll, true).MoveTelemetryToFlightEntry();
                cFlightsMigrated++;
            }
            if ((cFlightsMigrated % 20) == 0)
                System.Threading.Thread.Sleep(0);
        });

        DateTime dtEnd = DateTime.Now;

        lblMigrateStatus.Text += String.Format(CultureInfo.InvariantCulture, "{0} flights migrated, elapsed: {1} seconds", cFlightsMigrated, dtEnd.Subtract(dtStart).TotalSeconds);
    }

    protected void btnFindDupeTelemetry_Click(object sender, EventArgs e)
    {
        gvSetManagement.DataSource = TelemetryReference.FindDuplicateTelemetry();
        gvSetManagement.DataBind();
    }
    
    protected void btnFindOrphanedRefs_Click(object sender, EventArgs e)
    {
        gvSetManagement.DataSource = TelemetryReference.FindOrphanedRefs();
        gvSetManagement.DataBind();
    }

    protected void btnFindOrphanedFiles_Click(object sender, EventArgs e)
    {
        gvSetManagement.DataSource = TelemetryReference.FindOrphanedFiles();
        gvSetManagement.DataBind();
    }

    protected void btnViewAntipodes_Click(object sender, EventArgs e)
    {
        if (!FileUpload1.HasFile && FileUpload1.PostedFile.ContentLength > 0)
            return;

        byte[] rgbytes = new byte[FileUpload1.PostedFile.ContentLength];
        FileUpload1.PostedFile.InputStream.Read(rgbytes, 0, FileUpload1.PostedFile.ContentLength);
        FileUpload1.PostedFile.InputStream.Close();

        using (FlightData fdOriginal = new FlightData())
        {
            if (fdOriginal.ParseFlightData(System.Text.Encoding.UTF8.GetString(rgbytes)))
            {
                LatLong[] rgStraight = fdOriginal.GetPath();
                LatLong[] rgAntipodes = new LatLong[rgStraight.Length];
                for (int i = 0; i < rgStraight.Length; i++)
                    rgAntipodes[i] = rgStraight[i].Antipode;
                mfbGMapStraight.Map.Path = rgStraight;
                mfbGMapReconstituded.Map.Path = rgAntipodes;
                pnlMaps.Visible = true;
            }
        }
    }
}