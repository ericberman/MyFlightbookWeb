<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbFooter" Codebehind="mfbFooter.ascx.cs" %>
<%@ Register Src="RSSCurrency.ascx" TagName="RSSCurrency" TagPrefix="uc1" %>
<div class="footerContainer">
    <asp:Panel ID="pnlClassic" runat="server" Width="100%"
        meta:resourcekey="pnlClassicResource1">
        <hr />
        <div class="footerItem">
            <ul>
                <li><asp:HyperLink ID="lnkPrivacy" NavigateUrl="~/Public/Privacy.aspx"
                runat="server" Text="Privacy policy" meta:resourcekey="lnkPrivacyResource1"></asp:HyperLink></li>
                <li><asp:HyperLink ID="HyperLink1" NavigateUrl="~/Public/TandC.aspx" runat="server" 
                Text="Terms of Use" meta:resourcekey="HyperLink1Resource1"></asp:HyperLink></li>
                <li><asp:HyperLink ID="lnkDevelopers" runat="server" Text="Developers" NavigateUrl="~/Public/Developer.aspx" meta:resourcekey="lnkDevelopers1"></asp:HyperLink></li>
            </ul>
        </div>
        <div class="footerItem">
            <ul>
                <li><asp:HyperLink ID="lnkContact" NavigateUrl="~/Public/ContactMe.aspx" 
                runat="server" Text="Contact Us" meta:resourcekey="lnkContactResource1"></asp:HyperLink></li>
                <li><asp:HyperLink ID="lnkFAQ" runat="server" NavigateUrl="~/Public/FAQ.aspx" 
                    Text="FAQ" meta:resourcekey="lnkFAQResource1"></asp:HyperLink></li>
                <li id="cellVideos" runat="server">
                    <asp:HyperLink ID="lnkVideos" runat="server" Text="Videos" meta:resourcekey="lnkVideosResource1"></asp:HyperLink>
                </li>
                <li><asp:HyperLink ID="lnkBlog" runat="server" Text="Blog" Target="_blank" meta:resourcekey="lnkBlogResource1"></asp:HyperLink></li>
            </ul>
        </div>
        <div class="footerItem">
            <ul>
                <li><asp:HyperLink ID="lnkPDA" NavigateUrl="~/DefaultMini.aspx" runat="server" 
                Text="Mobile Access" meta:resourcekey="lnkPDAResource1"></asp:HyperLink></li>
            </ul>
            <div style="margin-top:3px; margin-bottom:6px;" ID="divSSLSeal" runat="server"><span id="siteseal"><script async src="https://seal.godaddy.com/getSeal?sealID=MbSEyzG679EfYseNolHmMeTjb9SSTum9qZBBZbXTqqb8vBPbEJNZtpY0EX4b"></script></span></div>        </div>
        <div class="footerItem">
            <ul>
                <li id="cellFacebook" runat="server">
                    <asp:HyperLink ID="lnkFacebook" Target="_blank" runat="server">
                        <div style="display:inline-block; width: 40px; text-align:center;"><asp:Image ID="imgFacebook" runat="server"
                        ImageUrl="~/images/facebookicon.gif" AlternateText="Facebook" 
                        ToolTip="Facebook" meta:resourcekey="imgFacebookResource1" /></div>
                        <asp:Label ID="lblFollowFacebook" runat="server"></asp:Label>
                    </asp:HyperLink>
                </li>
                <li id="cellTwitter" runat="server">
                    <asp:HyperLink ID="lnkTwitter" runat="server" Target="_blank">
                        <div style="display:inline-block; width: 40px; text-align:center;"><asp:Image ID="imgTwitter" runat="server" Height="16px" Width="16px" 
                            ImageUrl="~/images/twitter20x20.png" AlternateText="Twitter" 
                            ToolTip="Twitter" meta:resourcekey="imgTwitterResource1" /></div>
                        <asp:Label ID="lblFollowTwitter" runat="server"></asp:Label>
                    </asp:HyperLink>
                </li>
                <li><uc1:RSSCurrency ID="RSSCurrency1" runat="server" /></li>
            </ul>
        </div>
    </asp:Panel>
    <asp:Panel ID="pnlMobile" runat="server" Visible="False" 
            meta:resourcekey="pnlMobileResource1">
        <br />
        <asp:Label ID="lblViewOptions" runat="server" Text="View in" 
            meta:resourcekey="lblViewOptionsResource1"></asp:Label>
        <asp:HyperLink ID="lnkViewClassic" NavigateUrl="~/Default.aspx?m=no" 
            runat="server" Text="Classic View" meta:resourcekey="lnkViewClassicResource1"></asp:HyperLink>
    </asp:Panel>
    <div><asp:Label ID="lblCopyright" runat="server" meta:resourcekey="lblCopyrightResource1"></asp:Label></div>
</div>
