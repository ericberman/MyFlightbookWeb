<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Inherits="Public_ViewPublicFlight" Title="" Codebehind="ViewPublicFlight.aspx.cs" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMap" TagPrefix="uc2" %>
<%@ Register Src="../Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbFacebookFan.ascx" tagname="mfbFacebookFan" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbGoogleAdSense.ascx" tagname="mfbGoogleAdSense" tagprefix="uc4" %>
<%@ Register src="../Controls/mfbAirportServices.ascx" tagname="mfbAirportServices" tagprefix="uc5" %>
<%@ Register Src="../Controls/fbComment.ascx" TagName="fbComment" TagPrefix="uc6" %>
<%@ Register src="../Controls/mfbVideoEntry.ascx" tagname="mfbVideoEntry" tagprefix="uc7" %>
<%@ Register Src="~/Controls/mfbEditableImage.ascx" TagName="mfbEditableImage" TagPrefix="uc8" %>
<%@ Register Src="~/Controls/mfbMiniFacebook.ascx" TagPrefix="uc1" TagName="mfbMiniFacebook" %>
<%@ Register Src="~/Controls/imageSlider.ascx" TagPrefix="uc1" TagName="imageSlider" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>

<asp:Content id="Content2" contentplaceholderid="cpPageTitle" runat="Server">
    <span id="FullPageTop" runat="server">
        <asp:Label ID="lblHeader" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightHeader %>"></asp:Label>
    </span>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:Panel ID="pnlDetails" runat="server">
        <span><asp:HyperLink ID="lnkUser" runat="server"></asp:HyperLink></span> - 
        <span><asp:HyperLink ID="lnkRoute" runat="server"></asp:HyperLink></span>
        <span style="white-space:pre-line;"><asp:Label ID="lblComments" runat="server" Text=""></asp:Label></span>
        <span><asp:ImageButton ID="btnEdit" runat="server" ToolTip="<%$ Resources:LogbookEntry, PublicFlightEditThisFlight %>" ImageUrl="~/images/pencilsm.png" AlternateText="<%$ Resources:LogbookEntry, PublicFlightEditThisFlight %>" Visible="False" OnClick="btnEdit_Click" /></span>
    </asp:Panel>
    <div style="margin-left:auto;margin-right:auto; width:640px;"><uc7:mfbVideoEntry ID="mfbVideoEntry1" runat="server" CanAddVideos="false" /></div>
    <uc1:mfbImageList ID="mfbIlFlight" ImageClass="Flight" runat="server" Columns="4" CanEdit="false" MaxImage="-1" MapLinkType="ZoomOnLocalMap" AltText="" Visible="false" />
    <div>
        <div style="max-width:480px; margin-left:auto; margin-right:auto; " runat="server" id="divImages">
            <uc1:imageSlider runat="server" ID="imgsliderFlights" />
        </div>
        <uc1:mfbImageList ID="mfbIlAirplane" ImageClass="Aircraft" MaxImage="-1" CanEdit="false" Columns="2" runat="server" Visible="false" />
        <div id="divMap" runat="server">
            <div style="text-align:center;">
                <asp:HyperLink ID="lnkZoomOut" runat="server" Visible="False" Text="<%$ Resources:Airports, MapZoomOut %>">
                </asp:HyperLink>&nbsp;&nbsp;&nbsp;&nbsp;<asp:HyperLink ID="lnkShowMapOnly" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowOnlyMap %>"></asp:HyperLink>&nbsp;&nbsp;
                <asp:LinkButton ID="lnkViewKML" runat="server" Visible="false" 
                    onclick="lnkViewKML_Click" Text="<%$ Resources:LogbookEntry, PublicFlightKMLDownload %>"></asp:LinkButton>
                <div style="float:right; text-align:left;">
                    <uc1:popmenu runat="server" ID="popmenu" OffsetX="-160">
                        <MenuContent>
                            <h3><% =Resources.Airports.mapShowOptions %></h3>
                            <div><asp:CheckBox ID="ckShowPath" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowPath %>" AutoPostBack="true" OnCheckedChanged="ckShowPath_CheckedChanged" /></div>
                            <div><asp:CheckBox ID="ckShowRoute" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowRoute %>" AutoPostBack="true" OnCheckedChanged="ckShowRoute_CheckedChanged" /></div>
                            <div><asp:CheckBox ID="ckShowImages" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowImages %>" AutoPostBack="true" OnCheckedChanged="ckShowImages_CheckedChanged" /></div>
                        </MenuContent>
                    </uc1:popmenu>
                </div>
            </div>
            <div style="margin-left:auto; margin-right:auto;">
                <uc2:mfbGoogleMap ID="MfbGoogleMap1" runat="server" Width="100%" AllowResize="false" Height="400px" />
                <div style="text-align:center">
                    <asp:Panel ID="pnlDistance" runat="server" Visible="false">
                        <asp:Label ID="lblDistance" runat="server" Text=""></asp:Label>
                    </asp:Panel>
                </div>
            </div>
            <uc5:mfbAirportServices ID="mfbAirportServices1" runat="server" ShowFBO="false" ShowHotels="false" ShowInfo="false" ShowMetar="false" ShowZoom="true" />
        </div>
    </div>
    <div>
        <asp:HiddenField ID="hdnID" runat="server" Visible="False" />
        <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label>
        <div id="FullPageBottom" runat="server">
            <div style="text-align:center">
                <uc4:mfbGoogleAdSense ID="mfbGoogleAdSense1" runat="server" LayoutStyle="adStyleHorizontal" />
            </div>
        </div>
    </div>
    <div runat="server" id="pnlFB" style="margin-left:auto; margin-right:auto; width: 400px;">
        <div><uc1:mfbMiniFacebook runat="server" ID="mfbMiniFacebook" AddMetaTagHints="true" /></div>
        <div><uc6:fbComment ID="fbComment" runat="server"></uc6:fbComment></div>
    </div>
</asp:content>
