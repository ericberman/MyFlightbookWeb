<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbImpersonate" Codebehind="mfbImpersonate.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<asp:Panel ID="pnlImpersonate" runat="server" DefaultButton="btnFindUsers">
    Find User: <asp:TextBox ID="txtImpersonate" runat="server" ></asp:TextBox>
    <asp:Button
        ID="btnFindUsers" runat="server" Text="Find" 
        ValidationGroup="ValidateImpersonate" onclick="btnFindUsers_Click" />
    <asp:GridView ID="gvUsers" runat="server" AutoGenerateColumns="False" 
        EnableModelValidation="True" 
        DataSourceID="sqlUsers" onrowcommand="gvUsers_RowCommand">
        <Columns>
            <asp:BoundField DataField="Username" HeaderText="User Name" />
            <asp:TemplateField HeaderText="Email">
                <ItemTemplate>
                    <%# String.Format(System.Globalization.CultureInfo.CurrentCulture, "{0} {1} &lt;{2}&gt;", Eval("FirstName"), Eval("LastName"), Eval("email")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Actions">
                <ItemTemplate>
                    <asp:Button ID="btnImpersonate" CommandName="Impersonate" runat="server" Text="Impersonate" CommandArgument='<%# Bind("PKID") %>' />
                    <asp:Button ID="btnResetPass" CommandName="ResetPassword" runat="server" Text="Reset Password" CommandArgument='<%# Bind("PKID") %>' />
                    <asp:Button ID="btnDeleteUser" runat="server" Text="Delete User" CommandName="DeleteUser" CommandArgument='<%# Bind("PKID") %>' />
                    <cc1:ConfirmButtonExtender ID="btnDelete_ConfirmButtonExtender" runat="server" 
                        TargetControlID="btnDeleteUser" ConfirmOnFormSubmit="True" ConfirmText="Are you sure you want to DELETE THIS USER?  This action cannot be undone!">
                    </cc1:ConfirmButtonExtender>
                    <asp:Button ID="btnDeleteFlights" runat="server" 
                        Text="Delete only flights for user" CommandName="DeleteFlightsForUser" CommandArgument='<%# Bind("PKID") %>' />
                    <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" 
                        TargetControlID="btnDeleteFlights" ConfirmOnFormSubmit="True" ConfirmText="Are you sure you want to delete the FLIGHTS for this user?  This action cannot be undone!">
                    </cc1:ConfirmButtonExtender>
                    <asp:Button ID="btnSendMessage" runat="server" Text="Send Message" CommandName="SendMessage" CommandArgument='<%# Bind("PKID") %>' />
                    <asp:Button ID="btnEndowClub" runat="server" Text="Endow Club Creation" CommandName="EndowClub" CommandArgument='<%# Bind("PKID") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    <asp:SqlDataSource ID="sqlUsers" runat="server" 
        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
        SelectCommand=""></asp:SqlDataSource>
    <p><asp:Label ID="lblResetErr" runat="server"></asp:Label>&nbsp;
        <asp:Label ID="lblPwdUsername" runat="server" Text="" Visible="false"></asp:Label>
        <asp:Button ID="btnSendInEmail" runat="server" EnableViewState="false" 
            Text="Send as email" Visible="false" onclick="btnSendInEmail_Click" /></p>
    <asp:Panel ID="pnlSendEmail" runat="server" DefaultButton="btnSend" Visible="false" Width="500px">
        To: <br />
        <asp:Label ID="lblRecipient" runat="server" Text=""></asp:Label><br />
        Subject:<br /><asp:TextBox ID="txtSubject" runat="server" Width="100%"></asp:TextBox><br />
        Body:<br /><asp:TextBox ID="txtBody" runat="server" Rows="5" Width="100%" TextMode="MultiLine"></asp:TextBox><br />
        <asp:Button ID="btnSend" runat="server" Text="Send" onclick="btnSend_OnClick" />&nbsp;
        <asp:Button ID="btnCancel" runat="server" Text="Cancel" 
            onclick="btnCancel_onClick" />
    </asp:Panel>
</asp:Panel>