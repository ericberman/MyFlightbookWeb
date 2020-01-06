using MyFlightbook;
using MyFlightbook.Templates;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_BrowseTemplates : System.Web.UI.Page
{
    #region properties
    const string szvsUserTemplates = "vsUserTemplates";
    const string szvsAddedTemplates = "vsAddedTemplates";
    const string szvsPublicTemplates = "vsPublicTemplates";

    protected IEnumerable<PropertyTemplate> PublicTemplates
    {
        get { return (IEnumerable<PropertyTemplate>)ViewState[szvsPublicTemplates]; }
        set { ViewState[szvsPublicTemplates] = value; }
    }
    protected IEnumerable<PropertyTemplate> UserPropertyTemplates
    {
        get { return (IEnumerable<PropertyTemplate>)ViewState[szvsUserTemplates]; }
        set { ViewState[szvsUserTemplates] = value; }
    }

    protected HashSet<int> AddedTemplates
    {
        get
        {
            if (ViewState[szvsAddedTemplates] == null)
                ViewState[szvsAddedTemplates] = new HashSet<int>();
            return (HashSet<int>)ViewState[szvsAddedTemplates];
        }
    }
    #endregion

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            lblDesc1.Text = Branding.ReBrand(Resources.LogbookEntry.TemplateBrowseHeaderDescription);
            UserPropertyTemplates = UserPropertyTemplate.TemplatesForUser(User.Identity.Name);
            PublicTemplates = UserPropertyTemplate.PublicTemplates();
            Refresh();
        }
    }

    protected void Refresh()
    {
        gvPublicTemplates.DataSource = TemplateCollection.GroupTemplates(PublicTemplates);
        gvPublicTemplates.DataBind();
    }

    protected PropertyTemplate MatchingOwnedTemplate(PropertyTemplate pt)
    {
        return UserPropertyTemplates.FirstOrDefault(ptUser => ptUser.Group == pt.Group && ptUser.Name.CompareCurrentCultureIgnoreCase(pt.Name) == 0);
    }

    protected void gvTemplates_RowCommand(object sender, GridViewCommandEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.CommandName.CompareCurrentCultureIgnoreCase("_add") == 0)
        {
            int id = Convert.ToInt32(e.CommandArgument, CultureInfo.InvariantCulture);
            UserPropertyTemplate pt = new UserPropertyTemplate(id);
            PropertyTemplate ptMatch = MatchingOwnedTemplate(pt);
            PersistablePropertyTemplate pptNew = pt.CopyPublicTemplate(User.Identity.Name);
            if (ptMatch != null)
                pptNew.ID = ptMatch.ID;
            pptNew.Commit();
            AddedTemplates.Add(id);
            Refresh();
        }
    }

    protected void gvTemplates_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");

        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            UserPropertyTemplate pt = (UserPropertyTemplate)e.Row.DataItem;

            bool fOwned = pt.Owner.CompareCurrentCultureIgnoreCase(User.Identity.Name) == 0;

            MultiView mvStatus = (MultiView)e.Row.FindControl("mvStatus");

            if (fOwned)
                mvStatus.SetActiveView((View)mvStatus.FindControl("vwOwned"));
            else if (AddedTemplates.Contains(pt.ID))
                mvStatus.SetActiveView((View)mvStatus.FindControl("vwAdded"));
            else
                mvStatus.SetActiveView((View)mvStatus.FindControl("vwUnOwned"));

            if (MatchingOwnedTemplate(pt) != null)
                ((AjaxControlToolkit.ConfirmButtonExtender)e.Row.FindControl("confirmOverwrite")).Enabled = true;
        }
    }
}