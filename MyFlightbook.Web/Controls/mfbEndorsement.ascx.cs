using MyFlightbook.Instruction;
using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2015-2024 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEndorsement : System.Web.UI.UserControl, IEndorsementListUpdate
{
    const string szVSEndorsement = "endorsement";
    protected Endorsement endorsement { 
        get { return (Endorsement)ViewState[szVSEndorsement]; }
        set { ViewState[szVSEndorsement] = value; }
    }

    /// <summary>
    /// We render explicitly so that writing to a ZIP - which may have neither page context nor HttpContext.Current - can work.
    /// </summary>
    /// <param name="writer"></param>
    public override void RenderControl(HtmlTextWriter writer)
    {
        RenderHTML(endorsement, writer);
    }

    protected void Page_Load(object sender, EventArgs e) { }

    public void SetEndorsement(Endorsement e)
    {
        endorsement = e ?? throw new ArgumentNullException(nameof(e));
    }

    public void RenderHTML(Endorsement e, HtmlTextWriter tw)
    {
        if (e == null)
            throw new ArgumentNullException(nameof(e));
        if (tw == null)
            throw new ArgumentNullException(nameof(tw));

        e.RenderHTML(tw);
    }
}