<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="PropertyTable.aspx.cs" Inherits="MyFlightbook.Web.Public.PropertyTable" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentTitle" ContentPlaceHolderID="cpPageTitle" runat="server">
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <h2>Available property types</h2>
    <asp:GridView ID="gvProps" runat="server" AutoGenerateColumns="false">
        <Columns>
            <asp:BoundField DataField="PropTypeID" HeaderText="PropTypeID" />
            <asp:BoundField DataField="Title" HeaderText="Title" />
            <asp:BoundField DataField="SortKey" HeaderText="SortKey" />
            <asp:TemplateField HeaderText="Type">
                <ItemTemplate><%# Eval("Type").ToString() %> (<%# ((int) Eval("Type")).ToString() %>)</ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Description" HeaderText="Description" />
        </Columns>
    </asp:GridView>
    <p>The Type field indicates the data type for a given property.  The value for a given property is expressed in a specific flight property as:</p>
    <table>
        <tr><td>cfpBoolean</td><td>BoolValue</td></tr>
        <tr><td>cfpDate</td><td>DateValue</td></tr>
        <tr><td>cfpDateTime</td><td>DateValue</td></tr>
        <tr><td>cfpDecimal</td><td>DecValue</td></tr>
        <tr><td>cfpCurrency</td><td>DecValue</td></tr>
        <tr><td>cfpInteger</td><td>IntValue</td></tr>
        <tr><td>cfpString</td><td>TextValue</td></tr>
    </table>
</asp:Content>