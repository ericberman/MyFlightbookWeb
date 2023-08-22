<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSubscriptionManager.ascx.cs" Inherits="Controls_mfbSubscriptionManager" %>
<asp:UpdatePanel ID="UpdatePanel1" runat="server">
    <ContentTemplate>
        <div class="prefSectionRow">
            <div>
                <asp:CheckBox ID="ckCurrencyWeekly" runat="server" Text="<%$ Resources:Profile, EmailCurrencyName %>" onclick="setLocalPrefChecked('emailCurrencyWeekly', this);"  />
                <table style="margin-left: 2em;">
                    <tr>
                        <td><asp:CheckBox ID="ckCurrencyExpiring" runat="server" onclick="setLocalPrefChecked('emailCurrencyExpiring', this);" /></td>
                        <td><asp:Label ID="lblAsNeeded" runat="server" Text="<%$ Resources:Profile, EmailCurrencyExpiration %>" AssociatedControlID="ckCurrencyExpiring"></asp:Label></td>
                    </tr>
                    <tr runat="server" id="rowPromo">
                        <td></td>
                        <td><asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>" />&nbsp;<asp:Label ID="lblCurrencyExpirationPromotion" runat="server" /></td>
                    </tr>
                </table>
            </div>
            <div><asp:CheckBox ID="ckTotalsWeekly" runat="server" Text="<%$ Resources:Profile, EmailTotalsName %>" onclick="setLocalPrefChecked('emailTotalsWeekly', this);"  /></div>
            <div><asp:CheckBox ID="ckMonthly" runat="server" Text="<%$ Resources:Profile, EmailMonthlyName %>" onclick="setLocalPrefChecked('emailTotalsMonthly', this);"  /></div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
