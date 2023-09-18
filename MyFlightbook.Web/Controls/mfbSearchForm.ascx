<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSearchForm.ascx.cs" Inherits="Controls_mfbSearchForm" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="mfbLogbook.ascx" TagName="mfbLogbook" TagPrefix="uc2" %>
<%@ Register src="mfbTotalSummary.ascx" tagname="mfbTotalSummary" tagprefix="uc3" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc4" %>
<%@ Register Src="~/Controls/popmenu.ascx" TagPrefix="uc2" TagName="popmenu" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc2" TagName="mfbTooltip" %>
<asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch">
    <table>
        <tr style="vertical-align: top">
            <td>
                <p class="header" runat="server" id="DatesHeader">
                    <asp:Localize ID="locDates" runat="server" Text="<%$ Resources:FlightQuery, HeaderDates %>" /> 
                    <asp:Label ID="lblDatesLabel" runat="server" /></p>
                <asp:Panel ID="pnlDates" runat="server" style="overflow:hidden">
                    <table>
                        <tr style="vertical-align: top">
                            <td>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbAllTime" Text="<%$ Resources:FlightQuery, DatesAll %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbYTD" Text="<%$ Resources:FlightQuery, DatesYearToDate %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbPrevYear" Text="<%$ Resources:FlightQuery, DatesPrevYear %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbThisMonth" Text="<%$ Resources:FlightQuery, DatesThisMonth %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbPrevMonth" Text="<%$ Resources:FlightQuery, DatesPrevMonth %>" runat="server" /></div>
                            </td>
                            <td>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbTrailing30" Text="<%$ Resources:FlightQuery, DatesPrev30Days %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbTrailing90" Text="<%$ Resources:FlightQuery, DatesPrev90Days %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbTrailing6Months" Text="<%$ Resources:FlightQuery, DatesPrev6Month %>" runat="server" /></div>
                                <div><asp:RadioButton GroupName="DateRange" ID="rbTrailing12" Text="<%$ Resources:FlightQuery, DatesPrev12Month %>" runat="server" /></div>
                            </td>
                        </tr>
                        <tr>
                            <td colspan="2">
                                <asp:RadioButton GroupName="DateRange" ID="rbCustom" Text="<%$ Resources:FlightQuery, DatesFrom %>" runat="server" />
                                <uc4:mfbTypeInDate ID="mfbTIDateFrom" DefaultType="None" runat="server" />&nbsp;<asp:Label ID="lblDateTo" runat="server" AssociatedControlID="rbCustom" Text="<%$ Resources:FlightQuery, DatesTo %>" />
                                <uc4:mfbTypeInDate ID="mfbTIDateTo" runat="server" DefaultType="None" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <p class="header" runat="server" id="TextHeader">
                    <asp:Localize ID="locFreeformText" runat="server" Text="<%$ Resources:FlightQuery, ContainsText %>" />
                </p>
                <asp:Panel ID="pnlText" runat="server" style="overflow:hidden">
                    <asp:TextBox ID="txtRestrict" runat="server" Width="50%" /> 
                    <asp:HyperLink ID="lnkTextTips" runat="server" NavigateUrl="~/mvc/faq?q=63#63" Target="_blank" Text="<%$ Resources:FlightQuery, SearchTipsToolTip %>" />
                </asp:Panel>
                <p class="header" runat="server" id="AirportsHeader">
                    <asp:Localize ID="locAirports" runat="server" Text="<%$ Resources:FlightQuery, HeaderAirports %>" /> 
                    <asp:Label ID="lblAirportsLabel" runat="server" /></p>
                <asp:Panel ID="pnlAirports" runat="server" style="overflow:hidden" >
                        <div>
                            <asp:TextBox ID="txtAirports" runat="server" Width="50%" />
                            <uc2:mfbTooltip runat="server" ID="MfbTooltip1">
                                <TooltipBody>
                                    <% =Resources.FlightQuery.SearchTipsAirportToolTip %>
                                </TooltipBody>
                            </uc2:mfbTooltip>
                        </div>
                        <asp:RadioButtonList ID="rblFlightDistance" runat="server" RepeatDirection="Horizontal">
                            <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:FlightQuery, FlightRangeAll %>" />
                            <asp:ListItem Value="1" Text="<%$ Resources:FlightQuery, FlightRangeLocal %>" />
                            <asp:ListItem Value="2" Text="<%$ Resources:FlightQuery, FlightRangeNonLocalLong %>" />
                        </asp:RadioButtonList>
                </asp:Panel>
                <p class="header" runat="server" id="AirplanesHeader">
                    <asp:Localize ID="locAircraft" runat="server" Text="<%$ Resources:FlightQuery, HeaderAircraft %>" />
                    <asp:Label ID="lblAirplaneLabel" runat="server" /></p>
                <asp:Panel ID="pnlAirplanes" runat="server" style="overflow:hidden">
                    <asp:UpdatePanel ID="updpanelAircraft" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklAircraft" runat="server"
                                DataTextField="DisplayTailNumber" DataValueField="AircraftID" 
                                RepeatColumns="6" RepeatDirection="Horizontal">
                            </asp:CheckBoxList>
                            <asp:Panel ID="pnlShowAllAircraft" runat="server">
                                <asp:LinkButton ID="lnkShowAllAircraft" runat="server" Text="<%$ Resources:FlightQuery, FlightAircraftShowAll %>" OnClick="lnkShowAllAircraft_Click" />
                            </asp:Panel>
                            <div>
                                <asp:CheckBox ID="ckAllAircraft" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllAircraft_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" />
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="AircraftCharsHeader">
                    <asp:Localize ID="locAircraftCharacteristics" runat="server" Text="<%$ Resources:FlightQuery, HeaderAircraftFeature %>" /> 
                    <asp:Label ID="lblAircraftCharsLabel" runat="server" /></p>
                <asp:Panel ID="pnlAircraftType" runat="server" style="overflow:hidden">
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:CheckBox ID="ckTailwheel" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureTailwheel %>" /></div>
                        <div><asp:CheckBox ID="ckHighPerf" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureHighPerformance %>" /></div>
                        <div><asp:CheckBox ID="ckGlass" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureGlass %>" /></div>
                        <div><asp:CheckBox ID="ckTAA" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureTAAShort %>" />
                            <uc2:mfbTooltip runat="server" ID="mfbTooltip">
                                <TooltipBody><%= Resources.Makes.TAADefinition %></TooltipBody>
                            </uc2:mfbTooltip>
                        </div>
                        <div><asp:CheckBox ID="ckMotorGlider" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureMotorGlider %>" /></div>
                        <div><asp:CheckBox ID="ckMultiEngineHeli" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureMultiEngineHelicopter %>" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:CheckBox ID="ckComplex" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureComplex%>" /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                            <asp:CheckBox ID="ckRetract" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureRetractableGear %>" /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                            <asp:CheckBox ID="ckProp" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureConstantSpeedProp %>"  /></div>
                        <div>&nbsp;&nbsp;&nbsp;&nbsp;
                            <asp:CheckBox ID="ckCowl" runat="server" Text="<%$ Resources:FlightQuery, AircraftFeatureFlaps %>" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:RadioButton ID="rbEngineAny" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureEngineAny %>" Checked="True" runat="server" /></div>
                        <div><asp:RadioButton ID="rbEnginePiston" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeaturePiston %>" runat="server" /></div>
                        <div><asp:RadioButton ID="rbEngineTurboprop" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureTurboprop %>" runat="server" /></div>
                        <div><asp:RadioButton ID="rbEngineJet" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureJet %>" runat="server" /></div>
                        <div><asp:RadioButton ID="rbEngineTurbine" runat="server" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureTurbine %>" /></div>
                        <div><asp:RadioButton ID="rbEngineElectric" runat="server" GroupName="EngineGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureElectric %>" /></div>
                    </div>
                    <div style="display:inline-block; vertical-align:top;">
                        <div><asp:RadioButton ID="rbInstanceAny" GroupName="InstanceGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureSimOrReal %>" Checked="True" runat="server" /></div>
                        <div><asp:RadioButton ID="rbInstanceReal" GroupName="InstanceGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureReal %>" runat="server" /></div>
                        <div><asp:RadioButton ID="rbInstanceTrainingDevices" GroupName="InstanceGroup" Text="<%$ Resources:FlightQuery, AircraftFeatureTrainingDevice %>" runat="server" /></div>
                    </div>
                </asp:Panel>
                <p class="header" runat="server" id="MakesHeader">
                    <asp:Localize ID="locMakes" runat="server" Text="<%$ Resources:FlightQuery, HeaderModels %>" />
                    <asp:Label ID="lblMakesLabel" runat="server" />
                </p>
                <asp:Panel ID="pnlMakes" runat="server" Style="overflow: hidden">
                    <asp:UpdatePanel ID="updpanelMakes" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklMakes" runat="server" DataTextField="DisplayName" DataValueField="MakeModelID" RepeatColumns="3" RepeatDirection="Horizontal">
                            </asp:CheckBoxList>
                            <div><asp:CheckBox ID="ckAllMakes" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllMakes_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></div>
                            <div>
                                <asp:Label ID="lblModelContainsPrompt" runat="server" Text="<%$ Resources:FlightQuery, ContainsMakeModelText %>" />
                                <asp:TextBox ID="txtModelNameText" runat="server" />
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="CatClassHeader">
                    <asp:Localize ID="locCatClass" runat="server" Text="<%$ Resources:FlightQuery, HeaderCategoryClass %>" /> 
                    <asp:Label ID="lblCatClass" runat="server" /></p>
                <asp:Panel ID="pnlCatClass" runat="server" style="overflow:hidden">
                    <asp:UpdatePanel ID="updpanelCatClass" runat="server">
                        <ContentTemplate>
                            <asp:CheckBoxList ID="cklCatClass" DataValueField="IDCatClassAsInt" DataTextField="CatClass" runat="server" RepeatColumns="4" RepeatDirection="Horizontal">
                            </asp:CheckBoxList>
                            <div><asp:CheckBox ID="ckAllCatClass" runat="server" AutoPostBack="true" OnCheckedChanged="ckAllCatClass_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
                <p class="header" runat="server" id="FlightCharsHeader">
                    <asp:Localize ID="locFlightCharacteristics" runat="server" Text="<%$ Resources:FlightQuery, HeaderFlightCharacteristics %>" />
                    <asp:Label ID="lblFlightChars" runat="server" /></p>
                <asp:Panel ID="pnlFlightCharacteristics" runat="server" style="overflow:hidden">
                    <asp:Panel ID="pnlFlightCharsConjunction" runat="server">
                        <asp:Localize ID="locConjPromptFC1" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt1 %>" />
                        <asp:DropDownList ID="cmbFlightCharsConjunction" runat="server">
                            <asp:ListItem Selected="True" Text="<%$ Resources:FlightQuery, ConjunctionAll %>" Value="All" />
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionAny %>" Value="Any" />
                            <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionNone %>" Value="None" />
                        </asp:DropDownList>
                        <asp:Localize ID="locConjPromptFC2" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt2 %>" />
                    </asp:Panel>
                    <table>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckAnyLandings" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureAnyLandings %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckFSLanding" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureFSLanding %>"  />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckNightLandings" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureFSNightLanding %>"  />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckApproaches" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureApproaches %>"  />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckHolds" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureHolds %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckXC" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureXC %>" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckIMC" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureIMC %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckSimIMC" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureSimIMC %>"  />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckAnyInstrument" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureIMCOrSimIMC %>" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckGroundSim" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureGroundsim %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckNight" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureNight %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckDual" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureDual %>" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckCFI" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureCFI %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckSIC" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureSIC %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckPIC" runat="server" Text="<%$ Resources:FlightQuery, FlightFeaturePIC %>" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckTotal" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureTotalTime %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckPublic" runat="server" Text="<%$ Resources:FlightQuery, FlightFeaturePublic %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckHasTelemetry" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureTelemetryShort %>" />
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:CheckBox ID="ckHasImages" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureHasImages %>" />
                            </td>
                            <td>
                                <asp:CheckBox ID="ckIsSigned" runat="server" Text="<%$ Resources:FlightQuery, FlightFeatureSignedShort %>" />
                            </td>
                            <td>
                            </td>
                        </tr>
                    </table>
                    <asp:Panel ID="pnlCustomProps" runat="server" style="overflow:hidden">
                        <asp:Panel ID="pnlPropsConjunction" runat="server">
                            <asp:Localize ID="locConjPromptProps1" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt1 %>" />
                            <asp:DropDownList ID="cmbPropertiesConjunction" runat="server">
                                <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionAll %>" Value="All" />
                                <asp:ListItem Selected="True" Text="<%$ Resources:FlightQuery, ConjunctionAny %>" Value="Any" />
                                <asp:ListItem Text="<%$ Resources:FlightQuery, ConjunctionNone %>" Value="None" />
                            </asp:DropDownList>
                            <asp:Localize ID="locConjPromptProps2" runat="server" Text="<%$ Resources:FlightQuery, ConjunctionPrompt2 %>" />
                        </asp:Panel>

                        <asp:CheckBoxList ID="cklCustomProps" DataValueField="PropTypeID" 
                            DataTextField="Title" runat="server" RepeatColumns="4" 
                            RepeatDirection="Horizontal">
                        </asp:CheckBoxList>
                        <div><asp:LinkButton ID="lnkShowAllProps" OnClick="lnkShowAllProps_Click" Visible="false" runat="server" Text="<%$ Resources:LogbookEntry, SearchAllProperties %>" /></div>
                    </asp:Panel>
                </asp:Panel>
            </td>
        </tr>
        <tr>
            <td style="text-align:right; vertical-align:middle;">
                <asp:Button ID="btnReset" runat="server" Text="<%$ Resources:FlightQuery, SearchReset %>" onclick="btnReset_Click" />
                <asp:Button ID="btnSearch" runat="server" Text="<%$ Resources:FlightQuery, SearchFindNow %>" OnClick="btnSearch_Click" />
                <asp:Label ID="lblSearchPop" runat="server">
                    <a href="javascript:showModalById('<% =pnlCanned.ClientID %>','<%= Resources.FlightQuery.SaveQueryManage %>', 400);"><% =Resources.FlightQuery.SaveQueryManage %></a>
                </asp:Label>
                <asp:Panel runat="server" ID="pnlCanned" style="display:none" DefaultButton="btnSearchNamed">
                    <div><% =Resources.FlightQuery.SaveQueryNamePrompt %></div>
                    <div>
                        <asp:TextBox ID="txtQueryName" runat="server" />
                        <asp:Button ID="btnSearchNamed" runat="server" Text="<%$ Resources:FlightQuery, SaveQueryPrompt %>" OnClick="btnSearch_Click" />
                    </div>
                    <hr />
                    <asp:GridView ID="gvSavedQueries" BorderStyle="None" BorderWidth="0px" CellPadding="3" GridLines="None" ShowHeader="false" runat="server" AutoGenerateColumns="false" OnRowCommand="gvSavedQueries_RowCommand">
                        <Columns>
                            <asp:ButtonField ImageUrl="~/images/x.gif" ButtonType="Image" CommandName="_Delete" />
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:LinkButton ID="lnkLoad" CommandName="_Load" Text='<%#: Eval("QueryName") %>' CommandArgument='<%# Container.DataItemIndex %>' runat="server" />
                                </ItemTemplate>
                                </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <%# Resources.FlightQuery.SaveQueryNoSavedQueries %>
                        </EmptyDataTemplate>
                    </asp:GridView>
                    
                </asp:Panel>
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
