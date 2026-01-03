using MyFlightbook.Weather.ADDS;
using System;
using System.Collections.Generic;
using System.Web;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook
{

    /// <summary>
    /// Concrete implementation of an IRequestContext, should capture/replace all need for HttpContext.Current
    /// </summary>
    public class MVCRequestContext : IRequestContext
    {
        public object GetSessionValue(string key)
        {
            return HttpContext.Current?.Session?[key];
        }
        public void SetSessionValue(string key, object value)
        {
            if (HttpContext.Current?.Session != null)
                HttpContext.Current.Session[key] = value;
        }

        public bool IsAuthenticated => HttpContext.Current.Request?.IsAuthenticated ?? false;

        public bool IsSecure => HttpContext.Current.Request?.IsSecureConnection ?? false;

        public bool IsLocal => HttpContext.Current.Request?.IsLocal ?? false;

        public string CurrentUserName => HttpContext.Current.User?.Identity?.Name;

        public Uri CurrentRequestUrl => HttpContext.Current.Request?.Url;

        public string CurrentRequestHostAddress => HttpContext.Current.Request?.UserHostAddress;

        public string CurrentRequestUserAgent => HttpContext.Current.Request?.UserAgent;

        public IEnumerable<string> CurrentRequestLanguages => HttpContext.Current.Request?.UserLanguages;

        public string GetCookie(string name)
        {
            return HttpContext.Current.Request.Cookies[name]?.Value;
        }

        public void SetCookie(string name, string value, DateTime? expires = null)
        {
            if (HttpContext.Current?.Response != null)
            {
                HttpCookie cookie = new HttpCookie(name, value);
                if (expires.HasValue)
                    cookie.Expires = expires.Value;
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        public void RemoveCookie(string name)
        {
            HttpContext.Current.Response.Cookies[name].Expires = DateTime.Now.AddDays(-1);  // force the browser to remove it.
            HttpContext.Current.Request.Cookies.Remove(name);   // prevent new reading of the cookie on this end.
        }

        public IEnumerable<string> SessionKeys
        {
            get { return (HttpContext.Current.Session?.Keys as IEnumerable<string>) ?? Array.Empty<string>(); }
        }
    }
}