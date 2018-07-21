using MyFlightbook;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class makes : System.Web.UI.Page
{
    private const int defaultPageSize = 25;

    protected int PageSize { get; set; }
    protected string Order {get; set;}

    protected ModelQuery ActiveQuery
    {
        get { return JsonConvert.DeserializeObject<ModelQuery>(hdnQueryJSON.Value); }
        set { hdnQueryJSON.Value = JsonConvert.SerializeObject(value); }
    }

    [WebMethod()]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
    public static string[] HtmlRowsForMakes(string szRestrict, int skip, int pageSize)
    {
        List<string> lst = new List<string>();

        // We have no Page, so things like Page_Load don't get called.
        // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
        using (Page p = new FormlessPage())
        {
            p.Controls.Add(new HtmlForm());
            using (StringWriter sw = new StringWriter(CultureInfo.CurrentCulture))
                HttpContext.Current.Server.Execute(p, sw, false);

            ModelQuery mq = JsonConvert.DeserializeObject<ModelQuery>(szRestrict);
            mq.Skip = skip;
            mq.Limit = pageSize;

            foreach (MakeModel m in MakeModel.MatchingMakes(mq))
            {
                Controls_mfbMakeListItem mli = (Controls_mfbMakeListItem)p.LoadControl("~/Controls/mfbMakeListItem.ascx");
                HtmlTableRow tr = new HtmlTableRow();
                p.Form.Controls.Add(tr);
                HtmlTableCell tc = new HtmlTableCell();
                tr.Cells.Add(tc);
                tc.VAlign = "top";
                tc.Controls.Add(mli);
                // Now, write it out.
                StringBuilder sb = new StringBuilder();
                using (StringWriter sw = new StringWriter(sb, CultureInfo.CurrentCulture))
                    using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                    {
                        try
                        {
                            mli.SortMode = mq.SortMode;
                            mli.Model = m;
                            mli.ModelLink.NavigateUrl = VirtualPathUtility.ToAbsolute(mli.ModelLink.NavigateUrl);
                            tr.RenderControl(htmlTW);
                            lst.Add(sb.ToString());
                        }
                        catch (ArgumentException) { } // don't write bogus or incomplete HTML
                    }
            }
        }

        return lst.ToArray();
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        PageSize = defaultPageSize;

        if (!IsPostBack)
        {
            ActiveQuery = new ModelQuery() { SortMode = ModelQuery.ModelSortMode.ModelName, SortDir = ModelQuery.ModelSortDirection.Ascending, Limit = PageSize, Skip = 0, IncludeSampleImages = true };
            UpdateSortHeaders(ActiveQuery.SortMode);
        }

        this.Master.SelectedTab = tabID.actMakes;
        this.Master.Layout = MasterPage.LayoutMode.Accordion;
        this.Title = (string)GetLocalResourceObject("PageResource2.Title");
    }

    protected void UpdateFilter() 
    {
        ModelQuery mq = ActiveQuery;
        mq.Skip = 0;
        mq.FullText = txtFilter.Text;
        mq.Model = txtModel.Text;
        mq.ModelName = txtModelName.Text;
        mq.TypeName = txtTypeName.Text;
        mq.ManufacturerName = txtManufacturer.Text;
        mq.CatClass = txtCatClass.Text;
        ActiveQuery = mq;

        tblHeaderRow.Visible = true;
        gvMakes.DataSource = MakeModel.MatchingMakes(mq);
        gvMakes.DataBind();
    }

    protected void FilterTextChanged(object sender, System.EventArgs e)
    {
        gvMakes.PageIndex = 0;
        UpdateFilter();
    }

    protected void MakesRowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            Controls_mfbMakeListItem mli = (Controls_mfbMakeListItem)e.Row.FindControl("mfbMakeListItem1");
            ModelQuery mq = ActiveQuery;
            mli.SortMode = mq.SortMode;
            mli.Model = (MakeModel) e.Row.DataItem;
        }
    }

    protected void ShowButton(Control lnk, Boolean fShow)
    {
        if (lnk != null)
            lnk.Visible = fShow;
    }

    protected void btnAddNew_Click(object sender, EventArgs e)
    {
        Response.Redirect("~/Member/EditMake.aspx?id=-1");
    }

    protected void UpdateSortHeaders(ModelQuery.ModelSortMode sortmode)
    {
        lnkSortCatclass.Font.Bold = (sortmode == ModelQuery.ModelSortMode.CatClass);
        lnkSortCatclass.Font.Size = (sortmode == ModelQuery.ModelSortMode.CatClass) ? FontUnit.Larger : FontUnit.Empty;
        lnkSortManufacturer.Font.Bold = (sortmode == ModelQuery.ModelSortMode.Manufacturer);
        lnkSortManufacturer.Font.Size = (sortmode == ModelQuery.ModelSortMode.Manufacturer) ? FontUnit.Larger : FontUnit.Empty;
        lnkSortModel.Font.Bold = (sortmode == ModelQuery.ModelSortMode.ModelName);
        lnkSortModel.Font.Size = (sortmode == ModelQuery.ModelSortMode.ModelName) ? FontUnit.Larger : FontUnit.Empty;
    }

    protected void SetSort(ModelQuery.ModelSortMode sortmode)
    {
        ModelQuery mq = ActiveQuery;
        if (mq.SortMode == sortmode)
            mq.SortDir = (mq.SortDir == ModelQuery.ModelSortDirection.Ascending) ? ModelQuery.ModelSortDirection.Descending : ModelQuery.ModelSortDirection.Ascending;
        else
        {
            mq.SortMode = sortmode;
            mq.SortDir = ModelQuery.ModelSortDirection.Ascending;
        }
        ActiveQuery = mq;
        UpdateSortHeaders(sortmode);
        
        UpdateFilter();
    }

    protected void lnkSortCatclass_Click(object sender, EventArgs e)
    {
        SetSort(ModelQuery.ModelSortMode.CatClass);
    }
    
    protected void lnkSortManufacturer_Click(object sender, EventArgs e)
    {
        SetSort(ModelQuery.ModelSortMode.Manufacturer);
    }

    protected void lnkSortModel_Click(object sender, EventArgs e)
    {
        SetSort(ModelQuery.ModelSortMode.ModelName);
    }

    protected void lnkAdvanced_Click(object sender, EventArgs e)
    {
        mvSearchForm.SetActiveView(vwAdvancedSearch);
        txtFilter.Text = string.Empty;
    }

    protected void lnkSimpleSearch_Click(object sender, EventArgs e)
    {
        mvSearchForm.SetActiveView(vwSimpleSearch);
        txtCatClass.Text = txtManufacturer.Text = txtModel.Text = txtModelName.Text = txtTypeName.Text = string.Empty;
    }
}
