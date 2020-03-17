using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_AccountQuestions : System.Web.UI.UserControl, IEditableTextControl
{
    public string Text
    {
        get { return cmbQuestions.Text; }
        set { cmbQuestions.Text = value; }
    }

    public string ValidationGroup
    {
        get { return cmbQuestions.ValidationGroup; }
        set { cmbQuestions.ValidationGroup = valQuestionRequired.ValidationGroup = valQuestionLength.ValidationGroup = value; }
    }

    public bool Required
    {
        get { return lblRequired.Visible; }
        set { lblRequired.Visible = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            cmbQuestions.DataSource = MyFlightbook.ProfileAdmin.SuggestedSecurityQuestions;
            cmbQuestions.DataBind();
        }
    }

    public event EventHandler TextChanged;

    protected void cmbQuestions_TextChanged(object sender, EventArgs e)
    {
        TextChanged?.Invoke(sender, e);
    }
}