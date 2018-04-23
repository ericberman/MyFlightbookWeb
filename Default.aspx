<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Public_Home" Title="" %>
<%@ Register Src="Controls/mfbCurrency.ascx" TagName="mfbCurrency" TagPrefix="uc3" %>
<%@ Register Src="Controls/RSSCurrency.ascx" TagName="RSSCurrency" TagPrefix="uc2" %>
<%@ Register Src="Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="Controls/mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc4" %>
<%@ Register src="Controls/mfbFacebookFan.ascx" tagname="mfbFacebookFan" tagprefix="uc6" %>
<%@ Register src="Controls/mfbGoogleAdSense.ascx" tagname="mfbGoogleAdSense" tagprefix="uc5" %>
<%@ Register src="Controls/mfbSignIn.ascx" tagname="mfbSignIn" tagprefix="uc7" %>
<%@ Register src="Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc8" %>
<%@ Register src="Controls/fbComment.ascx" tagname="fbComment" tagprefix="uc9" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:LinkButton ID="lnkViewMobile" runat="server" Visible="false" OnClick="lnkViewMobile_Click">Switch to Mobile View<br /></asp:LinkButton>
    <asp:MultiView ID="mvWelcome" runat="server">
        <asp:View ID="vwWelcomeNewUser" runat="server">
            <div style="float:right; min-width:250px; margin:5px;"><uc6:mfbFacebookFan ID="mfbFacebookFan1" runat="server" ShowStream="true" /></div>
            <uc7:mfbSignIn ID="mfbSignIn1" runat="server" />
            <asp:Literal ID="litAppDesc" runat="server"></asp:Literal>
        </asp:View>
        <asp:View ID="vwWelcomeBack" runat="server">
            <div>
                <div class="EntryBlock" style="float:left;">
                    <h2><asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:LocalizedText, DefaultPageAddFlightHeader %>"></asp:Localize></h2>
                    <uc4:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="EnteredFlight" />
                </div>
                <div style="float:left">
                    <div id="divCurrency" class="EntryBlock">
                        <h2>
                            <asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:Currency, FlyingStatus %>"></asp:Literal></h2>
                        <uc3:mfbCurrency ID="MfbCurrency1" runat="server" />
                    </div>
                    <uc6:mfbFacebookFan ID="mfbFacebookFan2" runat="server" ShowStream="true" />
                </div>
            </div>
            <div style="clear:both;">&nbsp;</div>
        </asp:View>
    </asp:MultiView>
    <h2><asp:Localize ID="locRecentFlightsHeader" runat="server"></asp:Localize></h2>
        <p><asp:Label ID="lblRecentFlightsStats" Font-Bold="true" runat="server" Text="Label"></asp:Label> <asp:Label ID="lblSomeShown" runat="server" Text="<%$ Resources:LocalizedText, RecentFlightsBelow %>"></asp:Label></p>
        <uc8:mfbGoogleMapManager ID="mfbGoogleMapManagerRecentFlights" Width="600px" runat="server" /><br />
        <asp:GridView ID="gvRecentFlights" runat="server" OnRowDataBound="gvRecentFlights_AddPictures" 
        GridLines="None" Visible="true" AutoGenerateColumns="false" onpageindexchanging="OnPageIndexChanging"
        CellPadding="5" EnableViewState="false" Width="950px" PageSize="5"
        AllowPaging="true" AllowSorting="false" ShowFooter="false" 
        ShowHeader="false" >
            <HeaderStyle HorizontalAlign="Left" />
            <RowStyle CssClass="publicFlight" />
            <Columns>
                <asp:TemplateField>
                    <ItemStyle VerticalAlign="Top" />
                    <ItemTemplate>
                        <%# ((DateTime) Eval("Date")).ToShortDateString() %>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
                    <ItemStyle VerticalAlign="Top" />
                    <ItemTemplate>
                        <asp:Panel ID="pnlFlight" runat="server" style="vertical-align:middle">
                            <div><span style="font-weight:bold"><asp:Label ID="lblTail" runat="server"><%# Eval("TailNumDisplay") %></asp:Label></span> <a href="<%= ResolveUrl("~/public/ViewPublicFlight.aspx") %>/<%# Eval("FlightID") %>"><asp:Label ID="lblroute" runat="server"><%# Eval("Route") %></asp:Label></a></div>
                            <asp:Label ID="lblComments" runat="server" style="display:block"><%# Eval("Comment").ToString().Linkify() %></asp:Label>
                            <asp:PlaceHolder ID="plcAircraft" runat="server"></asp:PlaceHolder> 
                        </asp:Panel>
                        <asp:PlaceHolder ID="plcFlightPix" runat="server"></asp:PlaceHolder>
                        <div style="margin-left:50px;"><uc9:fbComment ID="fbComment1" runat="server" NumberOfPosts="1" /></div>
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    <div style="text-align:center">
        <uc5:mfbGoogleAdSense ID="mfbGoogleAdSense1" runat="server" LayoutStyle="adStyleHorizontal" />
    </div>
</asp:content>
