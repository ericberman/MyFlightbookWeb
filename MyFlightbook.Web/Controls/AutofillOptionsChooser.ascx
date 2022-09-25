<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AutofillOptionsChooser.ascx.cs" Inherits="AutofillOptionsChooser" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>

<div>
    <div style="float:left; padding:3px">
        <table>
            <tr>
                <td colspan="2">
                    <div><asp:Label ID="lblTOSpeed" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionTakeoffSpeed %>" /></div>
                    <asp:RadioButtonList ID="rblTakeOffSpeed" DataValueField="Value" DataTextField="Text" RepeatColumns="2" runat="server"></asp:RadioButtonList>
                </td>
            </tr>
            <tr>
                <td><asp:CheckBox ID="ckIncludeHeliports" runat="server" /></td>
                <td>
                    <div>
                        <asp:Label ID="lblIncludeHeliports" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionIncludeHeliports %>" AssociatedControlID="ckIncludeHeliports"></asp:Label>
                        <uc1:mfbTooltip runat="server" ID="ttHeliports">
                            <TooltipBody>
                                <div style="max-width:250px; min-width:140px">
                                    <%= Resources.LocalizedText.AutofillOptionsIncludeHeliportsTip %>
                                </div>
                            </TooltipBody>
                        </uc1:mfbTooltip>
                    </div>
                </td>
            </tr>
            <tr>
                <td><asp:CheckBox ID="ckEstimateNight" runat="server" /></td>
                <td>
                    <div>
                        <asp:Label ID="lblEstimateNight" runat="server" Text="<%$ Resources:LocalizedText, AutofillOptionEstimateNight %>" AssociatedControlID="ckEstimateNight"></asp:Label>
                        <uc1:mfbTooltip runat="server" ID="ttNight">
                            <TooltipBody>
                                <div style="max-width:250px; min-width:140px">
                                    <%= Resources.LocalizedText.AutofillOptionEstimateNightTip %>
                                </div>
                            </TooltipBody>
                        </uc1:mfbTooltip>
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
