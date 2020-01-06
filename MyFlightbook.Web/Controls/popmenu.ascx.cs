using System;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2015-2019 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_popmenu : System.Web.UI.UserControl, INamingContainer
{
    [TemplateContainer(typeof(MenuContentTemplate)), PersistenceMode(PersistenceMode.InnerDefaultProperty), TemplateInstance(TemplateInstance.Single)]
    public ITemplate MenuContent { get; set; }

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public int OffsetX
    {
        get { return hme.OffsetX; }
        set { hme.OffsetX = value; }
    }

    public int OffsetY
    {
        get { return hme.OffsetY; }
        set { hme.OffsetY = value; }
    }

    public PlaceHolder Container { get { return plcMenuContent; } }

    protected string SafariHackScript
    {
        get
        {
            // Issue #362: if you navigate away on Safari with a pop-up still visible, then it remains (permanently) visible upon hitting the back button.
            // So, we hide it on page unload.
            if (Request == null || Request.UserAgent == null)
                return string.Empty;

            string ua = Request.UserAgent.ToUpperInvariant();
            if (ua.Contains("SAFARI") && !ua.Contains("CHROME"))
                return String.Format(System.Globalization.CultureInfo.InvariantCulture, "<script type=\"text/javascript\">window.addEventListener('beforeunload', function (e) {{ document.getElementById('{0}').style.display = 'none'; e.returnValue = '';}});</script>", pnlMenuContent.ClientID);
            return string.Empty;
        }
    }

    protected override void OnInit(EventArgs e)
    {
        if (MenuContent != null)
            MenuContent.InstantiateIn(plcMenuContent);
        base.OnInit(e);
    }

    public class MenuContentTemplate : Control, INamingContainer
    {
        public MenuContentTemplate()
        {
        }
    }
}