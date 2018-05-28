<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbHeader.ascx.cs" Inherits="Controls_mfbHeader" %>
<%@ Register Src="XMLNav.ascx" TagName="XMLNav" TagPrefix="uc2" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<div class="noprint">
    <asp:Panel runat="server" ID="FullHeader" style="width: 100%">
        <div id="headerBar">
            <div id="headerLogo">
                <asp:HyperLink ID="lnkLogo" NavigateUrl="~/Default.aspx" runat="server"></asp:HyperLink>
            </div>
            <div id="headerSearchAndSign">
                <div id="headerSearchBox">
                    <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSearch" style="padding-top:3px;">
                        <div style="border: 1px solid darkgray; border-radius: 14px; height: 24px; display: table-cell; vertical-align: middle; text-align:left; padding-left: 8px; padding-right:3px; ">
                            <asp:Image ID="Image1" runat="server" ImageUrl="~/images/Search.png" ImageAlign="AbsMiddle" Height="20px" />
                            <asp:TextBox ID="txtSearch" runat="server" Width="120px" Font-Size="8pt" BorderStyle="None" style="vertical-align:middle"></asp:TextBox>
                            <cc1:TextBoxWatermarkExtender
                                ID="TextBoxWatermarkExtender1" runat="server" TargetControlID="txtSearch" EnableViewState="false"
                                WatermarkText="<%$ Resources:LocalizedText, SearchBoxWatermark %>" WatermarkCssClass="watermark">
                            </cc1:TextBoxWatermarkExtender>
                        </div>
                        <asp:Button ID="btnSearch" style="display:none" runat="server" Text="<%$ Resources:LocalizedText, SearchBoxGo %>" CausesValidation="false" onclick="btnSearch_Click" Font-Size="9px" CssClass="itemlabel" />
                    </asp:Panel>
                </div>
                <div id="headerLoginStatus">
                    <asp:Label ID="lblUser" runat="server" Text="<%$ Resources:LocalizedText, LoginStatusWelcome %>" Visible="false"></asp:Label> 
                    <asp:LoginStatus ID="LoginStatus1" runat="server" LogoutPageUrl="~/secure/login.aspx" LoginText="<%$ Resources:LocalizedText, LoginStatusSignIn %>" LogoutText="<%$ Resources:LocalizedText, LoginStatusSignOut %>" LogoutAction="RedirectToLoginPage" />
                    <asp:Label ID="lblCreateAccount" runat="server" Text=""> | 
                    <asp:HyperLink ID="lnkCreateAccount" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, LoginStatusCreateAccount %>"></asp:HyperLink></asp:Label>
                </div>
                <asp:Panel ID="pnlDonate" runat="server">
                    <asp:HyperLink ID="lnkDonate" CssClass="boldface" runat="server" NavigateUrl="~/Member/EditProfile.aspx/pftDonate"></asp:HyperLink>
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
