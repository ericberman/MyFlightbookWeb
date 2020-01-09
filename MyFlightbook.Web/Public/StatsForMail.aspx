<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_StatsForMail" Codebehind="StatsForMail.aspx.cs" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="../Controls/adminStats.ascx" tagname="adminStats" tagprefix="uc1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link href="http://myflightbook.com/Logbook/Public/stylesheet.css" rel="stylesheet" type="text/css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:ScriptManager ID="ToolkitScriptManager1" runat="server"></asp:ScriptManager>
        <uc1:adminStats ID="adminStats1" runat="server" Visible="false" />
        <asp:Label ID="lblErr" runat="server" Text="You are not authorized to view this." CssClass="error" Visible="false"></asp:Label>
        <asp:Label ID="lblSuccess" runat="server" Text="Stats have been successfully sent" CssClass="success" Visible="false"></asp:Label>
    </div>
    </form>
</body>
</html>
