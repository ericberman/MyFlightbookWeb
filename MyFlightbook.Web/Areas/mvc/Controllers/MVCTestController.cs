using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Web;
using System.Web.Mvc;
using MyFlightbook;

/******************************************************
 * 
 * Copyright (c) 2007-2022 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Web.Areas.mvc.Controllers
{
    public class MVCTestController : Controller
    {
        // GET: mvc/MVCTest
        public ActionResult Index()
        {
            ViewBag.WelcomeMessage = (User.Identity.IsAuthenticated) ?
                String.Format(CultureInfo.CurrentCulture, "Hello {0}!", MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName) :
                "Hi.  Why aren't you signed in?";
            ViewBag.Title = "Sample MVC Page";
            return View();
        }

        [Authorize]
        public ActionResult Index2()
        {
            ViewBag.WelcomeMessage = (User.Identity.IsAuthenticated) ?
                String.Format(CultureInfo.CurrentCulture, "Hello {0}! - if you're here, you are authenticated!", MyFlightbook.Profile.GetUser(User.Identity.Name).UserFullName) :
                "Hi.  Why aren't you signed in?  You shouldn't be able to see this!!!";
            ViewBag.Title = "Sample AUTHORIZED MVC Page";
            return View("Index");
        }
    }
}