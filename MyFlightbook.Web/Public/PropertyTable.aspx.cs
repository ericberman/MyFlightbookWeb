using System;
using System.Web.UI;

/******************************************************
 * 
 * Copyright (c) 2020 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Public
{
    public partial class PropertyTable : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                gvProps.DataSource = CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.IsAuthenticated ? Page.User.Identity.Name : null);
                gvProps.DataBind();
            }
        }
    }
}