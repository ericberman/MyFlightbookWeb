<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="AllMakes.aspx.cs" Inherits="Public_AllMakes" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbMakeListItem.ascx" tagname="mfbMakeListItem" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc2" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:MultiView ID="mvLevelToShow" runat="server">
        <asp:View ID="vwManufacturers" runat="server">
            <h1>Aircraft Manufacturers</h1>
            <asp:GridView ID="gvManufacturers" runat="server" EnableViewState="false" AutoGenerateColumns="false" GridLines="None" AllowPaging="false" ShowHeader="false">
                <Columns>
                    <asp:HyperLinkField DataTextField="ManufacturerName" DataNavigateUrlFields="ManufacturerID" DataNavigateUrlFormatString="AllMakes.aspx/{0}" />
                </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwModels" runat="server">
            <h1>Models</h1>
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
            <h1>Aircraft</h1>
            <asp:GridView ID="gvAircraft" runat="server" AutoGenerateColumns="False" EnableViewState="false"
                OnRowDataBound="AddPictures" GridLines="None"
                HeaderStyle-HorizontalAlign="left" CellSpacing="0" CellPadding="5">
                <Columns>
                    <asp:TemplateField HeaderText="">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <div>
                                <asp:Label ID="lblTail" Font-Bold="true" runat="server" Text='<%# Eval("DisplayTailNumber") %>'></asp:Label>
                                - <%# Eval("ModelDescription")%> - <%# Eval("ModelCommonName")%> (<asp:Label ID="lblCatClass" runat="server" Text=""></asp:Label>) <%# Eval("InstanceTypeDescription")%> 
                                <asp:Label ID="lblAircraftErr" runat="server" CssClass="error" EnableViewState="false" Text="" Visible="false"></asp:Label>
                                <div style="float:right">
                                    <asp:FormView ID="fvModelCaps" runat="server">
                                        <ItemTemplate>
                                            <asp:Panel ID="pnlAttributes" runat="server">
                                                <ul>
                                                <asp:Repeater ID="rptAttributes" runat="server" DataSource='<%# MakeModel.GetModel(Convert.ToInt32(Eval("MakeModelID"))).AttributeList() %>'>
                                                    <ItemTemplate>
                                                        <li><%# Container.DataItem %></li>
                                                    </ItemTemplate>
                                                </asp:Repeater>
                                                </ul>
                                            </asp:Panel>
                                        </ItemTemplate>
                                    </asp:FormView>
                                </div>
                            </div>
                            <div>
                                <asp:PlaceHolder ID="plcImages" runat="server"></asp:PlaceHolder>
                            </div>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <AlternatingRowStyle BackColor="#E0E0E0" />
            </asp:GridView>
        </asp:View>
    </asp:MultiView>
</asp:Content>

