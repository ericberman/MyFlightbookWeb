using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2009-2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_FBPost : System.Web.UI.Page
{
    private const string keyFlightEntry = "keyFlightEntry";

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            this.Master.SelectedTab = tabID.tabUnknown;
            int id = util.GetIntParam(Request, "id", -1);
            if (id >= 0)
            {
                LogbookEntry le = new LogbookEntry();
                if (le.FLoadFromDB(id, User.Identity.Name))
                {
                    ViewState[keyFlightEntry] = le;
                }
                else
                {
                    pnlShareOrNot.Visible = false;
                    pnlNotYours.Visible = true;
                }
            }
        }

        mfbMiniFacebook.FlightEntry = (LogbookEntry)ViewState[keyFlightEntry];
        if (mfbMiniFacebook.FlightEntry != null && mfbMiniFacebook.FlightEntry.fIsPublic)
            Response.Redirect(mfbMiniFacebook.FBRedirURL.OriginalString);
    }

    private void RedirectToFacebook(Boolean fShareFirst)
    {
        LogbookEntry le = mfbMiniFacebook.FlightEntry;
        if (le != null)
        {
            if (fShareFirst)
            {
                le.fIsPublic = true;
                le.FCommit();
            }
            Response.Redirect(mfbMiniFacebook.FBRedirURL.OriginalString);
        }
    }

    protected void btnShare_Click(object sender, EventArgs e)
    {
        RedirectToFacebook(true);
    }

    protected void btnNoShare_Click(object sender, EventArgs e)
    {
        RedirectToFacebook(false);
    }
}
