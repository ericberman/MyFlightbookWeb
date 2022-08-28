<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbHeader.ascx.cs" Inherits="MyFlightbook.Controls.mfbHeader" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc2" TagName="mfbSearchbox" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<div class="noprint">
    <asp:Panel runat="server" ID="FullHeader" style="width: 100%">
        <div id="headerBar">
            <div id="headerLogo">
                <asp:HyperLink ID="lnkLogo" NavigateUrl="~/Default.aspx" runat="server"></asp:HyperLink>
            </div>
            <div id="headerProfile">
                <div id="headerSearchAndSign">
                    <div id="headerSearchBox">
                        <uc2:mfbSearchbox runat="server" ID="mfbSearchbox" OnSearchClicked="btnSearch_Click" Hint="<%$ Resources:LocalizedText, SearchBoxWatermark %>" />
                    </div>
                    <div id="headerLoginStatus">
                        <asp:MultiView ID="mvLoginStatus" runat="server">
                            <asp:View ID="vwSignedIn" runat="server">
                                <img runat="server" id="imgHdSht" class="headerHeadSht popTrigger" src="~/Public/tabimages/ProfileTab.png" />
                                <div class="popMenuContent popMenuHidden" id="loginPop">
                                    <div style="padding:4px; text-align:center;">
                                        <div><asp:Label ID="lblMemberSince" runat="server" /></div>
                                        <div><asp:Label ID="lblLastLogin" runat="server" /></div>
                                        <div runat="server" id="itemLastActivity"><asp:Label ID="lblLastActivity" runat="server" /></div>
                                        <div id="signOutRow"><asp:LoginStatus ID="LoginStatus2" runat="server" LogoutPageUrl="~/secure/login.aspx" Width="100%" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" /></div>
                                    </div>
                                </div>
                            </asp:View>
                            <asp:View ID="vwNotSignedIn" runat="server">
                            </asp:View>
                        </asp:MultiView>
                        <div id="headerWelcome">
                            <asp:MultiView ID="mvWelcome" runat="server">
                                <asp:View ID="vwWelcomeAuth" runat="server">
                                    <div style="padding: 2px; margin-top: 2px; width: 180px"><asp:Label ID="lblUser" runat="server" style="vertical-align:middle;" /></div>
                                    <div style="padding:2px; font-weight: bold;"><asp:HyperLink ID="lnkDonate" runat="server" NavigateUrl="~/Member/EditProfile.aspx/pftDonate" /></div>
                                </asp:View>
                                <asp:View ID="vwWelcomeNotAuth" runat="server">
                                    <asp:LoginStatus ID="LoginStatus1" runat="server" LogoutPageUrl="~/secure/login.aspx" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" />
                                     | <asp:HyperLink ID="lnkCreateAccount" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, LoginStatusCreateAccount %>" />
                                </asp:View>
                            </asp:MultiView>
                        </div>
                    </div>
                </div>
            </div>
            <div id="headerMiddleContainer">
                <asp:MultiView ID="mvCrossSellOrEvents" runat="server">
                    <asp:View ID="vwMobileCrossSell" runat="server">
                        <div id="headerMobileCrossSell">
                            <asp:Image ID="imgSmartphone" ImageUrl="~/images/Smartphone.png" runat="server" style="vertical-align:middle" GenerateEmptyAlternateText="true" />
                            <asp:MultiView ID="mvXSell" runat="server" ActiveViewIndex="0">
                                <asp:View runat="server" ID="vwGeneric">
                                    <asp:HyperLink ID="lnkDownload" NavigateUrl="~/Public/MobileApps.aspx" runat="server"></asp:HyperLink>
                                    <asp:Localize ID="Localize5" Text="<%$ Resources:LocalizedText, HeaderDownloadIsFree %>" runat="server"></asp:Localize> 
                                </asp:View>
                                <asp:View runat="server" ID="vwIOS">
                                    <asp:HyperLink ID="lnkDownloadIPhone" NavigateUrl="~/Public/MobileApps.aspx?p=0" runat="server" Text=""></asp:HyperLink>.
                                </asp:View>
                                <asp:View runat="server" ID="vwDroid">
                                    <asp:HyperLink ID="lnkDownloadAndroid" NavigateUrl="~/Public/MobileApps.aspx?p=2" runat="server" Text=""></asp:HyperLink>.
                                </asp:View>
                            </asp:MultiView>
                        </div>
                    </asp:View>
                    <asp:View ID="vwUpcomingEvent" runat="server">
                        <asp:Panel ID="pnlWebinar" runat="server" style="height: 32px; text-align:center; padding: 5px; margin-top: 4px; margin-bottom: 4px; background-color:#E8E8E8; border-radius: 5px;">
                            <span onclick="showWebinar();" style="vertical-align:middle"><asp:Literal ID="litWebinar" runat="server"></asp:Literal></span>
                        </asp:Panel>
                        <div id="webinarPop" style="display:none;">
                            <div style="text-align:left">
                                <asp:Label ID="lblWebinarDetails" style="white-space:pre-line" runat="server" />
                            </div>
                            <div style="text-align:center"><asp:Button ID="btnDismiss" runat="server" Text="Close" OnClientClick="hideWebinar()" /></div>
                        </div>
                        <script type="text/javascript">
                            function showWebinar() {
                                $("#webinarPop").dialog({ autoOpen: true, closeOnEscape: true, width: 400, modal: true });
                            }
                            function hideWebinar() {
                                $("#webinarPop").dialog("close");
                            }
                        </script>
                    </asp:View>
                </asp:MultiView>

                <div id="headerMenuContainer">
                    <ul id="menu-bar">
                        <asp:PlaceHolder ID="plcMenuBar" runat="server" />
                    </ul>
                </div>
            </div>
        </div>
    </asp:Panel>
</div>
