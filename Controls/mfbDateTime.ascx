<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbDateTime.ascx.cs" Inherits="Controls_mfbDateTime" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:TextBox ID="txtDateTime" runat="server" TextMode="DateTime"></asp:TextBox>
<cc1:TextBoxWatermarkExtender ID="TextBoxWatermarkExtender1" runat="server" EnableViewState="false" TargetControlID="txtDateTime" WatermarkCssClass="watermark">
</cc1:TextBoxWatermarkExtender>
(<a href='<% =String.Format(System.Globalization.CultureInfo.InvariantCulture, "javascript:$find(\"{0}\").set_text(\"{1}\");", TextBoxWatermarkExtender1.ClientID, DateTime.UtcNow.UTCDateFormatString()) %>'><%=Resources.LocalizedText.DateTimeNow %></a>)
<asp:CustomValidator ID="CustomValidator1" runat="server" 
    ControlToValidate="txtDateTime" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:LocalizedText, DateTimeErrInvalid %>"
    ForeColor="" onservervalidate="CustomValidator1_ServerValidate"></asp:CustomValidator>