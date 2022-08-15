using System;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc
{
    public class mvcAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "mvc";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            context.MapRoute(
                "mvc_default",
                "mvc/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}