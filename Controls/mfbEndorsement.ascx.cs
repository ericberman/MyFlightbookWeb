using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using MyFlightbook.Instruction;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Controls_mfbEndorsement : System.Web.UI.UserControl, IEndorsementListUpdate
{
    protected void Page_Load(object sender, EventArgs e)
    {

    }

    public void SetEndorsement(Endorsement e)
    {
        if (e == null)
            throw new ArgumentNullException("e");
        FormView1.DataSource = new List<Endorsement> { e };
        FormView1.DataBind();
        System.Web.UI.HtmlControls.HtmlTableRow tr = (System.Web.UI.HtmlControls.HtmlTableRow)FormView1.FindControl("rowCreationDate");
        tr.Visible = e.CreationDate.Date.CompareTo(e.Date.Date) != 0;
    }
}