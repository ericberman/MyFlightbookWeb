<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="MiniRecents.aspx.cs" Inherits="Member_MiniRecents" %>

<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <uc1:mfbLogbook ID="mfbLogbook1" runat="server" MiniMode="true" />
</asp:Content>

