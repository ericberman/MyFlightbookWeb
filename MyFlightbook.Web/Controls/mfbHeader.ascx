<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbHeader.ascx.cs" Inherits="Controls_mfbHeader" %>
<%@ Register Src="XMLNav.ascx" TagName="XMLNav" TagPrefix="uc2" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc2" TagName="mfbSearchbox" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<div class="noprint">
    <asp:Panel runat="server" ID="FullHeader" style="width: 100%">
        <div id="headerBar">
            <div id="headerLogo">
                <asp:HyperLink ID="lnkLogo" NavigateUrl="~/Default.aspx" runat="server"></asp:HyperLink>
            </div>
            <div id="headerSearchAndSign">
                <div id="headerSearchBox">
                    <uc2:mfbSearchbox runat="server" ID="mfbSearchbox" OnSearchClicked="btnSearch_Click" Hint="<%$ Resources:LocalizedText, SearchBoxWatermark %>" />
                </div>
                <div id="headerLoginStatus">
                    <asp:MultiView ID="mvLoginStatus" runat="server">
                        <asp:View ID="vwSignedIn" runat="server">
                            <asp:Panel runat="server" ID="pnlUser" style="padding: 2px; margin-top: 2px; width: 180px">
                                <asp:Image ID="imgProfile" runat="server" style="width:12pt; height:12pt; vertical-align:middle" ImageUrl="~/Public/tabimages/ProfileTab.png" />&nbsp;
                                <asp:Label ID="lblUser" runat="server" style="vertical-align:middle;"></asp:Label>  
                            </asp:Panel>
                            <asp:Panel ID="pnlMenuContent" runat="server" CssClass="popMenuContent" style="padding: 3px; display:none; width: 180px">
                                <div style="padding:4px; text-align:center;">
                                    <div><asp:Label ID="lblMemberSince" runat="server"></asp:Label></div>
                                    <div><asp:Label ID="lblLastLogin" runat="server"></asp:Label></div>
                                    <div runat="server" id="itemLastActivity"><asp:Label ID="lblLastActivity" runat="server"></asp:Label></div>
                                    <div><asp:LoginStatus ID="LoginStatus2" runat="server" LogoutPageUrl="~/secure/login.aspx" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" /></div>
                                </div>
                            </asp:Panel>
                            <asp:dropshadowextender ID="DropShadowExtender1" runat="server" TargetControlID="pnlMenuContent" Opacity=".5"></asp:dropshadowextender>
                            <asp:HoverMenuExtender ID="hme" HoverCssClass="hoverPopMenu" runat="server" TargetControlID="pnlUser" PopupControlID="pnlMenuContent" PopupPosition="Bottom"></asp:HoverMenuExtender>
                        </asp:View>
                        <asp:View ID="vwNotSignedIn" runat="server">
                            <asp:LoginStatus ID="LoginStatus1" runat="server" LogoutPageUrl="~/secure/login.aspx" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" />
                            <asp:Label ID="lblCreateAccount" runat="server"> | 
                            <asp:HyperLink ID="lnkCreateAccount" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, LoginStatusCreateAccount %>"></asp:HyperLink></asp:Label>
                        </asp:View>
                    </asp:MultiView>
                </div>
                <asp:Panel ID="pnlDonate" runat="server">
                    <asp:HyperLink ID="lnkDonate" Font-Bold="true" runat="server" NavigateUrl="~/Member/EditProfile.aspx/pftDonate"></asp:HyperLink>
                </asp:Panel>
            </div>
            <div id="headerMiddleContainer">
                <asp:MultiView ID="mvCrossSellOrEvents" runat="server">
                    <asp:View ID="vwMobileCrossSell" runat="server">
                        <div id="headerMobileCrossSell">
                            <asp:Image ID="imgSmartphone" ImageUrl="~/images/Smartphone.png" runat="server" style="vertical-align:middle" GenerateEmptyAlternateText="true" />
                            <asp:MultiView ID="mvXSell" runat="server" ActiveViewIndex="0">
                                <asp:View runat="server" ID="vwGeneric">
                                    <asp:HyperLink ID="lnkDownload" NavigateUrl="~/Public/MobileApps.aspx" runat="server"></asp:HyperLink>,
                                    <asp:Localize ID="Localize5" Text="<%$ Resources:LocalizedText, HeaderDownloadIsFree %>" runat="server"></asp:Localize> 
                                </asp:View>
                                <asp:View runat="server" ID="vwIOS">
                                    <asp:HyperLink ID="lnkDownloadIPhone" NavigateUrl="~/Public/MobileApps.aspx?p=0" runat="server" Text=""></asp:HyperLink>.
                                </asp:View>
                                <asp:View runat="server" ID="vwDroid">
                                    <asp:HyperLink ID="lnkDownloadAndroid" NavigateUrl="~/Public/MobileApps.aspx?p=2" runat="server" Text=""></asp:HyperLink>.
                                </asp:View>
                                <asp:View runat="server" ID="vwW7Phone">
                                    <asp:HyperLink ID="lnkDownloadWindowsPhone" NavigateUrl="~/Public/MobileApps.aspx?p=3" runat="server" Text=""></asp:HyperLink>.
                                </asp:View>
                            </asp:MultiView>
                        </div>
                    </asp:View>
                    <asp:View ID="vwUpcomingEvent" runat="server">
                        <asp:Panel ID="pnlWebinar" runat="server" style="height: 32px; text-align:center; padding: 5px; margin-top: 4px; margin-bottom: 4px; background-color:#E8E8E8; border-radius: 5px;">
                            <span style="vertical-align:middle"><asp:Literal ID="litWebinar" runat="server"></asp:Literal> <asp:HyperLink ID="lnkDetails" runat="server">Details</asp:HyperLink></span>
                        </asp:Panel>
                        <asp:Panel ID="pnlWebinarDetails" runat="server" CssClass="modalpopup" style="display:none;" DefaultButton="btnDismiss">
                            <div style="text-align:left">
                                <asp:Label ID="lblWebinarDetails" style="white-space:pre-line" runat="server"></asp:Label>
                            </div>
                            <div style="text-align:center"><asp:Button ID="btnDismiss" runat="server" Text="Close" /></div>
                        </asp:Panel>
                        <cc1:ModalPopupExtender ID="mpeEvent" BackgroundCssClass="modalBackground" CancelControlID="btnDismiss" TargetControlID="pnlWebinar" PopupControlID="pnlWebinarDetails" runat="server"></cc1:ModalPopupExtender>
                    </asp:View>
                </asp:MultiView>

                <div id="headerMenuContainer">
                    <uc2:XMLNav ID="XMLNav1" runat="server" XmlSrc="~/NavLinks.xml" SelectedItem="tabHome" MenuStyle="HoverPop" />
                </div>
            </div>
        </div>
    </asp:Panel>
</div>
