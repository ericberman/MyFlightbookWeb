using MyFlightbook;
using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbLogbookSidebar : System.Web.UI.UserControl
{

    private TabList m_tl = null;

    #region properties
    /// <summary>
    /// The currently selected tab.
    /// </summary>
    public tabID CurrentTab {get; set;}

    public TabList TabList {
        get { return m_tl; }
        set { m_tl = value; BuildSidebar(); }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
    }

    protected void BuildSidebar()
    {
        if (this.TabList == null)
            return;

        ProfileRoles.UserRole userRole = MyFlightbook.Profile.GetUser(Page.User.Identity.Name).Role;

        plcSidebar.Controls.Clear();

        foreach (TabItem ti in this.TabList.Tabs)
        {
            // don't display empty tabs.  Used for flight analysis.
            if (String.IsNullOrEmpty(ti.Text))
                continue;

            if (ti.Roles.Count > 0 && !ti.Roles.Contains(userRole))
                continue;

            Boolean fSelected = (ti.ID == CurrentTab);

            if (fSelected)
            {
                Label l = new Label();
                plcSidebar.Controls.Add(l);
                l.Text = ti.Text;
                l.CssClass = "sidebarSelected";
            }
            else
            {
                HyperLink a = new HyperLink();
                plcSidebar.Controls.Add(a);
                a.NavigateUrl = ti.Link;
                a.Text = ti.Text;
                a.CssClass = "sidebarUnSelected";
            }
        }
    }
}
