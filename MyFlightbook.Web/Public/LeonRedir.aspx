<%@ Page Language="C#" AutoEventWireup="true" Codebehind="LeonRedir.aspx.cs" MasterPageFile="~/MasterPage.master" Async="true" Inherits="MyFlightbook.OAuth.Leon.LeonRedir" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblPageHeader" runat="server" Text="<%$ Resources:LogbookEntry, LeonImportHeader %>" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvLeonState" runat="server">
        <asp:View ID="vwUnauthenticated" runat="server">
            <asp:Label ID="lblUnauth" runat="server" Text="<%$ Resources:LogbookEntry, LeonMustBeSignedIn %>" />
        </asp:View>
        <asp:View ID="vwNoAuthToken" runat="server">
            <p><asp:Label ID="lblNoAuthToken" runat="server" /></p>
            <div><asp:LinkButton ID="lnkAuthorize" runat="server" OnClick="lnkAuthorize_Click" /></div>
        </asp:View>
        <asp:View ID="vwAuthorized" runat="server">
            <div><asp:Label ID="lblAuthedHeader" runat="server" /></div>
            <ul>
                <li><asp:HyperLink ID="lnkImport" runat="server" Text="<%$ Resources:LogbookEntry, LeonGoToImport %>" NavigateUrl="~/Member/Import.aspx" /></li>
                <li><asp:LinkButton ID="btnDeAuth" runat="server" OnClick="btnDeAuth_Click" /></li>
            </ul>
        </asp:View>
    </asp:MultiView>
</asp:Content>
