<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TwoFactorAuthVerifyCode.ascx.cs" Inherits="MyFlightbook.Web.Controls.TwoFactorAuthVerifyCode" %>
<asp:Panel ID="pnlValidate" runat="server" DefaultButton="btnVerifyCode">
    <asp:TextBox ID="txtCode" runat="server" ValidationGroup="tfaCode"></asp:TextBox>
    <asp:Button ID="btnVerifyCode" runat="server" Text="<%$ Resources:Profile, TFAValidateCode %>" OnClick="btnVerifyCode_Click" ValidationGroup="tfaCode" CausesValidation="true" />
    <ajaxToolkit:TextBoxWatermarkExtender ID="wmeCode" TargetControlID="txtCode" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Profile, TFAEnterCodePrompt %>" runat="server" />
    <asp:RequiredFieldValidator ID="reDigitsReq" runat="server" ErrorMessage="<%$ Resources:Profile, TFAErrCodeFormat %>" ControlToValidate="txtCode" CssClass="error" Display="Dynamic"></asp:RequiredFieldValidator>
    <asp:RegularExpressionValidator ID="reDigits"  runat="server" ErrorMessage="<%$ Resources:Profile, TFAErrCodeFormat %>" ControlToValidate="txtCode" CssClass="error" Display="Dynamic" ValidationExpression="\d{6}"></asp:RegularExpressionValidator>
    <ajaxToolkit:FilteredTextBoxExtender ID="ftbNumeric" runat="server" TargetControlID="txtCode" FilterMode="ValidChars" ValidChars="0123456789" FilterType="Numbers" />
</asp:Panel>
