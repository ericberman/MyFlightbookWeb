using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.FlightCurrency;

/******************************************************
 * 
 * Copyright (c) 2007-2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbCurrency : System.Web.UI.UserControl
{
    protected string m_szOKStyle = "currencyok";
    protected string m_szNotCurrentStyle = "currencyexpired";
    protected string m_szCurrencyDueStyle = "currencynearlydue";
    protected string m_szCurrencyGap = "currencygap";
    protected string m_szCurrencyLabel = "currencylabel";

    #region Properties

    #region CSS styles
    /// <summary>
    /// The name of the CSS style to use for a row that indicates currency
    /// </summary>
    public string CssOK
    {
        get { return m_szOKStyle; }
        set { m_szOKStyle = value; }
    }

    /// <summary>
    /// The name of the CSS style to use for a row that indicates out of currency
    /// </summary>
    public string CssNotCurrent
    {
        get { return m_szNotCurrentStyle; }
        set { m_szNotCurrentStyle = value; }
    }

    /// <summary>
    /// The name of the CSS style to use for a row that indicates that currency is close to expiring.
    /// </summary>
    public string CssCurrencyNearlyDue
    {
        get { return m_szCurrencyDueStyle; }
        set { m_szCurrencyDueStyle = value; }
    }

    /// <summary>
    /// The name of the CSS style to use for the text indicating how to close the currency gap
    /// </summary>
    public string CssCurrencyGap
    {
        get { return m_szCurrencyGap; }
        set { m_szCurrencyGap = value; }
    }

    /// <summary>
    /// The name of the CSS style for the currency labels.
    /// </summary>
    public string CssCurrencyLabel
    {
        get { return m_szCurrencyLabel; }
        set { m_szCurrencyLabel = value; }
    }
    #endregion

    /// <summary>
    /// The name of the user for whom the currency table is being displayed
    /// </summary>
    public string UserName {get; set; }

    /// <summary>
    /// Specifies whether to use CSS or in-line styles.  In-line is useful when the stylesheet cannot be assumed, such as in an RSS feed.
    /// </summary>
    public bool UseInlineFormatting {get; set;}

    /// <summary>
    /// Set to true to disable autorefresh on initial GET. You'll need to call RefreshCurrencyTable if you want to refresh currency.
    /// </summary>
    public bool SuppressAutoRefresh { get; set; }
    #endregion

    protected string CSSForItem(CurrencyState cs)
    {
        switch (cs)
        {
            case CurrencyState.GettingClose:
                return CssCurrencyNearlyDue;
            case CurrencyState.NotCurrent:
                return CssNotCurrent;
            case CurrencyState.OK:
                return CssOK;
        }
        return string.Empty;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            if (!SuppressAutoRefresh)
                RefreshCurrencyTable();
        }
    }

    public void RefreshCurrencyTable()
    {
        gvCurrency.DataSource = CurrencyStatusItem.GetCurrencyItemsForUser(String.IsNullOrEmpty(UserName) ? Page.User.Identity.Name : UserName, true);
        gvCurrency.DataBind();

        // HACK - We do this here because Page_Load may not be called if this is for an RSS feed.
        if (lnkDisclaimer.NavigateUrl.Contains("~"))
            lnkDisclaimer.NavigateUrl = "http://" + MyFlightbook.Branding.CurrentBrand.HostName + VirtualPathUtility.ToAbsolute(lnkDisclaimer.NavigateUrl);
    }

    protected void gvCurrency_RowDataBound(object sender, GridViewRowEventArgs e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        if (e.Row.RowType == DataControlRowType.DataRow)
        {
            if (UseInlineFormatting)
            {
                CurrencyStatusItem csi = (CurrencyStatusItem)e.Row.DataItem;

                Label lblTitle = (Label)e.Row.FindControl("lblTitle");
                lblTitle.Style["font-size"] = "12px";
                lblTitle.Style["font-weight"] = "normal";

                Label lblStatus = (Label)e.Row.FindControl("lblStatus");
                lblStatus.Style["font-size"] = "12px";
                switch (csi.Status)
                {
                    case CurrencyState.OK:
                        lblStatus.Style["font-weight"] = "normal";
                        lblStatus.Style["color"] = "green";
                        break;
                    case CurrencyState.NotCurrent:
                        lblStatus.Style["font-weight"] = "bold";
                        lblStatus.Style["color"] = "red";
                        break;
                    case CurrencyState.GettingClose:
                        lblStatus.Style["font-weight"] = "bold";
                        lblStatus.Style["color"] = "blue";
                        break;
                    default:
                        break;
                }

                Label lblDiscrepancy = (Label)e.Row.FindControl("lblDiscrepancy");
                lblDiscrepancy.Style["font-weight"] = "normal";
                lblDiscrepancy.Style["font-size"] = "9px";

                lblTitle.Style["font-family"] = lblStatus.Style["font-family"] = lblDiscrepancy.Style["font-family"] = "Arial";
            }
        }
    }
}
