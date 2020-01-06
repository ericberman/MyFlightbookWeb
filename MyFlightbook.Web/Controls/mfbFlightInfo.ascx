<%@ Control Language="C#" AutoEventWireup="true"
    Inherits="Controls_mfbFlightInfo" Codebehind="mfbFlightInfo.ascx.cs" %>
<%@ Register Src="mfbDateTime.ascx" TagName="mfbDateTime" TagPrefix="uc2" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc3" %>
<%@ Register src="mfbTimeZone.ascx" tagname="mfbTimeZone" tagprefix="uc1" %>
<%@ Register src="popmenu.ascx" tagname="popmenu" tagprefix="uc4" %>
<asp:Panel ID="pnlFlightInfo" runat="server">
    <asp:Panel ID="pnlHobbs" runat="server" CssClass="flightinfoitem">
        <table>
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblHobbs" runat="server" Text="<%$ Resources:LogbookEntry, Hobbs %>" Font-Bold="True"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblHobbsStart" runat="server" Text="<%$ Resources:LogbookEntry, ShortStart %>"></asp:Label>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="decHobbsStart" runat="server" Width="70" />
                    <asp:Image ID="imgHighWater" onclick="javascript:onHobbsAutofill();" runat="server" ImageUrl="~/images/cross-fill.png" ToolTip="<%$ Resources:LogbookEntry, HobbsCrossfillTip %>" AlternateText="<%$ Resources:LogbookEntry, HobbsCrossfillTip %>" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblHobbsEnd" runat="server" Text="<%$ Resources:LogbookEntry, ShortEnd %>"></asp:Label>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="decHobbsEnd" runat="server" Width="70" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="pnlEngine" runat="server" CssClass="flightinfoitem">
        <table style="vertical-align:baseline">
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblEngine" runat="server" Text="<%$ Resources:LogbookEntry, FieldEngineUTC %>" Font-Bold="True"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEngineStart" runat="server" Text="<%$ Resources:LogbookEntry, ShortStart %>"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbEngineStart" runat="server" />&nbsp;&nbsp;&nbsp;
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEngineEnd" runat="server" Text="<%$ Resources:LogbookEntry, ShortEnd %>"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbEngineEnd" runat="server" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="pnlFlight" runat="server" CssClass="flightinfoitem">
        <table>
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblFlight" runat="server" Text="<%$ Resources:LogbookEntry, FieldFlightUTC %>" Font-Bold="True"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblFlightStart" runat="server" Text="<%$ Resources:LogbookEntry, ShortStart %>"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbFlightStart" runat="server" />&nbsp;&nbsp;&nbsp;
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblFlightEnd" runat="server" Text="<%$ Resources:LogbookEntry, ShortEnd %>"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbFlightEnd" runat="server" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <div style="clear:both;"></div>
    <asp:Panel ID="pnlFlightData" runat="server" CssClass="flightinfoitem">
        <table>
            <tr class="itemlabel">
                <td>
                    <asp:Label ID="lblTelemetryData" Font-Bold="True" runat="server" Text="<%$ Resources:LogbookEntry, FieldTelemetry %>"></asp:Label>
                    <asp:HyperLink ID="lnkLearnMore" CssClass="fineprint" runat="server" 
                        NavigateUrl="~/Public/FlightDataKey.aspx" Target="_blank" Text="<%$ Resources:LogbookEntry, FieldTelemetryLearnMore %>"></asp:HyperLink>
                </td>
                <td>
                    <asp:MultiView ID="mvData" runat="server">
                        <asp:View ID="vwNoData" runat="server">
                            <asp:FileUpload ID="mfbUploadFlightData" runat="server" />
                        </asp:View>
                        <asp:View ID="vwData" runat="server">
                            <asp:HyperLink ID="lnkFlightData" Target="_blank" runat="server">
                                <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Clip.png" 
                                    ToolTip="<%$ Resources:LogbookEntry, FlightHasDataTooltip %>" /> 
                            </asp:HyperLink>
                            <asp:Label ID="lblFlightHasData" runat="server" 
                                Text="<%$ Resources:LogbookEntry, FlightHasData %>"></asp:Label>
                                <asp:LinkButton ID="lnkUploadNewData" runat="server" 
                                onclick="lnkUploadNewData_Click" Text="<%$ Resources:LogbookEntry, TelemetryAttachNew %>"></asp:LinkButton>&nbsp;&nbsp;
                                <asp:LinkButton ID="lnkDeletedata" runat="server" CausesValidation="False"
                                onclick="lnkDeletedata_Click" Text="<%$ Resources:LogbookEntry, TelemetryDelete %>"></asp:LinkButton>
                                <cc1:ConfirmButtonExtender
                                    ID="ConfirmButtonExtender1" runat="server" 
                                ConfirmText="<%$ Resources:LocalizedText, FlightInfoConfirmDelete %>" 
                                TargetControlID="lnkDeleteData" Enabled="True">
                                </cc1:ConfirmButtonExtender>
                        </asp:View>
                    </asp:MultiView>
                </td>
            </tr>
        </table>
    </asp:Panel>
    <div style="clear:both;"></div>
    <asp:Panel ID="pnlAutoFill" runat="server" CssClass="flightinfoitem">
        <table>
            <tr class="itemlabel">
                <td style="max-width: 600px">
                    <asp:Label ID="lblAutoFill" Font-Bold="True" runat="server" Text="<%$ Resources:LogbookEntry, AutoFillPrompt %>"></asp:Label><br />
                    <asp:Label ID="lblAutoFillDesc" runat="server" Text="<%$ Resources:LogbookEntry, AutoFillDescription %>"></asp:Label>
                </td>
                <td><asp:Button ID="btnAutoFill" runat="server" onclick="onAutofill" 
                        Text="<%$ Resources:LogbookEntry, AutoFill %>"></asp:Button></td>
                <td>
                    <uc4:popmenu ID="popmenu1" runat="server">
                        <MenuContent>
                            <div>
                                <div style="float:left; padding:3px">
                                    <table>
                                        <tr>
                                            <td colspan="2">
                                                <div><asp:Label ID="lblTOSpeed" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionTakeoffSpeed %>" /></div>
                                                <asp:RadioButtonList ID="rblTakeOffSpeed" RepeatColumns="2" runat="server"></asp:RadioButtonList>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><asp:CheckBox ID="ckIncludeHeliports" runat="server" /></td>
                                            <td>
                                                <div>
                                                    <asp:Label ID="lblIncludeHeliports" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionIncludeHeliports %>" AssociatedControlID="ckIncludeHeliports"></asp:Label>
                                                    <asp:Label ID="lblIncludeHeliportsTip" runat="server" Text="[?]" CssClass="hint"></asp:Label>
                                                    <cc1:HoverMenuExtender ID="hmeHoverHeliports" runat="server" OffsetX="-180" OffsetY="10" TargetControlID="lblIncludeHeliportsTip" PopupControlID="pnlIncludeHeliportsTip"></cc1:HoverMenuExtender>
                                                    <asp:Panel ID="pnlIncludeHeliportsTip" runat="server" style="max-width:250px; min-width:140px" CssClass="hintPopup">
                                                        <%=Resources.LocalizedText.AutofillOptionsIncludeHeliportsTip %>
                                                    </asp:Panel>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><asp:CheckBox ID="ckEstimateNight" runat="server" /></td>
                                            <td>
                                                <div>
                                                    <asp:Label ID="lblEstimateNight" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionEstimateNight %>" AssociatedControlID="ckEstimateNight"></asp:Label>
                                                    <asp:Label ID="lblEstimateNightTip" runat="server" Text="[?]" CssClass="hint"></asp:Label>
                                                    <cc1:HoverMenuExtender ID="hmeHoverEstimateNight" runat="server" OffsetX="-180" OffsetY="10" TargetControlID="lblEstimateNightTip" PopupControlID="pnlEstimateNightTip"></cc1:HoverMenuExtender>
                                                    <asp:Panel ID="pnlEstimateNightTip" runat="server" style="max-width:250px; min-width:140px" CssClass="hintPopup">
                                                        <%=Resources.LocalizedText.AutofillOptionEstimateNightTip %>
                                                    </asp:Panel>
                                                </div>
                                            </td>
                                        </tr>
                                        <tr>
                                            <td><asp:CheckBox ID="ckRoundNearest10th" runat="server" /></td>
                                            <td>
                                                <asp:Label ID="lblRoundNearest10th" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionRound10th %>" AssociatedControlID="ckRoundNearest10th"></asp:Label>
                                            </td>
                                        </tr>
                                    </table>
                                </div>
                                <div style="float:left; padding:3px; margin-left: 3px; border-left: 1px solid black">
                                    <div><%=Resources.LocalizedText.AutoFillOptionNightDefinition %></div>
                                    <asp:RadioButtonList ID="rblNightCriteria" runat="server">
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightDefinitionTwilight %>" Value="EndOfCivilTwilight" Selected="True"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightDefinitionSunset %>" Value="Sunset"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightDefinitionSunsetPlus15 %>" Value="SunsetPlus15"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightDefinitionSunsetPlus30 %>" Value="SunsetPlus30"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightDefinitionSunsetPlus60 %>" Value="SunsetPlus60"></asp:ListItem>
                                    </asp:RadioButtonList>
                                    <div><%=Resources.LocalizedText.AutoFillOptionNightLandingDefinition %></div>
                                    <asp:RadioButtonList ID="rblNightLandingCriteria" runat="server">
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightLandingDefinitionSunsetPlus1Hour %>" Value="SunsetPlus60" Selected="True"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:LocalizedText, AutoFillOptionNightLandingDefinitionNight %>" Value="Night"></asp:ListItem>
                                    </asp:RadioButtonList>
                                </div>
                            </div>
                        </MenuContent>
                    </uc4:popmenu>
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:HiddenField ID="hdnFlightID" Value="-1" runat="server" />
    <uc1:mfbTimeZone ID="mfbTimeZone1" runat="server" />
</asp:Panel>
<script>
    addLoadEvent(function () {
        document.getElementById('<% =imgHighWater.ClientID %>').style.display = (currentlySelectedAircraft) ? "inline-block" : "none";
    });

    function onHobbsAutofill() {
        if (!currentlySelectedAircraft)
            return;

        var id = currentlySelectedAircraft();

        if (id === null || id === '')
            return;

        var params = new Object();
        params.idAircraft = id;
        var d = JSON.stringify(params);
        $.ajax(
        {
            url: '<% =ResolveUrl("~/Member/LogbookNew.aspx/HighWaterMarkHobbsForAircraft") %>',
            type: "POST", data: d, dataType: "json", contentType: "application/json",
            error: function (xhr, status, error) {
                window.alert(xhr.responseJSON.Message);
                if (onError !== null)
                    onError();
            },
            complete: function (response) { },
            success: function (response) {
                $find('<% =decHobbsStart.EditBoxWE.ClientID %>').set_text(response.d);
            }
        });
    }
</script>
