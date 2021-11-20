using MyFlightbook.CloudStorage;
using System;
using System.Globalization;
using System.IO;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.MemberPages
{
    public partial class Download : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Master.SelectedTab = tabID.lbtDownload;

            if (!IsPostBack)
            {
                Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
                Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, pf.UserFullName);
                locDonate.Text = Branding.ReBrand(Resources.LocalizedText.CloudStorageDonate);

                lnkSaveDropbox.Enabled = !String.IsNullOrEmpty(pf.DropboxAccessToken);
                lnkSaveOneDrive.Enabled = pf.OneDriveAccessToken != null && !String.IsNullOrEmpty(pf.OneDriveAccessToken.RefreshToken);
                lnkSaveGoogleDrive.Enabled = pf.GoogleDriveAccessToken != null && !String.IsNullOrEmpty(pf.GoogleDriveAccessToken.RefreshToken);
            }
        }

        protected string CSVFileName
        {
            get { return new LogbookBackup(Profile.GetUser(Page.User.Identity.Name)).BackupFilename(Branding.CurrentBrand); }
        }

        protected void lnkDownloadCSV_Click(object sender, EventArgs e)
        {
            mfbDownload1.User = Page.User.Identity.Name;
            mfbDownload1.UpdateData();

            Response.Clear();
            Response.ContentType = "text/csv";
            // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
            string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}", CSVFileName);
            Response.AddHeader("Content-Disposition", szDisposition);
            mfbDownload1.ToStream(Response.OutputStream);
            Response.End();
        }

        protected void lnkDownloadImagesZip_Click(object sender, EventArgs e)
        {
            Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}", Branding.ReBrand(String.Format(CultureInfo.InvariantCulture, "{0}.zip", Resources.LocalizedText.ImagesBackupFilename)).Replace(" ", "-")));
            using (FileStream fs = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, Int16.MaxValue, FileOptions.DeleteOnClose))
            {
                new LogbookBackup(pf).WriteZipOfImagesToStream(fs, Branding.CurrentBrand);
                fs.Seek(0, SeekOrigin.Begin);
                fs.CopyTo(Response.OutputStream);
            }
            Response.End();
        }

        protected void ShowDropboxError(string message)
        {
            lblDropBoxFailure.Visible = true;
            lblDropBoxFailure.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.DropboxError, message);
        }

        protected async void lnkSaveDropbox_Click(object sender, EventArgs e)
        {
            Profile pf = MyFlightbook.Profile.GetUser(User.Identity.Name);
            LogbookBackup lb = new LogbookBackup(pf);

            try
            {
                if (await new MFBDropbox(pf).ValidateDropboxToken().ConfigureAwait(false) == MFBDropbox.TokenStatus.None)
                    return;

                Dropbox.Api.Files.FileMetadata result = await lb.BackupToDropbox().ConfigureAwait(false);

                if (ckIncludeImages.Checked)
                    result = await lb.BackupImagesToDropbox(Branding.CurrentBrand).ConfigureAwait(false);

                lblDropBoxSuccess.Visible = true;
            }
            catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
            {
                if (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath != null && ex.ErrorResponse.AsPath.Value.Reason.IsInsufficientSpace)
                    ShowDropboxError(Resources.LocalizedText.DropboxErrorOutOfSpace);
                else
                    ShowDropboxError(ex.Message);
            }
            catch (Exception ex) when (ex is Dropbox.Api.ApiException<Dropbox.Api.Auth.TokenFromOAuth1Error> || ex is Dropbox.Api.AuthException || ex is Dropbox.Api.BadInputException || ex is Dropbox.Api.HttpException || ex is UnauthorizedAccessException || ex is MyFlightbookException)
            {
                ShowDropboxError(ex.Message);
            }
        }

        protected async void lnkSaveOneDrive_Click(object sender, EventArgs e)
        {
            MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            LogbookBackup lb = new LogbookBackup(pf);

            if (pf.OneDriveAccessToken == null || String.IsNullOrEmpty(pf.OneDriveAccessToken.RefreshToken))
                return;
            try
            {
                OneDrive od = new OneDrive(pf);
                await lb.BackupToOneDrive(od).ConfigureAwait(false);

                if (ckIncludeImages.Checked)
                    await lb.BackupImagesToOneDrive(od).ConfigureAwait(false);

                // if we are here we were successful, so save the updated refresh token
                pf.OneDriveAccessToken = od.AuthState;
                pf.FCommit();

                lblDropBoxSuccess.Visible = true;
            }
            catch (Exception ex) when (ex is OneDriveMFBException || ex is MyFlightbookException || !(ex is OutOfMemoryException))
            {
                ShowDropboxError(ex.Message);
            }
        }


        protected async void lnkSaveGoogleDrive_Click(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            LogbookBackup lb = new LogbookBackup(pf);

            if (pf.GoogleDriveAccessToken == null || String.IsNullOrEmpty(pf.GoogleDriveAccessToken.RefreshToken))
                return;

            try
            {
                GoogleDrive gd = new GoogleDrive(pf);

                await lb.BackupToGoogleDrive(gd).ConfigureAwait(false);

                if (ckIncludeImages.Checked)
                    await lb.BackupImagesToGoogleDrive(gd).ConfigureAwait(false);

                // if we are here we were successful, so save the updated refresh token
                pf.GoogleDriveAccessToken = gd.AuthState;
                pf.FCommit();

                lblDropBoxSuccess.Visible = true;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                ShowDropboxError(ex.Message);
            }
        }
    }
}