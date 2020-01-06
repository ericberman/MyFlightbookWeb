<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_ClubControls_TimeZone" Codebehind="TimeZone.ascx.cs" %>
<asp:DropDownList CausesValidation="true" ID="cmbTimezones" runat="server" DataTextField="DisplayName" DataValueField="Id" AppendDataBoundItems="true">
    <asp:ListItem Text="<%$ Resources:Schedule, ItemEmptyTimezone %>" Value=""></asp:ListItem>
</asp:DropDownList><br />
<asp:RequiredFieldValidator ID="valRequiredTimezone" ControlToValidate="cmbTimeZones" CssClass="error" 
    runat="server" ErrorMessage="<%$ Resources:Schedule, errNoTimezone %>" InitialValue=""
    Display="Dynamic"></asp:RequiredFieldValidator>
