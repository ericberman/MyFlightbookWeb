using System;
using System.Net.Mail;

/******************************************************
 * 
 * Copyright (c) 2007-2025 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Utility.Email
{
    public class SmtpSupport : IEmailSender
    {
        public void SendEmail(MailMessage msg)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));

            using (SmtpClient smtpClient = new SmtpClient())
            {
                if (!smtpClient.Host.Contains("local"))
                    smtpClient.EnableSsl = true;
                System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;

                smtpClient.Send(msg);
            }
        }
    }
}