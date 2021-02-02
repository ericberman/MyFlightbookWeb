using System;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2016-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.SponsoredAds
{
    public partial class AdTracker : System.Web.UI.Page
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
                        // Yeah, it's a click, but if "imp=1" is present, treat it as an impression, not a click.
                        if (util.GetIntParam(Request, "imp", 0) != 0)
                            ad.AddImpression();
                        else
                        {
                            ad.AddClick();
                            Response.Redirect(ad.TargetLink);
                        }
                    }
                }
            }
        }
    }
}