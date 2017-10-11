using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
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

            Panel p = new Panel();
            plcSidebar.Controls.Add(p);

            p.CssClass = (fSelected) ? "sidebarSelected" : "sidebarUnSelected";

            string szClass = "sidebarItem";

            if (fSelected)
            {
                Label l = new Label();
                p.Controls.Add(l);
                l.Text = ti.Text;
                l.CssClass = szClass;
            }
            else
            {
                HyperLink a = new HyperLink();
                p.Controls.Add(a);
                a.NavigateUrl = ti.Link;
                a.Text = ti.Text;
                a.CssClass = szClass;
            }
        }
    }
}
