<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbDateTime" Codebehind="mfbDateTime.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:TextBox ID="txtDateTime" runat="server"></asp:TextBox>
<cc1:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" runat="server" EnableViewState="false" TargetControlID="txtDateTime" WatermarkCssClass="watermark">
</cc1:TextBoxWatermarkExtender>
(<a href='<% =String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:setNowUTCWithOffset(\"{0}\");", TextBoxWatermarkExtender1.ClientID) %>'><%=Resources.LocalizedText.DateTimeNow %></a>)
<asp:CustomValidator ID="CustomValidator1" runat="server" 
    ControlToValidate="txtDateTime" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:LocalizedText, DateTimeErrInvalid %>"
    ForeColor="" onservervalidate="CustomValidator1_ServerValidate"></asp:CustomValidator>