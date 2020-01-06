<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbTweetThis" Codebehind="mfbTweetThis.ascx.cs" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<asp:HyperLink ID="lnkTweetThis" runat="server" style="cursor:pointer;">
    <asp:Image ID="imgTweetThis" runat="server" ImageUrl="~/images/twitter20x20.png" AlternateText="<%$ Resources:LocalizedText, TweetThis %>" ToolTip="<%$ Resources:LocalizedText, TweetThis %>"  style="padding-right:4px;" />
    <asp:Label ID="lblTweetThis" runat="server" Text="<%$ Resources:LocalizedText, TweetThis %>"></asp:Label>
</asp:HyperLink>