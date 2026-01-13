using System;
using System.Collections.Generic;
using System.Net.Mail;

/******************************************************
 * 
 * Copyright (c) 2007-2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Injection
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

        public void AddAdminsToMessage(MailMessage msg, bool fTo, uint RoleMask)
        {
            if (msg == null)
                throw new ArgumentNullException(nameof(msg));
            IEnumerable<ProfileBase> lst = Profile.GetNonUsers();
            foreach (ProfileBase pf in lst)
            {
                if ((RoleMask & (uint)pf.Role) != 0)
                {
                    MailAddress ma = new MailAddress(pf.Email, pf.UserFullName);
                    if (fTo)
                        msg.To.Add(ma);
                    else
                        msg.Bcc.Add(ma);
                }
            }
        }
    }
}