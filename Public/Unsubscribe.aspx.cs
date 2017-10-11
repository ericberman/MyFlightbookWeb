using System;
using MyFlightbook;
using MyFlightbook.Encryptors;

/******************************************************
 * 
 * Copyright (c) 2013-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_Unsubscribe : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            string szUEncrypted = util.GetStringParam(Request, "u");
            try
            {
                if (String.IsNullOrEmpty(szUEncrypted))
                    throw new MyFlightbookException(Resources.Profile.errUnsubscribeNoEmail);
                string szUser = (new UserAccessEncryptor()).Decrypt(szUEncrypted);
                if (String.IsNullOrEmpty(szUser))
                    throw new MyFlightbookException(Resources.Profile.errUnsubscribeNoEmail);
                Profile pf = MyFlightbook.Profile.GetUser(szUser);
                if (pf == null)
                    throw new MyFlightbookException(Resources.Profile.errUnsubscribeNotFound);
                pf.Subscriptions = 0;
                pf.FCommit();
                lblSuccess.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Profile.UnsubscribeSuccessful, pf.Email);
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }
}