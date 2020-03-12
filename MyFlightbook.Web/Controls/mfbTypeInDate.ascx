<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbTypeInDate.ascx.cs" Inherits="Controls_mfbTypeInDate" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:TextBox ID="txtDate" runat="server" TextMode="Date"></asp:TextBox>
<cc1:CalendarExtender ID="CalendarExtender1" runat="server" EnableViewState="false" TargetControlID="txtDate" Format="d" >
</cc1:CalendarExtender>
<asp:CustomValidator ID="valDateOK" runat="server" ControlToValidate="txtDate" CssClass="error"
    ErrorMessage="<%$ Resources:LocalizedText, TypeInDateInvalidDate %>"
    OnServerValidate="DateIsValid" Display="Dynamic"></asp:CustomValidator>
<cc1:TextBoxWatermarkExtender ID="wme" EnableViewState="false" runat="server" WatermarkCssClass="watermark" TargetControlID="txtDate">
</cc1:TextBoxWatermarkExtender>
