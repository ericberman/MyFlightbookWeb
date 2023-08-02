using MyFlightbook.Payments;
using System;
using System.Collections.Generic;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbDonate : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        public void InitDonations()
        {
            List<Gratuity> lstKnownGratuities = new List<Gratuity>(Gratuity.KnownGratuities);
            lstKnownGratuities.Sort((g1, g2) => { return g1.Threshold.CompareTo(g2.Threshold); });
            rptAvailableGratuities.DataSource = lstKnownGratuities;
            rptAvailableGratuities.DataBind();

            pnlPaypalCanceled.Visible = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("canceled") == 0;
            pnlPaypalSuccess.Visible = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("success") == 0;
            lblDonatePrompt.Text = Branding.ReBrand(Resources.LocalizedText.DonatePrompt);
            lnkBuySwag.NavigateUrl = Branding.CurrentBrand.SwagRef;
            lnkBuySwag.Visible = !String.IsNullOrEmpty(Branding.CurrentBrand.SwagRef);
            gvDonations.DataSource = Payment.RecordsForUser(Page.User.Identity.Name);
            gvDonations.DataBind();

            List<EarnedGratuity> lst = EarnedGratuity.GratuitiesForUser(Page.User.Identity.Name, Gratuity.GratuityTypes.Unknown);
            lst.RemoveAll(eg => eg.CurrentStatus == EarnedGratuity.EarnedGratuityStatus.Expired);
            if (pnlEarnedGratuities.Visible = (lst.Count > 0))
            {
                rptEarnedGratuities.DataSource = lst;
                rptEarnedGratuities.DataBind();
            }
            iframeDonate.Src = "~/Member/Donate.aspx".ToAbsoluteURL(Request).ToString();
        }
    }
}