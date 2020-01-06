<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSearchForm" Codebehind="mfbSearchForm.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc2" %>
<%@ Register src="mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc4" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc2" TagName="popmenu" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>


<asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" 
    meta:resourcekey="pnlSearchResource1">
    <table>
        <tr style="vertical-align: top">
            <td>
                <p class="header" runat="server" id="DatesHeader">
                    <asp:Localize ID="locDates" runat="server" Text="Flight Dates" 
                        meta:resourcekey="locDatesResource1"></asp:Localize> 
                    <asp:Label ID="lblDatesLabel" runat="server" 
                        meta:resourcekey="lblDatesLabelResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlDates" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlDatesResource1">
                    <table>
                        <tr style="vertical-align: top">
                            <td>
                                <asp:RadioButton GroupName="DateRange" ID="rbAllTime" Text="All Time"
                                    runat="server" meta:resourcekey="rbAllTimeResource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbYTD" Text="Year-to-Date"
                                    runat="server" meta:resourcekey="rbYTDResource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbPrevYear" Text="Previous Year"
                                    runat="server" meta:resourcekey="rbPrevYearResource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbThisMonth" Text="This Month"
                                    runat="server" meta:resourcekey="rbThisMonthResource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbPrevMonth" Text="Previous Month"
                                    runat="server" meta:resourcekey="rbPrevMonthResource1" /><br />
                            </td>
                            <td>
                                <asp:RadioButton GroupName="DateRange" ID="rbTrailing30" Text="Trailing 30 days"
                                    runat="server" meta:resourcekey="rbTrailing30Resource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbTrailing90" Text="Trailing 90 days"
                                    runat="server" meta:resourcekey="rbTrailing90Resource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbTrailing6Months" Text="Trailing 6 Months"
                                    runat="server" meta:resourcekey="rbTrailing6MonthsResource1" /><br />
                                <asp:RadioButton GroupName="DateRange" ID="rbTrailing12" 
                                    Text="Trailing 12 months" runat="server" 
                                    meta:resourcekey="rbTrailing12Resource1" /><br />
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <asp:RadioButton GroupName="DateRange" ID="rbCustom" Text="From: "
                                    runat="server" meta:resourcekey="rbCustomResource1" /><uc4:mfbTypeInDate ID="mfbTIDateFrom" DefaultType="None" runat="server" />&nbsp;<asp:Label ID="lblDateTo" runat="server" Text="To:" meta:resourcekey="lblDateToResource1"></asp:Label> <uc4:mfbTypeInDate ID="mfbTIDateTo" runat="server" DefaultType="None" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <p class="header" runat="server" id="TextHeader"><asp:Localize ID="locFreeformText" 
                        runat="server" Text="Text of flight contains" 
                        meta:resourcekey="locFreeformTextResource1"></asp:Localize> 
                    <asp:Label ID="lblTextLabel" runat="server" 
                        meta:resourcekey="lblTextLabelResource1"></asp:Label>:
                </p>
                <asp:Panel ID="pnlText" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlTextResource1">
                    <asp:TextBox ID="txtRestrict" runat="server" Width="50%" 
                        meta:resourcekey="txtRestrictResource1"></asp:TextBox>
                </asp:Panel>
                <p class="header" runat="server" id="FlightCharsHeader">
                    <asp:Localize ID="locFlightCharacteristics" runat="server" 
                        Text="Flight had the following characteristics:" 
                        meta:resourcekey="locFlightCharacteristicsResource1"></asp:Localize>
                         <asp:Label ID="lblFlightChars" runat="server" 
                        meta:resourcekey="lblFlightCharsResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlFlightCharacteristics" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlFlightCharacteristicsResource1">
                    <asp:Panel ID="pnlFlightCharsConjunction" runat="server">
                        <asp:Localize ID="locConjPromptFC1" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt1 %>"></asp:Localize>
                        <asp:DropDownList ID="cmbFlightCharsConjunction" runat="server">
                            <asp:ListItem Selected="True" Text="<%$ Resources:FlightQuery, ConjunctionAll %>" Value="All"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionAny %>" Value="Any"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionNone %>" Value="None"></asp:ListItem>
                        </asp:DropDownList>
                        <asp:Localize ID="locConjPromptFC2" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt2 %>"></asp:Localize>
                    </asp:Panel>
                    <table>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckAnyLandings" runat="server" Text="Landings" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckFSLanding" runat="server" Text="Full-stop Landings (Day)" 
                                    meta:resourcekey="ckFSLandingResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckNightLandings" runat="server" 
                                    Text="Full-stop Landings (Night)" 
                                    meta:resourcekey="ckNightLandingsResource1" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckApproaches" runat="server" Text="Instrument Approaches" 
                                    meta:resourcekey="ckApproachesResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckHolds" runat="server" Text="Holding procedures" 
                                    meta:resourcekey="ckHoldsResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckXC" runat="server" Text="Cross-country time" 
                                    meta:resourcekey="ckXCResource1" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckIMC" runat="server" Text="Actual IMC time" 
                                    meta:resourcekey="ckIMCResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckSimIMC" runat="server" Text="Simulated IMC time" 
                                    meta:resourcekey="ckSimIMCResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckAnyInstrument" runat="server" Text="IMC OR Simulated Instr." />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckGroundSim" runat="server" Text="Ground Sim" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckNight" runat="server" Text="Night flight time" 
                                    meta:resourcekey="ckNightResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckDual" runat="server" Text="Dual time" 
                                    meta:resourcekey="ckDualResource1" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckCFI" runat="server" Text="CFI time" 
                                    meta:resourcekey="ckCFIResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckSIC" runat="server" Text="SIC time" 
                                    meta:resourcekey="ckSICResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckPIC" runat="server" Text="PIC time" 
                                    meta:resourcekey="ckPICResource1" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckTotal" runat="server" Text="Total Time" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckPublic" runat="server" 
                                    Text="Visible to others" 
                                    meta:resourcekey="ckPublicResource1" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckHasTelemetry" runat="server" Text="Telemetry data" 
                                    meta:resourcekey="ckHasTelemetryResource1" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckHasImages" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureHasImages %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckIsSigned" runat="server" 
                                    Text="Instructor signature" meta:resourcekey="ckIsSignedResource1" />
                            </td>
                            <td>
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <p class="header" runat="server" id="AirportsHeader"><asp:Localize ID="locAirports" 
                        runat="server" Text="Flight visited any of these airports" 
                        meta:resourcekey="locAirportsResource1"></asp:Localize> 
                    <asp:Label ID="lblAirportsLabel" runat="server" 
                        meta:resourcekey="lblAirportsLabelResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlAirports" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlAirportsResource1">
                        <asp:TextBox ID="txtAirports" runat="server" Width="50%" 
                            meta:resourcekey="txtAirportsResource1"></asp:TextBox>
                        <br />
                        <asp:RadioButtonList ID="rblFlightDistance" runat="server" 
                            RepeatDirection="Horizontal" meta:resourcekey="rblFlightDistanceResource1">
                            <asp:ListItem Selected="True" Value="0" Text="All Flights" 
                                meta:resourcekey="ListItemResource1"></asp:ListItem>
                            <asp:ListItem Value="1" Text="Local Flights Only" 
                                meta:resourcekey="ListItemResource2"></asp:ListItem>
                            <asp:ListItem Value="2" Text="Non-Local Flights" 
                                meta:resourcekey="ListItemResource3"></asp:ListItem>
                        </asp:RadioButtonList>
                </asp:Panel>
                <p class="header" runat="server" id="AirplanesHeader">
                    <asp:Localize ID="locAircraft" runat="server" 
                        Text="Flight was in one of these aircraft:" 
                        meta:resourcekey="locAircraftResource1"></asp:Localize>
                         <asp:Label ID="lblAirplaneLabel" runat="server" 
                        meta:resourcekey="lblAirplaneLabelResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlAirplanes" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlAirplanesResource1">
                    <asp:UpdatePanel ID="updpanelAircraft" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklAircraft" runat="server"
                                           DataTextField="DisplayTailNumber" DataValueField="AircraftID" 
                                RepeatColumns="6" RepeatDirection="Horizontal" 
                                meta:resourcekey="cklAircraftResource1">
                            </asp:CheckBoxList>
                            <asp:Panel ID="pnlShowAllAircraft" runat="server">
                                <asp:LinkButton ID="lnkShowAllAircraft" runat="server" Text="Show All Aircraft" meta:resourcekey="lnkShowAllAircraft1" OnClick="lnkShowAllAircraft_Click"></asp:LinkButton>
                            </asp:Panel>
                            <table>
                                <tr>
                                    <td><asp:CheckBox ID="ckAllAircraft" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllAircraft_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></td>
                                </tr>
                            </table>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="MakesHeader">
                    <asp:Localize ID="locMakes"
                        runat="server" Text="Flight was in one of these models:"
                        meta:resourcekey="locMakesResource1"></asp:Localize>
                    <asp:Label ID="lblMakesLabel" runat="server"
                        meta:resourcekey="lblMakesLabelResource1"></asp:Label>
                </p>
                <asp:Panel ID="pnlMakes" runat="server" Style="overflow: hidden"
                    meta:resourcekey="pnlMakesResource1">
                    <asp:UpdatePanel ID="updpanelMakes" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklMakes" runat="server"
                                DataTextField="DisplayName" DataValueField="MakeModelID"
                                RepeatColumns="3" RepeatDirection="Horizontal"
                                meta:resourcekey="cklMakesResource1">
                            </asp:CheckBoxList>
                            <table>
                                <tr>
                                    <td><asp:CheckBox ID="ckAllMakes" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllMakes_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></td>
                                </tr>
                            </table>
                            <div>
                                <asp:Label ID="Label1" runat="server" meta:resourcekey="Label1Resource1" Text="Model contains:"></asp:Label>
                                <asp:TextBox ID="txtModelNameText" runat="server" meta:resourcekey="txtModelNameTextResource1"></asp:TextBox>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="CatClassHeader"><asp:Localize ID="locCatClass" 
                        runat="server" Text="Flight was in one of these categories/classes:" 
                        meta:resourcekey="locCatClassResource1"></asp:Localize> 
                    <asp:Label ID="lblCatClass" runat="server" 
                        meta:resourcekey="lblCatClassResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlCatClass" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlCatClassResource1">
                    <asp:UpdatePanel ID="updpanelCatClass" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklCatClass" DataValueField="IDCatClassAsInt" 
                                DataTextField="CatClass" runat="server" RepeatColumns="4" 
                                RepeatDirection="Horizontal" meta:resourcekey="cklCatClassResource1">
                            </asp:CheckBoxList>
                            <table>
                                <tr>
                                    <td><asp:CheckBox ID="ckAllCatClass" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllCatClass_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></td>
                                </tr>
                            </table>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="AircraftCharsHeader">
                    <asp:Localize ID="locAircraftCharacteristics" runat="server" 
                        Text="Flight aircraft had these characteristics" 
                        meta:resourcekey="locAircraftCharacteristicsResource1"></asp:Localize> 
                    <asp:Label ID="lblAircraftCharsLabel" runat="server" 
                        meta:resourcekey="lblAircraftCharsLabelResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlAircraftType" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlAircraftTypeResource1">
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:CheckBox ID="ckTailwheel" runat="server" Text="Tailwheel" 
                                       meta:resourcekey="ckTailwheelResource1" /></div>
                        <div><asp:CheckBox ID="ckHighPerf" runat="server" Text="High Performance" 
                                       meta:resourcekey="ckHighPerfResource1" /></div>
                        <div><asp:CheckBox ID="ckGlass" runat="server" Text="Glass Cockpit" 
                                       meta:resourcekey="ckGlassResource1" /></div>
                        <div>
                            <asp:CheckBox ID="ckTAA" runat="server" Text="TAA" 
                                       meta:resourcekey="ckTAA1" />
                            <uc2:mfbTooltip runat="server" ID="mfbTooltip">
                                <TooltipBody><%=Resources.Makes.TAADefinition %></TooltipBody>
                            </uc2:mfbTooltip>
                        </div>
                        <div><asp:CheckBox ID="ckMotorGlider" runat="server" 
                                       Text="<%$ Resources:FlightQuery, AircraftFeatureMotorGlider %>" 
                                       meta:resourcekey="ckMotorGliderResource1" /></div>
                        <div><asp:CheckBox ID="ckMultiEngineHeli" runat="server" 
                                       Text="<%$ Resources:FlightQuery, AircraftFeatureMultiEngineHelicopter %>" meta:resourcekey="ckMultiEngineHeliResource1" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:CheckBox ID="ckComplex" runat="server" 
                                       Text="Complex" meta:resourcekey="ckComplexResource1" /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                                   <asp:CheckBox ID="ckRetract" runat="server" Text="Retractable gear" 
                                       meta:resourcekey="ckRetractResource1" /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                                    <asp:CheckBox ID="ckProp" runat="server" Text="Constant speed prop" 
                                       meta:resourceKey="ckPropResource2" /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                                   <asp:CheckBox ID="ckCowl" runat="server" Text="Flaps" 
                                       meta:resourcekey="ckCowlResource1" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:RadioButton ID="rbEngineAny" GroupName="EngineGroup" Text="Any Engine" 
                                       Checked="True" runat="server" meta:resourcekey="rbEngineAnyResource1" /></div>
                        <div><asp:RadioButton ID="rbEnginePiston" GroupName="EngineGroup" Text="Piston" 
                                       runat="server" meta:resourcekey="rbEnginePistonResource1" /></div>
                        <div><asp:RadioButton ID="rbEngineTurboprop" GroupName="EngineGroup" 
                                       Text="TurboProp" runat="server" meta:resourcekey="rbEngineTurbopropResource1" /></div>
                        <div><asp:RadioButton ID="rbEngineJet" GroupName="EngineGroup" Text="Jet" 
                                       runat="server" meta:resourcekey="rbEngineJetResource1" /></div>
                        <div><asp:RadioButton ID="rbEngineTurbine" runat="server" GroupName="EngineGroup" 
                                       Text="Turbine (Any)" meta:resourcekey="rbEngineTurbineResource1" /></div>
                        <div><asp:RadioButton ID="rbEngineElectric" runat="server" GroupName="EngineGroup" 
                                       Text="Electric" meta:resourcekey="rbEngineElectricResource1" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:RadioButton ID="rbInstanceAny" GroupName="InstanceGroup" 
                                       Text="All aircraft" Checked="True" runat="server" 
                                       meta:resourcekey="rbInstanceAnyResource1" /></div>
                        <div><asp:RadioButton ID="rbInstanceReal" GroupName="InstanceGroup" 
                                       Text="Real Aircraft" runat="server" 
                                       meta:resourcekey="rbInstanceRealResource1" /></div>
                        <div><asp:RadioButton ID="rbInstanceTrainingDevices" GroupName="InstanceGroup" 
                                       Text="Training Device (FTD/ATD/FFS)" runat="server" 
                                       meta:resourcekey="rbInstanceTrainingDevicesResource1" /></div>
                    </div>
                </asp:Panel>
                <p class="header" runat="server" id="CustomPropsHeader"><asp:Localize ID="locProps" 
                        runat="server" Text="Flight had these properties:" 
                        meta:resourcekey="locPropsResource1"></asp:Localize> 
                    <asp:Label ID="lblCustomPropsLabel" runat="server" 
                        meta:resourcekey="lblCustomPropsLabelResource1"></asp:Label>:</p>
                <asp:Panel ID="pnlCustomProps" runat="server" style="overflow:hidden" 
                    meta:resourcekey="pnlCustomPropsResource1">
                    <asp:Panel ID="pnlPropsConjunction" runat="server">
                        <asp:Localize ID="locConjPromptProps1" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt1 %>"></asp:Localize>
                        <asp:DropDownList ID="cmbPropertiesConjunction" runat="server">
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionAll %>" Value="All"></asp:ListItem>
                            <asp:ListItem Selected="True" Text="<%$ Resources:FlightQuery, ConjunctionAny %>" Value="Any"></asp:ListItem>
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionNone %>" Value="None"></asp:ListItem>
                        </asp:DropDownList>
                        <asp:Localize ID="locConjPromptProps2" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt2 %>"></asp:Localize>
                    </asp:Panel>

                    <asp:CheckBoxList ID="cklCustomProps" DataValueField="PropTypeID" 
                        DataTextField="Title" runat="server" RepeatColumns="4" 
                        RepeatDirection="Horizontal" meta:resourcekey="cklCustomPropsResource1">
                    </asp:CheckBoxList>
                </asp:Panel>
            </td>
        </tr>
        <tr>
            <td style="text-align:right; vertical-align:middle;">
                <asp:Button ID="btnReset" runat="server" Text="Reset" 
                    onclick="btnReset_Click" meta:resourcekey="btnResetResource1" />
                <asp:Button ID="btnSearch" runat="server" Text="Find Matching Flights" 
                    OnClick="btnSearch_Click" meta:resourcekey="btnGetTotalsResource1" />
                <div style="vertical-align:text-bottom; display:inline-block;">
                    <uc2:popmenu runat="server" ID="popmenu">
                        <MenuContent>
                            <div style="padding:4px; text-align:left;">
                                <div><% =Resources.FlightQuery.SaveQueryNamePrompt %></div>
                                <asp:TextBox ID="txtQueryName" runat="server"></asp:TextBox>
                                <asp:GridView ID="gvSavedQueries" BorderStyle="None" BorderWidth="0px" CellPadding="3" GridLines="None" ShowHeader="false" runat="server" AutoGenerateColumns="false" OnRowCommand="gvSavedQueries_RowCommand">
                                    <Columns>
                                        <asp:ButtonField ImageUrl="~/images/x.gif" ButtonType="Image" CommandName="_Delete" />
                                        <asp:ButtonField ButtonType="Link" CommandName="_Load" DataTextField="QueryName" />
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <%# Resources.FlightQuery.SaveQueryNoSavedQueries %>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </div>
                        </MenuContent>
                    </uc2:popmenu>
                </div>
            </td>
        </tr>
    </table>
</asp:Panel>
<cc1:CollapsiblePanelExtender ID="cpeDates" runat="server" TargetControlID="pnlDates"
    CollapsedSize="0" ExpandControlID="DatesHeader" 
    CollapseControlID="DatesHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblDatesLabel" BehaviorID="ctl00_cpeDates">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeText" runat="server" TargetControlID="pnlText"
    CollapsedSize="0" ExpandControlID="TextHeader" 
    CollapseControlID="TextHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblTextLabel" BehaviorID="ctl00_cpeText">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeAirplanes" runat="server" TargetControlID="pnlAirplanes"
    CollapsedSize="0" ExpandControlID="AirplanesHeader" 
    CollapseControlID="AirplanesHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblAirplaneLabel" BehaviorID="ctl00_cpeAirplanes">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeAirports" runat="server" TargetControlID="pnlAirports"
    CollapsedSize="0" ExpandControlID="AirportsHeader" 
    CollapseControlID="AirportsHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblAirportsLabel" BehaviorID="ctl00_cpeAirports">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeMakes" runat="server" TargetControlID="pnlMakes"
    CollapsedSize="0" ExpandControlID="MakesHeader" 
    CollapseControlID="MakesHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblMakesLabel" BehaviorID="ctl00_cpeMakes">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeAircraftChars" runat="server" TargetControlID="pnlAircraftType"
    CollapsedSize="0" ExpandControlID="AircraftCharsHeader" 
    CollapseControlID="AircraftCharsHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblAircraftCharsLabel" BehaviorID="ctl00_cpeAircraftChars">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeFlightCharacteristics" runat="server" TargetControlID="pnlFlightCharacteristics"
    CollapsedSize="0" ExpandControlID="FlightCharsHeader" 
    CollapseControlID="FlightCharsHeader" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblFlightChars" BehaviorID="ctl00_cpeFlightCharacteristics">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeCatClass" runat="server" TargetControlID="pnlCatClass"
    CollapsedSize="0" ExpandControlID="CatClassHeader" CollapseControlID="CatClassHeader"
    Collapsed="True" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblCatClass" BehaviorID="ctl00_cpeCatClass">
</cc1:CollapsiblePanelExtender>
<cc1:CollapsiblePanelExtender ID="cpeCustomProps" runat="server" TargetControlID="pnlCustomProps"
    CollapsedSize="0" ExpandControlID="CustomPropsHeader" CollapseControlID="CustomPropsHeader"
    Collapsed="True" 
    CollapsedText="<%$ Resources:LocalizedText, ClickToShow %>" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>"
    TextLabelID="lblCustomPropsLabel" BehaviorID="ctl00_cpeCustomProps">
</cc1:CollapsiblePanelExtender>