<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSubscriptionManager" Codebehind="mfbSubscriptionManager.ascx.cs" %>
<asp:UpdatePanel ID="UpdatePanel1" runat="server">
    <ContentTemplate>
        <div class="prefSectionRow">
            <asp:CheckBoxList ID="cklEmailSubscriptions" runat="server" DataTextField="Name" DataValueField="Type">
            </asp:CheckBoxList>
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
