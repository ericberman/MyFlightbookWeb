using MyFlightbook;
using MyFlightbook.Payments;
using MyFlightbook.Subscriptions;
using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2017-2020 MyFlightbook LLC
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
            lblCurrencyExpirationPromotion.Text = Branding.ReBrand(Resources.Profile.EmailCurrencyExpirationPromotion);
            rowPromo.Visible = !(ckCurrencyExpiring.Enabled = EarnedGratuity.UserQualifies(Page.User.Identity.Name, Gratuity.GratuityTypes.CurrencyAlerts));

            EmailSubscriptionManager esm = new EmailSubscriptionManager(m_pf.Subscriptions);

            ckCurrencyWeekly.Checked = esm.HasSubscription(SubscriptionType.Currency);
            ckCurrencyExpiring.Checked = esm.HasSubscription(SubscriptionType.Expiration);
            ckTotalsWeekly.Checked = esm.HasSubscription(SubscriptionType.Totals);
            ckMonthly.Checked = esm.HasSubscription(SubscriptionType.MonthlyTotals);
        }
    }

    protected void btnUpdateEmailPrefs_Click(object sender, EventArgs e)
    {
        Profile m_pf = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);

        EmailSubscriptionManager esm = new EmailSubscriptionManager(m_pf.Subscriptions);
        esm.SetSubscription(SubscriptionType.Currency, ckCurrencyWeekly.Checked);
        esm.SetSubscription(SubscriptionType.Expiration, ckCurrencyExpiring.Checked);
        esm.SetSubscription(SubscriptionType.Totals, ckTotalsWeekly.Checked);
        esm.SetSubscription(SubscriptionType.MonthlyTotals, ckMonthly.Checked);

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