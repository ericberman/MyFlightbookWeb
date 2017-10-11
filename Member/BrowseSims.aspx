<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="BrowseSims.aspx.cs" Inherits="Member_BrowseSims" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locHeader" runat="server" Text="<%$ Resources:LocalizedText, SelectSimsExistingSimulators %>"></asp:Localize></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
<p><asp:Localize ID="locDescription" runat="server" Text="<%$ Resources:LocalizedText, SelectSimsHeaderText %>"></asp:Localize></p>
    <table>
    <tr>
        <td><asp:Localize ID="locSelectType" runat="server" Text="<%$ Resources:LocalizedText, SelectSimsSelectType %>"></asp:Localize></td>
        <td>
            <asp:DropDownList ID="cmbSimType" runat="server" AutoPostBack="true" AppendDataBoundItems="true" DataSourceID="sqldsInstanceTypes" DataTextField="Description" DataValueField="ID"
                onselectedindexchanged="cmbSimType_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:LocalizedText, SelectSimsAllTypes %>" Value="1"></asp:ListItem>
            </asp:DropDownList>
            <asp:SqlDataSource ID="sqldsInstanceTypes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT * FROM AircraftInstanceTypes WHERE ID > 1"></asp:SqlDataSource>
        </td>
    </tr>
    <tr>
        <td><asp:Localize ID="locSelectEmulatedMake" runat="server" Text="<%$ Resources:LocalizedText, SelectSimsSelectEmulatedMake %>"></asp:Localize></td>
        <td>
            <asp:DropDownList ID="cmbModels" runat="server" DataValueField="ModelID" 
                DataTextField="Description" AppendDataBoundItems="true" AutoPostBack="true" 
                onselectedindexchanged="cmbModels_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:LocalizedText, SelectSimsAllMakes %>" Value="-1"></asp:ListItem>
            </asp:DropDownList>
        </td>
    </tr>
    </table>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:GridView ID="gvSims" runat="server" AutoGenerateColumns="false" OnRowCommand="addAircraft"
            OnRowDataBound="gvSims_DataBound" GridLines="None" CellPadding="5" >
        <Columns>
            <asp:TemplateField HeaderText="Aircraft being emulated">
                <ItemTemplate>
                    <%# Eval("ModelCommonName") %> - <a href="EditAircraft.aspx?id=<%# Eval("AircraftID") %>" target="_blank"><%# Eval("TailNumber") %></a>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Type of Simulator/FTD/ATD">
                <ItemTemplate>
                    <%# Eval("InstanceTypeDescription") %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="">
                <ItemTemplate>
                    <asp:Button ID="btnAddThisAircraft" runat="server" CommandName="AddAircraft" Text="Use This" />
                    <asp:Label ID="lblAlreadyPresent" runat="server" Text="This is already in your aircraft list"></asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <asp:Localize ID="locNoMatchingSims" Text="<%$ Resources:LocalizedText, SelectSimsNoMatchingSimsFound %>" runat="server"></asp:Localize>
        </EmptyDataTemplate>
    </asp:GridView>
</asp:Content>

