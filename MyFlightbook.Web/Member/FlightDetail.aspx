<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="FlightDetail.aspx.cs" Inherits="MyFlightbook.MemberPages.FlightDetail" culture="auto" Async="true" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbGoogleMapManager.ascx" TagName="mfbGoogleMapManager"
    TagPrefix="uc1" %>
<%@ Register Src="~/Controls/mfbAirportServices.ascx" TagPrefix="uc5" TagName="mfbAirportServices" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc1" TagName="mfbImageList" %>
<%@ Register Src="~/Controls/mfbHoverImageList.ascx" TagPrefix="uc1" TagName="mfbHoverImageList" %>
<%@ Register Src="~/Controls/mfbSignature.ascx" TagPrefix="uc1" TagName="mfbSignature" %>
<%@ Register Src="~/Controls/mfbQueryDescriptor.ascx" TagPrefix="uc1" TagName="mfbQueryDescriptor" %>
<%@ Register Src="~/Controls/METAR.ascx" TagPrefix="uc1" TagName="METAR" %>
<%@ Register Src="~/Controls/mfbEditableImage.ascx" TagPrefix="uc1" TagName="mfbEditableImage" %>
<%@ Register Src="~/Controls/mfbVideoEntry.ascx" TagPrefix="uc1" TagName="mfbVideoEntry" %>
<%@ Register Src="~/Controls/mfbBadgeSet.ascx" TagPrefix="uc1" TagName="mfbBadgeSet" %>
<%@ Register Src="~/Controls/mfbFlightContextMenu.ascx" TagPrefix="uc1" TagName="mfbFlightContextMenu" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc1" TagName="popmenu" %>
<%@ Register Src="~/Controls/mfbSendFlight.ascx" TagPrefix="uc1" TagName="mfbSendFlight" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/GoogleChart.ascx" TagPrefix="uc1" TagName="GoogleChart" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locPageHeader" runat="server" Text="<%$ Resources:LogbookEntry, FlightDetailsHeader %>" />
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <script type="text/javascript">
        function dropPin(p, s) {
            var gm = getMfbMap();
            gm.oms.addMarker(gm.addEventMarker(p, s));
        }

        function onAccordionPaneShown(idx) {
            $("#" + "<% =hdnSelectedTab.ClientID %>").val(idx);
        }

        var fChartLoaded = false;
        var fRawDataLoaded = false;

        var proxyControl = null;
        $(() => {
            proxyControl = new accordionProxy($("#accordionproxycontainer"), {
                defaultPane: "<% =SelectedTabProxy %>",
                proxies: [
                    { idButton: "apcFlight", idTarget: "targetDetailsFlight", onclick: function () { onAccordionPaneShown("<% =DetailsTab.Flight.ToString() %>"); } },
                    { idButton: "apcAircraft", idTarget: "targetDetailsAircraft", onclick: function () { onAccordionPaneShown("<% =DetailsTab.Aircraft.ToString() %>"); } },
                    {
                        idButton: "<% =apcChart.ClientID %>", idTarget: "targetDetailsChart", onclick: function () {
                            onAccordionPaneShown("<% =DetailsTab.Chart.ToString() %>");
                            if (!fChartLoaded) {
                                var params = new Object();
                                params.idFlight = <% =CurrentFlightID %>;
                                $.ajax({
                                    url: "<% =ResolveUrl("~/mvc/flights/GetTelemetryAnalysisForUser") %>",
                                    type: "POST", data: JSON.stringify(params), dataType: "html", contentType: 'application/json',
                                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                                    complete: function () { },
                                    success: function (r) {
                                        fChartLoaded = true;
                                        $("#analysisContainer").html(r);
                                        chartDataChanged();
                                    }
                                });
                            }
                        }
                    },
                    {
                        idButton: "<% =apcRaw.ClientID %>", idTarget: "targetDetailsRaw", onclick: function () {
                            onAccordionPaneShown("<% =DetailsTab.Data.ToString() %>");
                            if (!fRawDataLoaded) {
                                var params = new Object();
                                params.idFlight = <% =CurrentFlightID %>;
                                $.ajax({
                                    url: '<% =ResolveUrl("~/mvc/flights/RawTelemetryAsTable") %>',
                                    type: "POST", data: JSON.stringify(params), dataType: "html", contentType: "application/json",
                                    error: function (xhr, status, error) { window.alert(xhr.responseText); },
                                    complete: function (response) { },
                                    success: function (response) {
                                        $("#divRawData").html(response);
                                    }
                                });
                            }
                        }
                    },
                    { idButton: "<% =apcDownload.ClientID %>", idTarget: "targetDetailsDownload", onclick: function () { onAccordionPaneShown("<% =DetailsTab.Download.ToString() %>"); } }
                ]
            });
        });
    </script>
    <asp:HiddenField ID="hdnSelectedTab" runat="server" />
    <div>
        <asp:MultiView ID="mvReturn" runat="server" ActiveViewIndex="0">
            <asp:View ID="vwReturnOwner" runat="server">
                <asp:HyperLink ID="lnkListView" runat="server" Text="<%$ Resources:LogbookEntry, flightDetailsReturnToLogbook %>" NavigateUrl="~/Member/LogbookNew.aspx" />
            </asp:View>
            <asp:View ID="vwReturnStudent" runat="server">
                <asp:Image ID="ib" ImageAlign="AbsMiddle" ImageUrl="~/images/back.png" runat="server" /><asp:HyperLink ID="lnkReturnStudent" runat="server"></asp:HyperLink>
            </asp:View>
        </asp:MultiView>
    </div>
    <p>
        <asp:Localize ID="locOverview" runat="server"
            Text="<%$ Resources:LogbookEntry, FlightDetailsDesc %>"></asp:Localize>
        <asp:HyperLink ID="lnkKey" NavigateUrl="~/mvc/pub/FlightDataKey"
            runat="server" Target="_blank" Text="<%$ Resources:LogbookEntry, FlightDetailsLearnAboutTelemetry %>" />
    </p>
    <asp:Panel runat="server" ID="pnlFlightDesc" CssClass="detailsHeaderBar shadowed">
        <asp:HiddenField ID="hdnNextID" runat="server" />
        <asp:HiddenField ID="hdnPrevID" runat="server" />
        <table style="width:100%">
            <tr style="vertical-align:middle">
                <td><asp:LinkButton ID="lnkPreviousFlight" runat="server" Font-Names="Arial" Text="<%$ Resources:LogbookEntry, PreviousFlight %>" Font-Size="20pt" OnClick="lnkPreviousFlight_Click" /></td>
                <td>            
                    <p>
                        <asp:Label ID="lblFlightDate" Font-Bold="True" Font-Size="Larger" runat="server" />
                        <asp:Label ID="lblFlightAircraft" runat="server" />
                        <asp:Label ID="lblCatClass" runat="server" />
                        <asp:Label ID="lblRoute" Font-Bold="true" runat="server" />
                        <span style="white-space:pre-line" runat="server" dir="auto"><asp:Literal ID="litDesc" runat="server" /></span>
                    </p>
                    <div style="text-align:left">
                        <uc1:mfbTooltip ID="mfbTTCatClass" runat="server" BodyContent="<%$ Resources:LogbookEntry, LogbookAltCatClassTooltip %>" HoverControlID="lblCatClass" />
                    </div>
                    <asp:Panel ID="pnlFilter" runat="server" Visible="false" >
                        <div style="display:inline-block;"><%=Resources.LocalizedText.ResultsFiltered %></div>
                            <uc1:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
                    </asp:Panel>
                </td>
                <td><asp:LinkButton ID="lnkNextFlight" runat="server" Font-Names="Arial" Text="<%$ Resources:LogbookEntry, NextFlight %>" Font-Size="20pt" OnClick="lnkNextFlight_Click" /></td>
            </tr>
        </table>
    </asp:Panel>
    <div>
        <asp:Label ID="lblPageErr" runat="server" CssClass="error" EnableViewState="False" /></div>
    <div id="accordionproxycontainer">
        <div id="apcFlight"><% =Resources.Tabs.AnalysisFlight%></div>
        <div id="apcAircraft"><% =Resources.Tabs.AnalysisAircraft%></div>
        <div id="apcChart" runat="server"><% =Resources.Tabs.AnalysisChart %></div>
        <div id="apcRaw" runat="server"><% =Resources.Tabs.AnalysisRaw %></div>
        <div id="apcDownload" runat="server"><% =Resources.Tabs.AnalysisDownload %></div>
        <asp:Panel ID="pnlAccordionMenuContainer" style="display: inline-block;" runat="server">
            <uc1:popmenu runat="server" ID="popmenu" Visible='<%# ((String) Eval("User")).CompareCurrentCultureIgnoreCase(Page.User.Identity.Name) == 0 %>' OffsetX="-160">
                <MenuContent>
                    <div style="text-align:left">
                        <uc1:mfbFlightContextMenu runat="server" ID="mfbFlightContextMenu"
                            EditTargetFormatString="~/Member/LogbookNew.aspx/{0}"
                            SignTargetFormatString="~/Member/RequestSigs.aspx?id={0}"
                            OnDeleteFlight="mfbFlightContextMenu_DeleteFlight" 
                            />
                    </div>
                </MenuContent>
            </uc1:popmenu>
        </asp:Panel>
    </div>
    <uc1:mfbSendFlight runat="server" ID="mfbSendFlight" />
    <div id="targetDetailsFlight" style="display:none;">
        <asp:FormView ID="fmvLE" runat="server" OnDataBound="fmvLE_DataBound" Width="100%">
            <ItemTemplate>
                <div class="detailsSection">
                    <table>
                        <tr>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldApproaches %></td>
                            <td style="padding: 3px; min-width: 1cm"><%# Eval("Approaches").FormatInt() %></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldHold %></td>
                            <td style="padding: 3px; min-width: 1cm"><%# Eval("fHoldingProcedures").FormatBoolean() %></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldLanding %></td>
                            <td style="padding: 3px; min-width: 1cm"><%# Eval("Landings").FormatInt() %></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldDayLandings %></td>
                            <td style="padding: 3px; min-width: 1cm"><%# Eval("FullStopLandings").FormatInt() %></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldNightLandings %></td>
                            <td style="padding: 3px; min-width: 1cm"><%# Eval("NightLandings").FormatInt() %></td>
                        </tr>
                        <tr>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldXCountry %></td>
                            <td style="padding: 3px"><%# Eval("CrossCountry").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldNight %></td>
                            <td style="padding: 3px"><%# Eval("Nighttime").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.Totals.SimIMC %></td>
                            <td style="padding: 3px"><%# Eval("SimulatedIFR").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.LogbookEntry.FieldIMC %></td>
                            <td style="padding: 3px"><%# Eval("IMC").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="padding: 3px; font-weight: bold"><% = Resources.Totals.Ground %></td>
                            <td style="padding: 3px"><%# Eval("GroundSim").FormatDecimal(Viewer.UsesHHMM)%></td>
                        </tr>
                        <tr>
                            <td style="font-weight: bold"><% = Resources.LogbookEntry.FieldDual %></td>
                            <td><%# Eval("Dual").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="font-weight: bold"><% = Resources.LogbookEntry.FieldCFI %></td>
                            <td><%# Eval("CFI").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="font-weight: bold"><% = Resources.LogbookEntry.FieldSIC %></td>
                            <td><%# Eval("SIC").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="font-weight: bold"><% = Resources.LogbookEntry.FieldPIC %></td>
                            <td><%# Eval("PIC").FormatDecimal(Viewer.UsesHHMM)%></td>
                            <td style="font-weight: bold"><% = Resources.LogbookEntry.FieldTotal %></td>
                            <td><%# Eval("TotalFlightTime").FormatDecimal(Viewer.UsesHHMM)%></td>
                        </tr>
                    </table>
                    <asp:Repeater ID="rptProps" runat="server" DataSource='<%# Eval("PropertiesWithReplacedApproaches") %>'>
                        <ItemTemplate>
                            <div><%# Container.DataItem %></div>
                        </ItemTemplate>
                    </asp:Repeater>
                    <div><%# Eval("EngineTimeDisplay") %></div>
                    <div><%# Eval("FlightTimeDisplay") %></div>
                    <div><%# Eval("HobbsDisplay") %></div>
                </div>
                <asp:Panel ID="pnlSignature" CssClass="detailsSection" runat="server" Visible='<%# ((LogbookEntry.SignatureState) Eval("CFISignatureState")) != LogbookEntry.SignatureState.None %>'>
                    <uc1:mfbSignature runat="server" ID="mfbSignature" />
                </asp:Panel>
                <asp:Panel ID="pnlRoute" runat="server" Visible="<%# RoutesList(CurrentFlight.Route).MasterList.GetNormalizedAirports().Length > 0 %>" CssClass="detailsSection">
                    <h3><%#: Eval("Route").ToString().ToUpper() %></h3>
                    <uc5:mfbAirportServices runat="server" ID="mfbAirportServices1" ShowZoom="true" ShowInfo="true" ShowMetar="true" />
                    <p><asp:Label ID="lblDistanceForFlight" runat="server" /></p>
                    <asp:Panel ID="pnlMetars" runat="server">
                        <asp:UpdatePanel ID="updp1" runat="server">
                            <ContentTemplate>
                                <uc1:METAR runat="server" ID="METARDisplay" />
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </asp:Panel>
                </asp:Panel>
                <div style="text-align:center">
                    <uc1:mfbImageList ID="mfbilFlight" runat="server" Columns="2" MapLinkType="ZoomOnLocalMap" CanEdit="false" MaxImage="-1" ImageClass="Flight" IncludeDocs="false" />
                </div>
                <div><uc1:mfbVideoEntry runat="server" ID="mfbVideoEntry1" CanAddVideos="false" /></div>
                <asp:Repeater ID="rptBadges" runat="server">
                    <ItemTemplate>
                        <div><uc1:mfbBadgeSet runat="server" ID="mfbBadgeSet" BadgeSet='<%# Container.DataItem %>' /></div>
                    </ItemTemplate>
                </asp:Repeater>
            </ItemTemplate>
        </asp:FormView>
    </div>
    <div id="targetDetailsAircraft" style="display:none;">
        <asp:FormView ID="fmvAircraft" runat="server" Width="100%" OnDataBound="fmvAircraft_DataBound">
            <ItemTemplate>
                <table>
                    <tr>
                        <td style="padding: 3px">
                            <uc1:mfbHoverImageList runat="server" ID="mfbHoverImageList" ImageListKey='<%# Eval("AircraftID") %>'
                                ImageListAltText='<%# Eval("TailNumber") %>' MaxWidth="150px" ImageListDefaultImage='<%# Eval("DefaultImage") %>'
                                ImageListDefaultLink='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", Eval("AircraftID")) %>' ImageClass="Aircraft" CssClass="activeRow" />
                        </td>
                        <td style="padding: 3px">
                            <asp:Panel ID="pnlAircraftID" runat="server">
                                <div>
                                    <asp:HyperLink ID="lnkEditAircraft" Font-Size="Larger" Font-Bold="True" runat="server" NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}", Eval("AircraftID")) %>' Text='<%# Eval("DisplayTailNumber") %>' />
                                    - <%#: Eval("ModelDescription")%> - <%#: Eval("ModelCommonName")%> (<%# Eval("CategoryClassDisplay") %>)
                                </div>
                                <div><%# Eval("InstanceTypeDescription")%></div>
                            </asp:Panel>
                            <asp:Panel ID="pnlAircraftDetails" runat="server" CssClass="activeRow">
                                <div style="white-space: pre"><%# Eval("PublicNotes").ToString().Linkify() %></div>
                                <div style="white-space: pre"><%# Eval("PrivateNotes").ToString().Linkify() %></div>
                                <asp:Panel ID="pnlAttributes" runat="server">
                                    <ul>
                                        <asp:Repeater ID="rptAttributes" runat="server" DataSource='<%# MakeModel.GetModel((int) Eval("ModelID")).AttributeList() %>'>
                                            <ItemTemplate>
                                                <li><%# Container.DataItem %></li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ul>
                                </asp:Panel>
                            </asp:Panel>
                        </td>
                    </tr>
                </table>
            </ItemTemplate>
        </asp:FormView>
    </div>
    <div id="targetDetailsChart" style="display: none;">
        <uc1:GoogleChart runat="server" ID="GoogleChart" Visible="false" />
        <div id="analysisContainer">
            <div style="text-align: center;"><asp:Image runat="server" ID="imgAnalysisInProgress" ImageUrl="~/images/progress.gif" /></div>
        </div>
    </div>
    <div id="targetDetailsRaw" style="display:none;">
        <asp:Panel ID="Panel1" runat="server"
            Style="width: 90%; text-align: center; margin-left: 5%; margin-right: 5%; background-color: #DDDDDD; border: solid 1px gray;"
            Height="400px" ScrollBars="Auto">
            <div id="divRawData">
                <asp:Image ID="Image1" runat="server" ImageUrl="~/images/progress.gif" />
            </div>
        </asp:Panel>
    </div>
    <div id="targetDetailsDownload" style="display:none;">
        <table style="margin-left:auto; margin-right:auto;">
            <tr>
                <td><asp:Label ID="lblOriginalFormatPrompt" runat="server" Text="<%$ Resources:FlightData, FlightDataOriginalFormat %>"></asp:Label></td>
                <td>
                    <asp:Label ID="lblOriginalFormat" runat="server"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Localize ID="locDownloadPrompt" runat="server" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadAsPrompt %>" />
                </td>
                <td>
                    <asp:DropDownList ID="cmbFormat" runat="server">
                        <asp:ListItem Selected="True" Value="Original" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadOriginal %>" />
                        <asp:ListItem Value="CSV" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadCSV %>" />
                        <asp:ListItem Enabled="False" Value="KML" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadKML %>" />
                        <asp:ListItem Enabled="False" Value="GPX" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadGPX %>" />
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Localize ID="locSpeedUnitsPrompt" runat="server" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitsPrompt %>" />
                </td>
                <td>
                    <asp:DropDownList ID="cmbSpeedUnits" runat="server">
                        <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitKts %>" />
                        <asp:ListItem Value="1" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitMPH %>" />
                        <asp:ListItem Value="4" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitKmH %>" />
                        <asp:ListItem Value="2" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitMPS %>" />
                        <asp:ListItem Value="3" Text="<%$ Resources:LogbookEntry, flightDetailsSpeedUnitFPS %>" />
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Localize ID="locAltUnitsPrompt" runat="server" Text="<%$ Resources:LogbookEntry, flightDetailsAltUnitsPrompt %>" />
                </td>
                <td>
                    <asp:DropDownList ID="cmbAltUnits" runat="server">
                        <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:LogbookEntry, flightDetailsAltUnitFeet %>" />
                        <asp:ListItem Value="1" Text="<%$ Resources:LogbookEntry, flightDetailsAltUnitMeters %>" />
                    </asp:DropDownList>
                </td>
            </tr>
            <tr ><td>&nbsp;</td><td></td></tr>
            <tr>
                <td></td>
                <td>
                    <div>
                        <asp:Button ID="btnDownload" runat="server" CausesValidation="False" OnClick="btnDownload_Click" Text="<%$ Resources:LogbookEntry, flightDetailsDownloadButton %>" />
                        &nbsp;&nbsp;&nbsp;
                        <asp:LinkButton ID="lnkSendCloudAhoy" runat="server" OnClick="lnkSendCloudAhoy_Click">
                            <asp:Image ID="imgClopudAhoy" style="padding-right: 10px" ImageUrl="~/images/cloudahoy-sm.png" AlternateText="<%$ Resources:LogbookEntry, SendToCloudAhoy %>" ToolTip="<%$ Resources:LogbookEntry, SendToCloudAhoy %>" runat="server" />
                            <asp:Label ID="lblSendCloudAhoy" runat="server" Text="<%$ Resources:LogbookEntry, SendToCloudAhoy %>"></asp:Label>
                        </asp:LinkButton>
                    </div>
                    <div><asp:Label ID="lblCloudAhoyErr" runat="server" CssClass="error" EnableViewState="false"></asp:Label></div>
                    <asp:Panel ID="pnlCloudAhoySuccess" runat="server" EnableViewState="false" Visible="false">
                        <asp:Label ID="lblSendCloudAhoySuccess" runat="server" Text="<%$ Resources:LogbookEntry, SendToCloudAhoySuccess %>" CssClass="success" />
                        <ul class="nextStep">
                            <li><asp:HyperLink ID="lnkViewOnCloudAhoy" runat="server" NavigateUrl="https://www.cloudahoy.com/flights" Target="_blank" Text="<%$ Resources:LogbookEntry, SendToCloudAhoyView %>" /></li>
                        </ul>
                    </asp:Panel>
                </td>
            </tr>
        </table>
    </div>
    <asp:Panel ID="pnlErrors" runat="server" Visible="False" CssClass="callout"
        Style="width: 90%; margin-left: 5%; margin-right: 5%; text-align: center;">
        <span style="text-align: center; font-weight: bold">
            <asp:Localize ID="locErrorsFound" runat="server" Text="<%$ Resources:LogbookEntry, flightDetailsErrorsFound %>" />
            <asp:Label ID="lblShowerrors" CssClass="error" runat="server" />
        </span>
        <asp:Panel ID="pnlErrorDetail" runat="server" Style="height: 0px; overflow: hidden">
            <asp:Label ID="lblErr" runat="server" />
            <ajaxToolkit:CollapsiblePanelExtender
                ID="CollapsiblePanelExtender1" runat="server" Collapsed="True" TargetControlID="pnlErrorDetail"
                CollapseControlID="lblShowerrors" ExpandControlID="lblShowerrors"
                CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
                TextLabelID="lblShowerrors" BehaviorID="CollapsiblePanelExtender1"></ajaxToolkit:CollapsiblePanelExtender>
        </asp:Panel>
    </asp:Panel>
    <asp:Panel runat="server" ID="pnlMap" Style="width: 80%; margin-left: 10%; margin-right: 10%;">
        <asp:Panel ID="pnlMapControls" runat="server">
        <asp:HyperLink ID="lnkZoomToFit" runat="server"
            Text="<%$ Resources:LogbookEntry, flightDetailsZoomToFit %>" />&nbsp;|&nbsp;
            <asp:LinkButton ID="lnkClearEvents" runat="server" OnClick="lnkClearEvents_Click" Text="<%$ Resources:LogbookEntry, flightDetailsClearMarkers %>" />
        </asp:Panel>
        <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" AllowResize="false" Height="600px" />
    </asp:Panel>
    <asp:HiddenField ID="hdnFlightID" runat="server" />
</asp:Content>
