<%@ Page Language="C#" Title="" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="login" Codebehind="login.aspx.cs" %>
<%@ Register Src="../Controls/mfbSignIn.ascx" TagName="mfbSignIn" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="contentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locHeader" runat="server"></asp:Localize>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <div style="padding: 10px; max-width:700px; margin-left:auto; margin-right: auto;">
        <uc1:mfbSignIn ID="mfbSignIn1" runat="server" />
    </div>
</asp:content>
