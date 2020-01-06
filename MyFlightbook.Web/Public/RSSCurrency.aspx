<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_RSSCurrency" Codebehind="RSSCurrency.aspx.cs" %>

<%@ Register Src="../Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc1" %>
<%@ Register Src="~/Controls/mfbTotalSummary.ascx" TagPrefix="uc1" TagName="mfbTotalSummary" %>


<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
    <link id="cssMain" href="https://myflightbook.com/logbook/Public/stylesheet.css" rel="stylesheet" type="text/css" /> 
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:MultiView ID="mvData" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwCurrency" runat="server">
                <uc1:mfbCurrency ID="mfbCurrency1" runat="server" UseInlineFormatting="true" />
            </asp:View>
            <asp:View ID="vwTotals" runat="server">
                <uc1:mfbTotalSummary runat="server" ID="mfbTotalSummary" />
            </asp:View>
        </asp:MultiView>
    </div>
    </form>
</body>
</html>
