<%@ Control Language="C#" AutoEventWireup="true" Codebehind="pageHeader.ascx.cs" Inherits="Controls_PrintingLayouts_pageHeader" %>
<table style="width:100%">
    <tr>
        <td style="width:33%"><asp:Label ID="lblAddress" runat="server" style="white-space: pre-wrap"><%# CurrentUser.Address %></asp:Label></td>
        <td style="width:34%; text-align: center;" colspan="7">
            <img style="max-width: 120px;" src="<% = ResolveUrl(Branding.CurrentBrand.LogoURL) %>" />
        </td>
        <td style="width:33%; text-align:right;">
            <div><%# CurrentUser.UserFullName %></div>
            <div><%=Resources.LogbookEntry.PrintHeaderLicense %> <%# CurrentUser.License %></div>
        </td>
    </tr>
</table>