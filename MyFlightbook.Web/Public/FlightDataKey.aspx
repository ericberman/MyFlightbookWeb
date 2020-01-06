<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_FlightDataKey" Title="" Codebehind="FlightDataKey.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text="<%$ Resources:FlightData, FlightDataHeader %>"></asp:Label>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <% =Branding.ReBrand(Resources.FlightData.FlightDataKey) %>
    <div class="detailsSection">
        <asp:Localize ID="locImport" runat="server" Text="<%$ Resources:LogbookEntry, BulkImportPrompt %>"></asp:Localize>
        <asp:HyperLink ID="lnkBulkImport" NavigateUrl="~/Member/ImportTelemetry.aspx" runat="server" Text="<%$ Resources:LogbookEntry, BulkImportLink %>"></asp:HyperLink>
    </div>
    <asp:GridView ID="gvKnownColumns" runat="server" CellPadding="3" AutoGenerateColumns="false" GridLines="None" ShowFooter="false" ShowHeader="true">
        <Columns>
            <asp:BoundField DataField="Column" HeaderText="<%$ Resources:FlightData, headerColumnName %>" ItemStyle-VerticalAlign="Top" HeaderStyle-HorizontalAlign="Left" ItemStyle-Font-Bold="true" />
            <asp:TemplateField HeaderText="<%$ Resources:FlightData, headerColumnDescription %>">
                <ItemTemplate>
                    <div><%# Eval("ColumnDescription") %></div>
                    <div class="fineprint"><%# Eval("ColumnNotes") %></div>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
                <HeaderStyle HorizontalAlign="Left" />
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    <p> <asp:HyperLink ID="lnkTellUsMoreData" runat="server" NavigateUrl="~/Public/ContactMe.aspx" Text="<%$ Resources:FlightData, FlightDataContactUs %>"></asp:HyperLink>.</p>
</asp:Content>
