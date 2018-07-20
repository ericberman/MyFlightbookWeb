using MyFlightbook;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2012-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

/// <summary>
/// Note that we MUST DO THIS in a separate page from admin.aspx because this page has request validation off (in order to accept HTML).
/// </summary>
public partial class Member_FAQEdit : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Profile p = MyFlightbook.Profile.GetUser(Page.User.Identity.Name);
        if (!p.CanManageData)
            throw new MyFlightbookException("FAQEdit: You are not authorized to view this page.");
        this.Master.SelectedTab = tabID.admFAQ;
    }

    protected void btnInsert_Click(object sender, EventArgs e)
    {
        DBHelper dbh = new DBHelper("INSERT INTO FAQ SET Category=?Category, Question=?Question, Answer=?Answer");
        if (dbh.DoNonQuery((comm) =>
        {
            comm.Parameters.AddWithValue("Category", txtCategory.Text);
            comm.Parameters.AddWithValue("Question", txtQuestion.Text);
            comm.Parameters.AddWithValue("Answer", txtAnswer.Text);
        }))
        {
            FAQItem.FlushFAQCache();
            txtCategory.Text = txtQuestion.Text = txtAnswer.Text = string.Empty;
            gvFAQ.DataBind();
        }
    }

    protected void gvFAQ_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        FAQItem.FlushFAQCache();
        Controls_mfbHtmlEdit t = (Controls_mfbHtmlEdit)gvFAQ.Rows[e.RowIndex].FindControl("txtAnswer");
        sqlFAQ.UpdateParameters["Answer"] = new Parameter("Answer", System.Data.DbType.String, t.FixedHtml);
    }
}