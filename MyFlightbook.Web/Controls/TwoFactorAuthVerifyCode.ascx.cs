using Google.Authenticator;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls
{
    public partial class TwoFactorAuthVerifyCode : UserControl
    {
        public event EventHandler<EventArgs> TFACodeVerified;
        public event EventHandler<EventArgs> TFACodeFailed;

        const string szVSFailureCount = "vsFailureCount";
        const string szVSAuthCode = "vsAuthCode";

        public int FailureCount
        {
            get { return (ViewState[szVSFailureCount] == null) ? 0 : (int)ViewState[szVSFailureCount]; }
            private set { ViewState[szVSFailureCount] = value; }
        }

        public string AuthCode
        {
            get { return ((string)ViewState[szVSAuthCode] ?? string.Empty).Trim(); }
            set { ViewState[szVSAuthCode] = value; }
        }

        protected string valGroup
        {
            get { return "tfaCode" + ID; }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                FailureCount = 0;
                util.SetValidationGroup(pnlValidate, valGroup);
            }
        }

        protected void btnVerifyCode_Click(object sender, EventArgs e)
        {
            Page.Validate(valGroup);
            if (!Page.IsValid)
                return;

            if (String.IsNullOrEmpty(AuthCode))
                throw new InvalidOperationException("Validation required but no authcode provided");

            TwoFactorAuthenticator tfa = new TwoFactorAuthenticator();
            if (tfa.ValidateTwoFactorPIN(AuthCode, txtCode.Text, new TimeSpan(0, 2, 0)))
                TFACodeVerified?.Invoke(this, new EventArgs());
            else
            {
                FailureCount++;
                System.Threading.Thread.Sleep(1000); // pause for a second to thwart dictionary attacks.
                TFACodeFailed?.Invoke(this, new EventArgs());
            }
            txtCode.Text = string.Empty;    // clear it regardless.
        }
    }
}