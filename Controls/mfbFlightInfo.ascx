<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbFlightInfo.ascx.cs"
    Inherits="Controls_mfbFlightInfo" %>
<%@ Register Src="mfbDateTime.ascx" TagName="mfbDateTime" TagPrefix="uc2" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc3" %>
<%@ Register src="mfbTimeZone.ascx" tagname="mfbTimeZone" tagprefix="uc1" %>
<%@ Register src="popmenu.ascx" tagname="popmenu" tagprefix="uc4" %>
<asp:Panel ID="pnlFlightInfo" runat="server" 
    meta:resourcekey="pnlFlightInfoResource1">
    <asp:Panel ID="pnlHobbs" runat="server" CssClass="flightinfoitem" 
        meta:resourcekey="pnlHobbsResource1">
        <table>
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblHobbs" runat="server" Text="Hobbs" Font-Bold="True" 
                        meta:resourcekey="lblHobbsResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblHobbsStart" runat="server" Text="Start:" 
                        meta:resourcekey="lblHobbsStartResource1"></asp:Label>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="decHobbsStart" runat="server" Width="70" />
                    &nbsp;&nbsp;&nbsp;
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblHobbsEnd" runat="server" Text="End:" 
                        meta:resourcekey="lblHobbsEndResource1"></asp:Label>
                </td>
                <td>
                    <uc3:mfbDecimalEdit ID="decHobbsEnd" runat="server" Width="70" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="pnlEngine" runat="server" CssClass="flightinfoitem" 
        meta:resourcekey="pnlEngineResource1">
        <table style="vertical-align:baseline">
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblEngine" runat="server" Text="Engine (UTC)" Font-Bold="True" 
                        meta:resourcekey="lblEngineResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEngineStart" runat="server" Text="Start:" 
                        meta:resourcekey="lblEngineStartResource1"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbEngineStart" runat="server" />&nbsp;&nbsp;&nbsp;
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblEngineEnd" runat="server" Text="End:" 
                        meta:resourcekey="lblEngineEndResource1"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbEngineEnd" runat="server" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:Panel ID="pnlFlight" runat="server" CssClass="flightinfoitem" 
        meta:resourcekey="pnlFlightResource1">
        <table>
            <tr class="itemlabel">
                <td colspan="2">
                    <asp:Label ID="lblFlight" runat="server" Text="Flight (UTC)" Font-Bold="True" 
                        meta:resourcekey="lblFlightResource1"></asp:Label>
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblFlightStart" runat="server" Text="Start:" 
                        meta:resourcekey="lblFlightStartResource1"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbFlightStart" runat="server" />&nbsp;&nbsp;&nbsp;
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Label ID="lblFlightEnd" runat="server" Text="End:" 
                        meta:resourcekey="lblFlightEndResource1"></asp:Label>
                </td>
                <td>
                    <uc2:mfbDateTime ID="mfbFlightEnd" runat="server" />
                </td>
            </tr>
        </table>
    </asp:Panel>
    <div style="clear:both;"></div>
    <asp:Panel ID="pnlFlightData" runat="server" CssClass="flightinfoitem" 
        meta:resourcekey="pnlFlightDataResource1">
        <table>
            <tr class="itemlabel">
                <td>
                    <asp:Label ID="lblTelemetryData" Font-Bold="True" runat="server" 
                        Text="Flight telemetry data" meta:resourcekey="lblTelemetryDataResource1"></asp:Label>
                    <asp:HyperLink ID="lnkLearnMore" CssClass="fineprint" runat="server" 
                        NavigateUrl="~/Public/FlightDataKey.aspx" Target="_blank" Text="Learn More" 
                        meta:resourcekey="lnkLearnMoreResource1"></asp:HyperLink>
                </td>
                <td>
                    <asp:MultiView ID="mvData" runat="server">
                        <asp:View ID="vwNoData" runat="server">
                            <asp:FileUpload ID="mfbUploadFlightData" runat="server" 
                                meta:resourcekey="mfbUploadFlightDataResource1" />
                        </asp:View>
                        <asp:View ID="vwData" runat="server">
                            <asp:HyperLink ID="lnkFlightData" Target="_blank" runat="server">
                                <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Clip.png" 
                                    ToolTip="Flight has associated data" meta:resourcekey="Image1Resource1" /> 
                            </asp:HyperLink>
                            <asp:Label ID="lblFlightHasData" runat="server" 
                                Text="This flight has attached data." 
                                meta:resourcekey="lblFlightHasDataResource1"></asp:Label>
                                <asp:LinkButton ID="lnkUploadNewData" runat="server" 
                                onclick="lnkUploadNewData_Click" Text="Attach new data" 
                                meta:resourcekey="lnkUploadNewDataResource1"></asp:LinkButton>&nbsp;&nbsp;
                                <asp:LinkButton ID="lnkDeletedata" runat="server" CausesValidation="False"
                                onclick="lnkDeletedata_Click" Text="Delete this data" 
                                meta:resourcekey="lnkDeletedataResource1"></asp:LinkButton>
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
    <asp:Panel ID="pnlAutoFill" runat="server" CssClass="flightinfoitem" 
        meta:resourcekey="pnlAutoFillResource1">
        <table>
            <tr class="itemlabel">
                <td>
                    <asp:Label ID="lblAutoFill" Font-Bold="True" runat="server" Text="Auto-fill" 
                        meta:resourcekey="lblAutoFillResource1"></asp:Label><br />
                    <asp:Label ID="lblAutoFillDesc" runat="server" 
                        Text="Auto-fill fills in missing information that can be determined from your route of flight, the times provided above, and any optional telemetry file you provide above." 
                        meta:resourcekey="lblAutoFillDescResource1"></asp:Label>
                </td>
                <td><asp:Button ID="btnAutoFill" runat="server" onclick="onAutofill" 
                        Text="AutoFill" meta:resourcekey="btnAutoFillResource1"></asp:Button></td>
                <td>
                    <uc4:popmenu ID="popmenu1" runat="server">
                        <MenuContent>
                            <div style="padding:3px"><asp:Label ID="lblTOSpeed" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionTakeoffSpeed %>" /></div>
                            <div>
                                <asp:RadioButtonList ID="rblTakeOffSpeed" runat="server"></asp:RadioButtonList>
                            </div>
                            <div style="padding:3px"><asp:CheckBox ID="ckIncludeHeliports" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionIncludeHeliports %>" /></div>
                            <div style="padding:3px"><asp:CheckBox ID="ckEstimateNight" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionEstimateNight %>" /></div>
                        </MenuContent>
                    </uc4:popmenu>
                </td>
            </tr>
        </table>
    </asp:Panel>
    <asp:HiddenField ID="hdnFlightID" Value="-1" runat="server" />
    <uc1:mfbTimeZone ID="mfbTimeZone1" runat="server" />
</asp:Panel>
