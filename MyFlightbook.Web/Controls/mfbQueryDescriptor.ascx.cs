using MyFlightbook;
using System;
using System.Collections.Generic;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2016-2020 MyFlightbook LLC
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
        List<QueryFilterItem> src = new List<QueryFilterItem>((DataSource == null || DataSource.IsDefault) ? Array.Empty<QueryFilterItem>() : DataSource.QueryFilterItems);

        // Add a "Clear all" if more than one restriction (redundant if only one restriction).
        if (src.Count > 1)
            src.Add(new QueryFilterItem(Resources.FlightQuery.ClearAllCriteria, string.Empty, string.Empty));

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
            throw new ArgumentNullException(nameof(fic));

        QueryUpdated?.Invoke(sender, fic);
    }
}