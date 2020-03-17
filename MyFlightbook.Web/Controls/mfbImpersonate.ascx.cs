using MyFlightbook;
using System;
using System.Web;
using System.Globalization;
using System.Text;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbImpersonate : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        pnlImpersonate.Visible = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).CanSupport && !ProfileRoles.IsImpersonating(Page.User.Identity.Name);
    }

    protected void btnFindUsers_Click(object sender, EventArgs e)
    {
        string[] rgWords = txtImpersonate.Text.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        if (rgWords.Length == 0)
            return;

        StringBuilder sb = new StringBuilder();
        int i = 0;
        sqlUsers.SelectParameters.Clear();
        foreach (string szWord in rgWords)
        {
            string sz = szWord.Trim();
            if (sz.Length == 0)
                continue;
            if (sb.Length > 0)
                sb.Append(" AND ");
            sb.AppendFormat(CultureInfo.InvariantCulture, " SearchString LIKE ?param{0} ", i);
            sqlUsers.SelectParameters.Add(String.Format(CultureInfo.InvariantCulture, "param{0}", i), String.Format(CultureInfo.InvariantCulture, "%{0}%", sz));
            i++;
        }

        const string szQ = @"SELECT Users.*, IF(Username like ?fullName, 1, 0) AS Bias, CONCAT_WS(' ', email, username, firstname, lastname) AS SearchString 
                            FROM Users 
                            HAVING {0}
                            ORDER BY bias DESC, length(Username) ASC 
                            LIMIT 200";
        sqlUsers.SelectParameters.Add("fullName", String.Format(CultureInfo.InvariantCulture, "%{0}%", txtImpersonate.Text));
        sqlUsers.SelectCommand = String.Format(CultureInfo.InvariantCulture, szQ, sb.ToString());
        sqlUsers.SelectCommandType = SqlDataSourceCommandType.Text;
        sqlUsers.Select(DataSourceSelectArguments.Empty);
        gvUsers.DataBind();
    }

    protected void btnSendInEmail_Click(object sender, EventArgs e)
    {
        string szPass = lblResetErr.Text;
        string szUser = lblPwdUsername.Text;
        if (!String.IsNullOrEmpty(szPass) && !String.IsNullOrEmpty(szUser))
        {
            string szEmail = Branding.ReBrand(Resources.EmailTemplates.ChangePassEmail.Replace("<% Password %>", szPass));
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.ResetPasswordEmailSubject, Branding.CurrentBrand.AppName), szEmail, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), false, false);
        }
    }

    protected void gvUsers_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        MembershipUser mu = Membership.GetUser(e.CommandArgument, false);
        if (mu == null)
            return;

        lblResetErr.Text = "";

        string szUser = mu.UserName;
        if (!ProfileAdmin.ValidateUser(szUser))
            return;

        // SendMessage is available for any valid user; other commands are not valid for the current admin
        if (e.CommandName.CompareOrdinalIgnoreCase("SendMessage") == 0)
        {
            pnlSendEmail.Visible = true;
            lblRecipient.Text = HttpUtility.HtmlEncode(mu.UserName);
            return;
        }

        if (String.Compare(szUser, Page.User.Identity.Name, StringComparison.InvariantCultureIgnoreCase) == 0)
            return;

        if (e.CommandName.CompareOrdinalIgnoreCase("Impersonate") == 0)
        {
            ProfileRoles.ImpersonateUser(Page.User.Identity.Name, szUser);
            Response.Redirect("~/Member/LogbookNew.aspx");
        }
        else if (e.CommandName.CompareOrdinalIgnoreCase("ResetPassword") == 0)
        {
            // Need to get the user offline
            mu = ProfileAdmin.ADMINUserFromName(szUser);
            if (mu == null) { lblResetErr.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, "User '{0}' not found", szUser)); }
            else
            {
                string szPass = mu.ResetPassword();
#pragma warning disable CA3002 // Watch out for injection
                lblResetErr.Text = szPass;
                lblPwdUsername.Text = szUser;
#pragma warning restore CA3002 // Watch out for injection
                btnSendInEmail.Visible = true;
            }
        }
        else if (e.CommandName.CompareOrdinalIgnoreCase("DeleteUser") == 0)
        {
            try
            {
                ProfileAdmin.DeleteForUser(mu, MyFlightbook.ProfileAdmin.DeleteLevel.EntireUser);
                lblResetErr.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, "User {0} ({1}) successfully deleted.", mu.UserName, mu.Email));
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) { lblResetErr.Text = ex.Message; }
        }
        else if (e.CommandName.CompareOrdinalIgnoreCase("DeleteFlightsForUser") == 0)
        {
            try
            {
                ProfileAdmin.DeleteForUser(mu, MyFlightbook.ProfileAdmin.DeleteLevel.OnlyFlights);
                lblResetErr.Text = HttpUtility.HtmlEncode(String.Format(CultureInfo.CurrentCulture, "Flights for User {0} ({1}) successfully deleted.", mu.UserName, mu.Email));
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException)) { lblResetErr.Text = ex.Message; }
        }
        else if (e.CommandName.CompareOrdinalIgnoreCase("EndowClub") == 0)
        {
            DBHelper dbh = new DBHelper();
            dbh.DoNonQuery("INSERT INTO earnedgratuities SET idgratuitytype=3, username=?szUser, dateEarned=Now(), dateExpired=Date_Add(Now(), interval 30 day), reminderssent=0, dateLastReminder='0001-01-01 00:00:00'", (comm) => { comm.Parameters.AddWithValue("szUser", mu.UserName); });
        }
    }

    protected void btnSend_OnClick(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(lblRecipient.Text))
        {
            Profile pf = MyFlightbook.Profile.GetUser(lblRecipient.Text);
            util.NotifyUser(txtSubject.Text, txtBody.Text, new System.Net.Mail.MailAddress(pf.Email, pf.UserFullName), true, false);
        }

        btnCancel_onClick(sender, e);
    }

    protected void btnCancel_onClick(object sender, EventArgs e)
    {
        pnlSendEmail.Visible = false;
        lblRecipient.Text = "";
    }
}
