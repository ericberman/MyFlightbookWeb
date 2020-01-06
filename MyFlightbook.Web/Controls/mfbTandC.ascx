<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbTandC" Codebehind="mfbTandC.ascx.cs" %>
<p><% =MyFlightbook.Branding.ReBrand(Resources.LocalizedText.TermsAndConditions) %></p>
<p style="font-size: 12pt; font-weight:bold"><asp:Literal ID="litTandCAgree" runat="server" Text="<%$ Resources:LocalizedText, TermsAndConditionsAgree %>"></asp:Literal></p>
