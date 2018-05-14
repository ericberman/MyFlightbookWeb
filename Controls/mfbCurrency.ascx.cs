using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;
using MyFlightbook;
using MyFlightbook.FlightCurrency;

/******************************************************
 * 
 * Copyright (c) 2007-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbCurrency : System.Web.UI.UserControl
{
    protected string m_szOKStyle = "currencyok";
    protected string m_szNotCurrentStyle = "currencyexpired";
    protected string m_szCurrencyDueStyle = "currencynearlydue";
    protected string m_szCurrencyNoDateStyle = "currencynodate";
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
    /// The name of the CSS style to use for a row that is not date based (i.e., can't determine if it's expired or close to expiring)
    /// </summary>
    public string CssCurrencyNoDate
    {
        get { return m_szCurrencyNoDateStyle; }
        set { m_szCurrencyNoDateStyle = value; }
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
            case CurrencyState.NoDate:
                return CssCurrencyNoDate;
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
        gvCurrency.DataSource = CurrencyStatusItem.GetCurrencyItemsForUser(String.IsNullOrEmpty(UserName) ? Page.User.Identity.Name : UserName);
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
            CurrencyStatusItem csi = (CurrencyStatusItem)e.Row.DataItem;
            bool fLink = !String.IsNullOrEmpty(csi.AssociatedResourceLink);
            MultiView mv = (MultiView) e.Row.FindControl("mvTitle");
            mv.ActiveViewIndex = fLink ? 1 : 0;
            if (fLink)
                ((HyperLink)e.Row.FindControl("lnkTitle")).NavigateUrl = csi.AssociatedResourceLink ?? String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?fq={2}", Branding.CurrentBrand.HostName, VirtualPathUtility.ToAbsolute("~/Member/LogbookNew.aspx"), csi.Query == null ? string.Empty : HttpUtility.UrlEncode(csi.Query.ToBase64CompressedJSONString()));

            if (UseInlineFormatting)
            {
                Panel p = (Panel) e.Row.FindControl("pnlTitle");
                p.Style["font-size"] = "12px";
                p.Style["font-weight"] = "normal";

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
                    case CurrencyState.NoDate:
                        lblStatus.Style["font-weight"] = "bold";
                        lblStatus.Style["color"] = "black";
                        break;
                    default:
                        break;
                }

                Label lblDiscrepancy = (Label)e.Row.FindControl("lblDiscrepancy");
                lblDiscrepancy.Style["font-weight"] = "normal";
                lblDiscrepancy.Style["font-size"] = "9px";

                p.Style["font-family"] = lblStatus.Style["font-family"] = lblDiscrepancy.Style["font-family"] = "Arial";
            }
        }
    }
}
