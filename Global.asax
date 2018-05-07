<%@ Application Language="C#" %>

<script RunAt="server">
    /******************************************************
     * 
     * Copyright (c) 2015 MyFlightbook LLC
     * Contact myflightbook-at-gmail.com for more information
     *
    *******************************************************/

    protected void Application_Start(object sender, EventArgs e)
    {
        // Code that runs on application startup
        MyFlightbook.ShuntState.Init();
        ScriptManager.ScriptResourceMapping.AddDefinition("jquery", new ScriptResourceDefinition { Path = "https://code.jquery.com/jquery-1.10.1.min.js" });
        System.Web.UI.ValidationSettings.UnobtrusiveValidationMode = UnobtrusiveValidationMode.WebForms;
    }

    protected void Application_End(object sender, EventArgs e)
    {
        //  Code that runs on application shutdown

    }

    protected void Application_Error(object sender, EventArgs e)
    {
        // Code that runs when an unhandled error occurs
        StringBuilder ErrorMessage = new StringBuilder();
        Exception myError = null;

        myError = Server.GetLastError();

        if (Context.Request.IsLocal)
            return;

        HttpException err = myError as HttpException;
        if (err != null && err.GetHttpCode() == 404)
            return;

        if (Context != null)
        {
            ErrorMessage.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Request = {0}://{1}{2}\r\nUser = {3}\r\nLanguage={4}\r\nBy: {5} {6} {7}\r\n\r\n\r\n", Context.Request.IsSecureConnection ? "https" : "http",
                Context.Request.Url.Host, Context.Request.Url.PathAndQuery, Context.User.Identity.Name,
                Context.Request.UserLanguages == null ? "(none)" : String.Join(", ", Context.Request.UserLanguages),
            Context.Request.UserAgent, Context.Request.UserHostAddress, Context.Request.UserHostName);
            if (Context.Request != null && Context.Request.UrlReferrer != null)
                ErrorMessage.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "Referrer requested: {0}\r\n", Context.Request.UrlReferrer.ToString());

            string szKey = Context.User.Identity.Name + "-LastPage";
            if (Session[szKey] != null)
                ErrorMessage.Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, "Last page requested by user = {0}\r\n", Session[szKey].ToString()));
        }

        MyFlightbook.util.NotifyAdminException(ErrorMessage.ToString(), myError);
    }

    protected void Session_Start(object sender, EventArgs e)
    {
        // Code that runs when a new session is started

    }

    protected void Session_End(object sender, EventArgs e)
    {
        // Code that runs when a session ends. 
        // Note: The Session_End event is raised only when the sessionstate mode
        // is set to InProc in the Web.config file. If session mode is set to StateServer 
        // or SQLServer, the event is not raised.

    }

</script>

