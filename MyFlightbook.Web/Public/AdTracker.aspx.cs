using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.SponsoredAd;
using System.Globalization;

public partial class Public_AdTracker : System.Web.UI.Page
{
    protected void Page_Init(object sender, EventArgs e)
    {
        if (Request.PathInfo.Length > 0)
        {
            int idAd = -1;
            try { idAd = Convert.ToInt32(Request.PathInfo.Substring(1), CultureInfo.InvariantCulture); }
            catch (FormatException) { }

            if (idAd > 0)
            {
                SponsoredAd ad = SponsoredAd.GetAd(idAd);
                if (ad != null)
                {
                    ad.AddClick();
                    GoogleAnalytics1.RedirURL = ad.TargetURL;
                }
            }
        }
    }
}