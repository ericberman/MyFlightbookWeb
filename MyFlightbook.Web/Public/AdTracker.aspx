<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_AdTracker" Codebehind="AdTracker.aspx.cs" %>

<%@ Register src="../Controls/GoogleAnalytics.ascx" tagname="GoogleAnalytics" tagprefix="uc1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Image ID="imgWorking" ImageUrl="~/images/ajax-loader.gif" runat="server" />
        <uc1:GoogleAnalytics ID="GoogleAnalytics1" runat="server" />
    </div>
    </form>
</body>
</html>
