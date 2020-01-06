<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_AllMakes" Codebehind="AllMakes.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbMakeListItem.ascx" tagname="mfbMakeListItem" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc2" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:MultiView ID="mvHeader" runat="server">
        <asp:View ID="vwManHeader" runat="server">Aircraft Manufacturers</asp:View>
        <asp:View ID="vwModelsHeader" runat="server">Models</asp:View>
        <asp:View ID="vwAircraftHeader" runat="server"><asp:Label ID="lblModel" runat="server" Text=""></asp:Label></asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvLevelToShow" runat="server">
        <asp:View ID="vwManufacturers" runat="server">
            <asp:GridView ID="gvManufacturers" runat="server" EnableViewState="false" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" ShowHeader="false">
                <Columns>
                    <asp:HyperLinkField DataTextField="ManufacturerName" DataNavigateUrlFields="ManufacturerID" DataNavigateUrlFormatString="AllMakes.aspx/{0}" />
                </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwModels" runat="server">
            <asp:GridView ID="gvMakes" GridLines="None" CellPadding="10" runat="server" EnableViewState="False"
                AutoGenerateColumns="False" OnRowDataBound="MakesRowDataBound" 
                    ShowHeader="False" AllowPaging="false"
                    Width="100%">
                    <Columns>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource1">
                            <ItemTemplate>
                                <uc1:mfbMakeListItem ID="mfbMakeListItem1" runat="server" SuppressImages="true" />
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                    </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwAircraft" runat="server">
            <ul>
                <asp:Repeater ID="rptAttributes" runat="server">
                    <ItemTemplate>
                        <li><%# Container.DataItem %></li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
            <asp:GridView ID="gvAircraft" runat="server" AutoGenerateColumns="False" EnableViewState="false"
                OnRowDataBound="AddPictures" GridLines="None"
                HeaderStyle-HorizontalAlign="left" CellSpacing="0" CellPadding="5">
                <Columns>
                    <asp:TemplateField HeaderText="">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <div>
                                <asp:Label ID="lblTail" Font-Bold="true" runat="server" Text='<%# Eval("DisplayTailNumber") %>'></asp:Label>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <div>
                                <uc1:mfbImageList runat="server" ID="mfbAircraftImages" ImageClass="Aircraft" MaxImage="3" Columns="3" CanEdit="false" />
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <AlternatingRowStyle BackColor="#E0E0E0" />
            </asp:GridView>
        </asp:View>
    </asp:MultiView>
</asp:Content>

