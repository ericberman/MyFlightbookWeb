using MyFlightbook.SponsoredAd;
using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_AdTracker : System.Web.UI.Page
{
    protected void Page_Init(object sender, EventArgs e)
    {
        if (Request.PathInfo.Length > 0)
        {
            int idAd = -1;
            try { idAd = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture); }
            catch (Exception ex) when (ex is FormatException) { }

            if (idAd > 0)
            {
                SponsoredAd ad = SponsoredAd.GetAd(idAd);
                if (ad != null)
                {
                    ad.AddClick();
                    GoogleAnalytics1.RedirHref = ad.TargetLink;
                }
            }
        }
    }
}