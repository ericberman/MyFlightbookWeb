using System;
using MyFlightbook;

public partial class Controls_PrintingLayouts_pageHeader : System.Web.UI.UserControl
{
    public string UserName { get; set; }

    private Profile m_user = null;
    protected Profile CurrentUser
    {
        get
        {
            if (m_user == null)
                m_user = MyFlightbook.Profile.GetUser(UserName);
            return m_user;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}