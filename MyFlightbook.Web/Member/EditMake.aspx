<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Inherits="EditMake" Title="MyFlightbook: Edit aircraft makes and models" Codebehind="EditMake.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditMake.ascx" tagname="mfbEditMake" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbATDFTD.ascx" tagname="mfbATDFTD" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc4" %>
<%@ Register Src="~/Controls/AircraftControls/AircraftList.ascx" TagPrefix="uc1" TagName="AircraftList" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblEditModel" runat="server" Text="Add Model"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div class="noprint">
        <asp:MultiView ID="mvMake" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwEdit" runat="server">
                <uc2:mfbATDFTD ID="mfbATDFTD1" runat="server" />
                <uc1:mfbEditMake ID="mfbEditMake1" runat="server" OnMakeUpdated="MakeUpdated" />
                <div><asp:HyperLink ID="lnkViewAircraft" runat="server" Visible="false" Target="_blank">ADMIN - View Aircraft</asp:HyperLink>&nbsp;</div>
                <div><asp:HyperLink ID="lnkViewTotals" runat="server" Visible="false"></asp:HyperLink></div>
            </asp:View>
            <asp:View ID="vwView" runat="server">
                <div style="vertical-align:middle">
                    <asp:Label ID="lblMakeModel" runat="server" Font-Size="Larger" Font-Bold="true"></asp:Label>&nbsp;&nbsp;
                    <asp:ImageButton ID="imgEditAircraftModel" ToolTip="<%$ Resources:Makes, editModelPrompt %>" ImageUrl="~/images/pencilsm.png" runat="server" OnClick="imgEditAircraftModel_Click" />
                </div>
                <ul>
                    <asp:Repeater ID="rptAttributes" runat="server">
                        <ItemTemplate>
                            <li>
                                <asp:MultiView ID="mvAttribute" runat="server" ActiveViewIndex='<%# String.IsNullOrEmpty((string) Eval("Link")) ? 0 : 1 %>'>
                                    <asp:View ID="vwNoLink" runat="server">
                                        <%# Eval("Value") %>
                                    </asp:View>
                                    <asp:View ID="vwLink" runat="server">
                                        <asp:HyperLink ID="lnkAttrib" runat="server" Text='<%# Eval("Value") %>' NavigateUrl='<%# Eval("Link") %>'></asp:HyperLink>
                                    </asp:View>
                                </asp:MultiView>
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </asp:View>
        </asp:MultiView>
        <br />
        <asp:MultiView ID="mvInstances" runat="server">
            <asp:View ID="vwSampleImages" runat="server">
                    <uc1:mfbImageList runat="server" ID="mfbImageList" NoRequery="true" CanEdit="false" CanMakeDefault="false" IncludeDocs="false" MaxImage="16" />
            </asp:View>
            <asp:View ID="vwAircraft" runat="server">
                <uc1:AircraftList runat="server" ID="AircraftList1" OnAircraftDeleted="AircraftList1_AircraftDeleted" OnFavoriteChanged="AircraftList1_FavoriteChanged" />
            </asp:View>
        </asp:MultiView>
    </div>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpMain" runat="Server">
</asp:content>
