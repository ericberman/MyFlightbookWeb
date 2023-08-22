using MyFlightbook;
using MyFlightbook.Payments;
using MyFlightbook.Subscriptions;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2017-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSubscriptionManager : UserControl
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Profile m_pf = Profile.GetUser(Page.User.Identity.Name);

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
}