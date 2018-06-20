using MyFlightbook.Instruction;
using System;
using System.Collections.Generic;

/******************************************************
 * 
 * Copyright (c) 2015-2018 MyFlightbook LLC
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
    }
}