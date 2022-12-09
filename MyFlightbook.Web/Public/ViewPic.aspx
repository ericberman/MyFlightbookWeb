<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Codebehind="ViewPic.aspx.cs" Inherits="MyFlightbook.Image.ViewPic" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditableImage.ascx" tagname="mfbEditableImage" tagprefix="uc1" %>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <uc1:mfbEditableImage ID="mfbEditableImage1" runat="server" />
</asp:content>
