<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_oAuthAuthorizationManager" Codebehind="oAuthAuthorizationManager.ascx.cs" %>
<p><asp:Localize ID="locOAuthHeader" Text="<%$ Resources:oAuth, oAuthedAppsListHeader %>" runat="server" ></asp:Localize></p>
<asp:GridView ID="gvOAuthClients" DataKeyNames="ClientId" CellPadding="10" CellSpacing="10" OnRowDataBound="gvOAuthClients_RowDataBound" OnRowDeleting="gvOAuthClients_RowDeleting" AutoGenerateColumns="False" runat="server" GridLines="None" ShowHeader="False">
    <Columns>
        <asp:BoundField DataField="AuthorizationId" Visible="False" ItemStyle-VerticalAlign="Top" />
        <asp:BoundField DataField="ClientId" ItemStyle-VerticalAlign="Top"  />
        <asp:TemplateField ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <asp:MultiView ID="mvScopesRequested" runat="server">
                    <asp:View ID="vwNoScopes" runat="server">
                        <%  =Resources.oAuth.oAuthNoScopesDefined %>
                    </asp:View>
                    <asp:View ID="vwRequestedScopes" runat="server">
                            <asp:Repeater ID="rptPermissions" runat="server">
                                <ItemTemplate>
                                    <li><%# Container.DataItem %></li>
                                </ItemTemplate>
                            </asp:Repeater>
                    </asp:View>
                </asp:MultiView>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:CommandField ButtonType="Image" DeleteImageUrl="../images/x.gif" ShowDeleteButton="True" ShowCancelButton="False" InsertVisible="False" ItemStyle-VerticalAlign="Top"  />
    </Columns>
    <EmptyDataTemplate>
        <p><% =Resources.oAuth.oAuthNoApplicationsFound %></p>
    </EmptyDataTemplate>
</asp:GridView>
