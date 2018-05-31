<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditFlight.ascx.cs" Inherits="Controls_mfbEditFlight" %>
<%@ Register Src="mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc2" %>
<%@ Register Src="mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbFlightInfo.ascx" tagname="mfbFlightInfo" tagprefix="uc4" %>
<%@ Register src="mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc5" %>
<%@ Register src="mfbTwitter.ascx" tagname="mfbTwitter" tagprefix="uc6" %>
<%@ Register src="mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc7" %>
<%@ Register src="mfbFileUpload.ascx" tagname="mfbFileUpload" tagprefix="uc9" %>
<%@ Register src="mfbFacebook.ascx" tagname="mfbFacebook" tagprefix="uc11" %>
<%@ Register src="mfbVideoEntry.ascx" tagname="mfbVideoEntry" tagprefix="uc12" %>
<%@ Register src="mfbEditPropSet.ascx" tagname="mfbEditPropSet" tagprefix="uc13" %>
<%@ Register src="mfbFlightProperties.ascx" tagname="mfbFlightProperties" tagprefix="uc10" %>
<%@ Register src="mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc14" %>
<asp:Panel ID="pnlContainer" runat="server" DefaultButton="btnAddFlight" 
    meta:resourcekey="pnlContainerResource1">
    <asp:Panel ID="pnlFlightInfo" runat="server" CssClass="flightinfoblock" 
        meta:resourcekey="pnlFlightInfoResource1">
        <div class="header">
            <asp:Label ID="lblSectionGeneralInfo" runat="server" Text="General Flight Info" 
                meta:resourcekey="Label6Resource1"></asp:Label>
            
        </div>
        <div class="itemtime">
            <div class="itemlabel"><asp:Label ID="Label7" runat="server" Text="Date of Flight" 
                    meta:resourcekey="Label7Resource1"></asp:Label></div>
            <div class="itemdata">
                <uc1:mfbTypeInDate ID="mfbDate" runat="server" TabIndex="1" DefaultType="Today" />
                <div>
                <asp:CustomValidator ID="valDate" runat="server" ErrorMessage="Date of flight should be today or in the past." CssClass="error"
                    onservervalidate="valDate_ServerValidate" Display="Dynamic" 
                    meta:resourcekey="valDateResource1"></asp:CustomValidator></div>
            </div>
        </div>
        <div class="itemtime">
            <div class="itemlabel"><asp:Label ID="Label8" runat="server" Text="Aircraft:" 
                    meta:resourcekey="Label8Resource1"></asp:Label>
                <asp:LinkButton ID="lnkAddAircraft" runat="server" CausesValidation="False" 
                    onclick="lnkAddAircraft_Click" Text="(Add...)" 
                    meta:resourcekey="lnkAddAircraftResource1"></asp:LinkButton>
            </div>
            <div class="itemdata">
                <asp:DropDownList ID="cmbAircraft" runat="server" TabIndex="2" Width="200px" 
                    meta:resourcekey="cmbAircraftResource1">
                </asp:DropDownList>
                <asp:RequiredFieldValidator ID="RequiredFieldValidator2" runat="server" 
                    ControlToValidate="cmbAircraft" Display="Dynamic"
                    CssClass="error" 
                    ErrorMessage='Please select an aircraft, or click "Add" to add a new one' 
                    meta:resourcekey="RequiredFieldValidator2Resource1"></asp:RequiredFieldValidator>
                <div>
                    <asp:Label ID="lblShowCatClass" runat="server" CssClass="fineprint" 
                    meta:resourcekey="lblShowCatClassResource1"></asp:Label>
                    <uc14:mfbTooltip ID="mfbTooltip1" runat="server" BodyContent="<%$ Resources:LocalizedText, EditFlightAltCatclassTooltip %>" />
                </div>
            </div>
            <asp:Panel ID="pnlAltCatClass" runat="server" CssClass="flightinfoitem" 
                Height="0px" style="overflow:hidden; padding-bottom:4px;" 
                meta:resourcekey="pnlAltCatClassResource1">
                    <asp:DropDownList ID="cmbCatClasses" TabIndex="3" runat="server" AppendDataBoundItems="true"  DataValueField="IDCatClassAsInt" 
                        DataTextField="CatClass" EnableViewState="false" 
                        meta:resourcekey="cmbCatClassesResource1">
                        <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:LocalizedText, EditFlightDefaultCatClass %>"></asp:ListItem>
                    </asp:DropDownList>
            </asp:Panel>
            <cc1:CollapsiblePanelExtender ID="cpeAltCatClass" runat="server" 
                CollapseControlID="lblShowCatClass" ExpandControlID="lblShowCatClass" CollapsedText="<%$ Resources:LocalizedText, ClickToShowAlternateCatClass %>"
             ExpandedText="<%$ Resources:LocalizedText, ClickToHideAlternateCatClass %>" 
                TargetControlID="pnlAltCatClass" TextLabelID="lblShowCatClass" Collapsed="True" 
                Enabled="True" >
            </cc1:CollapsiblePanelExtender>
        </div>
        <div class="itemtime">
            <div class="itemlabel">
                <asp:Label ID="Label9" runat="server" Text="Route:" 
                    meta:resourcekey="Label9Resource1"></asp:Label>
                <uc14:mfbTooltip ID="mfbTooltip2" runat="server" BodyContent="<%$ Resources:Airports, MapNavaidTip %>" />
            </div>
            <div class="itemdata">
                <asp:TextBox ID="txtRoute" runat="server" Font-Size="Small" TabIndex="4" 
                    Width="200px" meta:resourcekey="txtRouteResource1"></asp:TextBox>
            </div>
        </div>
        <div class="itemtime">
            <div class="itemlabel"><asp:Label ID="Label10" runat="server" Text="Comments:" 
                    meta:resourcekey="Label10Resource1"></asp:Label>
            </div>
            <div class="itemdata">
                <asp:TextBox ID="txtComments" runat="server" TextMode="MultiLine" dir="auto" Font-Names="Arial"
                    Font-Size="Small" TabIndex="5" Rows="3" Width="200px" 
                    meta:resourcekey="txtCommentsResource1"></asp:TextBox>
            </div>
        </div>
    </asp:Panel>
    <asp:Panel ID="pnlFlightTimes" runat="server" CssClass="timesblock" 
        meta:resourcekey="pnlFlightTimesResource1">
        <div class="header">
            <asp:Localize ID="locTimesHeader" runat="server" Text="Times" 
                meta:resourcekey="locTimesHeaderResource1"></asp:Localize>
        </div>
        <div id="rowTimes1">
            <div class="itemtimeleft">
                <div id="divApproaches" class="itemlabel"><asp:Label ID="Label11" runat="server" 
                        Text="Approaches" meta:resourcekey="Label11Resource1"></asp:Label>
                    <uc14:mfbTooltip ID="mfbTooltip3" runat="server" BodyContent="<%$ Resources:LocalizedText, EditFlightApproachTooltip %>" />
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="intApproaches" EditingMode="Integer" Width="40px" TabIndex="6" runat="server" />&nbsp;<asp:CheckBox 
                        ID="ckHold" runat="server" Text="Hold" TabIndex="7" 
                        meta:resourcekey="ckHoldResource1" />
                    
                </div>
            </div>
            <div class="itemtimeright">
                <div id="divLandings" class="itemlabel"><asp:Label ID="Label12" runat="server" 
                        Text="Total Landings" meta:resourcekey="Label12Resource1"></asp:Label> 
                    <asp:Label ID="lblShowLandingDetails" runat="server" CssClass="fineprint" 
                        meta:resourcekey="lblShowLandingDetailsResource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="intLandings" EditingMode="Integer" Width="40px" TabIndex="8" runat="server" />
                    <asp:CustomValidator ID="valCheckFullStop" 
                        OnServerValidate="CheckFullStopCount" runat="server" ErrorMessage="Total landings must be greater than or equal to the number of full stop landings"
                        CssClass="error" Display="Dynamic" 
                        meta:resourcekey="valCheckFullStopResource1"></asp:CustomValidator>
                    <asp:Panel ID="pnlLandingDetails" runat="server" Height="0px" style="overflow:hidden;" meta:resourcekey="pnlLandingDetailsResource1">
                        <div>
                            <div class="itemtimeleft">
                                <asp:Label ID="Label13" runat="server" Text="Full Stop (Day):" meta:resourcekey="Label13Resource1"></asp:Label>
                            </div>
                            <div class="itemdata">
                                <uc7:mfbDecimalEdit ID="intFullStopLandings" EditingMode="Integer" Width="40px" TabIndex="9" runat="server" />
                            </div>
                        </div>
                        <div>
                            <div class="itemtimeleft">
                                <asp:Label ID="Label14" runat="server" Text="Full Stop (Night):" meta:resourcekey="Label14Resource1"></asp:Label>
                            </div>
                            <div class="itemdata">
                                <uc7:mfbDecimalEdit ID="intNightLandings" EditingMode="Integer" Width="40px" TabIndex="10" runat="server" />
                            </div>
                        </div>
                    </asp:Panel>
                </div>
            </div>
            <cc1:CollapsiblePanelExtender ID="cpeLandingDetails" runat="server" CollapsedSize="0"   
                CollapseControlID="lblShowLandingDetails" 
                ExpandControlID="lblShowLandingDetails" Collapsed="True" 
                CollapsedText="<%$ Resources:LocalizedText, ClickToShowDetails %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHideDetails %>" 
                TargetControlID="pnlLandingDetails" TextLabelID="lblShowLandingDetails" 
                Enabled="True"></cc1:CollapsiblePanelExtender>                
        </div>
        <div id="rowTimes2">
            <div class="itemtimeleft">
                <div id="divXC" class="itemlabel"><asp:Label ID="Label15" runat="server" 
                        Text="Cross-Country" meta:resourcekey="Label15Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decXC" Width="40px" TabIndex="11" runat="server" />
                </div>
            </div>
            <div class="itemtimeright">
                <div id="divNight" class="itemlabel"><asp:Label ID="Label16" runat="server" 
                        Text="Night" meta:resourcekey="Label16Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decNight" Width="40px" TabIndex="12" runat="server" />
                </div>
            </div>
        </div>
        <div id="rowTimes3">
            <div class="itemtimeleft">
                <div id="divSimIFR" class="itemlabel"><asp:Label ID="Label17" runat="server" 
                        Text="Simulated Instrument" meta:resourcekey="Label17Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decSimulatedIFR" Width="40px" TabIndex="13" runat="server" />
                </div>
            </div>
            <div class="itemtimeright">
                <div id="divIMC" class="itemlabel"><asp:Label ID="Label18" runat="server" 
                        Text="IMC" meta:resourcekey="Label18Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decIMC"  Width="40px" TabIndex="14" runat="server" />
                </div>
            </div>
        </div>
        <div id="rowTimes4">
            <div class="itemtimeleft">
                <div id="divGroundSim" class="itemlabel"><asp:Label ID="Label19" runat="server" 
                        Text="Ground Sim" meta:resourcekey="Label19Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decGrndSim" Width="40px" TabIndex="15" runat="server" />
                </div>
            </div>
            <div class="itemtimeright">
                <div id="divDual" class="itemlabel"><asp:Label ID="Label20" runat="server" 
                        Text="Dual" meta:resourcekey="Label20Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decDual" Width="40px" TabIndex="16" runat="server" />
                </div>
            </div>
        </div>
        <div id="rowTimes5">
            <div class="itemtimeleft" id="divCFI" runat="server">
                <div class="itemlabel"><asp:Label ID="Label21" runat="server" 
                        Text="CFI (Instructor)" meta:resourcekey="Label21Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decCFI" Width="40px" TabIndex="17" runat="server" />
                </div>
            </div>
            <div class="itemtimeright" id="divSIC" runat="server">
                <div id="div1" class="itemlabel"><asp:Label ID="Label22" runat="server" 
                        Text="SIC (Second in Command)" meta:resourcekey="Label22Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decSIC" Width="40px" TabIndex="18" runat="server" />
                </div>
            </div>
        </div>        
        <div id="rowTimes6">
            <div class="itemtimeleft">
                <div id="divPIC" class="itemlabel"><asp:Label ID="Label23" runat="server" 
                        Text="PIC" meta:resourcekey="Label23Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decPIC" Width="40px" TabIndex="19" runat="server" />
                </div>
            </div>
            <div class="itemtimeright">
                <div id="divTotal" class="itemlabel"><asp:Label ID="Label24" runat="server" 
                        Text="Total Time" meta:resourcekey="Label24Resource1"></asp:Label>
                </div>
                <div class="itemdata">
                    <uc7:mfbDecimalEdit ID="decTotal" Width="40px" TabIndex="20" runat="server" /> 
                </div>
            </div>
        </div>
    </asp:Panel>
    <div id="rowTimes7" class="fullblock">
        <asp:MultiView ID="mvPropEdit" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwPropSet" runat="server">
                <uc13:mfbEditPropSet ID="mfbEditPropSet1" runat="server" />
            </asp:View>
            <asp:View ID="vwLegacyProps" runat="server">
                <uc10:mfbFlightProperties ID="mfbFlightProperties1" Enabled="false" runat="server" />
            </asp:View>
        </asp:MultiView>
    </div>
    <asp:Panel ID="pnlFlightDetailsContainer" runat="server" CssClass="fullblock" 
        meta:resourcekey="pnlFlightDetailsContainerResource1">
        <div runat="server" id="FlightDetailsHeader" class="header">
            <asp:Label ID="Label1" runat="server" 
                Text="Times and Telemetry" meta:resourcekey="Label1Resource1"></asp:Label>&nbsp;<asp:Label 
                ID="lblExpandCollapse" runat="server" 
                meta:resourcekey="lblExpandCollapseResource1"></asp:Label>
        </div>
        <div>
            <asp:Panel ID="pnlFlightDetails" runat="server" Height="0px" 
                style="overflow:hidden;" meta:resourcekey="pnlFlightDetailsResource1">
                <uc4:mfbFlightInfo ID="mfbFlightInfo1" runat="server" OnAutoFill="AutoFill" InitialTabIndex="21" />
            </asp:Panel>
            <cc1:CollapsiblePanelExtender ID="cpeFlightDetails" runat="server" 
                TargetControlID="pnlFlightDetails" CollapsedSize="0" ExpandControlID="FlightDetailsHeader"
             CollapseControlID="FlightDetailsHeader" Collapsed="True" 
                CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
                TextLabelID="lblExpandCollapse" Enabled="True"></cc1:CollapsiblePanelExtender>
        </div>
    </asp:Panel>
    <asp:Panel ID="pnlPictures" runat="server" CssClass="fullblock" 
        meta:resourcekey="pnlPicturesResource1">
        <div class="header"><asp:Label ID="lblPixForFlight" runat="server" 
                Text="" meta:resourcekey="lblPixForFlightResource1"></asp:Label>
        </div>
        <div>
            <uc5:mfbMultiFileUpload ID="mfbMFUFlightImages" Mode="Ajax" Class="Flight" IncludeDocs="false" RefreshOnUpload="true" runat="server"  />
            <br />
            <uc2:mfbImageList ID="mfbFlightImages" ImageClass="Flight"
                runat="server" AltText="Images from flight" CanEdit="true" Columns="4" 
                MaxImage="-1" />
        </div>
        <div>
            <uc12:mfbVideoEntry ID="mfbVideoEntry1" CanDelete="true" runat="server" />
        </div>
    </asp:Panel>
    <asp:Panel ID="pnlTwitter" runat="server" CssClass="fullblock" 
        meta:resourcekey="pnlTwitterResource1">
        <div class="header">
            <asp:Label ID="lblSharingPrompt" runat="server" Text="Sharing" 
                meta:resourcekey="lblSharingPromptResource1"></asp:Label>
        </div>
        <div>
            <table>
                <tr>
                    <td>
                        <asp:CheckBox ID="ckPublic" CssClass="itemlabel" runat="server" TabIndex="30" 
                            meta:resourcekey="ckPublicResource1" /></td>
                    <td>
                        <asp:Label ID="Label4" AssociatedControlID="ckPublic" runat="server" 
                            CssClass="itemlabel" EnableViewState="False"><asp:Image
                                ID="imgMFBPublic" runat="server" AlternateText="Share Flight Details" 
                                ImageUrl="~/images/MFBIcon20x20.png" EnableViewState="False" 
                                meta:resourcekey="imgMFBPublicResource1" />
                        </asp:Label>
                    </td>
                    <td>
                        <asp:Label ID="Label26" 
                            AssociatedControlID="ckPublic" runat="server" 
                            CssClass="itemlabel" EnableViewState="False"
                            Text="Share details such as route, comments, and pictures with others"
                            meta:resourcekey="Label26Resource1"></asp:Label>
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:CheckBox ID="ckUpdateTwitter"  CssClass="itemlabel" runat="server" 
                            TabIndex="29" meta:resourcekey="ckUpdateTwitterResource1" />
                    </td>
                    <td>
                        <asp:Label ID="Label5" AssociatedControlID="ckUpdateTwitter" runat="server" 
                            CssClass="itemlabel" EnableViewState="False">
                        <asp:Image ID="Image2" runat="server" AlternateText="Twitter" ToolTip="Twitter" 
                            ImageUrl="~/images/twitter20x20.png" EnableViewState="False" 
                            meta:resourcekey="Image2Resource1" />
                        </asp:Label>
                    </td>
                    <td>
                        <asp:MultiView ID="mvTwitter" runat="server">
                            <asp:View runat="server" ID="vwTwitterActive">
                                <asp:Label ID="lblTwitter" AssociatedControlID="ckUpdateTwitter" runat="server" 
                                    CssClass="itemlabel" EnableViewState="False" 
                                    Text="Tweet this flight on Twitter" meta:resourcekey="lblTwitterResource1"></asp:Label>
                            </asp:View>
                            <asp:View runat="server" ID="vwTwitterInactive">
                                <asp:LinkButton ID="lnkSetUpTwitter" runat="server" 
                                    onclick="lnkSetUpTwitter_Click" 
                                    Text="Set up to tweet selected flights to your feed on Twitter" 
                                    meta:resourcekey="lnkSetUpTwitterResource1"></asp:LinkButton>
                            </asp:View>
                        </asp:MultiView>
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:CheckBox ID="ckFacebook" TabIndex="30" runat="server" 
                            meta:resourcekey="ckFacebookResource1" />
                    </td>
                    <td>
                        <asp:Label ID="Label3" AssociatedControlID="ckFacebook" runat="server" 
                            CssClass="itemlabel" EnableViewState="False">
                        <asp:Image 
                            ID="Image5" runat="server" ImageUrl="~/images/facebookicon.gif"  
                            AlternateText="Facebook" ToolTip="Facebook" EnableViewState="False" 
                            meta:resourcekey="Image5Resource1" />
                        </asp:Label>
                    </td>
                    <td>
                        <asp:MultiView ID="mvFacebook" runat="server">
                            <asp:View ID="vwFacebookActive" runat="server">
                                <asp:Label ID="lblFacebook" AssociatedControlID="ckFacebook" runat="server" 
                                    CssClass="itemlabel" EnableViewState="False" 
                                    Text="Post this flight on Facebook" meta:resourcekey="lblFacebookResource1"></asp:Label>
                            </asp:View>
                            <asp:View ID="vwFacebookInactive" runat="server">
                                <asp:LinkButton ID="lnkSetUpFacebook" runat="server" 
                                    onclick="lnkSetUpFacebook_Click" 
                                    Text="Set up to post selected flights to your account on Facebook" 
                                    meta:resourcekey="lnkSetUpFacebookResource1"></asp:LinkButton>
                            </asp:View>
                        </asp:MultiView>
                    </td>
                </tr>
            </table>
            <asp:HiddenField ID="hdnItem" runat="server" Value="-1" />
            <uc6:mfbTwitter ID="mfbTwitter" runat="server" />
            <uc11:mfbFacebook ID="mfbFacebook1" runat="server" />
        </div>
    </asp:Panel>
    <asp:Panel ID="pnlSubmit" runat="server" CssClass="fullblock" 
        meta:resourcekey="pnlSubmitResource1">
        <div style="text-align:center;">
            <asp:Button ID="btnCancel" runat="server" Text="<%$ Resources:LogbookEntry, EditFlightInlineCancel %>" OnClick="btnCancel_Click" Visible="false" />&nbsp;&nbsp;
            <asp:Button ID="btnAddFlight" runat="server" Text="Add Flight" 
                OnClick="btnAddFlight_Click" TabIndex="31" 
                meta:resourcekey="btnAddFlightResource1"/>
        </div>
        <div><asp:Label ID="lblError" runat="server" CssClass="error" 
            EnableViewState="False" meta:resourcekey="lblErrorResource1"></asp:Label>
        </div>
        <div>&nbsp;</div>
    </asp:Panel>
    <asp:Panel ID="pnlAdminFixSignature" runat="server" Visible="false">
        <table>
            <tr>
                <td>Saved State:</td>
                <td><asp:Label ID="lblSigSavedState" runat="server" ></asp:Label></td>
            </tr>
            <tr>
                <td>Sanity check:</td>
                <td><asp:Label ID="lblSigSanityCheck" runat="server" ></asp:Label></td>
            </tr>
            <tr>
                <td>Saved Hash:</td>
                <td><asp:Label ID="lblSigSavedHash" runat="server" ></asp:Label></td>
            </tr>
            <tr>
                <td>Current Hash:</td>
                <td><asp:Label ID="lblSigCurrentHash" runat="server" ></asp:Label></td>
            </tr>
            <tr>
                <td><asp:Button ID="btnAdminFixSignature" runat="server" Text="Fix Signature" OnClick="btnAdminFixSignature_Click" /></td>
                <td>(Set the state to match reality)</td>
            </tr>
            <tr>
                <td><asp:Button ID="btnAdminForceValid" runat="server" Text="Force Valid" OnClick="btnAdminForceValid_Click" /></td>
                <td>(Recompute the flight hash based on current values to force it to be valid)</td>
            </tr>
        </table>
    </asp:Panel>
</asp:Panel>
