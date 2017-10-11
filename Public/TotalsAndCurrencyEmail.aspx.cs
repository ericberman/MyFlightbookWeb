using System;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.UI;
using MyFlightbook;
using MyFlightbook.Encryptors;
using MyFlightbook.Payments;
using MyFlightbook.Subscriptions;

/******************************************************
 * 
 * Copyright (c) 2012-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_TotalsAndCurrencyEmail : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            string szAuthKey = util.GetStringParam(Request, "k");
            string szUser = util.GetStringParam(Request, "u");
            string szParam = util.GetStringParam(Request, "p");

            // This page is public, so that it doesn't require any authentication, making it easy to set up a scheduled task.
            // SO, we do the following:
            // If you request the page from ANOTHER machine, we return an error
            // If you request it from THIS machine, then we perform a very simple authentication (pass an encrypted datetime) to ourselves.
            // If we receive this request with a valid encrypted key, we return the email for the specified user.
            if (String.IsNullOrEmpty(szAuthKey))
            {
                // see if this is coming from the local machine
                string szIPThis = System.Net.Dns.GetHostAddresses(Request.Url.Host)[0].ToString();
                if (String.Compare(Request.UserHostAddress, szIPThis, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    // request came from this machine - make a request to ourselves and send it out in email
                    EmailSubscriptionManager em = new EmailSubscriptionManager();
                    em.ActiveBrand = Branding.CurrentBrand;
                    if (util.GetIntParam(Request, "dbg", 0) != 0)
                        em.UserRestriction = Page.User.Identity.Name;
                    string szTasksToRun = util.GetStringParam(Request, "tasks");
                    if (!String.IsNullOrEmpty(szTasksToRun))
                    {
                        try { em.TasksToRun = (EmailSubscriptionManager.SelectedTasks)Convert.ToInt32(szTasksToRun, CultureInfo.InvariantCulture); }
                        catch (FormatException)
                        { em.TasksToRun = EmailSubscriptionManager.SelectedTasks.All; }
                    }
                    new Thread(new ThreadStart(em.NightlyRun)).Start();
                    lblSuccess.Visible = true;
                }
                else
                {
                    lblErr.Visible = true;
                }
            }
            else
            {
                try
                {
                    AdminAuthEncryptor enc = new AdminAuthEncryptor();
                    string szDate = enc.Decrypt(szAuthKey);
                    DateTime dt = DateTime.Parse(szDate, CultureInfo.InvariantCulture);
                    double elapsedSeconds = DateTime.Now.Subtract(dt).TotalSeconds;
                    if (elapsedSeconds < 0 || elapsedSeconds > 10)
                        throw new MyFlightbookException("Unauthorized attempt to view stats for mail");

                    Profile pf = MyFlightbook.Profile.GetUser(szUser);
                    EmailSubscriptionManager em = new EmailSubscriptionManager(pf.Subscriptions);

                    bool fHasCurrency = em.HasSubscription(SubscriptionType.Currency);
                    bool fHasTotals = em.HasSubscription(SubscriptionType.Totals);
                    bool fHasMonthly = em.HasSubscription(SubscriptionType.MonthlyTotals);

                    bool fMonthlySummary = (String.Compare(szParam, "monthly", StringComparison.OrdinalIgnoreCase) == 0);

                    if (!fHasCurrency && !fHasTotals && !fMonthlySummary)
                        throw new MyFlightbookException("Email requested but no subscriptions found!");

                    if (fMonthlySummary && !fHasMonthly)
                        throw new MyFlightbookException("Monthly email requested but user does not subscribe to monthly email");

                    // Donation solicitation: thank-them if they've made a donation within the previous 12 months, else solicit.
                    lblThankyou.Text = Branding.ReBrand(Resources.LocalizedText.DonateThankYouTitle);
                    lblSolicitDonation.Text = Branding.ReBrand(Resources.LocalizedText.DonatePrompt);
                    lnkDonateNow.Text = Branding.ReBrand(Resources.LocalizedText.DonateSolicitation);
                    lnkDonateNow.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}/logbook/Member/EditProfile.aspx/pftDonate", Branding.CurrentBrand.HostName);
                    mvDonations.SetActiveView(Payment.TotalPaidSinceDate(DateTime.Now.AddYears(-1), szUser) > 0 ? vwThankyou : vwPleaseGive);

                    // Fix up the unsubscribe link.
                    lnkUnsubscribe.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}/logbook/Member/EditProfile.aspx/{1}", Branding.CurrentBrand.HostName, tabID.pftPrefs.ToString());
                    lnkQuickUnsubscribe.NavigateUrl = String.Format(CultureInfo.InvariantCulture, "http://{0}/logbook/Public/Unsubscribe.aspx?u={1}", Branding.CurrentBrand.HostName, HttpUtility.UrlEncode(new UserAccessEncryptor().Encrypt(szUser)));

                    if (fMonthlySummary)
                    {
                        bool fAnnual = (DateTime.Now.Month == 1);  // if it's January, show prior year; else show YTD
                        lblIntroHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailMonthlyMailIntro, Branding.CurrentBrand.AppName);
                        DateTime dtPriorMonth = DateTime.Now.AddMonths(-1);
                        lblTotal.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsPriorMonthHeader, dtPriorMonth.ToString("MMMM", CultureInfo.CurrentCulture), dtPriorMonth.Year);
                        lblYTD.Text = fAnnual ? String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsPriorYearHeader, DateTime.Now.Year - 1) : String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailTotalsYTDHeader, DateTime.Now.Year);
                        pnlTotals.Visible = pnlYTD.Visible = true;

                        mfbTotalSummary.Username = mfbTotalSummaryYTD.Username = pf.UserName;

                        FlightQuery fqPriorMonth = new FlightQuery(pf.UserName);
                        fqPriorMonth.DateRange = FlightQuery.DateRanges.PrevMonth;
                        mfbTotalSummary.CustomRestriction = fqPriorMonth;

                        FlightQuery fqYTD = new FlightQuery(pf.UserName);
                        fqYTD.DateRange = fAnnual ? FlightQuery.DateRanges.PrevYear : FlightQuery.DateRanges.YTD;
                        mfbTotalSummaryYTD.CustomRestriction = fqYTD;
                    }
                    else
                    {
                        lblIntroHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailWeeklyMailIntro, Branding.CurrentBrand.AppName);
                        lblCurrency.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EmailCurrencyHeader, DateTime.Now.ToLongDateString());
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
                        mfbCurrency.RefreshCurrencyTable();
                        pnlCurrency.Visible = true;
                    }
                }
                catch (MyFlightbookException ex)
                {
                    MyFlightbookException.NotifyAdminException(ex);
                    throw;  // ensure that the success tag doesn't show!
                }
                catch (FormatException ex)
                {
                    MyFlightbookException.NotifyAdminException(ex);
                    throw;
                }
            }
        }
    }
}