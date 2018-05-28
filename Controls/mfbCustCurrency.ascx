<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbCustCurrency.ascx.cs" Inherits="Controls_mfbCustCurrency" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Panel ID="pnlCustCurrency" runat="server" DefaultButton="btnAddCurrencyRule">
    <table>
        <tr>
            <td>
                <asp:Localize ID="locCustCurrencyName" runat="server" Text="<%$ Resources:Currency, CustomCurrencyNamePrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ID="txtRuleName" runat="server" CausesValidation="True"
                    ValidationGroup="vgAddCurrencyRule"></asp:TextBox>
                <cc1:TextBoxWatermarkExtender ID="txtRuleName_TextBoxWatermarkExtender"
                    runat="server" TargetControlID="txtRuleName"
                    WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Currency, CustomCurrencyWatermarkName %>"></cc1:TextBoxWatermarkExtender>
                <asp:RequiredFieldValidator ID="valCCName" runat="server"
                    ControlToValidate="txtRuleName" CssClass="error" Display="Dynamic"
                    ErrorMessage="<%$ Resources:Currency, CustomCurrencyErrNeedName %>"
                    ValidationGroup="vgAddCurrencyRule"></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="valNameLength" Display="Dynamic" CssClass="error" runat="server" ErrorMessage="<%$ Resources:Currency, CustomCurrencyNeedShorterName %>" ValidationGroup="vgAddCurrencyRule" ControlToValidate="txtRuleName" ValidationExpression="^\s*.{1,40}\s*$"></asp:RegularExpressionValidator>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Localize ID="locCustCurrencyMinEventsPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyPerformPrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:DropDownList ID="cmbLimitType" runat="server">
                    <asp:ListItem Selected="True" Value="Minimum" Text="<%$ Resources:Currency, CustomCurrencyMinEventsPrompt %>"></asp:ListItem>
                    <asp:ListItem Value="Maximum" Text="<%$ Resources:Currency, CustomCurrencyMaxEventsPrompt %>"></asp:ListItem>
                </asp:DropDownList>
                <asp:TextBox ID="txtNumEvents" Width="50px" runat="server"></asp:TextBox>
                <cc1:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1"
                    runat="server" TargetControlID="txtNumEvents"
                    WatermarkCssClass="watermark" WatermarkText="0"></cc1:TextBoxWatermarkExtender>
                <cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender1"
                    runat="server" FilterType="Numbers"
                    TargetControlID="txtNumEvents"></cc1:FilteredTextBoxExtender>
                <asp:DropDownList ID="cmbEventTypes" runat="server"
                    ValidationGroup="vgAddCurrencyRule">
                </asp:DropDownList>
                <div>
                    <asp:RequiredFieldValidator ID="valRequireEvents" runat="server"
                        ControlToValidate="txtNumEvents" CssClass="error" Display="Dynamic"
                        ErrorMessage="<%$ Resources:Currency, errCustomCurrencyInvalidEventCount %>"
                        ValidationGroup="vgAddCurrencyRule"></asp:RequiredFieldValidator>
                    <asp:CustomValidator ID="valRangeCheckEvents" runat="server"
                        ControlToValidate="txtNumEvents" CssClass="error" Display="Dynamic"
                        ErrorMessage="<%$ Resources:Currency, errCustomCurrencyInvalidEventCount %>"
                        OnServerValidate="valRangeCheckEvents_ServerValidate"
                        ValidationGroup="vgAddCurrencyRule"></asp:CustomValidator>
                </div>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Localize ID="locCustCurrencyTimeHorizonPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyPrecedingPrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ID="txtTimeFrame" Width="50px" runat="server"></asp:TextBox>
                <cc1:TextBoxWatermarkExtender ID="txtTimeFrame_TextBoxWatermarkExtender"
                    runat="server" TargetControlID="txtTimeFrame"
                    WatermarkCssClass="watermark" WatermarkText="0"></cc1:TextBoxWatermarkExtender>
                <cc1:FilteredTextBoxExtender ID="txtTimeFrame_FilteredTextBoxExtender"
                    runat="server" FilterType="Numbers"
                    TargetControlID="txtTimeFrame"></cc1:FilteredTextBoxExtender>
                <asp:DropDownList ID="cmbMonthsDays" runat="server"
                    ValidationGroup="vgAddCurrencyRule" AutoPostBack="True" OnSelectedIndexChanged="cmbMonthsDays_SelectedIndexChanged">
                </asp:DropDownList>
                <div>
                    <asp:RequiredFieldValidator ID="valCheckRequiredTimeframe" runat="server"
                        ControlToValidate="txtTimeFrame" CssClass="error" Display="Dynamic"
                        ErrorMessage="<%$ Resources:Currency, CustomCurrencyErrInvalidTimeFrame %>"
                        ValidationGroup="vgAddCurrencyRule"></asp:RequiredFieldValidator>
                    <asp:CustomValidator ID="valRangeCheckForTimeFrame" runat="server"
                        ControlToValidate="txtTimeFrame" CssClass="error" Display="Dynamic"
                        ErrorMessage="<%$ Resources:Currency, CustomCurrencyErrBadRange %>"
                        OnServerValidate="valRangeCheckForTimeFrame_ServerValidate"
                        ValidationGroup="vgAddCurrencyRule"></asp:CustomValidator>
                </div>
            </td>
        </tr>
        <tr>
            <td colspan="2">
                <asp:Label ID="locCustCurrencyApplicationPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyRestrictPrompt %>" Font-Bold="True"></asp:Label>
            </td>
        </tr>
        <tr style="vertical-align:top;">
            <td>
                <asp:Localize ID="locCustCurrencyModelPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyRestrictModels %>"></asp:Localize>
            </td>
            <td>
                <asp:ListBox ID="lstModels" runat="server" ValidationGroup="vgAddCurrencyRule" SelectionMode="Multiple"
                    DataTextField="DisplayName" DataValueField="MakeModelID" Width="300px"></asp:ListBox>
            </td>
        </tr>
        <tr style="vertical-align:top;">
            <td>
                <asp:Localize ID="locCustCurrencyAircraftPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyRestrictAircraft %>"></asp:Localize>
            </td>
            <td>
                <asp:ListBox ID="lstAircraft" runat="server"
                    ValidationGroup="vgAddCurrencyRule" SelectionMode="Multiple"
                    DataTextField="LongDescription" DataValueField="AircraftID" Width="300px"></asp:ListBox>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Localize ID="locCustCurrencyCategoryPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyRestrictCategory %>"></asp:Localize>
            </td>
            <td>
                <asp:DropDownList ID="cmbCategory" runat="server" AppendDataBoundItems="True"
                    ValidationGroup="vgAddCurrencyRule">
                    <asp:ListItem Selected="True" Text="<%$ Resources:Currency, CustomCurrencyRestrictAny %>" Value=""></asp:ListItem>
                </asp:DropDownList>
            </td>
        </tr>
        <tr>
            <td>
                <asp:Localize ID="locCustCurrencyCatClassPrompt" runat="server"
                    Text="<%$ Resources:Currency, CustomCurrencyRestrictCatClass %>"></asp:Localize>
            </td>
            <td>
                <asp:DropDownList ID="cmbCatClass" runat="server" AppendDataBoundItems="True"
                    DataTextField="CatClass" DataValueField="IdCatClass"
                    ValidationGroup="vgAddCurrencyRule">
                    <asp:ListItem Selected="True" Text="<%$ Resources:Currency, CustomCurrencyRestrictAny %>" Value="0"></asp:ListItem>
                </asp:DropDownList>
            </td>
        </tr>
        <tr>
            <td>&nbsp;</td>
            <td>
                <asp:Button ID="btnAddCurrencyRule" runat="server"
                    OnClick="btnAddCurrencyRule_Click" Text="<%$ Resources:Currency, CustomCurrencyAddRule %>"
                    ValidationGroup="vgAddCurrencyRule" />
                <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
            </td>
        </tr>
    </table>
    <asp:HiddenField ID="hdnCCId" Value="-1" runat="server" />
</asp:Panel>

