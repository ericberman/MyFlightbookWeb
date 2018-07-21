<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbHeader.ascx.cs" Inherits="Controls_mfbHeader" %>
<%@ Register Src="XMLNav.ascx" TagName="XMLNav" TagPrefix="uc2" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="~/Controls/mfbSearchbox.ascx" TagPrefix="uc2" TagName="mfbSearchbox" %>

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
                    <asp:Label ID="lblUser" runat="server" Text="<%$ Resources:LocalizedText, LoginStatusWelcome %>" Visible="false"></asp:Label> 
                    <asp:LoginStatus ID="LoginStatus1" runat="server" LogoutPageUrl="~/secure/login.aspx" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" />
                    <asp:Label ID="lblCreateAccount" runat="server" Text=""> | 
                    <asp:HyperLink ID="lnkCreateAccount" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, LoginStatusCreateAccount %>"></asp:HyperLink></asp:Label>
                </div>
                <asp:Panel ID="pnlDonate" runat="server">
                    <asp:HyperLink ID="lnkDonate" Font-Bold="true" runat="server" NavigateUrl="~/Member/EditProfile.aspx/pftDonate"></asp:HyperLink>
                </asp:Panel>
            </div>
            <div id="headerMiddleContainer">
                <div id="headerMobileCrossSell">
                    <asp:Image ID="imgSmartphone" ImageUrl="~/images/Smartphone.png" runat="server" style="vertical-align:middle" />
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
                <div id="headerMenuContainer">
                    <uc2:XMLNav ID="XMLNav1" runat="server" XmlSrc="~/NavLinks.xml" SelectedItem="tabHome" MenuStyle="HoverPop" />
                </div>
            </div>
        </div>
        <div class="tabbargradient" style="width:100%" runat="server" id="gradientBar">&nbsp;</div>
    </asp:Panel>
</div>
