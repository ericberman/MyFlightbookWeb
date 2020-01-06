<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbFacebookFan" Codebehind="mfbFacebookFan.ascx.cs" %>
<asp:Panel runat="server" ID="pnlFacebook">
    <div class="fb-page" data-href="<% =Branding.CurrentBrand.FacebookFeed %>" data-tabs="timeline" data-height="600" data-small-header="true" data-adapt-container-width="true" data-hide-cover="true" data-show-facepile="true">
        <blockquote cite="<% =Branding.CurrentBrand.FacebookFeed %>" class="fb-xfbml-parse-ignore"><a href="<% =Branding.CurrentBrand.FacebookFeed %>">MyFlightbook</a></blockquote>
    </div>
</asp:Panel>
