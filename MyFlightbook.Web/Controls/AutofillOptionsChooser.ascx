<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AutofillOptionsChooser.ascx.cs" Inherits="AutofillOptionsChooser" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
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
