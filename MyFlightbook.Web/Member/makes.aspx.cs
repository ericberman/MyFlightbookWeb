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
 * Copyright (c) 2007-2019 MyFlightbook LLC
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
    public static string[] HtmlRowsForMakes(string szRestrict, int skip, int pageSize)
    {
        List<string> lst = new List<string>();

        // We have no Page, so things like Page_Load don't get called.
        // We fix this by faking a page and calling Server.Execute on it.  This sets up the form and - more importantly - causes Page_load to be called on loaded controls.
        using (Page p = new FormlessPage())
        {
            p.Controls.Add(new HtmlForm());
            using (StringWriter sw1 = new StringWriter(CultureInfo.CurrentCulture))
                HttpContext.Current.Server.Execute(p, sw1, false);

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
                StringWriter sw = null;
                try
                {
                    sw = new StringWriter(sb, CultureInfo.CurrentCulture);
                    using (HtmlTextWriter htmlTW = new HtmlTextWriter(sw))
                    {
                        sw = null;
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
                finally
                {
                    if (sw != null)
                        sw.Dispose();
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
            string szQuery = util.GetStringParam(Request, "q");
            if (!String.IsNullOrEmpty(szQuery))
            {
                string szJSon = Convert.FromBase64String(szQuery).Uncompress();
                ActiveQuery = JsonConvert.DeserializeObject<ModelQuery>(szJSon, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate });
                QueryToForm(ActiveQuery);
                UpdateFilter();
            }

            UpdateSortHeaders(ActiveQuery.SortMode);
        }

        this.Master.SelectedTab = tabID.actMakes;
        this.Title = (string)GetLocalResourceObject("PageResource2.Title");
    }

    protected void QueryToForm(ModelQuery mq)
    {
        mfbSearchbox.SearchText = mq.FullText;
        txtModel.Text = mq.Model;
        txtModelName.Text = mq.ModelName;
        txtTypeName.Text = mq.TypeName;
        txtManufacturer.Text = mq.ManufacturerName;
        txtCatClass.Text = mq.CatClass;

        mvSearchForm.SetActiveView(mq.Model.Length + mq.ModelName.Length + mq.TypeName.Length + mq.ManufacturerName.Length + mq.CatClass.Length > 0 ? vwAdvancedSearch : vwSimpleSearch);
    }

    protected ModelQuery QueryFromForm(ModelQuery mq)
    {
        mq.Skip = 0;
        mq.FullText = mfbSearchbox.SearchText;
        mq.Model = txtModel.Text;
        mq.ModelName = txtModelName.Text;
        mq.TypeName = txtTypeName.Text;
        mq.ManufacturerName = txtManufacturer.Text;
        mq.CatClass = txtCatClass.Text;
        return mq;
    }

    protected void UpdateFilter() 
    {
        ModelQuery mq = ActiveQuery = QueryFromForm(ActiveQuery);

        tblHeaderRow.Visible = true;
        gvMakes.DataSource = MakeModel.MatchingMakes(mq);
        gvMakes.DataBind();
    }

    protected void FilterTextChanged(object sender, System.EventArgs e)
    {
        string szJSon = JsonConvert.SerializeObject(QueryFromForm(ActiveQuery), new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        string szQ = Convert.ToBase64String(szJSon.Compress());
        Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}?q={3}", Request.Url.Scheme, Request.Url.Host, Request.Url.AbsolutePath, HttpUtility.UrlEncode(szQ)));
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
        mfbSearchbox.SearchText = string.Empty;
    }

    protected void lnkSimpleSearch_Click(object sender, EventArgs e)
    {
        mvSearchForm.SetActiveView(vwSimpleSearch);
        txtCatClass.Text = txtManufacturer.Text = txtModel.Text = txtModelName.Text = txtTypeName.Text = string.Empty;
    }
}
