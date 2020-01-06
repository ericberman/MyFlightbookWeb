<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_ImgDbg" Codebehind="ImgDbg.aspx.cs" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbFileUpload.ascx" tagname="mfbFileUpload" tagprefix="uc2" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Debug image:
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <uc1:mfbMultiFileUpload ID="mfbMultiFileUpload1" runat="server" />
    <br />
    <asp:Button ID="btnTest" runat="server" Text="Debug" onclick="btnTest_Click" />
    <br />
    <asp:Label
        ID="lblDiagnose" runat="server" Text=""></asp:Label>
</asp:Content>

