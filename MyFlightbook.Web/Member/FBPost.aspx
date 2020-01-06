<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_FBPost" Title="Post to Facebook" Codebehind="FBPost.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbMiniFacebook.ascx" tagname="mfbMiniFacebook" tagprefix="uc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:Panel ID="pnlShareOrNot" runat="server" DefaultButton="btnShare">
    <p><asp:Localize ID="locSharePrivateFlight" runat="server" Text="<%$ Resources:LocalizedText, FacebookSharePrivateFlight %>"></asp:Localize></p>
    <asp:Literal ID="litSharingDescriptions" runat="server" Text="<%$ Resources:LocalizedText, SharingDescription %>"></asp:Literal>
        <div style="margin-left:25%; margin-right: 25%; text-align: center;">
            <asp:Button ID="btnShare" runat="server" Text="<%$ Resources:LocalizedText, FacebookSharePrivateFlightGoAhead %>" 
                onclick="btnShare_Click" /> &nbsp;&nbsp;&nbsp;&nbsp;
            <asp:Button ID="btnNoShare" runat="server" Text="<%$ Resources:LocalizedText, FacebookSharePrivateFlightKeepPrivate %>" 
                onclick="btnNoShare_Click" />
        </div>
        <uc1:mfbMiniFacebook ID="mfbMiniFacebook" runat="server" Visible="false" />
    </asp:Panel>
    <asp:Panel ID="pnlNotYours" runat="server" style="margin-left: 25%; margin-right: 25%; text-align: center;" Visible="false">
         <br />
        <asp:Label ID="lblNotYours" runat="server" Text="<%$ Resources:LocalizedText, FacebookSharePrivateFlightNotYours %>" CssClass="error"></asp:Label><br />
   </asp:Panel>
   <br />
   <div style="margin-left: 75%;">
        <a href="javascript:window.close();"><asp:Localize ID="locCloseWindow" runat="server" Text="<%$ Resources:LocalizedText, CloseWindow %>"></asp:Localize></a>
   </div>
</asp:Content>

