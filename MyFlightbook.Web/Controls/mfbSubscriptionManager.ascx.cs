using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using MyFlightbook;
using MyFlightbook.Subscriptions;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSubscriptionManager : System.Web.UI.UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Profile m_pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);

        if (!IsPostBack)
        {
            EmailSubscriptionManager esm = new EmailSubscriptionManager(m_pf.Subscriptions);
            foreach (EmailSubscription es in esm.Subscriptions)
            {
                ListItem li = new ListItem(es.Name, es.Type.ToString(), true);
                li.Selected = es.IsSubscribed;
                cklEmailSubscriptions.Items.Add(li);
            }
        }
    }

    protected void btnUpdateEmailPrefs_Click(object sender, EventArgs e)
    {
        Profile m_pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);

        List<EmailSubscription> l = new List<EmailSubscription>();
        foreach (ListItem li in cklEmailSubscriptions.Items)
            l.Add(new EmailSubscription((SubscriptionType)Enum.Parse(typeof(SubscriptionType), li.Value), li.Text, li.Selected));

        EmailSubscriptionManager esm = new EmailSubscriptionManager(m_pf.Subscriptions);
        esm.Subscriptions = l;
        m_pf.Subscriptions = esm.ToUint();

        try
        {
            m_pf.FCommit();
            lblEmailPrefsUpdated.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblEmailPrefsUpdated.Visible = true;
            lblEmailPrefsUpdated.Text = ex.Message;
            lblEmailPrefsUpdated.CssClass = "error";
        }
    }
}