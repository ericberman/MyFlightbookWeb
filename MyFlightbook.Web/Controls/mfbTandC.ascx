<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbTandC.ascx.cs" Inherits="Controls_mfbTandC" %>
<p><% =MyFlightbook.Branding.ReBrand(Resources.LocalizedText.TermsAndConditions) %></p>
<p style="font-size: 12pt; font-weight:bold"><asp:Literal ID="litTandCAgree" runat="server" Text="<%$ Resources:LocalizedText, TermsAndConditionsAgree %>"></asp:Literal></p>
