<%@ Page Language="C#" AutoEventWireup="true" Inherits="Public_Export" Codebehind="Export.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbDownload.ascx" tagname="mfbDownload" tagprefix="uc1" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
</head>
<body>
    <form id="form1" runat="server">
        <uc1:mfbDownload ID="mfbDownload1" runat="server" ShowLogbookData="true"  OfferPDFDownload="false" />
        <br />
    </form>
</body>
</html>
