using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Configuration;
using System.IO;
using System.Collections;
using System.Globalization;
using MyFlightbook;
using MyFlightbook.Image;
using MyFlightbook.SocialMedia;

/******************************************************
 * 
 * Copyright (c) 2015-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_AdminImages : System.Web.UI.Page
{
    protected string szBase = "";

    protected const string szVSDBKey = "imagesFromDatabaseVS";
    protected string m_szLinkTemplate = String.Empty;

    #region Properties
    protected MFBImageInfo.ImageClass CurrentSource { get; set; }

    /// <summary>
    /// A dictionary of images to admin.
    /// </summary>
    Dictionary<string, List<MFBImageInfo>> ImageDictionary
    {
        get
        {
            if (ViewState[szVSDBKey] != null)
                return (Dictionary<string, List<MFBImageInfo>>)ViewState[szVSDBKey];
            else
            {
                Dictionary<string, List<MFBImageInfo>> dict = MFBImageInfo.FromDB(CurrentSource);
                ViewState[szVSDBKey] = dict;
                return dict;
            }
        }
    }

    /// <summary>
    /// A sorted list of the image keys, sorted numerically (if flight/aircraft), otherwise alphabetically.
    /// </summary>
    List<string> SortedKeys
    {
        get
        {
            List<string> lstKeys = ImageDictionary.Keys.ToList<string>();
            if (CurrentSource == MFBImageInfo.ImageClass.Aircraft || CurrentSource == MFBImageInfo.ImageClass.Flight)
                lstKeys.Sort((sz1, sz2) => { return Convert.ToInt32(sz2, CultureInfo.InvariantCulture) - Convert.ToInt32(sz1, CultureInfo.InvariantCulture); });
            else
                lstKeys.Sort();

            return lstKeys;
        }
    }

    protected int TotalImageRows
    {
        get { return Convert.ToInt32(hdnImageCount.Value, CultureInfo.InvariantCulture); }
        set { hdnImageCount.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    protected int CurrentImageRowOffset
    {
        get { return Convert.ToInt32(hdnCurrentOffset.Value, CultureInfo.InvariantCulture); }
        set { hdnCurrentOffset.Value = value.ToString(CultureInfo.InvariantCulture); }
    }

    private Dictionary<string, List<MFBImageInfo>> QueryResults
    {
        get { return (ViewState["QueryResults"] == null) ? null : (Dictionary<string, List<MFBImageInfo>>)ViewState["QueryResults"]; }
        set { ViewState["QueryResults"] = value; }
    }

    protected const int PageSize = 50;
    #endregion

    [Serializable]
    protected class DirKey : IComparable
    {
        public string Key { get; set; }
        public int SortID { get; set; }

        public DirKey()
        {
        }

        public int CompareTo(object obj)
        {
            DirKey dk = (DirKey)obj;
            if (this.SortID > dk.SortID)
                return -1;
            else if (this.SortID == dk.SortID)
                return 0;
            else
                return 1;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabAdmin;
        if (!MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanManageData)
        {
            util.NotifyAdminEvent("Attempt to view admin page", String.Format("User {0} tried to hit the admin page.", Page.User.Identity.Name), ProfileRoles.maskSiteAdminOnly);
            Response.Redirect("~/HTTP403.htm");
        }

        bool fNumericKeySort = true;
        string szRoot = util.GetStringParam(Request, "r");

        CurrentSource = MFBImageInfo.ImageClass.Flight;

        try
        {
            CurrentSource = (MFBImageInfo.ImageClass)Enum.Parse(typeof(MFBImageInfo.ImageClass), szRoot);
        }
        catch { }

        switch (CurrentSource)
        {
            case MFBImageInfo.ImageClass.Flight:
                m_szLinkTemplate = "~/member/logbookNew.aspx/{0}?a=1";
                break;
            case MFBImageInfo.ImageClass.Aircraft:
                m_szLinkTemplate = "~/member/EditAircraft.aspx?a=1&id={0}";
                break;
            case MFBImageInfo.ImageClass.Endorsement:
                fNumericKeySort = false;
                break;
            case MFBImageInfo.ImageClass.BasicMed:
                break;
        }

        szBase = MFBImageInfo.BasePathFromClass(CurrentSource);

        bool fIsSync = (util.GetIntParam(Request, "sync", 0) != 0);
        bool fIsS3Orphan = (util.GetIntParam(Request, "dels3orphan", 0) != 0);
        int cAutoMigrate = util.GetIntParam(Request, "automigrate", 0);

        List<DirKey> lstDk = null;

        if (!IsPostBack)
        {
            if (fIsSync)
            {
                lstDk = new List<DirKey>();

                if (!String.IsNullOrEmpty(szBase))
                {
                    DirectoryInfo dir = new DirectoryInfo(System.Web.Hosting.HostingEnvironment.MapPath(szBase));
                    DirectoryInfo[] rgSubDir = dir.GetDirectories();
                    int i = 0;
                    foreach (DirectoryInfo di in rgSubDir)
                    {
                        // Delete the directory if it is empty
                        FileInfo[] rgfi = di.GetFiles();
                        DirectoryInfo[] rgdi = di.GetDirectories();

                        if (rgfi.Length == 0 && rgdi.Length == 0)
                        {
                            di.Delete();
                            continue;
                        }

                        DirKey dk = new DirKey();

                        dk.Key = di.Name;
                        if (fNumericKeySort)
                        {
                            try
                            {
                                dk.SortID = Convert.ToInt32(dk.Key);
                            }
                            catch
                            {
                                dk.SortID = i;
                            }
                        }
                        else
                            dk.SortID = i;

                        i++;

                        lstDk.Add(dk);
                    }
                }
                lstDk.Sort();
            }
            else if (cAutoMigrate == 0)
            {
                // Get the total # of image rows
                DBHelper dbh = new DBHelper(String.Format("SELECT COUNT(DISTINCT imagekey) AS NumRows FROM images WHERE virtpathid={0}", (int)CurrentSource));
                dbh.ReadRow((comm) => { }, (dr) => { TotalImageRows = Convert.ToInt32(dr["NumRows"]); });
                CurrentImageRowOffset = 0;
            }
        }

        if (fIsSync)
            SyncImages(lstDk);
        else if (fIsS3Orphan)
            DeleteS3Orphans();
        else if (cAutoMigrate != 0)
        {
            txtLimitFiles.Text = Math.Min(Math.Max(cAutoMigrate, 10), 100).ToString();
            btnMigrateImages_Click(sender, e);
        }
        else
            UpdateGrid();
    }

    #region Delete S3 Orphans
    protected void DeleteS3Orphans()
    {
        Response.Clear();
        Response.Write(String.Format("<html><head><link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" /></head><body><p>", VirtualPathUtility.ToAbsolute("~/Public/stylesheet.css")));

        // Get a list of all of the images in the DB for this category:
        UpdateProgress(1, 0, String.Format("Getting images for {0} from S3", CurrentSource.ToString()));

        bool fIsLiveSite = Branding.CurrentBrand.MatchesHost(Request.Url.Host);
        bool fIsPreview = !String.IsNullOrEmpty(Request["preview"]);

        AWSImageManagerAdmin.ADMINDeleteS3Orphans(CurrentSource,
            (cFiles, cBytesTotal, cOrphans, cBytesToFree) =>
            { UpdateProgress(4, 100, String.Format("{0} orphaned files ({1:#,##0} bytes) found out of {2} files ({3:#,##0} bytes).", cOrphans, cBytesToFree, cFiles, cBytesTotal)); },
            () => { UpdateProgress(2, 0, "S3 Enumeration done, getting files from DB");},
            (szKey, percent) =>
            {
                UpdateProgress(3, percent, String.Format("{0}: {1}", fIsPreview ? "PREVIEW" : (fIsLiveSite ? "ACTUAL" : "NOT LIVE SITE"), szKey));
                return !fIsPreview && fIsLiveSite;
            });

        Response.Write("</p></body></html>");
        Response.End();
    }
    #endregion

    #region Sync Images to DB
    protected void SyncImages(List<DirKey> lstDk)
    {
        Response.Clear();
        Response.Write(String.Format("<html><head><link href=\"{0}\" rel=\"stylesheet\" type=\"text/css\" /></head><body><p>", VirtualPathUtility.ToAbsolute("~/Public/stylesheet.css")));

        DateTime dtStart = DateTime.Now;
        int cNewImages = 0;
        int cChangedImages = 0;
        int cDeletedImages = 0;
        int cImagesExamined = 0;
        List<MFBImageInfo> lstMfbiiNewOrChanged = new List<MFBImageInfo>();

        try
        {
            // Get a list of all of the images in the DB for this category:
            UpdateProgress(1, 0, String.Format("Getting images for {0} from DB", CurrentSource.ToString()));
            Dictionary<string, MFBImageInfo> dictDBResults = MFBImageInfo.AllImagesForClass(CurrentSource);

            int cDBEntriesToStart = dictDBResults.Count;

            // Get a list of all of the images in this category:
            int cDirectories = lstDk.Count;
            int cDirectoriesProcessed = 0;
            int c5Percent = Math.Max(1, cDirectories / 20);

            UpdateProgress(2, 0, String.Format("Getting images for {0} ({1} folders)...", CurrentSource.ToString(), cDirectories));
            ImageList il = new ImageList();
            il.Class = CurrentSource;
            foreach (DirKey dk in lstDk)
            {
                il.Key = dk.Key;
                il.Refresh(true);
                foreach (MFBImageInfo mfbii in il.ImageArray)
                {
                    MFBImageInfo mfbiiMatch = dictDBResults.ContainsKey(mfbii.PrimaryKey) ? dictDBResults[mfbii.PrimaryKey] : null;
                    if (mfbiiMatch == null)
                    {
                        // If no match was found, it's a new image
                        lstMfbiiNewOrChanged.Add(mfbii);
                        cNewImages++;
                    }
                    else
                    {
                        bool fCommentChanged = String.Compare(mfbii.Comment, mfbiiMatch.Comment, StringComparison.CurrentCultureIgnoreCase) != 0;
                        bool fLocChanged = !MyFlightbook.Geography.LatLong.AreEqual(mfbii.Location, mfbiiMatch.Location);

                        // if it's changed, we need to update it.
                        if (fCommentChanged || fLocChanged)
                        {
                            if (mfbii.Location != null && !mfbii.Location.IsValid)
                                mfbii.Location = null;

                            UpdateProgress(4, (100 * cDirectoriesProcessed) / cDirectories, String.Format("Changed {0}:{1}{2}",
                                mfbii.PrimaryKey,
                                fCommentChanged ? String.Format("<br />&nbsp;Comment: {0} => {1}", mfbiiMatch.Comment, mfbii.Comment) : string.Empty,
                                fLocChanged ? String.Format("<br />&nbsp;Location: {0} => {1}", (mfbiiMatch.Location == null ? "null" : mfbiiMatch.Location.ToString()), (mfbii.Location == null ? "null" : mfbii.Location.ToString())) : string.Empty));

                            lstMfbiiNewOrChanged.Add(mfbii);
                            cChangedImages++;
                        }

                        // Now remove it from the list of DB images.
                        dictDBResults.Remove(mfbii.PrimaryKey);
                        mfbiiMatch.UnCache();
                        mfbiiMatch = null;  // save some memory?
                    } 
                    mfbii.UnCache();

                    cImagesExamined++;
                }

                il.Clear();

                if (++cDirectoriesProcessed % c5Percent == 0)
                {
                    GC.Collect();
                    UpdateProgress(2, (100 * cDirectoriesProcessed) / cDirectories, String.Format("{0} Folders processed, {1} images found", cDirectoriesProcessed, cImagesExamined));
                }
            }

            UpdateProgress(3, 100, String.Format("Elapsed Time: {0} seconds", DateTime.Now.Subtract(dtStart).TotalSeconds));

            UpdateProgress(4, 0, String.Format("{0} image files found, {1} images in DB", cImagesExamined, cDBEntriesToStart));

            // Now see if anything got deleted but not from the DB
            int cImagesToDelete = dictDBResults.Values.Count;
            if (String.IsNullOrEmpty(Request["preview"]))
            {
                UpdateProgress(5, 0, String.Format("{0} images found in DB that weren't found as files; deleting these.", cImagesToDelete));
                foreach (MFBImageInfo mfbii in dictDBResults.Values)
                {
                    mfbii.DeleteFromDB();
                    cDeletedImages++;

                    if (cDeletedImages % 100 == 0)
                        UpdateProgress(5, (100 * cDeletedImages) / cImagesToDelete, String.Format("Deleted {0} images from DB", cDeletedImages));
                }

                // And finally add new images back
                UpdateProgress(6, 0, String.Format("{0} new and {1} changed images (total of {2}) to be added to/updated in the DB", cNewImages, cChangedImages, lstMfbiiNewOrChanged.Count));
                lstMfbiiNewOrChanged.ForEach((mfbii) => { mfbii.ToDB(); });
            }
            else
            {
                UpdateProgress(5, 0, String.Format("{0} images found in DB that weren't found as files; these are:", cImagesToDelete));
                foreach (MFBImageInfo mfbii in dictDBResults.Values)
                    UpdateProgress(5, 0, mfbii.PathThumbnail);
                UpdateProgress(6, 0, String.Format("{0} images found that are new or changed that weren't in DB; these are:", cNewImages + cChangedImages));
                lstMfbiiNewOrChanged.ForEach((mfbii) => { UpdateProgress(6, 0, mfbii.PathThumbnail); });
            }

            UpdateProgress(6, 100, "Finished!");
        }
        catch (Exception ex)
        {
            Response.Write(String.Format("<p class=\"error\">Exception sync'ing DB: {0}</p>", ex.Message));
        }

        Response.Write("</p></body></html>");
        Response.End();
    }

    protected void UpdateProgress(int step, int PercentComplete, string Message)
    {
        // Write out the parent script callback.
        Response.Write(String.Format(CultureInfo.CurrentCulture, "Step {0}: {1}% {2}<br />", step, PercentComplete, Message));
        Response.Flush();
    }
    #endregion

    #region GridView Management

    protected void UpdateGrid()
    {
        List<string> lstKeys = new List<string>();
        Dictionary<string, List<MFBImageInfo>> dictRows = QueryResults = MFBImageInfo.FromDB(CurrentSource, CurrentImageRowOffset, PageSize, out lstKeys);
        gvImages.DataSource = lstKeys;
        gvImages.DataBind();
        int curOffset = CurrentImageRowOffset;
        int lastRow = CurrentImageRowOffset + dictRows.Count;
        lblCurrentImageRange.Text = lblCurrentImageRange2.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.ImageRowsHeader, curOffset + 1, lastRow, TotalImageRows);
        btnPrevRange.Enabled = (CurrentImageRowOffset > 0);
        btnNextRange.Enabled = (lastRow < TotalImageRows);
    }

    protected void ImagesRowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e != null && e.Row.RowType == DataControlRowType.DataRow)
        {
            Dictionary<string, List<MFBImageInfo>> dictRows = QueryResults;
            string szKey = (string) e.Row.DataItem;
            Controls_mfbImageList mfbil = (Controls_mfbImageList) e.Row.FindControl("mfbImageList1");
            HyperLink lnk = (HyperLink)e.Row.FindControl("lnkID");

            List<MFBImageInfo> lst = (dictRows.ContainsKey(szKey) ? dictRows[szKey] : null);
            if (lst == null || lst.Count == 0)
                return;

            MFBImageInfo mfbii0 = lst[0];
            if (CurrentSource == MFBImageInfo.ImageClass.Flight)
            {
                ((LinkButton)e.Row.FindControl("lnkGetAircraft")).CommandArgument = szKey;
                ((Panel)e.Row.FindControl("pnlResolveAircraft")).Visible = true;
            }
            lnk.Text = szKey;
            lnk.NavigateUrl = (String.IsNullOrEmpty(m_szLinkTemplate) ? String.Empty : String.Format(CultureInfo.InvariantCulture, m_szLinkTemplate, szKey));
            mfbil.ImageClass = CurrentSource;
            mfbil.Key = szKey;
            mfbil.Images = new ImageList(CurrentSource, szKey, lst.ToArray());
            mfbil.Refresh(false);
        }
    }

    protected void gvImages_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e != null && String.Compare(e.CommandName, "GetAircraft", StringComparison.OrdinalIgnoreCase) == 0)
        {
            int idFlight = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            LogbookEntry le = new LogbookEntry(idFlight, Page.User.Identity.Name, LogbookEntry.LoadTelemetryOption.None, true);
            GridViewRow grow = (GridViewRow)((LinkButton)e.CommandSource).NamingContainer;
            PlaceHolder plc = (PlaceHolder)grow.FindControl("plcAircraft");
            HyperLink lnkAc = new HyperLink();
            plc.Controls.Add(lnkAc);
            lnkAc.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}&a=1", le.AircraftID);
            lnkAc.Target = "_blank";
            lnkAc.Text = le.TailNumDisplay;
        }
    }

    protected void btnPrevRange_Click(object sender, EventArgs e)
    {
        CurrentImageRowOffset -= PageSize;
        UpdateGrid();
    }
    protected void btnNextRange_Click(object sender, EventArgs e)
    {
        CurrentImageRowOffset += PageSize;
        UpdateGrid();
    }
    #endregion

    #region Image Migration to S3
    protected void btnMigrateImages_Click(object sender, EventArgs e)
    {
        Int32 cBytesDone = 0;
        Int32 cFilesDone = 0;
        Int32 cMBytesLimit = 100;
        Int32 cFilesLimit = 100;

        if (!Int32.TryParse(txtLimitMB.Text, out cMBytesLimit))
            cMBytesLimit = 100;

        Int32 cBytesLimit = cMBytesLimit * 1024 * 1024;

        if (!Int32.TryParse(txtLimitFiles.Text, out cFilesLimit))
            cFilesLimit = 100;

        Dictionary<string, List<MFBImageInfo>> images = ImageDictionary;
        foreach (string szKey in SortedKeys)
        {
            if (cBytesDone > cBytesLimit || cFilesDone >= cFilesLimit)
                break;
            AWSImageManagerAdmin im = new AWSImageManagerAdmin();
            foreach (MFBImageInfo mfbii in images[szKey])
            {
                Int32 cBytes = im.ADMINMigrateToS3(mfbii);
                if (cBytes >= 0)  // migration occured
                {
                    cBytesDone += cBytes;
                    cFilesDone++;
                }

                if (cBytesDone > cBytesLimit || cFilesDone >= cFilesLimit)
                    break;
            }
        }

        lblMigrateResults.Text = String.Format(CultureInfo.CurrentCulture, Resources.Admin.MigrateImagesTemplate, cFilesDone, cBytesDone);
    }
    #endregion
}