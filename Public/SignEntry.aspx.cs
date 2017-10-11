using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;
using MyFlightbook.Instruction;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

public partial class Public_SignEntry : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Master.SelectedTab = tabID.tabUnknown;
        string szUser = String.Empty;

        if (!IsPostBack)
        {
            lblErr.Text = String.Empty;
            string szAuthToken = util.GetStringParam(Request, "auth");
            if (!String.IsNullOrEmpty(szAuthToken))
            {
                using (MFBWebService ws = new MFBWebService())
                    szUser = ws.GetEncryptedUser(szAuthToken);
            }

            bool fIsLocalOrSecure = MFBWebService.CheckSecurity(Request);

            // If no valid auth token, fall back to the authenticated name.
            if (String.IsNullOrEmpty(szUser) && Page.User.Identity.IsAuthenticated && fIsLocalOrSecure)
                szUser = Page.User.Identity.Name;

            // Require a secure connection for other than debugging.
            if (!fIsLocalOrSecure && !Request.IsSecureConnection)
                szUser = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(szUser))
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                int idFlight = util.GetIntParam(Request, "idFlight", LogbookEntry.idFlightNew);
                if (idFlight == LogbookEntry.idFlightNew)
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                LogbookEntry le = new LogbookEntry();
                if (!le.FLoadFromDB(idFlight, szUser))
                    throw new MyFlightbookException(Resources.SignOff.errSignNotAuthorized);

                mfbSignFlight.Flight = le;
                CFIStudentMap sm = new CFIStudentMap(szUser);
                if (sm.Instructors.Count() == 0)
                {
                    mfbSignFlight.SigningMode = Controls_mfbSignFlight.SignMode.AdHoc;
                    mfbSignFlight.CFIProfile = null;
                    mvSignFlight.SetActiveView(vwAcceptTerms);
                }
                else
                {
                    cmbInstructors.DataSource = sm.Instructors;
                    cmbInstructors.DataBind();
                    mvSignFlight.SetActiveView(vwPickInstructor);
                }


                lblHeader.Text = String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.SignOff.SignFlightHeader, MyFlightbook.Profile.GetUser(le.User).UserFullName);
                lblDisclaimerResponse.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement1);
                lblDisclaimerResponse2.Text = Branding.ReBrand(Resources.SignOff.SignDisclaimerAgreement2);
            }
            catch (MyFlightbookException ex)
            {
                lblErr.Text = ex.Message;
            }
        }
    }

    protected void ChooseInstructor()
    {
        bool fIsNewInstructor = String.IsNullOrEmpty(cmbInstructors.SelectedValue);
        mfbSignFlight.CFIProfile = fIsNewInstructor ? null : MyFlightbook.Profile.GetUser(cmbInstructors.SelectedValue);
        mvSignFlight.SetActiveView(fIsNewInstructor ? vwAcceptTerms : vwSign);
    }

    protected void cmbInstructors_SelectedIndexChanged(object sender, EventArgs e)
    {
        ChooseInstructor();
    }

    protected void SigningFinished(object sender, EventArgs e)
    {
        mvSignFlight.SetActiveView(vwSuccess);
    }

    protected void btnAcceptResponsibility_Click(object sender, EventArgs e)
    {
        if (ckAccept.Checked)
            mvSignFlight.SetActiveView(vwSign);
        else
            lblErr.Text = Resources.SignOff.errAcceptDisclaimer;
    }

    protected void btnNewInstructor_Click(object sender, EventArgs e)
    {
        ChooseInstructor();
    }
}