<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
</head>

<body>
<h1>TEST!</h1>
<%
        dim sz
        sz = UCase(Request.ServerVariables("HTTP_HOST"))

        if (sz = "MYFLIGHTBOOK.COM" OR sz = "WWW.MYFLIGHTBOOK.COM" OR sz = "STAGING.MYFLIGHTBOOK.COM") then
                Response.Redirect("http://myflightbook.com/logbook/Default.aspx")
        end if
%>

</body>

</html>