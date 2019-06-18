using MyFlightbook;
using System;
using System.Globalization;
using System.Net.Mail;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSendFlight : System.Web.UI.UserControl
{
    #region Properties
    /// <summary>
    /// Location for the link to the flight in email
    /// </summary>
    public string SendPageTarget
    {
        get { return hdnFlightSendToTarget.Value; }
        set { hdnFlightSendToTarget.Value = value; }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e) {  }

    public void SendFlight(int idFlight)
    {
        hdnFlightToSend.Value = idFlight.ToString(CultureInfo.InvariantCulture);
        LogbookEntry le = new LogbookEntry(idFlight, Page.User.Identity.Name);
        fmvFlight.DataSource = new LogbookEntry[] { le };
        fmvFlight.DataBind();
        txtSendFlightEmail.Text = txtSendFlightMessage.Text = string.Empty;
        modalPopupSendFlight.Show();
    }

    protected void btnSendFlight_Click(object sender, EventArgs e)
    {
        Page.Validate("valSendFlight");
        if (Page.IsValid)
        {
            LogbookEntry le = new LogbookEntry(Convert.ToInt32(hdnFlightToSend.Value, CultureInfo.InvariantCulture), Page.User.Identity.Name);
            MyFlightbook.Profile pfSender = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
            string szRecipient = txtSendFlightEmail.Text;

            using (MailMessage msg = new MailMessage())
            {
                msg.Body = Branding.ReBrand(Resources.LogbookEntry.SendFlightBody.Replace("<% Sender %>", HttpUtility.HtmlEncode(pfSender.UserFullName))
                    .Replace("<% Message %>", HttpUtility.HtmlEncode(txtSendFlightMessage.Text))
                    .Replace("<% Date %>", le.Date.ToShortDateString())
                    .Replace("<% Aircraft %>", HttpUtility.HtmlEncode(le.TailNumDisplay))
                    .Replace("<% Route %>", HttpUtility.HtmlEncode(le.Route))
                    .Replace("<% Comments %>", HttpUtility.HtmlEncode(le.Comment))
                    .Replace("<% Time %>", le.TotalFlightTime.FormatDecimal(pfSender.UsesHHMM))
                    .Replace("<% FlightLink %>", le.SendFlightUri(Branding.CurrentBrand.HostName, SendPageTarget).ToString()));

                msg.Subject = String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.SendFlightSubject, pfSender.UserFullName);
                msg.From = new MailAddress(Branding.CurrentBrand.EmailAddress, String.Format(CultureInfo.CurrentCulture, Resources.SignOff.EmailSenderAddress, Branding.CurrentBrand.AppName, pfSender.UserFullName));
                msg.ReplyToList.Add(new MailAddress(pfSender.Email));
                msg.To.Add(new MailAddress(szRecipient));
                msg.IsBodyHtml = true;
                util.SendMessage(msg);
            }

            modalPopupSendFlight.Hide();
        }
        else
            modalPopupSendFlight.Show();
    }
}