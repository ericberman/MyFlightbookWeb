<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbGoogleAdSense.ascx.cs" Inherits="Controls_mfbGoogleAdSense" %>
<asp:MultiView ID="mvGoogleAd" runat="server">
    <asp:View ID="vwHorizontalAd" runat="server">
        <script async src='https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=<% =LocalConfig.SettingForKey("GoogleAdClient") %>'
             crossorigin="anonymous"></script>
        <!-- MyFlightbookHorizontal -->
        <ins class="adsbygoogle"
             style="display:inline-block;width:728px;height:90px"
             data-ad-client='<% =LocalConfig.SettingForKey("GoogleAdClient") %>'
             data-ad-slot='<% =LocalConfig.SettingForKey("GoogleAdHorizontalSlot") %>'></ins>
        <script>
            (adsbygoogle = window.adsbygoogle || []).push({});
        </script>
    </asp:View>
    <asp:View ID="vwVerticalAd" runat="server">
        <script async src='https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=<% =LocalConfig.SettingForKey("GoogleAdClient") %>'
             crossorigin="anonymous"></script>
        <!-- MyFlightbookVertical -->
        <ins class="adsbygoogle"
             style="display:inline-block;width:120px;height:600px"
             data-ad-client='<% =LocalConfig.SettingForKey("GoogleAdClient") %>'
             data-ad-slot='<% =LocalConfig.SettingForKey("GoogleAdVerticalSlot") %>'></ins>
        <script>
            (adsbygoogle = window.adsbygoogle || []).push({});
        </script>
    </asp:View>
</asp:MultiView>