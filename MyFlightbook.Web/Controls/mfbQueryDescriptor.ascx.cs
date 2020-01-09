using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2016 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbQueryDescriptor : System.Web.UI.UserControl
{
    const string szVSShowEmptyFilter = "szVSShowEmptyFilter";

    public FlightQuery DataSource { get; set; }

    public event EventHandler<FilterItemClicked> QueryUpdated = null;

    public bool ShowEmptyFilter
    {
        get { return (bool)(ViewState[szVSShowEmptyFilter] ?? false); }
        set { ViewState[szVSShowEmptyFilter] = value; }
    }

    public override void DataBind()
    {
        IEnumerable<QueryFilterItem> src = (DataSource == null || DataSource.IsDefault) ? new QueryFilterItem[0] : DataSource.QueryFilterItems;

        rptItems.DataSource = src;
        rptItems.DataBind();

        rptItems.Visible = (src.Count() > 0);
        pnlNoFilter.Visible = ShowEmptyFilter && !rptItems.Visible;
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void filterItem_DeleteItemClicked(object sender, FilterItemClicked fic)
    {
        if (fic == null)
            throw new ArgumentNullException("fic");

        if (QueryUpdated != null)
            QueryUpdated(sender, fic);
    }
}