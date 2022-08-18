<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbFooter.ascx.cs" Inherits="MyFlightbook.Controls.mfbFooter" %>
<div class="footerContainer">
    <asp:MultiView ID="mvClassicMobile" runat="server">
        <asp:View ID="vwClassic" runat="server">
            <hr />
            <div class="footerItem">
                <ul>
                    <li><asp:HyperLink ID="lnkAbout" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkPrivacy" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkTerms" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkDevelopers" runat="server"/></li>
                </ul>
            </div>
            <div class="footerItem">
                <ul>
                    <li><asp:HyperLink ID="lnkContact" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkFAQ" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkVideos" runat="server" /></li>
                    <li><asp:HyperLink ID="lnkBlog" runat="server" /></li>
                </ul>
            </div>
            <div class="footerItem">
                <ul><li><asp:HyperLink ID="lnkPDA" runat="server"  /></li></ul>
                <div style="margin-top:3px; margin-bottom:6px;" id="divSSLSeal" runat="server" visible="<%# Request.IsSecureConnection %>">
                    <span id="siteseal"><script async src="https://seal.godaddy.com/getSeal?sealID=MbSEyzG679EfYseNolHmMeTjb9SSTum9qZBBZbXTqqb8vBPbEJNZtpY0EX4b"></script></span>
                </div>        
            </div>
            <div class="footerItem">
                <ul>
                    <li style="padding: 2px;"><asp:HyperLink ID="lnkFacebook" runat="server" /></li>
                    <li style="padding: 2px;"><asp:HyperLink ID="lnkTwitter" runat="server" /></li>
                    <li class="footerItem"><asp:HyperLink ID="lnkRSS" runat="server" /></li>
                </ul>
            </div>
        </asp:View>
        <asp:View ID="vwMobile" runat="server">
            <asp:HyperLink ID="lnkViewClassic" runat="server" />
        </asp:View>
    </asp:MultiView>
    <div><%= String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.LocalizedText.CopyrightDisplay, DateTime.Now.Year) %></div>
</div>
