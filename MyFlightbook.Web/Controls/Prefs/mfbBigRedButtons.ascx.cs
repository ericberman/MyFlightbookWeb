using System;
using System.Globalization;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2010-2021 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbBigRedButtons : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Profile pf = Profile.GetUser(Page.User.Identity.Name);
                if (pf.PreferenceExists(MFBConstants.keyTFASettings))
                {
                    mvBigRedButtons.SetActiveView(vwStaticRedButtons);
                    tfaBRB.AuthCode = pf.GetPreferenceForKey(MFBConstants.keyTFASettings) as string;
                }
                else
                    mvBigRedButtons.SetActiveView(vwRedButtons);
            }
        }

        #region Account closure and bulk delete
        protected void btnDeleteFlights_Click(object sender, EventArgs e)
        {
            try
            {
                ProfileAdmin.DeleteFlightsForUser(Page.User.Identity.Name);
                lblDeleteFlightsCompleted.Visible = true;
            }
            catch (MyFlightbookException ex) { lblDeleteErr.Text = ex.Message; }
        }

        protected void btnCloseAccount_Click(object sender, EventArgs e)
        {
            try
            {
                ProfileAdmin.DeleteEntireUser(Page.User.Identity.Name);
                Response.Redirect("~");
            }
            catch (MyFlightbookException ex) { lblDeleteErr.Text = ex.Message; }
        }

        protected void btnDeleteUnusedAircraft_Click(object sender, EventArgs e)
        {
            int i = ProfileAdmin.DeleteUnusedAircraftForUser(Page.User.Identity.Name);
            lblDeleteErr.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.ProfileBulkDeleteAircraftDeleted, i);
            lblDeleteErr.CssClass = "success";
        }

        protected void tfaBRB_TFACodeFailed(object sender, EventArgs e)
        {
            lblBRB2faErr.Visible = true;
        }

        protected void tfaBRB_TFACodeVerified(object sender, EventArgs e)
        {
            mvBigRedButtons.SetActiveView(vwRedButtons);
        }
        #endregion
    }
}