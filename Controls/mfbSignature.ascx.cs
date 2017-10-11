using System;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2012-2017 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbSignature : System.Web.UI.UserControl
{
    private LogbookEntryDisplay m_le;

    public LogbookEntryDisplay Flight
    {
        get { return m_le; }
        set
        {
            m_le = value;
            rptSignature.DataSource = new LogbookEntryDisplay[] { m_le };
            rptSignature.DataBind();
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}