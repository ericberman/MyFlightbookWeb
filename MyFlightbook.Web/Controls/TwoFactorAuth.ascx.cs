using Google.Authenticator;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls
{
    public partial class TwoFActorAuth : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                Profile pf = Profile.GetUser(Page.User.Identity.Name);
                TwoFactorAuthVerifyCode.AuthCode = pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
                mvTFAState.SetActiveView(String.IsNullOrEmpty(TwoFactorAuthVerifyCode.AuthCode) ? vwNoTFA : vwTFAActive);
            }
        }

        public Boolean GenerateTwoFactorAuthentication()
        {
            Guid guid = Guid.NewGuid();
            String uniqueUserKey = guid.ToString().Replace("-", string.Empty).Substring(0, 10);
            TwoFactorAuthVerifyCode.AuthCode = uniqueUserKey;

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            SetupCode setupInfo = tfa.GenerateSetupCode(Branding.CurrentBrand.AppName, Profile.GetUser(Page.User.Identity.Name).Email, uniqueUserKey, false, 3);
            if (setupInfo != null)
            {
                imgQR.Src = setupInfo.QrCodeSetupImageUrl;
                lblKey.Text = setupInfo.ManualEntryKey;
                return true;
            }
            return false;
        }

        protected void lnkEnableTFA_Click(object sender, EventArgs e)
        {
            GenerateTwoFactorAuthentication();
            mvTFAState.SetActiveView(vwAddTFA);
        }

        protected void lnkDisableTFA_Click(object sender, EventArgs e)
        {
            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            pf.SetPreferenceForKey(MFBConstants.keyTFASettings, null, true);
            mvTFAState.SetActiveView(vwNoTFA);
        }

        protected void TwoFactorAuthVerifyCode_TFACodeFailed(object sender, EventArgs e)
        {
            lblCodeResult.Text = Resources.Profile.TFACodeFailed;
            lblCodeResult.CssClass = "error";
        }

        protected void TwoFactorAuthVerifyCode_TFACodeVerified(object sender, EventArgs e)
        {
            lblCodeResult.Text = Resources.Profile.TFACodeValidated;
            lblCodeResult.CssClass = "success";

            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            pf.SetPreferenceForKey(MFBConstants.keyTFASettings, TwoFactorAuthVerifyCode.AuthCode);
            mvTFAState.SetActiveView(vwTFAActive);
        }
    }
}