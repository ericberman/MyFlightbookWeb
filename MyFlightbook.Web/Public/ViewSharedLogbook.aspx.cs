using MyFlightbook.Web.Sharing;
using System;
using System.Collections.Generic;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Public
{
    public partial class ViewSharedLogbook : System.Web.UI.Page
    {
        private const string keyVSRestriction = "vsCurrentRestriction";
        private const string keyVSShareKey = "vsCurrentShareKey";
        protected FlightQuery Restriction
        {
            get
            {
                if (ViewState[keyVSRestriction] == null)
                    ViewState[keyVSRestriction] = new FlightQuery(Page.User.Identity.Name);
                return (FlightQuery)ViewState[keyVSRestriction];
            }
            set
            {
                ViewState[keyVSRestriction] = value;
            }
        }

        protected ShareKey CurrentShareKey
        {
            get { return (ShareKey)ViewState[keyVSShareKey]; }
            set { ViewState[keyVSShareKey] = value; }
        }

        protected void SetAccordionPane(string ID)
        {
            for (int i = 0; i < AccordionCtrl.Panes.Count; i++)
            {
                if (AccordionCtrl.Panes[i].ID == ID)
                {
                    AccordionCtrl.SelectedIndex = i;
                    return;
                }
            }
        }

        /// <summary>
        /// We set up the sharekey here because mfblogbook could try to load first - if it's an invalid sharekey, then we'll do the redirect without a db hit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(object sender, EventArgs e)
        {
            string guid = util.GetStringParam(Request, "g");
            ShareKey sk = ShareKey.ShareKeyWithID(guid);
            if (sk == null)
                Response.Redirect("~/HTTP403.htm");
            CurrentShareKey = sk;
            mfbLogbook.User = sk.Username;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ShareKey sk = CurrentShareKey;  // set in Page_Init
                Profile pf = Profile.GetUser(sk.Username);

                lblHeader.Text = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.LogbookForUserHeader, pf.UserFullName);

                // Indicate success.
                sk.FUpdateAccess();

                int privCount = sk.PrivilegeCount;

                apcAnalysis.Container.Style["display"] = apcCurrency.Container.Style["display"] = apcTotals.Container.Style["display"] = apcFilter.Container.Style["display"] = "none";

                if (sk.CanViewCurrency)
                {
                    apcCurrency.Container.Style["display"] = "inline-block";
                    mfbCurrency.Visible = true;
                    mfbCurrency.UserName = sk.Username;
                    mfbCurrency.RefreshCurrencyTable();
                    if (privCount == 1) // if ONLY showing currency, expand it and hide the accordion
                    {
                        pnlAccordionMenuContainer.Visible = false;
                        SetAccordionPane(acpPaneCurrency.ID);
                    }
                }

                if (sk.CanViewTotals)
                {
                    apcTotals.Container.Style["display"] = "inline-block";
                    mfbTotalSummary.Visible = true;
                    mfbTotalSummary.Username = sk.Username;
                    mfbTotalSummary.CustomRestriction = new FlightQuery(sk.Username);   // will call bind

                    if (privCount == 1) // if ONLY showing totals, expand it and hide the accordion
                    {
                        pnlAccordionMenuContainer.Visible = false;
                        SetAccordionPane(acpPaneTotals.ID);
                    }
                }

                if (sk.CanViewFlights)
                {
                    apcAnalysis.Container.Style["display"] = apcFilter.Container.Style["display"] = "inline-block";
                    mfbLogbook.Visible = true;
                    mfbSearchForm.Username = mfbLogbook.User = sk.Username;
                    mfbLogbook.RefreshData();

                    mfbChartTotals.Visible = true;
                    mfbChartTotals.Refresh(mfbLogbook.Data);
                }
            }

            if (mfbLogbook.Visible && apcAnalysis.Visible)
                mfbChartTotals.SourceData = mfbLogbook.Data;   // do this every time, since charttotals doesn't persist its data.
        }

        protected void UpdateForUser(string szUser)
        {
            FlightQuery r = Restriction;
            mfbTotalSummary.Username = mfbCurrency.UserName = mfbLogbook.User = szUser;
            mfbTotalSummary.CustomRestriction = mfbLogbook.Restriction = r;
            mfbCurrency.RefreshCurrencyTable();
            bool fRestrictionIsDefault = r.IsDefault;
            mfbQueryDescriptor.DataSource = fRestrictionIsDefault ? null : r;
            mfbQueryDescriptor.DataBind();
            apcFilter.LabelControl.Font.Bold = !fRestrictionIsDefault;
            apcFilter.IsEnhanced = !fRestrictionIsDefault;
            pnlFilter.Visible = !fRestrictionIsDefault;
            mfbLogbook.RefreshData();
        }

        protected void UpdateQuery()
        {
            Restriction = mfbSearchForm.Restriction;
            UpdateForUser(CurrentShareKey.Username);
            AccordionCtrl.SelectedIndex = -1;
            apcAnalysis.LazyLoad = true;
            mfbChartTotals.Visible = false;
            int idx = mfbAccordionProxyExtender.IndexForProxyID(apcAnalysis.ID);
            if (idx == AccordionCtrl.SelectedIndex)
                AccordionCtrl.SelectedIndex = -1;
            mfbAccordionProxyExtender.SetJavascriptForControl(apcAnalysis, false, idx);
        }

        public void ClearForm(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void ShowResults(object sender, EventArgs e)
        {
            UpdateQuery();
        }

        protected void mfbQueryDescriptor_QueryUpdated(object sender, FilterItemClicked fic)
        {
            if (fic == null)
                throw new ArgumentNullException(nameof(fic));
            mfbSearchForm.Restriction = Restriction.ClearRestriction(fic.FilterItem);
            UpdateQuery();
        }
    }
}