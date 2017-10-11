using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Public_MobileApps : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            litMobileAppPromo.Text = Branding.ReBrand(Resources.LocalizedText.MobileAppPromo);
            mvScreenShots.ActiveViewIndex = 0;

            int iPage = Request.Params["p"].SafeParseInt(-1);
            if (iPage >= 0 && iPage < mvScreenShots.Views.Count)
                mvScreenShots.ActiveViewIndex = cmbMobileTarget.SelectedIndex = iPage + 1;
            
            this.Master.SelectedTab = tabID.tabHome;
            this.Master.Title = Branding.CurrentBrand.AppName;
        }

        divStoreLogos.Visible = mvScreenShots.ActiveViewIndex == 0;
    }

    protected void DropDownList1_SelectedIndexChanged(object sender, EventArgs e)
    {
        mvScreenShots.ActiveViewIndex = cmbMobileTarget.SelectedIndex;
        divStoreLogos.Visible = mvScreenShots.ActiveViewIndex == 0;
    }
}