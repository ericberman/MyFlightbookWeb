using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Hosting;

/******************************************************
 * 
 * Copyright (c) 2026 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/


namespace MyFlightbook.Injection
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
            {
                if (value == null)
                    HttpContext.Current.Session.Remove(key);
                else
                    HttpContext.Current.Session[key] = value;
            }
        }

        public bool IsAuthenticated => HttpContext.Current?.Request?.IsAuthenticated ?? false;

        public bool IsSecure => HttpContext.Current?.Request?.IsSecureConnection ?? false;

        public bool IsLocal => HttpContext.Current?.Request?.IsLocal ?? false;

        public string CurrentUserName => HttpContext.Current?.User?.Identity?.Name;

        public Uri CurrentRequestUrl => HttpContext.Current?.Request?.Url;

        public string CurrentRequestHostAddress => HttpContext.Current?.Request?.UserHostAddress;

        public string CurrentRequestUserAgent => HttpContext.Current?.Request?.UserAgent;

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

        public string RelativeToAbsolute(string relativePath)
        {
            return VirtualPathUtility.ToAbsolute(relativePath);
        }

        public string RelativeToAbsoluteFilePath(string relativePath)
        {
            return HostingEnvironment.MapPath(relativePath);
        }

        public IEnumerable<string> SessionKeys
        {
            get { return (HttpContext.Current.Session?.Keys.Cast<string>()) ?? Array.Empty<string>(); }
        }

        public IUserProfile GetUser(string szUser)
        {
            return Profile.GetUser(szUser);
        }
    }

    /// <summary>
    /// Writeable HttpRequestBase implementation to allow creation of synthetic requests for token generation, etc.
    /// </summary>
    public class SyntheticTokenRequest : HttpRequestBase
    {
        private readonly NameValueCollection _form;
        private readonly Uri _uri;

        public SyntheticTokenRequest(Uri uri, NameValueCollection form)
        {
            _uri = uri;
            _form = form;
        }

        public override NameValueCollection Form => _form;
        public override NameValueCollection Headers => new NameValueCollection();
        public override Uri Url => _uri;
        public override string HttpMethod => "POST";
        public override string RequestType => "POST";
        public override NameValueCollection QueryString => new NameValueCollection();
        public override bool IsSecureConnection => _uri.Scheme == "https";
        public override NameValueCollection ServerVariables => new NameValueCollection
        {
            { "HTTPS", _uri.Scheme == "https" ? "on" : "off" },
            { "HTTP_HOST", _uri.Host },
            { "SERVER_PORT", _uri.Port.ToString(CultureInfo.InvariantCulture) },
            { "REQUEST_URI", _uri.AbsolutePath }
        };
    }
}