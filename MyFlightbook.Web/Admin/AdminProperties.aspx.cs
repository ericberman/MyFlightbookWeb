using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2009-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Admin
{
    public partial class AdminProperties : AdminPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            CheckAdmin(Profile.GetUser(Page.User.Identity.Name).CanManageData);
            Master.SelectedTab = tabID.admImages;
        }

        protected void btnNewCustomProp_Click(object sender, EventArgs e)
        {
            DBHelper dbh = new DBHelper("INSERT INTO custompropertytypes SET Title=?Title, FormatString=?FormatString, Type=?Type, Flags=?Flags, Description=?Desc, SortKey=''");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("Title", txtCustomPropTitle.Text);
                comm.Parameters.AddWithValue("FormatString", txtCustomPropFormat.Text);
                comm.Parameters.AddWithValue("Desc", txtCustomPropDesc.Text);
                comm.Parameters.AddWithValue("Type", Convert.ToInt32(cmbCustomPropType.SelectedValue, CultureInfo.InvariantCulture));
                comm.Parameters.AddWithValue("Flags", Convert.ToInt32(lblFlags.Text, CultureInfo.InvariantCulture));
            });

            CustomPropertyType.FlushGlobalCache();
            util.FlushCache();
            gvCustomProps.DataBind();
            txtCustomPropTitle.Text = txtCustomPropFormat.Text = "";
            cmbCustomPropType.SelectedIndex = 0;
            lblFlags.Text = Convert.ToString(0, CultureInfo.CurrentCulture);
            foreach (ListItem li in CheckBoxList1.Items)
                li.Selected = false;
        }

        protected void CustPropsRowEdited(object sender, EventArgs e)
        {
            CustomPropertyType.FlushGlobalCache();
        }

        protected void CheckBoxList1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = 0;

            foreach (ListItem li in CheckBoxList1.Items)
            {
                if (li.Selected)
                    i += Convert.ToInt32(li.Value, CultureInfo.InvariantCulture);
            }
            lblFlags.Text = i.ToString(CultureInfo.InvariantCulture);
        }
    }
}