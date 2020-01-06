<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbDecimalEdit" Codebehind="mfbDecimalEdit.ascx.cs" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="cc1" %>
<asp:TextBox ID="txtDecimal" runat="server" TextMode="Number"></asp:TextBox>
<asp:Image ID="imgXFill" ImageUrl="~/images/cross-fill.png" runat="server" Visible="false" ToolTip="<%$ Resources:LocalizedText, CrossfillPrompt %>"
     AlternateText="<%$ Resources:LocalizedText, CrossfillPrompt %>" />
<cc1:TextBoxWatermarkExtender ID="txtDecimal_TextBoxWatermarkExtender" WatermarkText="0.0" EnableViewState="false" 
    runat="server" Enabled="True" TargetControlID="txtDecimal" WatermarkCssClass="watermark">
</cc1:TextBoxWatermarkExtender>
<cc1:FilteredTextBoxExtender ID="FilteredTextBoxExtender" runat="server" FilterType="Custom" EnableViewState="false" 
    TargetControlID="txtDecimal" ValidChars="0123456789.">
</cc1:FilteredTextBoxExtender>        
<asp:RegularExpressionValidator ID="valNumber" runat="server" 
    ControlToValidate="txtDecimal" CssClass="error" Display="Dynamic" 
    ErrorMessage="<%$ Resources:LocalizedText, DecimalEditErrInvalid %>" 
    ValidationExpression="">
</asp:RegularExpressionValidator>  
<asp:RequiredFieldValidator ID="valRequired" runat="server" 
    ControlToValidate="txtDecimal" CssClass="error" Display="Dynamic" 
    ErrorMessage="<%$ Resources:LocalizedText, DecimalEditErrNotPresent %>" Enabled="false">
</asp:RequiredFieldValidator>
<asp:RegularExpressionValidator ID="valHHMMFormat" runat="server" EnableViewState="false" 
    ControlToValidate="txtDecimal" CssClass="error" Display="Dynamic" 
    ErrorMessage="<%$ Resources:LocalizedText, DecimalEditErrInvalidHHMM %>" 
    ValidationExpression="([0-9])*:([0-5][0-9])?"></asp:RegularExpressionValidator>
