using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MyFlightbook.CloudStorage;

public partial class Public_OneDriveRedir : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        Session[OneDrive.TokenSessionKey] = new OneDrive().ConvertToken(Request);
        Response.Redirect("~/member/EditProfile.aspx/pftPrefs?1dOAuth=1");
    }
}