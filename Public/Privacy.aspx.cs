using MyFlightbook;
using System;

/******************************************************
 * 
 * Copyright (c) 2009-2017 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_Privacy : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.Title = lblPrivacy.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrivacyPolicyHeader, Branding.CurrentBrand.AppName);
        Master.SelectedTab = tabID.tabUnknown;
        Master.Layout = MasterPage.LayoutMode.Accordion;
    }
}
