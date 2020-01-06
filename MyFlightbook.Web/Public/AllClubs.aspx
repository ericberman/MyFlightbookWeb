<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_AllClubs" Codebehind="AllClubs.aspx.cs" %>
<%@ Register Src="~/Controls/ClubControls/ViewClub.ascx" tagname="ViewClub" tagprefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <% =Branding.ReBrand(Resources.Club.HeaderAllClubs) %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:GridView ID="gvClubs" CellPadding="5" AutoGenerateColumns="false" runat="server" GridLines="None" OnRowDataBound="gvClubs_RowDataBound" ShowHeader="False">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <uc1:ViewClub runat="server" ID="viewClub1" LinkToDetails="true" />
                    <hr />
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>

