using Microsoft.OneDrive.Sdk;
using MyFlightbook;
using MyFlightbook.CloudStorage;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_Download : System.Web.UI.Page
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

    protected void lnkDownloadCSV_Click(object sender, EventArgs e)
    {
        mfbDownload1.User = Page.User.Identity.Name;
        mfbDownload1.UpdateData();

        Response.Clear();
        Response.ContentType = "text/csv";
        // Give it a name that is the brand name, user's name, and date.  Convert spaces to dashes, and then strip out ANYTHING that is not alphanumeric or a dash.
        string szFilename = String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, MyFlightbook.Profile.GetUser(Page.User.Identity.Name).UserFullName, DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Replace(" ", "-");
        string szDisposition = String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.csv", Regex.Replace(szFilename, "[^0-9a-zA-Z-]", ""));
        Response.AddHeader("Content-Disposition", szDisposition);
        Response.Write('\uFEFF');   // UTF-8 BOM.
        Response.Write(mfbDownload1.CSVData());
        Response.End();
    }

    protected void lnkDownloadImagesZip_Click(object sender, EventArgs e)
    {
        Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        Response.Clear();
        Response.ContentType = "application/octet-stream";
        Response.AddHeader("Content-Disposition", String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}", Branding.ReBrand(String.Format(CultureInfo.InvariantCulture, "{0}.zip", Resources.LocalizedText.ImagesBackupFilename)).Replace(" ", "-")));
        using (System.IO.MemoryStream ms = new LogbookBackup(pf).ZipOfImagesForUser(Branding.CurrentBrand))
            Response.BinaryWrite(ms.ToArray());
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
            if (await new MFBDropbox().ValidateDropboxToken(pf, true, true) == MFBDropbox.TokenStatus.None)
                return;

            Dropbox.Api.Files.FileMetadata result = await lb.BackupToDropbox();

            if (ckIncludeImages.Checked)
                result = await lb.BackupImagesToDropbox(Branding.CurrentBrand);

            lblDropBoxSuccess.Visible = true;
        }
        catch (Dropbox.Api.ApiException<Dropbox.Api.Auth.TokenFromOAuth1Error> ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (Dropbox.Api.AuthException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (Dropbox.Api.ApiException<Dropbox.Api.Files.UploadError> ex)
        {
            if (ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath != null && ex.ErrorResponse.AsPath.Value.Reason.IsInsufficientSpace)
                ShowDropboxError(Resources.LocalizedText.DropboxErrorOutOfSpace);
            else
                ShowDropboxError(ex.Message);
        }
        catch (Dropbox.Api.BadInputException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (Dropbox.Api.HttpException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (MyFlightbookException ex)
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
            OneDrive od = new OneDrive(pf.OneDriveAccessToken);
            await lb.BackupToOneDrive(od);

            if (ckIncludeImages.Checked)
                await lb.BackupImagesToOneDrive(od);

            // if we are here we were successful, so save the updated refresh token
            pf.OneDriveAccessToken.RefreshToken = od.AuthState.RefreshToken;
            pf.FCommit();

            lblDropBoxSuccess.Visible = true;
        }
        catch (OneDriveException ex)
        {
            ShowDropboxError(OneDrive.MessageForException(ex));
        }
        catch (MyFlightbookException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowDropboxError(ex.Message);
        }
    }


    protected async void lnkSaveGoogleDrive_Click(object sender, EventArgs e)
    {
        MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        LogbookBackup lb = new LogbookBackup(pf);

        if (pf.GoogleDriveAccessToken == null || String.IsNullOrEmpty(pf.GoogleDriveAccessToken.RefreshToken))
            return;

        try
        {
            GoogleDrive gd = new GoogleDrive(pf.GoogleDriveAccessToken);

            if (await gd.RefreshAccessToken())
                pf.FCommit();

            await lb.BackupToGoogleDrive(gd);

            if (ckIncludeImages.Checked)
                await lb.BackupImagesToGoogleDrive(gd);

            lblDropBoxSuccess.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            ShowDropboxError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowDropboxError(ex.Message);
        }
    }
}
