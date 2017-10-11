<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Shunt.aspx.cs" Inherits="Shunt" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>MyFlightbook Down for Maintenance</title>
    <link href="Public/stylesheet.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <a href="Default.aspx"><img src="Public/myflightbooknew.png" alt="MyFlightbook Logo" /></a>
        <h1>Scheduled Maintenance Underway</h1>
        <p><asp:label ID="lblShuntMsg" runat="server" text=""></asp:label></p>
    </div>
    </form>
</body>
</html>

