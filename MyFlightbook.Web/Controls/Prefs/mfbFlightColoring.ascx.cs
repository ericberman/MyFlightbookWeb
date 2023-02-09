using System;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2021-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls.Prefs
{
    public partial class mfbFlightColoring : UserControl
    {
        const string szVSQueries = "vsCannedQueries";
        private IEnumerable<CannedQuery> Queries
        {
            get { return (IEnumerable<CannedQuery>)ViewState[szVSQueries]; }
            set { ViewState[szVSQueries] = value; }
        }

        protected void UpdateQueryList()
        {
            Queries = CannedQuery.QueriesForUser(Page.User.Identity.Name);
            foreach (CannedQuery cq in Queries)
                cq.Refresh();
            gvCanned.DataSource = Queries;
            gvCanned.DataBind();
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Convert any keyword colors into cannedqueries.
                FlightColor.MigrateFlightColors(Page.User.Identity.Name);
                UpdateQueryList();
            }

            Profile pf = Profile.GetUser(Page.User.Identity.Name);
            txtPathSample.Text = pf.GetPreferenceForKey<string>(MFBConstants.keyPathColor, Mapping.MFBGoogleMapOptions.DefaultPathColor);
            lblPathSample.BackColor = FlightColor.TryParseColor(txtPathSample.Text);
            txtRteSample.Text = pf.GetPreferenceForKey<string>(MFBConstants.keyRouteColor, Mapping.MFBGoogleMapOptions.DefaultRouteColor);
            lblRteSample.BackColor = FlightColor.TryParseColor(txtRteSample.Text);
        }

        protected void gvCanned_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                CannedQuery cq = (CannedQuery)e.Row.DataItem;

                if (!String.IsNullOrWhiteSpace(cq.ColorString))
                {
                    TextBox t = (TextBox)e.Row.FindControl("txtQsamp");
                    Label l = (Label)e.Row.FindControl("lblQSamp");
                    t.Text = cq.ColorString;
                    l.BackColor = FlightColor.TryParseColor(cq.ColorString);
                }
            }
        }
    }
}