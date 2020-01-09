<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Secure_oAuthAuthorize" culture="auto" meta:resourcekey="PageResource1" Codebehind="oAuthAuthorize.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblAuthorize" runat="server" Text="Authorize application" meta:resourcekey="lblAuthorizeResource1"></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvAuthorize" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwOK" runat="server">
            <p>
                <asp:Label ID="lblAuthPrompt" runat="server" Text="The application below has requested access to your data.  Do you want to give it access?" meta:resourcekey="lblAuthPromptResource1"></asp:Label></p>
            <p><asp:Label ID="lblClientName" runat="server" Font-Bold="True" meta:resourcekey="lblClientNameResource1"></asp:Label></p>
            <p><% = Resources.oAuth.oAuthRequestedPermissions %></p>
            <asp:MultiView ID="mvScopesRequested" runat="server">
                <asp:View ID="vwNoScopes" runat="server">
                    <p class="error">
                        <%  =Resources.oAuth.oAuthNoScopesDefined %>
                    </p>
                </asp:View>
                <asp:View ID="vwRequestedScopes" runat="server">
                    <ul>
                        <asp:Repeater ID="rptPermissions" runat="server">
                            <ItemTemplate>
                                <li><%# Container.DataItem %></li>
                            </ItemTemplate>
                        </asp:Repeater>
                    </ul>
                </asp:View>
            </asp:MultiView>
            <p>
            <asp:Button ID="btnYes" runat="server" Text="Yes" OnClick="btnYes_Click" meta:resourcekey="btnYesResource1" />&nbsp;&nbsp;<asp:Button ID="btnNo" runat="server" Text="No" OnClick="btnNo_Click" meta:resourcekey="btnNoResource1" /></p>   
        </asp:View>
        <asp:View ID="vwErr" runat="server">
            <asp:Label ID="lblErr" runat="server" CssClass="error"></asp:Label>
        </asp:View>
    </asp:MultiView>
</asp:Content>

