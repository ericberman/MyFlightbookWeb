<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSearchbox.ascx.cs" Inherits="Controls_mfbSearchbox" %>
<asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" style="padding-top:3px;">
    <div class="searchBox">
        <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Search.png" style="vertical-align:middle" GenerateEmptyAlternateText="true" Height="20px" />
        <asp:TextBox ID="txtSearch" runat="server" Width="120px" Font-Size="8pt" BorderStyle="None" CssClass="noselect" style="vertical-align:middle" />
    </div>
    <asp:Button ID="btnSearch" style="display:none" runat="server" Text="<%$ Resources:LocalizedText, SearchBoxGo %>" CausesValidation="false" onclick="btnSearch_Click" Font-Size="9px" CssClass="itemlabel" />
</asp:Panel>