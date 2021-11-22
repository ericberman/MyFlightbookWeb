using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2021 MyFlightbook LLC
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
        }

        protected void btnUpdateColors_Click(object sender, EventArgs e)
        {
            foreach (GridViewRow gvr in gvCanned.Rows)
            {
                CannedQuery cq = Queries.ElementAt(gvr.RowIndex);
                cq.ColorString = ((TextBox) gvr.FindControl("txtQSamp")).Text;
                if (String.IsNullOrWhiteSpace(cq.ColorString))
                    cq.ColorString = null;  // remove it entirely.
                cq.Commit(false);
            }

            UpdateQueryList();

            lblColorsUpdated.Visible = true;
        }

        protected void gvCanned_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));

            if (e.CommandName.CompareOrdinal("_RemoveColor") == 0)
            {
                CannedQuery cq = new List<CannedQuery>(CannedQuery.QueriesForUser(Page.User.Identity.Name)).FirstOrDefault(cq2 => cq2.QueryName.CompareCurrentCultureIgnoreCase(e.CommandArgument.ToString()) == 0);
                ((TextBox)((Control)e.CommandSource).NamingContainer.FindControl("txtQSamp")).Text = string.Empty;
                ((Label)((Control)e.CommandSource).NamingContainer.FindControl("lblQSamp")).BackColor = System.Drawing.Color.Empty;
            }
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