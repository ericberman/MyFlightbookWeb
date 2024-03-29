﻿<%@ Control Language="C#" AutoEventWireup="true" Codebehind="pageFooter.ascx.cs" Inherits="MyFlightbook.Printing.pageFooter" %>
<div>&nbsp;</div>
<table style="width:100%">
    <tr>
        <td style="width:33%"><asp:Label ID="lblCertification" runat="server" Text="<%$ Resources:LogbookEntry, LogbookCertification %>"></asp:Label></td>
        <td style="width:34%; text-align:center">
            <asp:PlaceHolder ID="plcMiddle" runat="server">

            </asp:PlaceHolder>
        </td>
        <td style="width:33%; text-align:right">
            <asp:Panel runat="server" ID="pnlPageCount"><% =String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, PageNum, TotalPages) %></asp:Panel>
        </td>
    </tr>
</table>
<div><asp:Label ID="lblShowModified" runat="server" Text="<%$ Resources:LogbookEntry, FlightModifiedFooter %>" />&nbsp;</div>