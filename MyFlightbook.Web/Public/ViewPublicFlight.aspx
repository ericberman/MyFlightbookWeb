<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Codebehind="ViewPublicFlight.aspx.cs" Inherits="Public_ViewPublicFlight" Title="" %>
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
    <asp:Label ID="lblHeader" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightHeader %>"></asp:Label>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <div style="float:right; text-align:left;">
        <uc1:popmenu runat="server" ID="popmenu" OffsetX="-160">
            <MenuContent>
                <div runat="server" id="rowKML" visible="false" style="margin-top: 3px;">&nbsp;<asp:Image ID="imgDwn" ImageAlign="AbsMiddle" runat="server" ImageUrl="~/images/download.png" />&nbsp;&nbsp;<asp:LinkButton ID="lnkViewKML" runat="server" onclick="lnkViewKML_Click" Text="<%$ Resources:LogbookEntry, PublicFlightKMLDownload %>" style="vertical-align:middle" /></div>
                <h3><% =Resources.LogbookEntry.PublicFlightShowOptions %></h3>
                <div><asp:CheckBox ID="ckShowDetails" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowComponentDetails %>" AutoPostBack="true" Checked="true" OnCheckedChanged="ckShowDetails_CheckedChanged" /></div>
                <div><asp:CheckBox ID="ckShowPictures" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowComponentPictures %>" AutoPostBack="true" Checked="true" OnCheckedChanged="ckShowPictures_CheckedChanged" /></div>
                <div><asp:CheckBox ID="ckShowVids" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowComponentVideos %>" AutoPostBack="true" Checked="true" OnCheckedChanged="ckShowVids_CheckedChanged" /></div>
                <div><asp:CheckBox ID="ckShowMaps" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowComponentMap %>" AutoPostBack="true" Checked="true" OnCheckedChanged="ckShowMaps_CheckedChanged" /></div>
                <div>&nbsp;&nbsp;&nbsp;<asp:CheckBox ID="ckShowAirports" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightShowComponentAirports %>" AutoPostBack="true" Checked="true" OnCheckedChanged="ckShowAirports_CheckedChanged1" /></div>
                <div>&nbsp;&nbsp;&nbsp;<asp:CheckBox ID="ckShowPath" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowPath %>" AutoPostBack="true" OnCheckedChanged="ckShowPath_CheckedChanged" /></div>
                <div>&nbsp;&nbsp;&nbsp;<asp:CheckBox ID="ckShowRoute" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowRoute %>" AutoPostBack="true" OnCheckedChanged="ckShowRoute_CheckedChanged" /></div>
                <div>&nbsp;&nbsp;&nbsp;<asp:CheckBox ID="ckShowImages" runat="server" Checked="true" Text="<%$ Resources:Airports, mapShowImages %>" AutoPostBack="true" OnCheckedChanged="ckShowImages_CheckedChanged" /></div>
            </MenuContent>
        </uc1:popmenu>
    </div>
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
                <asp:HyperLink ID="lnkZoomOut" runat="server" Visible="False" Text="<%$ Resources:Airports, MapZoomOut %>" />
            </div>
            <div style="margin-left:auto; margin-right:auto;">
                <uc2:mfbGoogleMap ID="MfbGoogleMap1" runat="server" Width="100%" AllowResize="false" Height="400px" />
                <asp:Panel ID="pnlDistance" runat="server" Visible="false" style="text-align:center">
                    <asp:Label ID="lblDistance" runat="server" Text=""></asp:Label>
                </asp:Panel>
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
