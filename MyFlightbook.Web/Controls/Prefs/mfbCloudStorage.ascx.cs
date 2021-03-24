using DotNetOpenAuth.OAuth2;
using MyFlightbook.CloudStorage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbCloudStorage : System.Web.UI.UserControl
    {
        protected Profile CurrentUser { get { return Profile.GetUser(Page.User.Identity.Name); } }
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public void InitCloudProviders()
        {
            List<StorageID> lstCloud = new List<StorageID>(CurrentUser.AvailableCloudProviders);
            StorageID defaultStorage = CurrentUser.BestCloudStorage;
            foreach (StorageID sid in lstCloud)
                cmbDefaultCloud.Items.Add(new ListItem(CloudStorageBase.CloudStorageName(sid), sid.ToString()) { Selected = defaultStorage == sid });
            pnlDefaultCloud.Visible = lstCloud.Count > 1;   // only show a choice if more than one cloud provider is set up

            mvDropBoxState.SetActiveView(lstCloud.Contains(StorageID.Dropbox) ? vwDeAuthDropbox : vwAuthDropBox);
            mvGDriveState.SetActiveView(lstCloud.Contains(StorageID.GoogleDrive) ? vwDeAuthGDrive : vwAuthGDrive);
            mvOneDriveState.SetActiveView(lstCloud.Contains(StorageID.OneDrive) ? vwDeAuthOneDrive : vwAuthOneDrive);

            locAboutCloudStorage.Text = Branding.ReBrand(Resources.Profile.AboutCloudStorage);
            lnkAuthDropbox.Text = Branding.ReBrand(Resources.Profile.AuthorizeDropbox);
            lnkDeAuthDropbox.Text = Branding.ReBrand(Resources.Profile.DeAuthDropbox);
            locDropboxIsAuthed.Text = Branding.ReBrand(Resources.Profile.DropboxIsAuthed);
            lnkAuthorizeGDrive.Text = Branding.ReBrand(Resources.Profile.AuthorizeGDrive);
            lnkDeAuthGDrive.Text = Branding.ReBrand(Resources.Profile.DeAuthGDrive);
            locGoogleDriveIsAuthed.Text = Branding.ReBrand(Resources.Profile.GDriveIsAuthed);
            lnkAuthorizeOneDrive.Text = Branding.ReBrand(Resources.Profile.AuthorizeOneDrive);
            lnkDeAuthOneDrive.Text = Branding.ReBrand(Resources.Profile.DeAuthOneDrive);
            locOneDriveIsAuthed.Text = Branding.ReBrand(Resources.Profile.OneDriveIsAuthed);

            rblCloudBackupAppendDate.SelectedValue = CurrentUser.OverwriteCloudBackup.ToString(CultureInfo.InvariantCulture);
        }

        public void HandleOAuthRedirect()
        {
            if (!String.IsNullOrEmpty(Request.Params[MFBDropbox.szParamDropboxAuth])) // redirect from Dropbox oAuth request.
            {
                CurrentUser.DropboxAccessToken = JsonConvert.SerializeObject(new MFBDropbox().ConvertToken(Request));
                CurrentUser.FCommit();

                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
            }
            if (!String.IsNullOrEmpty(Request.Params[GoogleDrive.szParamGDriveAuth])) // redirect from GDrive oAuth request.
            {
                if (String.IsNullOrEmpty(util.GetStringParam(Request, "error")))
                {
                    CurrentUser.GoogleDriveAccessToken = new GoogleDrive().ConvertToken(Request);
                    CurrentUser.FCommit();
                }

                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
            }
            if (!String.IsNullOrEmpty(Request.Params[GooglePhoto.szParamGPhotoAuth])) // redirect from Google Photo oAuth request
            {
                if (String.IsNullOrEmpty(util.GetStringParam(Request, "error")))
                {
                    IAuthorizationState token = new GooglePhoto().ConvertToken(Request);
                    string szTokenJSON = JsonConvert.SerializeObject(token);
                    CurrentUser.SetPreferenceForKey(GooglePhoto.PrefKeyAuthToken, szTokenJSON);
                }
                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=social", Request.Path));
            }
            if (!String.IsNullOrEmpty(Request.Params[OneDrive.szParam1DriveAuth])) // redirect from OneDrive oAuth request.
            {
                CurrentUser.OneDriveAccessToken = (IAuthorizationState)Session[OneDrive.TokenSessionKey];
                CurrentUser.FCommit();

                Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
            }
        }

        protected void lnkAuthDropbox_Click(object sender, EventArgs e)
        {
            new MFBDropbox().Authorize(Request, ResolveUrl("~/Member/EditProfile.aspx/pftPrefs"), MFBDropbox.szParamDropboxAuth);
        }

        protected void lnkDeAuthDropbox_Click(object sender, EventArgs e)
        {
            CurrentUser.DropboxAccessToken = null;
            CurrentUser.FCommit();
            mvDropBoxState.SetActiveView(vwAuthDropBox);
        }

        protected void lnkAuthorizeOneDrive_Click(object sender, EventArgs e)
        {
            Uri uri = new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Url.Host, VirtualPathUtility.ToAbsolute("~/public/OneDriveRedir.aspx")));
            new OneDrive().Authorize(uri);
        }

        protected void lnkDeAuthOneDrive_Click(object sender, EventArgs e)
        {
            CurrentUser.OneDriveAccessToken = null;
            CurrentUser.FCommit();
            mvOneDriveState.SetActiveView(vwAuthOneDrive);
        }

        protected void lnkAuthorizeGDrive_Click(object sender, EventArgs e)
        {
            new GoogleDrive().Authorize(Request, Request.Url.AbsolutePath, GoogleDrive.szParamGDriveAuth);
        }

        protected void lnkDeAuthGDrive_Click(object sender, EventArgs e)
        {
            CurrentUser.GoogleDriveAccessToken = null;
            CurrentUser.FCommit();
            mvGDriveState.SetActiveView(vwAuthGDrive);
        }

        protected void cmbDefaultCloud_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Enum.TryParse<StorageID>(cmbDefaultCloud.SelectedValue, out StorageID sid))
            {
                CurrentUser.DefaultCloudStorage = sid;
                CurrentUser.FCommit();
            }
        }

        protected void rblCloudBackupAppendDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentUser.OverwriteCloudBackup = Convert.ToBoolean(rblCloudBackupAppendDate.SelectedValue, CultureInfo.InvariantCulture);
            CurrentUser.FCommit();
        }
    }
}