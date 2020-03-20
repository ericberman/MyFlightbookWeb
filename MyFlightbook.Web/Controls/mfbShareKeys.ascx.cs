using MyFlightbook.Web.Sharing;
using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;


/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Controls
{
    public partial class mfbShareKeys : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
                RefreshSharekeys();
            Page.ClientScript.RegisterClientScriptInclude("copytoClip", ResolveClientUrl("~/public/Scripts/CopyClipboard.js"));
        }

        protected void btnCreateShareLink_Click(object sender, EventArgs e)
        {
            ShareKey sk = new ShareKey(Page.User.Identity.Name)
            {
                Name = txtShareLinkName.Text,
                CanViewCurrency = ckShareLinkCurrency.Checked,
                CanViewFlights = ckShareLinkFlights.Checked,
                CanViewTotals = ckShareLinkTotals.Checked,
                CanViewAchievements = ckShareLinkAchievements.Checked,
                CanViewVisitedAirports = ckShareLinkAirports.Checked
            };
            try
            {
                sk.FCommit();
                txtShareLinkName.Text = string.Empty;
                ckShareLinkCurrency.Checked = ckShareLinkFlights.Checked = ckShareLinkTotals.Checked = ckShareLinkAchievements.Checked = ckShareLinkAirports.Checked = true;
                gvShareKeys.EditIndex = -1;
                RefreshSharekeys();
            }
            catch (MyFlightbookValidationException ex)
            {
                lblShareKeyErr.Text = ex.Message;
            }
        }

        protected void RefreshSharekeys()
        {
            gvShareKeys.DataSource = ShareKey.ShareKeysForUser(Page.User.Identity.Name);
            gvShareKeys.DataBind();
        }

        protected void gvShareKeys_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                ImageButton imgCopy = (ImageButton)e.Row.FindControl("imgCopyLink");
                Label lblCopied = (Label)e.Row.FindControl("lblLinkCopied");
                TextBox tx = (TextBox)e.Row.FindControl("txtShareLink");
                imgCopy.OnClientClick = String.Format(CultureInfo.InvariantCulture, "javascript:copyClipboard(null, '{0}', true, '{1}');return false;", tx.ClientID, lblCopied.ClientID);
            }
        }

        protected void gvShareKeys_RowEditing(object sender, GridViewEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvShareKeys.EditIndex = e.NewEditIndex;
            RefreshSharekeys();
        }

        protected void gvShareKeys_RowCancelingEdit(object sender, GridViewCancelEditEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            gvShareKeys.EditIndex = -1;
            RefreshSharekeys();
        }

        protected void gvShareKeys_RowUpdating(object sender, GridViewUpdateEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            ShareKey sk = ShareKey.ShareKeyWithID(e.Keys[0].ToString());
            if (sk == null)
                throw new InvalidOperationException("Unknown key: " + e.Keys[0].ToString());
            sk.Name = e.NewValues["Name"].ToString();
            sk.CanViewCurrency = (bool)e.NewValues["CanViewCurrency"];
            sk.CanViewTotals = (bool)e.NewValues["CanViewTotals"];
            sk.CanViewFlights = (bool)e.NewValues["CanViewFlights"];
            sk.CanViewAchievements = (bool)e.NewValues["CanViewAchievements"];
            sk.CanViewVisitedAirports = (bool)e.NewValues["CanViewVisitedAirports"];
            if (sk.FCommit())
            {
                gvShareKeys.EditIndex = -1;
                RefreshSharekeys();
            }
            else
                e.Cancel = true;
        }

        protected void gvShareKeys_RowCommand(object sender, CommandEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (String.Compare(e.CommandName, "_Delete", StringComparison.OrdinalIgnoreCase) == 0)
            {
                ShareKey sk = ShareKey.ShareKeyWithID(e.CommandArgument.ToString());
                if (sk != null)
                {
                    sk.FDelete();
                    RefreshSharekeys();
                }
            }
        }
    }
}