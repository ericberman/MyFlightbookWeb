<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TwoFactorAuth.ascx.cs" Inherits="MyFlightbook.Web.Controls.TwoFActorAuth" %>
<%@ Register Src="~/Controls/TwoFactorAuthVerifyCode.ascx" TagPrefix="uc1" TagName="TwoFactorAuthVerifyCode" %>
<asp:MultiView ID="mvTFAState" runat="server">
    <asp:View ID="vwAddTFA" runat="server">
        <p>
            <span style="font-size:larger; font-weight:bold">1.&nbsp</span><asp:Label ID="locDownload" runat="server" Text="<%$ Resources:Profile, TFADownloadGoogleAuthenticator %>"></asp:Label>
        </p>
        <p><span style="font-size:larger; font-weight:bold">2.&nbsp</span><asp:Localize ID="locSaveSecret" runat="server" Text="<%$ Resources:Profile, TFASaveSecretKey %>"></asp:Localize></p>
        <p><span style="font-size:larger; font-weight:bold">3.&nbsp</span><asp:Localize ID="locPickOptions" runat="server" Text="<%$ Resources:Profile, TFATwoOptionsPrompt %>"></asp:Localize></p>
        <div style="text-align:center">
            <div>
                <asp:Label ID="lblKeyPrompt" runat="server" Text="<%$ Resources:Profile, TFASecretKeyPrompt %>"></asp:Label>
                <asp:Label ID="lblKey" Font-Bold="true" runat="server"></asp:Label>
            </div>
            <img id="imgQR" runat="server" src="data:image/png;base64," />
        </div>
        <p><span style="font-size:larger; font-weight:bold">4.&nbsp</span><asp:Localize ID="locValidateCode" runat="server" Text="<%$ Resources:Profile, TFAValidateCodePrompt %>"></asp:Localize></p>
        <div style="text-align:center">
            <uc1:TwoFactorAuthVerifyCode runat="server" id="TwoFactorAuthVerifyCode" OnTFACodeFailed="TwoFactorAuthVerifyCode_TFACodeFailed" OnTFACodeVerified="TwoFactorAuthVerifyCode_TFACodeVerified" />
        </div>
    </asp:View>
    <asp:View ID="vwNoTFA" runat="server">
        <p><asp:Label ID="lblTFAover" runat="server" Text="<%$ Resources:Profile, TFAOverview %>"></asp:Label> <asp:HyperLink ID="lnkTFALearnMore" runat="server" Text="<%$ Resources:Profile, TFALearnMore %>" Target="_blank" NavigateUrl="https://en.wikipedia.org/wiki/Multi-factor_authentication"></asp:HyperLink></p>
        <p><% =Branding.ReBrand(Resources.Profile.TFAUseGA) %></p>
        <asp:LinkButton ID="lnkEnableTFA" runat="server" Font-Bold="true" Text="<%$ Resources:Profile, TFAEnable %>" OnClick="lnkEnableTFA_Click"></asp:LinkButton>
    </asp:View>
    <asp:View ID="vwTFAActive" runat="server">
        <p><asp:Label ID="lblTFAActive" runat="server" Text="<%$ Resources:Profile, TFAIsSetUp %>"></asp:Label></p>
        <p><asp:LinkButton ID="lnkDisableTFA" runat="server" Text="<%$ Resources:Profile, TFADisable %>" OnClick="lnkDisableTFA_Click"></asp:LinkButton></p>
    </asp:View>
    <asp:View ID="vwVerifyTFA" runat="server">
        <p><asp:Label ID="lblTFA2" runat="server" Font-Bold="true" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
        <p><asp:Label ID="lblTFAUseApp2" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
        <div style="text-align:center">
            <uc1:TwoFactorAuthVerifyCode runat="server" id="tfaVerifyDisable" OnTFACodeFailed="TwoFactorAuthVerifyCode_TFACodeFailed" OnTFACodeVerified="tfaVerifyDisable_TFACodeVerified" />
        </div>
    </asp:View>
</asp:MultiView>
<div><asp:Label ID="lblCodeResult" runat="server" EnableViewState="false"></asp:Label></div>
