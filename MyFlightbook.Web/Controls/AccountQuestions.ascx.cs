using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

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
        if (TextChanged != null)
            TextChanged(sender, e);
    }
}