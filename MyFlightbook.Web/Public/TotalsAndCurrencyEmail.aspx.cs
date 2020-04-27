using MyFlightbook;
using MyFlightbook.Encryptors;
using MyFlightbook.Currency;
using MyFlightbook.Payments;
using MyFlightbook.Subscriptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2012-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_TotalsAndCurrencyEmail : System.Web.UI.Page
{
    protected void KickOffRun()
    {
        // request came from this machine - make a request to ourselves and send it out in email
        EmailSubscriptionManager em = new EmailSubscriptionManager() { ActiveBrand = Branding.CurrentBrand };
        if (util.GetIntParam(Request, "dbg", 0) != 0)
            em.UserRestriction = Page.User.Identity.Name;
        string szTasksToRun = util.GetStringParam(Request, "tasks");
        if (!String.IsNullOrEmpty(szTasksToRun))
            em.TasksToRun = Int32.TryParse(szTasksToRun, out int tasks) ? (EmailSubscriptionManager.SelectedTasks) tasks : EmailSubscriptionManager.SelectedTasks.All;
        new Thread(new ThreadStart(em.NightlyRun)).Start();
        lblSuccess.Visible = true;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // see if this is coming from the local machine - reject anything that isn't.
        string szIPThis = System.Net.Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
        if (Request.UserHostAddress.CompareCurrentCultureIgnoreCase(szIPThis) != 0)
            throw new UnauthorizedAccessException("Attempt to view this page from other than local machine");

        if (!IsPostBack)
        {
            cssRef.Href = "~/Public/Stylesheet.css?v=20".ToAbsoluteURL(Request.Url.Scheme, Branding.CurrentBrand.HostName, Request.Url.Port).ToString();
            baseRef.Attributes["href"] = "~/Public/".ToAbsoluteURL(Request.Url.Scheme, Branding.CurrentBrand.HostName, Request.Url.Port).ToString();

            string szAuthKey = util.GetStringParam(Request, "k");
            string szUser = util.GetStringParam(Request, "u");
            string szParam = util.GetStringParam(Request, "p");

            // This page is public, so that it doesn't require any authentication, making it easy to set up a scheduled task.
            // SO, we do the following:
            // If you request the page from ANOTHER machine, we return an error
            // If you request it from THIS machine, then we perform a very simple authentication (pass an encrypted datetime) to ourselves.
            // If we receive this request with a valid encrypted key, we return the email for the specified user.
            if (String.IsNullOrEmpty(szAuthKey))
                KickOffRun();
            else
                SetUpCurrencyAndTotalsForUser(szAuthKey, szUser, szParam);
        }
    }

    protected void ValidateAuthorization(string szAuthKey)
    {
        if (szAuthKey.CompareCurrentCultureIgnoreCase("local") != 0 || !Page.User.Identity.IsAuthenticated)
        {
            AdminAuthEncryptor enc = new AdminAuthEncryptor();
            string szDate = enc.Decrypt(szAuthKey);
            DateTime dt = DateTime.Parse(szDate, CultureInfo.InvariantCulture);
            double elapsedSeconds = DateTime.Now.Subtract(dt).TotalSeconds;
            if (elapsedSeconds < 0 || elapsedSeconds > 10)
                throw new MyFlightbookException("Unauthorized attempt to view stats for mail");
        }
    }

    protected void SetUpCurrencyAndTotalsForUser(string szAuthKey, string szUser, string szParam)
    {
        try
        {
            ValidateAuthorization(szAuthKey);
            Profile pf = MyFlightbook.Profile.GetUser(szUser);
            EmailSubscriptionManager em = new EmailSubscriptionManager(pf.Subscriptions);

            IEnumerable<CurrencyStatusItem> rgExpiringCurrencies = null;
            IEnumerable<CurrencyStatusItem> rgPrecomputedCurrencies = null;
            if (pf.AssociatedData.TryGetValue(CurrencyStatusItem.AssociatedDateKeyExpiringCurrencies, out object o))
                rgExpiringCurrencies = (IEnumerable<CurrencyStatusItem>)o;
            if (pf.AssociatedData.TryGetValue(CurrencyStatusItem.AssociatedDataKeyCachedCurrencies, out object o2))
                rgPrecomputedCurrencies = (IEnumerable<CurrencyStatusItem>)o2;

            pf.AssociatedData.Remove(CurrencyStatusItem.AssociatedDateKeyExpiringCurrencies);
            pf.AssociatedData.Remove(CurrencyStatusItem.AssociatedDataKeyCachedCurrencies);

            bool fHasCurrency = em.HasSubscription(SubscriptionType.Currency) || (em.HasSubscription(SubscriptionType.Expiration) && rgExpiringCurrencies != null && rgPrecomputedCurrencies != null);
            bool fHasTotals = em.HasSubscription(SubscriptionType.Totals);
            bool fHasMonthly = em.HasSubscription(SubscriptionType.MonthlyTotals);

            bool fMonthlySummary = (String.Compare(szParam, "monthly", StringComparison.OrdinalIgnoreCase) == 0);

            if (!fHasCurrency && !fHasTotals && !fMonthlySummary)
                throw new MyFlightbookException("Email requested but no subscriptions found! User =" + szUser);

            if (fMonthlySummary && !fHasMonthly)
                throw new MyFlightbookException("Monthly email requested but user does not subscribe to monthly email.  User = " + szUser);

            // Donation solicitation: thank-them if they've made a donation within the previous 12 months, else solicit.
            lblThankyou.Text = Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle);
            lblSolicitDonation.Text = Branding.ReBrand(Resources.LocalizedText.DonatePrompt);
            lnkDonateNow.Text = Branding.ReBrand(Resources.LocalizedText.DonateSolicitation);
            lnkDonateNow.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}{1}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx/pftDonate"));
            mvDonations.SetActiveView(Payment.TotalPaidSinceDate(DateTime.Now.AddYears(-1), szUser) > 0 ? vwThankyou : vwPleaseGive);

            // Fix up the unsubscribe link.
            lnkUnsubscribe.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}{1}/{2}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Member/EditProfile.aspx"), tabID.pftPrefs.ToString());
            lnkQuickUnsubscribe.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}{1}?u={2}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Public/Unsubscribe.aspx"), HttpUtility.UrlEncode(new UserAccessEncryptor().Encrypt(szUser)));

            // And set HHMM mode explicitly (since not otherwise going to be set in totals
            mfbTotalSummary.UseHHMM = mfbTotalSummaryYTD.UseHHMM = pf.UsesHHMM;

            lblCurrency.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailCurrencyHeader, DateTime.Now.ToLongDateString());
            if (fMonthlySummary)
            {
                bool fAnnual = (DateTime.Now.Month == 1);  // if it's January, show prior year; else show YTD
                lblIntroHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailMonthlyMailIntro, Branding.CurrentBrand.AppName);
                DateTime dtPriorMonth = DateTime.Now.AddMonths(-1);
                lblTotal.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsPriorMonthHeader, dtPriorMonth.ToString("MMMM", CultureInfo.CurrentCulture), dtPriorMonth.Year);
                lblYTD.Text = fAnnual ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsPriorYearHeader, DateTime.Now.Year - 1) : String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsYTDHeader, DateTime.Now.Year);
                pnlTotals.Visible = pnlYTD.Visible = true;

                mfbTotalSummary.Username = mfbTotalSummaryYTD.Username = pf.UserName;

                FlightQuery fqPriorMonth = new FlightQuery(pf.UserName) { DateRange = FlightQuery.DateRanges.PrevMonth };
                mfbTotalSummary.CustomRestriction = fqPriorMonth;

                FlightQuery fqYTD = new FlightQuery(pf.UserName) { DateRange = fAnnual ? FlightQuery.DateRanges.PrevYear : FlightQuery.DateRanges.YTD };
                mfbTotalSummaryYTD.CustomRestriction = fqYTD;

                if (fAnnual)
                    mfbRecentAchievements.Refresh(szUser, new DateTime(DateTime.Now.Year - 1, 1, 1), new DateTime(DateTime.Now.Year - 1, 12, 31), true);
                else
                    mfbRecentAchievements.Refresh(szUser, new DateTime(DateTime.Now.Year, 1, 1), DateTime.Now, true);

                lblRecentAchievementsTitle.Text = mfbRecentAchievements.Summary;
                lblRecentAchievementsTitle.Visible = mfbRecentAchievements.AchievementCount > 0;
            }
            else
            {
                lblIntroHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailWeeklyMailIntro, Branding.CurrentBrand.AppName);
                lblTotal.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsHeader, DateTime.Now.ToLongDateString());

                if (fHasTotals)
                {
                    mfbTotalSummary.Username = pf.UserName;
                    mfbTotalSummary.CustomRestriction = new FlightQuery(pf.UserName);
                    pnlTotals.Visible = true;
                }
            }

            if (fHasCurrency || fMonthlySummary)
            {
                mfbCurrency.UserName = pf.UserName;
                mfbCurrency.RefreshCurrencyTable(rgPrecomputedCurrencies);
                pnlCurrency.Visible = true;

                if (rgExpiringCurrencies != null && rgExpiringCurrencies.Any())
                {
                    pnlExpiringCurrencies.Visible = true;
                    rptExpiring.DataSource = rgExpiringCurrencies;
                    rptExpiring.DataBind();
                }
            }
        }
        catch (Exception ex) when (ex is MyFlightbookException || ex is FormatException)
        {
            MyFlightbookException.NotifyAdminException(ex);
            throw;  // ensure that the success tag doesn't show!
        }
    }

}