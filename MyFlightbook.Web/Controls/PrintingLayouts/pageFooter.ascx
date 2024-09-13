<%@ Control Language="C#" AutoEventWireup="true" Codebehind="pageFooter.ascx.cs" Inherits="MyFlightbook.Printing.pageFooter" %>
<div>&nbsp;</div>
<div runat="server" id="divpagefooter">
    <div style="float: right">
        <asp:Panel runat="server" ID="pnlPageCount"><% =String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, PageNum, TotalPages) %></asp:Panel>
    </div>
    <div>
        <div><asp:Label ID="lblCertification" runat="server" Text="<%$ Resources:LogbookEntry, LogbookCertification %>" /><asp:Label ID="lblFullName" runat="server" Text="<%#: UserFullName %>" /></div>
    </div>
</div>
<div class="fineprint"><asp:PlaceHolder ID="plcMiddle" runat="server"></asp:PlaceHolder></div>
<div class="fineprint"><asp:Label ID="lblShowModified" runat="server" Text="<%$ Resources:LogbookEntry, FlightModifiedFooter %>" /></div>
