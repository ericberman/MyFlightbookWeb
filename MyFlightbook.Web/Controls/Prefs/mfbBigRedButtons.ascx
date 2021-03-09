<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbBigRedButtons.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbBigRedButtons" %>
<%@ Register Src="~/Controls/TwoFactorAuthVerifyCode.ascx" TagPrefix="uc1" TagName="TwoFactorAuthVerifyCode" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<div><asp:Localize ID="locRedButtons" runat="server" Text="<%$ Resources:Profile, BigRedButtonsHeader %>"></asp:Localize></div>
<asp:MultiView ID="mvBigRedButtons" runat="server">
    <asp:View ID="vwStaticRedButtons" runat="server">
        <p><asp:Label ID="lBRB1" runat="server" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
        <p><asp:Label ID="lBRB2" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
        <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaBRB" OnTFACodeFailed="tfaBRB_TFACodeFailed" OnTFACodeVerified="tfaBRB_TFACodeVerified" />
        <div><asp:Label ID="lblBRB2faErr" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false"></asp:Label></div>
    </asp:View>
    <asp:View ID="vwRedButtons" runat="server">
        <p><asp:Localize ID="locDeleteUnusedAircraft" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteAircraftPrompt %>"></asp:Localize></p>
        <div><asp:Button ID="btnDeleteUnusedAircraft" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteAircraft %>" OnClick="btnDeleteUnusedAircraft_Click" /></div>
        <p><asp:Localize ID="locDeleteFlights" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteFlightsPrompt %>"></asp:Localize></p>
        <div><asp:Button ID="btnDeleteFlights" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteFlights %>" OnClick="btnDeleteFlights_Click" /></div>
        <div><asp:Label ID="lblDeleteFlightsCompleted" runat="server" Text="<%$ Resources:Profile, ProfileDeleteFlightsCompleted %>" CssClass="success" Font-Bold="true" Visible="false" EnableViewState="false"></asp:Label></div>
        <cc1:ConfirmButtonExtender ID="confirmDeleteFlights" ConfirmText="<%$ Resources:Profile, ProfileBulkDeleteConfirm %>" TargetControlID="btnDeleteFlights" runat="server" />
        <p><asp:Localize ID="locCloseAccount" runat="server" Text="<%$ Resources:Profile, ProfileDeleteAccountPrompt %>"></asp:Localize></p>
        <div><asp:Button ID="btnCloseAccount" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileDeleteAccount %>" OnClick="btnCloseAccount_Click" /></div>
        <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" ConfirmText="<%$ Resources:Profile, ProfileDeleteAccountConfirm %>" TargetControlID="btnCloseAccount" runat="server" />
    </asp:View>
</asp:MultiView>
<asp:Label ID="lblDeleteErr" runat="server" EnableViewState="false" CssClass="error"></asp:Label>