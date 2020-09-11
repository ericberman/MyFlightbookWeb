using MyFlightbook;
using MyFlightbook.CloudStorage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2020 MyFlightbook LLC
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

            rowInsDownload.Visible = pnlSkyWatch.Visible = util.GetIntParam(Request, "ins", 0) != 0;
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
        using (MemoryStream ms = new MemoryStream())
        {
            new LogbookBackup(pf).WriteZipOfImagesToStream(ms, Branding.CurrentBrand);
            Response.BinaryWrite(ms.ToArray());
        }
        Response.End();
    }

    private void WriteInsurenceZip(Action<String, MemoryStream> dispatch)
    {
        if (dispatch == null)
            throw new ArgumentNullException(nameof(dispatch));

        Profile pf = Profile.GetUser(Page.User.Identity.Name);
        mfbDownload1.User = pf.UserName;
        mfbDownload1.UpdateData();

        string szFile = Regex.Replace(Branding.ReBrand(String.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", Branding.CurrentBrand.AppName, pf.UserFullName, Resources.LocalizedText.InsuranceBackupName)).Replace(" ", "-"), "[^0-9a-zA-Z-]", string.Empty);
        using (MemoryStream ms = new MemoryStream())
        {
            LogbookBackup lb = new LogbookBackup(pf) { IncludeImages = false };
            lb.WriteZipOfImagesToStream(ms, Branding.CurrentBrand, (zip) => {
                // Add the flights
                ZipArchiveEntry zae = zip.CreateEntry(CSVFileName);
                using (StreamWriter sw = new StreamWriter(zae.Open()))
                {
                    sw.Write('\uFEFF');   // UTF-8 BOM.
                    sw.Write(mfbDownload1.CSVData());
                }

                // And add the user's information, in JSON format.
                zae = zip.CreateEntry("PilotInfo.JSON");
                using (StreamWriter sw = new StreamWriter(zae.Open()))
                {
                    sw.Write(LogbookBackup.PilotInfoAsJSon(pf));
                }
            });

            dispatch(szFile, ms);
        }
    }

    protected void lnkDownloadInsurance_Click(object sender, EventArgs e)
    {
        Response.Clear();
        Response.ContentType = "application/octet-stream";

        WriteInsurenceZip((szFile, stream) =>
        {
            Response.AddHeader("Content-Disposition", String.Format(CultureInfo.InvariantCulture, "attachment;filename={0}.zip", szFile));
            Response.BinaryWrite(stream.ToArray());
        });
        Response.End();
    }


    protected void lnkPostInsurance_Click(object sender, EventArgs e)
    {
        const string szSkywatchEndpoint = "https://user.skywatch.ai/aviation/analyze_logs";

        WriteInsurenceZip((szFile, stream) =>
        {
            List<IDisposable> objectsToDispose = new List<IDisposable>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (MultipartFormDataContent formData = new MultipartFormDataContent())
                    {
                        StreamContent strc = new StreamContent(stream);
                        objectsToDispose.Add(strc);
                        formData.Add(strc, "Files", szFile);

                        StringContent sc = new StringContent(Profile.GetUser(Page.User.Identity.Name).Email);
                        objectsToDispose.Add(sc);
                        formData.Add(sc, "Email");

                        sc = new StringContent("(Multiple)");
                        objectsToDispose.Add(sc);
                        formData.Add(sc, "AircraftModel");

                        sc = new StringContent("(Unspecified)"); 
                        objectsToDispose.Add(sc);
                        formData.Add(sc, "NNumber");

                        HttpResponseMessage response = client.PostAsync(new Uri(szSkywatchEndpoint), formData).Result;
                        response.EnsureSuccessStatusCode();

                        // If we're here, we were successful.
                        lblInsErr.Text = Resources.LocalizedText.InsurancePostSuccessful;
                        lblInsErr.CssClass = "success";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                lblInsErr.Text = String.Format(CultureInfo.CurrentCulture, "{0}: {1}", Resources.LocalizedText.InsurancePostFailed, ex.Message);
            }
            finally
            {
                foreach (IDisposable disposable in objectsToDispose)
                    disposable.Dispose();
            }
        });
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
            if (await new MFBDropbox().ValidateDropboxToken(pf, true).ConfigureAwait(false) == MFBDropbox.TokenStatus.None)
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
            OneDrive od = new OneDrive(pf.OneDriveAccessToken);
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
        MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        LogbookBackup lb = new LogbookBackup(pf);

        if (pf.GoogleDriveAccessToken == null || String.IsNullOrEmpty(pf.GoogleDriveAccessToken.RefreshToken))
            return;

        try
        {
            GoogleDrive gd = new GoogleDrive(pf.GoogleDriveAccessToken);

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
