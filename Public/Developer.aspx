<%@ Page Title="Developers" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Developer.aspx.cs" Inherits="Public_Developer" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Developers
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <p>Interested in integrating your service with MyFlightbook?</p>
    <p>MyFlightbook supports the oAuth2 protocol.  You can learn more about this on the <a href="https://github.com/ericberman/MyFlightbookWeb/wiki">MyFlightbook wiki.</a></p>
    <asp:MultiView ID="mvServices" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwAuthenticated" runat="server">
            <div style="background-color:lightgray; border: 1px solid black; border-radius:5px; padding:5px; margin: 3px;">
                <uc1:Expando runat="server" ID="Expando">
                <Header><span style="font-weight:bold">Add a client</span></Header>
                <Body>
                    <table style="padding:3px;">
                        <tr style="vertical-align:top">
                            <td>Client ID</td>
                            <td>
                                <asp:TextBox ID="txtClient" runat="server" ValidationGroup="newClient"></asp:TextBox>
                                <asp:RequiredFieldValidator ValidationGroup="newClient" ID="valClient" ControlToValidate="txtClient" CssClass="error" Display="Dynamic" runat="server" ErrorMessage="You must choose a unique ID for your client."></asp:RequiredFieldValidator>
                            </td>
                            <td>
                                This is the unique ID for your client.
                            </td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td>Client Secret</td>
                            <td>
                                <asp:TextBox ID="txtSecret" runat="server" ValidationGroup="newClient"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="valSecret" ValidationGroup="newClient" ControlToValidate="txtSecret" CssClass="error" Display="Dynamic" runat="server" ErrorMessage="You must choose a secret for your client."></asp:RequiredFieldValidator>
                            </td>
                            <td>
                                This is the secret for your client.  Keep it safe!!!
                            </td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td>Client Name</td>
                            <td>
                                <asp:TextBox ID="txtName" runat="server" ValidationGroup="newClient"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="valName" ValidationGroup="newClient" ControlToValidate="txtName" CssClass="error" Display="Dynamic" runat="server" ErrorMessage="You must choose a name for your client."></asp:RequiredFieldValidator>
                            </td>
                            <td>
                                This is the name for your client, as displayed to your users.
                            </td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td>Callback URL:</td>
                            <td>
                                https://<asp:TextBox ID="txtCallback" runat="server" ValidationGroup="newClient"></asp:TextBox>
                                <asp:RequiredFieldValidator ID="valCallback" ValidationGroup="newClient" ControlToValidate="txtCallback" CssClass="error" Display="Dynamic" runat="server" ErrorMessage="You must provide a valid callback URL for your client."></asp:RequiredFieldValidator>
                            </td>
                            <td>
                                When authorizing a user, the redirect URL MUST match this.
                            </td>
                        </tr>
                        <tr style="vertical-align:top">
                            <td>Requested Scopes</td>
                            <td>
                                <asp:CheckBoxList ID="cklScopes" runat="server">
                                </asp:CheckBoxList>
                            </td>
                            <td>
                                What sorts of operations might you request for a given user?
                            </td>
                        </tr>
                    </table>
                    <asp:Button ID="btnAddClient" runat="server" Text="Add" OnClick="btnAddClient_Click" />
                    <div><asp:Label ID="lblErr" runat="server" Text="" CssClass="error" EnableViewState="false"></asp:Label></div>
                </Body>
            </uc1:Expando>
            </div>
            <h2>Your oAuth clients:</h2>
            <asp:GridView ID="gvMyServices" runat="server" GridLines="None" CellPadding="3" CellSpacing="3" DataKeyNames="ClientIdentifier" AutoGenerateColumns="false"
                OnRowEditing="gvMyServices_RowEditing" OnRowCancelingEdit="gvMyServices_RowCancelingEdit" OnRowUpdating="gvMyServices_RowUpdating" OnRowCommand="gvMyServices_RowCommand" >
                <EmptyDataTemplate>
                    <div>You have no oAuth clients</div>
                </EmptyDataTemplate>
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <asp:ImageButton ID="imgDelete" runat="server"
                                AlternateText="Delete Client" CommandArgument='<%# Bind("ClientIdentifier") %>'
                                CommandName="_Delete" ImageUrl="~/images/x.gif"
                                ToolTip="Delete Client" />
                            <cc1:ConfirmButtonExtender ID="confirmDeleteDeadline" runat="server"
                                ConfirmOnFormSubmit="True"
                                ConfirmText="Are you sure?  You can NOT undo this action, and it WILL disable the service for any user of your client!"
                                TargetControlID="imgDelete"></cc1:ConfirmButtonExtender>
                        </ItemTemplate>
                        <ItemStyle VerticalAlign="Top" />
                    </asp:TemplateField>
                    <asp:BoundField HeaderText="Client ID" DataField="ClientIdentifier" ReadOnly="true" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField HeaderText="Client Secret" DataField="ClientSecret" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField HeaderText="Client Name" DataField="ClientName" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField HeaderText="Callback URL" DataField="Callback" ItemStyle-VerticalAlign="Top" />
                    <asp:BoundField HeaderText="Scopes" DataField="Scope" ItemStyle-VerticalAlign="Top" />
                    <asp:CommandField ButtonType="Link" ShowEditButton="true" ShowDeleteButton="true" ItemStyle-VerticalAlign="Top" />
                </Columns>
            </asp:GridView>
            <div><asp:Label ID="lblErrGV" runat="server" Text="" CssClass="error" EnableViewState="false"></asp:Label></div>
        </asp:View>
        <asp:View ID="vwGuest" runat="server">
            <div>Sign in to create a client.</div>
        </asp:View>
    </asp:MultiView>
</asp:Content>


