using System;
using System.Web;
using System.Web.Security;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls
{
    public partial class mfbSignIn : System.Web.UI.UserControl
    {
        public string DefButtonUniqueID
        {
            get
            {
                Button btn = (Button)ctlSignIn.FindControl("LoginButton");
                return btn == null ? string.Empty : btn.UniqueID;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ctlSignIn.Focus();
            if (!IsPostBack)
            {
                HyperLink hl = (HyperLink)ctlSignIn.FindControl("CreateUserLink");
                hl.NavigateUrl += Request.Url.Query;
            }
        }

        protected void OnLoggingIn(object sender, LoginCancelEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            TextBox txtEmail = (TextBox)ctlSignIn.FindControl("txtEmail");
            TextBox txtUser = (TextBox)ctlSignIn.FindControl("UserName");

            if (txtEmail == null)
                throw new MyFlightbookException("No email field");
            if (txtUser == null)
                throw new MyFlightbookException("No username field");

            // Set the Username field based on the email address provided.
            string szUser = Membership.GetUserNameByEmail(txtEmail.Text);

            txtUser.Text = HttpUtility.HtmlEncode(szUser);

            // see if two-factor authentication is set up for this user
            // But only if the password provided is correct.
            Profile pf = Profile.GetUser(szUser);
            if (pf.PreferenceExists(MFBConstants.keyTFASettings))
            {
                TextBox txtPass = (TextBox)ctlSignIn.FindControl("Password");

                if (Membership.ValidateUser(szUser, txtPass.Text))
                {
                    TwoFactorAuthVerifyCode tfavc = (TwoFactorAuthVerifyCode)ctlSignIn.FindControl("tfavc");
                    tfavc.AuthCode = pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
                    MultiView mv = (MultiView)ctlSignIn.FindControl("mvSignIn");
                    mv.SetActiveView((View)ctlSignIn.FindControl("vwTFA"));
                    e.Cancel = true;
                    return;
                }

                // password didn't validate...fall through to regular error handling.
            }
        }

        protected void TwoFactorAuthVerifyCode_TFACodeFailed(object sender, EventArgs e)
        {
            Label lblCodeResult = (Label)ctlSignIn.FindControl("lblCodeResult");
            lblCodeResult.Text = Resources.Profile.TFACodeFailed;
            lblCodeResult.CssClass = "error";
        }

        protected void TwoFactorAuthVerifyCode_TFACodeVerified(object sender, EventArgs e)
        {
            // We already verified the password above.
            TextBox txtUser = (TextBox)ctlSignIn.FindControl("UserName");
            CheckBox ckRemember = (CheckBox)ctlSignIn.FindControl("RememberMe");
            FormsAuthentication.SetAuthCookie(txtUser.Text, ckRemember.Checked);
            Response.Redirect(String.IsNullOrEmpty(Request["ReturnUrl"]) ? ctlSignIn.DestinationPageUrl : Request["ReturnUrl"]);
        }

        protected void ctlSignIn_LoggedIn(object sender, EventArgs e)
        {
            // Set up the correct decimal formatting
            TextBox txtEmail = (TextBox)ctlSignIn.FindControl("txtEmail");
            Profile pf = Profile.GetUser(Membership.GetUserNameByEmail(txtEmail.Text));
            Session[MFBConstants.keyDecimalSettings] = pf.PreferenceExists(MFBConstants.keyDecimalSettings)
                ? pf.GetPreferenceForKey<DecimalFormat>(MFBConstants.keyDecimalSettings)
                : (object)null;
            Session[MFBConstants.keyMathRoundingUnits] = pf.MathRoundingUnit;
        }
    }
}