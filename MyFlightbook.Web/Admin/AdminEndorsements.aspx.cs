using System;
using System.Data;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminEndorsements : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageData);
            Master.SelectedTab = tabID.admEndorsements;
        }

        protected void btnAddTemplate_Click(object sender, EventArgs e)
        {
            DBHelper dbh = new DBHelper("INSERT INTO endorsementtemplates SET FARRef=?FARRef, Title=?Title, Text=?Text");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("FARRef", txtEndorsementFAR.Text);
                comm.Parameters.AddWithValue("Title", txtEndorsementTitle.Text);
                comm.Parameters.AddWithValue("Text", txtEndorsementTemplate.Text);
            });

            gvEndorsementTemplate.DataBind();
            txtEndorsementFAR.Text = txtEndorsementTemplate.Text = txtEndorsementTitle.Text = String.Empty;
        }

        protected void gvEndorsementTemplate_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                Instruction.mfbEditEndorsement ee = (Instruction.mfbEditEndorsement)e.Row.FindControl("mfbEditEndorsement1");
                if (ee != null)
                {
                    ee.EndorsementID = Convert.ToInt32(((DataRowView)e.Row.DataItem)["id"], CultureInfo.InvariantCulture);
                    util.SetValidationGroup(ee, "no-validation");
                }
            }
        }
    }
}