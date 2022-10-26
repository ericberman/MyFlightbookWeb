<%@ Control Language="C#" AutoEventWireup="true" Codebehind="pageHeader.ascx.cs" Inherits="Controls_PrintingLayouts_pageHeader" %>
<table style="width:100%; page-break-before:always;">
    <tr>
        <td class="printHeaderLeft"><asp:Label ID="lblAddress" runat="server"><%#: CurrentUser.Address %></asp:Label></td>
        <td class="printHeaderCenter" colspan="7">
            <img style="max-width: 120px;" src="<% = Branding.CurrentBrand.LogoHRef.ToAbsoluteURL(Request) %>" />
        </td>
        <td class="printHeaderRight">
            <div><%#: CurrentUser.UserFullName %></div>
            <div><%#: CurrentUser.LicenseDisplay %></div>
        </td>
    </tr>
</table>