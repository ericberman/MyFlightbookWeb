using System;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2007-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
 * Flow here inspired by https://www.troyhunt.com/everything-you-ever-wanted-to-know/.
 * Goal is to no longer send a password in clear-text.
 * 
 * So instead the steps are:
 * a) Enter your email address
 * b) We send a link to that address (even if it's bogus; we fail silently so that you can't tell if someone even has an account)
 * c) User clicks the link to view their Q/A
 * d) Upon providing the correct answer, they can provide a new password.  A temporary password is generated in the process, but they never see it.
 * 
*******************************************************/

namespace MyFlightbook.LogonPages
{
    public partial class ResetPass : Page
    {
        private const string szVSRequest = "vsPendingResetRequest";
        private const string szVSTempPass = "vsTempPasswd";

        protected PasswordResetRequest CurrentRequest
        {
            get { return (PasswordResetRequest)ViewState[szVSRequest]; }
            set { ViewState[szVSRequest] = value; }
        }

        protected string TempPassword
        {
            get { return (string)ViewState[szVSTempPass]; }
            set { ViewState[szVSTempPass] = value; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                string sz = util.GetStringParam(Request, "t");
                if (sz.Length > 36)
                    sz = sz.Substring(0, 36);
                InitializeForRequest(sz);
            }
        }

        protected void InitializeForRequest(string szResetID)
        {
            if (!String.IsNullOrEmpty(szResetID))
            {
                try
                {
                    PasswordResetRequest prr = CurrentRequest = new PasswordResetRequest(szResetID);
                    CheckStatus(prr);   // verify that it's an OK request.
                    Profile pf = Profile.GetUser(prr.UserName);
                    if (pf.PreferenceExists(MFBConstants.keyTFASettings))
                    {
                        tfaReset.AuthCode = pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
                        mvResetPass.SetActiveView(vwVerifyTFAPass);
                    }
                    else
                        mvResetPass.SetActiveView(vwVerify);

                    lblQuestion.Text = HttpUtility.HtmlEncode(Membership.GetUser(prr.UserName).PasswordQuestion);
                }
                catch (Exception ex) when (ex is ArgumentOutOfRangeException)
                {
                    lblErr.Text = Resources.LocalizedText.ResetPasswordInvalidRequest;
                }
                catch (Exception ex) when (ex is InvalidOperationException)
                {
                    lblErr.Text = ex.Message;
                }
            }
        }

        private static void CheckStatus(PasswordResetRequest prr)
        {
            if (prr == null)
                throw new ArgumentNullException(nameof(prr));

            switch (prr.Status)
            {
                case PasswordResetRequest.RequestStatus.Expired:
                    throw new InvalidOperationException(Resources.LocalizedText.ResetPasswordRequestExpired);
                case PasswordResetRequest.RequestStatus.Failed:
                case PasswordResetRequest.RequestStatus.Success:
                    throw new InvalidOperationException(Resources.LocalizedText.ResetPasswordRequestAlreadyUsed);
            }
        }

        protected void btnSendEmail_Click(object sender, EventArgs e)
        {
            Page.Validate("resetPassEmail");
            if (Page.IsValid)
            {
                lblEmailSent.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ResetPassEmailSent, HttpUtility.HtmlEncode(txtEmail.Text));
                mvResetPass.SetActiveView(vwEmailSent);
                string szUser = Membership.GetUserNameByEmail(txtEmail.Text);
                if (String.IsNullOrEmpty(szUser))
                {
                    // fail silently - don't do anything to acknowledge the existence or lack thereof of an account
                }
                else
                {
                    PasswordResetRequest prr = new PasswordResetRequest() { UserName = szUser };
                    prr.FCommit();

                    string szURL = "https://" + Request.Url.Host + Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "t=" + HttpUtility.UrlEncode(prr.ID);
                    string szEmailBody = Branding.ReBrand(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ResetPassEmail)).Replace("<% RESET_LINK %>", szURL);
                    MyFlightbook.Profile pf = MyFlightbook.Profile.GetUser(szUser);

                    util.NotifyUser(Branding.ReBrand(Resources.LocalizedText.ResetPasswordSubjectNew), szEmailBody, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
                }
            }
        }

        protected void btnSubmitAnswer_Click(object sender, EventArgs e)
        {
            Page.Validate("vgAnswer");
            if (Page.IsValid)
            {
                try
                {
                    string szUser = CurrentRequest.UserName;
                    TempPassword = Membership.GetUser(szUser).ResetPassword(txtAnswer.Text);
                    mvResetPass.SetActiveView(vwNewPass);
                }
                catch (MembershipPasswordException ex)
                {
                    CurrentRequest.Status = PasswordResetRequest.RequestStatus.Failed;
                    CurrentRequest.FCommit();
                    lblErr.Text = ex.Message;
                }
            }
        }

        protected void btnUpdatePass_Click(object sender, EventArgs e)
        {
            Page.Validate("valPassword");
            if (Page.IsValid)
            {
                try
                {
                    if (txtNewPass.Text.Length < 8)
                        throw new MyFlightbookException(Resources.Profile.errBadPasswordLength);
                    if (!Membership.Provider.ChangePassword(CurrentRequest.UserName, TempPassword, txtNewPass.Text))
                        throw new MyFlightbookException(Resources.Profile.errChangePasswordFailed);

                    if (Membership.ValidateUser(CurrentRequest.UserName, txtNewPass.Text))
                        FormsAuthentication.SetAuthCookie(CurrentRequest.UserName, false);

                    CurrentRequest.Status = PasswordResetRequest.RequestStatus.Success;
                    CurrentRequest.FCommit();
                    Response.Redirect("~/Default.aspx");
                }
                catch (MyFlightbookException ex)
                {
                    CurrentRequest.Status = PasswordResetRequest.RequestStatus.Failed;
                    CurrentRequest.FCommit();
                    lblErr.Text = ex.Message;
                }
            }
        }

        protected void tfaReset_TFACodeFailed(object sender, EventArgs e)
        {
            lblTFAReset.Visible = true;
        }

        protected void tfaReset_TFACodeVerified(object sender, EventArgs e)
        {
            mvResetPass.SetActiveView(vwVerify);
        }
    }
}