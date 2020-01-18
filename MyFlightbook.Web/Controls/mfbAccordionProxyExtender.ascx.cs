using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2016-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbAccordionProxyExtender : System.Web.UI.UserControl
{
    /// <summary>
    /// Object we can easily serialize for the javascript object
    /// </summary>
    protected class ProxyExtenderSettings
    {
        #region Properties
        public string AccordionControlClientID { get; set; }

        public IEnumerable<string> HeaderProxyPostbackIDs { get; set; }

        public IEnumerable<string> HeaderProxyClientIDs { get; set; }

        public string OpenCSSClass { get; set; }

        public string CloseCSSClass { get; set; }
        #endregion

        public ProxyExtenderSettings() { }
    }

    #region Properties
    #region public
    /// <summary>
    /// The ID of the accordion control being used by this control
    /// </summary>
    public string AccordionControlID { get; set; }

    /// <summary>
    /// A comma-separated list of *client* IDs of mfbAccordionProxyControl objects to collapse/expand the accordion control, in order.  I.e., the first ID shows/hides the first accordion pane, the second ID controls the 2nd accordion pane, etc.
    /// </summary>
    public string HeaderProxyIDs { get; set; }

    /// <summary>
    /// CSS class to use when the proxy control is selected (in the "open" state)
    /// </summary>
    public string OpenCSSClass { get; set; }

    /// <summary>
    /// CSS class to use when the proxy control is unselected (in the "closed" state)
    /// </summary>
    public string CloseCSSClass { get; set; }
    #endregion

    #region protected
    protected AjaxControlToolkit.Accordion AccordionControl
    {
        get { return (AjaxControlToolkit.Accordion)NamingContainer.FindControl(AccordionControlID); }
    }

    protected string AccordionControlClientID
    {
        get { return AccordionControl.ClientID + "_AccordionExtender"; }
    }

    protected IEnumerable<string> HeaderProxyIDArray
    {
        get { return String.IsNullOrEmpty(HeaderProxyIDs) ? new string[0] : HeaderProxyIDs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
    }

    protected IEnumerable<string> HeaderProxyClientIDs
    {
        get
        {
            List<string> lst = new List<string>();
            foreach (string szID in HeaderProxyIDArray)
            {
                Control c = NamingContainer.FindControl(szID);
                Controls_mfbAccordionProxyControl apc = c as Controls_mfbAccordionProxyControl;
                if (apc != null)
                    lst.Add(apc.Container.ClientID);
                else
                    lst.Add(c.ClientID);
            }
            return lst.ToArray();
        }
    }

    protected IEnumerable<string> HeaderProxyPostbackIDs
    {
        get
        {
            List<string> lst = new List<string>();
            foreach (string szID in HeaderProxyIDArray)
            {
                Control c = NamingContainer.FindControl(szID);
                Controls_mfbAccordionProxyControl apc = c as Controls_mfbAccordionProxyControl;
                if (apc != null)
                    lst.Add(apc.PostbackID);
                else
                    lst.Add(c.ClientID);
            }
            return lst.ToArray();
        }
    }

    protected string JScriptObjectName
    {
        get { return ClientID + "ape"; }
    }

    protected ProxyExtenderSettings Settings
    {
        get
        {
            return new ProxyExtenderSettings() { AccordionControlClientID = this.AccordionControlClientID, HeaderProxyClientIDs = this.HeaderProxyClientIDs, HeaderProxyPostbackIDs = this.HeaderProxyPostbackIDs, CloseCSSClass = this.CloseCSSClass, OpenCSSClass = this.OpenCSSClass };
        }
    }
    #endregion
    #endregion

    /// <summary>
    /// Wires up the onclick javascript and appropriate CSS for the specified control (should generally be an AccordionProxyControl
    /// </summary>
    /// <param name="c">The control to wire up</param>
    /// <param name="isSelected">True if this proxy control is selected</param>
    /// <param name="idx">The index of the control (i.e., to which pane does it correspond)</param>
    public void SetJavascriptForControl(Control c, bool isSelected, int idx)
    {
        Controls_mfbAccordionProxyControl apc = c as Controls_mfbAccordionProxyControl;
        string szClickScript = String.Format(CultureInfo.InvariantCulture, "javascript:{0}.proxyClicked({1});", JScriptObjectName, idx);
        string szPostbackScript = apc == null ? string.Empty : String.Format(CultureInfo.InvariantCulture, "javascript:{0}.proxyPostbackClicked({1});", JScriptObjectName, idx);
        string szCSS = isSelected ? OpenCSSClass : CloseCSSClass;

        WebControl wc = apc == null ? (WebControl)c : apc.Container;
        wc.CssClass = szCSS;
        wc.Attributes["onclick"] = (apc != null && apc.LazyLoad) ? szPostbackScript : szClickScript;
    }

    /// <summary>
    /// Finds the index of the proxy with the specified ID
    /// </summary>
    /// <param name="ID"></param>
    /// <returns></returns>
    public int IndexForProxyID(string ID)
    {
        int i = 0;
        foreach (string id in HeaderProxyIDArray)
        {
            if (String.CompareOrdinal(id, ID) == 0)
                return i;
            i++;
        }
        return -1;
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        Page.ClientScript.RegisterClientScriptInclude("accordionExtender", ResolveClientUrl("~/public/Scripts/mfbAccordionProxy.js?v=1"));
    }

    protected void Page_PreRender(object sender, EventArgs e)
    {
        int i = 0;
        AjaxControlToolkit.Accordion acc = AccordionControl;

        // Default css classes
        if (String.IsNullOrEmpty(OpenCSSClass))
            OpenCSSClass = "accordionMenuControllerSelected";
        if (String.IsNullOrEmpty(CloseCSSClass))
            CloseCSSClass = "accordionMenuControllerUnselected";

        foreach (string id in HeaderProxyIDArray)
        {
            SetJavascriptForControl(NamingContainer.FindControl(id), (i == acc.SelectedIndex), i);
            i++;
        }
    }
}