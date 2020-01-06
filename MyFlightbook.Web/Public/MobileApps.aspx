<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Public_MobileApps" Culture="auto" meta:resourcekey="PageResource1" Codebehind="MobileApps.aspx.cs" %>

<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    <asp:Localize ID="locHeader" runat="server" Text="Mobile Apps"
                meta:resourcekey="locHeaderResource1"></asp:Localize>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:UpdatePanel runat="server" ID="updatePanel">
        <ContentTemplate>
        <div style="display: inline-block; vertical-align: top;">
            <asp:Literal ID="litMobileAppPromo" runat="server"
                meta:resourcekey="litMobileAppPromoResource1"></asp:Literal>
            <h2>
                <asp:Localize ID="locPickPlatform" Text="Choose your platform: " runat="server"
                    meta:resourcekey="locPickPlatformResource1"></asp:Localize>
                <asp:DropDownList ID="cmbMobileTarget" runat="server"
                    OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged"
                    AutoPostBack="True" meta:resourcekey="cmbMobileTargetResource1">
                    <asp:ListItem Enabled="true" Selected="True" Text="(Select a platform)" Value="-1" meta:resourcekey="ListItemResource5"></asp:ListItem>
                    <asp:ListItem Enabled="true" Text="iPhone" Value="0"
                        meta:resourcekey="ListItemResource1"></asp:ListItem>
                    <asp:ListItem Enabled="true" Text="iPad" Value="1"
                        meta:resourcekey="ListItemResource2"></asp:ListItem>
                    <asp:ListItem Enabled="true" Text="Android" Value="2"
                        meta:resourcekey="ListItemResource3"></asp:ListItem>
                    <asp:ListItem Enabled="true" Text="Windows Phone 7" Value="3"
                        meta:resourcekey="ListItemResource4"></asp:ListItem>
                </asp:DropDownList>
            </h2>
        </div>
        <div runat="server" id="divStoreLogos" style="display: inline-block; vertical-align: top; margin-left: 30px;">
            <p>
                <object data="AppleAppStore.svg" type="image/svg+xml" width="150"></object>
            </p>
            <p>
                <a href="https://play.google.com/store/apps/details?id=com.myflightbook.android" target="_blank">
                    <asp:Image ID="imgGooglePlay" runat="server" ImageUrl="~/images/google-play-badge.png" AlternateText="Get it on Google Play" Width="150px" />
                </a>
            </p>
        </div>
        <asp:MultiView ID="mvScreenShots" runat="server">
            <asp:View ID="vwNone" runat="server"></asp:View>
            <asp:View ID="vwiPhone" runat="server">
                <p class="EntryBlock" style="text-align: center">
                    <asp:HyperLink ID="lnkDownloadIPhone" runat="server"
                        Text="Download iPhone/iPad App"
                        NavigateUrl="http://itunes.apple.com/us/app/myflightbook-for-iphone/id349983064?mt=8"
                        Target="_blank" Font-Bold="True"
                        meta:resourcekey="lnkDownloadIPhoneResource1"></asp:HyperLink>
                    <br />
                    <object data="AppleAppStore.svg" type="image/svg+xml"></object>
                </p>
                <div>
                            <asp:HyperLink ID="lnkIPhoneFull1" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone1.png" Target="_blank"><asp:Image ID="imgIPhone1" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone1.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull2" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone2.png" Target="_blank"><asp:Image ID="imgIPhone2" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone2.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull3" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone3.png" Target="_blank"><asp:Image ID="imgIPhone3" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone3.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull4" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone4.png" Target="_blank"><asp:Image ID="imgIPhone4" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone4.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull5" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone5.png" Target="_blank"><asp:Image ID="imgIPhone5" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone5.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull6" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone6.png" Target="_blank"><asp:Image ID="imgIPhone6" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone6.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull7" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone7.png" Target="_blank"><asp:Image ID="imgIPhone7" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone7.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPhoneFull8" runat="server" NavigateUrl="~/Public/iPhoneImages/Full/iPhone8.png" Target="_blank"><asp:Image ID="imgIPhone8" runat="server" ImageUrl="~/Public/iPhoneImages/iPhone8.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                </div>
            </asp:View>
            <asp:View ID="vwiPad" runat="server">
                <p class="EntryBlock" style="text-align: center">
                    <asp:HyperLink ID="lnkDownloadIPad" runat="server"
                        Text="Download iPhone/iPad App"
                        NavigateUrl="http://itunes.apple.com/us/app/myflightbook-for-iphone/id349983064?mt=8"
                        Target="_blank" Font-Bold="True"
                        meta:resourcekey="lnkDownloadIPadResource1"></asp:HyperLink>
                    <br />
                    <object data="AppleAppStore.svg" type="image/svg+xml"></object>
                </p>
                <div>
                            <asp:HyperLink ID="lnkIPadFull1" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad1.png" Target="_blank"><asp:Image ID="imgIPad1" runat="server" ImageUrl="~/Public/iPadImages/iPad1.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull2" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad2.png" Target="_blank"><asp:Image ID="imgIPad2" runat="server" ImageUrl="~/Public/iPadImages/iPad2.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull3" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad3.png" Target="_blank"><asp:Image ID="imgIPad3" runat="server" ImageUrl="~/Public/iPadImages/iPad3.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull4" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad4.png" Target="_blank"><asp:Image ID="imgIPad4" runat="server" ImageUrl="~/Public/iPadImages/iPad4.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull5" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad5.png" Target="_blank"><asp:Image ID="imgIPad5" runat="server" ImageUrl="~/Public/iPadImages/iPad5.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull6" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad6.png" Target="_blank"><asp:Image ID="imgIPad6" runat="server" ImageUrl="~/Public/iPadImages/iPad6.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull7" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad7.png" Target="_blank"><asp:Image ID="imgIPad7" runat="server" ImageUrl="~/Public/iPadImages/iPad7.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                            <asp:HyperLink ID="lnkIPadFull8" runat="server" NavigateUrl="~/Public/iPadImages/Full/iPad8.png" Target="_blank"><asp:Image ID="imgIPad8" runat="server" ImageUrl="~/Public/iPadImages/iPad8.png" AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" /></asp:HyperLink>
                </div>
            </asp:View>
            <asp:View ID="vwAndroid" runat="server">
                <p class="EntryBlock" style="text-align: center">
                    <asp:HyperLink ID="lnkDownloadAndroidPlay" runat="server"
                        Text="Download Android App"
                        NavigateUrl="https://market.android.com/details?id=com.myflightbook.android"
                        Target="_blank" Font-Bold="True"
                        meta:resourcekey="lnkDownloadAndroidPlayResource1"></asp:HyperLink>
                    <br />
                    <a href="https://play.google.com/store/apps/details?id=com.myflightbook.android" target="_blank">
                        <asp:Image ID="Image2" runat="server" ImageUrl="~/images/google-play-badge.png" AlternateText="Get it on Google Play" ImageAlign="Middle" Style="padding:3px;" /></a>
                </p>
                <div>
                        <asp:Image ID="imgAndroid9" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android1.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid10" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android2.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid11" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android3.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid12" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android4.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid13" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android5.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid14" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android6.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgAndroid7" runat="server" 
                            ImageUrl="~/Public/AndroidImages/Android7.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                </div>
            </asp:View>
            <asp:View ID="vwWinPhone" runat="server">
                <p class="EntryBlock" style="text-align: center">
                    <a target="_blank" href="http://windowsphone.com/s?appid=606179af-90e7-41b9-be1b-71607cdec86c" style="font-weight: bold">
                        <asp:Localize ID="locGetWin7" Text="Download Windows Phone App from Windows Marketplace" runat="server" meta:resourcekey="locGetWin7Resource1"></asp:Localize>
                        <br />
                        <asp:Image runat="server" ID="imgWPhoneStore" ImageUrl="~/Public/WP7Images/154x40_WPS_Download_cyan.png" meta:resourcekey="imgWPhoneStoreResource1" />
                    </a>
                </p>
                <div>
                        <asp:Image ID="imgWP717" runat="server" ImageUrl="~/Public/WP7Images/WP71.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgWP718" runat="server" ImageUrl="~/Public/WP7Images/WP72.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgWP719" runat="server" ImageUrl="~/Public/WP7Images/WP73.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                    <asp:Image ID="imgWP720" runat="server" ImageUrl="~/Public/WP7Images/WP74.png"
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>"
                        Width="400px" />
                        <asp:Image ID="imgWP721" runat="server" ImageUrl="~/Public/WP7Images/WP75.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgWP722" runat="server" ImageUrl="~/Public/WP7Images/WP76.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                        <asp:Image ID="imgWP723" runat="server" ImageUrl="~/Public/WP7Images/WP77.png" 
                            AlternateText="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                            ToolTip="<%$ Resources:LocalizedText, MobileAppSampleImageAltText %>" 
                        Width="400px" />
                </div>
            </asp:View>
        </asp:MultiView>
    </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>

