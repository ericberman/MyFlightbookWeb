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
        protected void UpdateQueryList()
        {
            gvCanned.DataSource = CannedQuery.QueriesForUser(Page.User.Identity.Name); ;
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

            Dictionary<string, CannedQuery> dCq = new Dictionary<string, CannedQuery>();
            foreach (CannedQuery cq in CannedQuery.QueriesForUser(Page.User.Identity.Name))
                dCq[cq.QueryName] = cq;
            foreach (GridViewRow gvr in gvCanned.Rows)
            {
                CannedQuery cq = dCq[((HiddenField)gvr.FindControl("hdnKey")).Value];    // shouldn't ever fail.
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

        protected void btnAddQuery_Click(object sender, EventArgs e)
        {
            if (sender == null)
                throw new ArgumentNullException(nameof(sender));
            TextBox t = (TextBox) ((Control)sender).NamingContainer.FindControl("txtKey1");
            if (!String.IsNullOrEmpty(t?.Text))
            {
                FlightColor fc = new FlightColor() { KeyWord = t.Text, ColorString="FFF1D4" };
                fc.AsQuery(Page.User.Identity.Name).Commit(false);
                t.Text = string.Empty;
                UpdateQueryList();
            }
        }
    }
}