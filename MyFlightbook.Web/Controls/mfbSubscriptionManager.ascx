<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSubscriptionManager.ascx.cs" Inherits="Controls_mfbSubscriptionManager" %>
<asp:UpdatePanel ID="UpdatePanel1" runat="server">
    <ContentTemplate>
        <div class="prefSectionRow">
            <div>
                <asp:CheckBox ID="ckCurrencyWeekly" runat="server" Text="<%$ Resources:Profile, EmailCurrencyName %>" />
                <table style="margin-left: 2em;">
                    <tr>
                        <td><asp:CheckBox ID="ckCurrencyExpiring" runat="server" /></td>
                        <td><asp:Label ID="lblAsNeeded" runat="server" Text="<%$ Resources:Profile, EmailCurrencyExpiration %>" AssociatedControlID="ckCurrencyExpiring"></asp:Label></td>
                    </tr>
                    <tr runat="server" id="rowPromo">
                        <td></td>
                        <td><asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>&nbsp;<asp:Label ID="lblCurrencyExpirationPromotion" runat="server"></asp:Label></td>
                    </tr>
                </table>
            </div>
            <div><asp:CheckBox ID="ckTotalsWeekly" runat="server" Text="<%$ Resources:Profile, EmailTotalsName %>" /></div>
            <div><asp:CheckBox ID="ckMonthly" runat="server" Text="<%$ Resources:Profile, EmailMonthlyName %>" /></div>
        </div>
        <div class="prefSectionRow">
            <asp:Button ID="btnUpdateEmailPrefs" runat="server"  
                Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" 
                ValidationGroup="valPrefs" onclick="btnUpdateEmailPrefs_Click" />
            <br />
            <asp:Label ID="lblEmailPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False"></asp:Label>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
