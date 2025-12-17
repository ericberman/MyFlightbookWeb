using System;
using System.Web.Mvc;

/******************************************************
 * 
 * Copyright (c) 2007-2025 MyFlightbook LLC
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
                "uploadFlightImage",
                "public/UploadPicture.aspx",
                new { controller = "Image", action = "UploadFlightImage" }
            );
            context.MapRoute(
                "uploadAircraftImage",
                "public/UploadAirplanePicture.aspx",
                new { controller = "Image", action = "UploadAircraftImage" }
            );
            context.MapRoute(
                "uploadEndorsementImage",
                "public/UploadEndorsement.aspx",
                new { controller = "Image", action = "UploadEndorsement" }
            );
            context.MapRoute(
                "mvc_allmakes",
                "mvc/AllMakes/{idman}/{idmodel}",
                new { controller = "AllMakes", action = "Index", idman = "0", idmodel = "0"}
                );
            context.MapRoute(
                "mvc_default",
                "mvc/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}