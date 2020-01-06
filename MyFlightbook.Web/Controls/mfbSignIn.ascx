<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSignIn" Codebehind="mfbSignIn.ascx.cs" %>
<asp:Panel ID="pnlSignIn" runat="server">
    <div style="display:inline-block; padding:5px; vertical-align:top">
        <asp:Login ID="ctlSignIn" runat="server" BorderPadding="4"
            BorderStyle="Solid" BorderWidth="1px" CreateUserText=""
            CreateUserUrl="~/Logon/newuser.aspx" DestinationPageUrl="~/Member/LogbookNew.aspx"
            LoginButtonText=""
            MembershipProvider="MySqlMembershipProvider" PasswordRecoveryText=""
            PasswordRecoveryUrl="~/Logon/ResetPass.aspx" TitleText=""
            OnLoggingIn="OnLoggingIn">
            <TitleTextStyle Font-Bold="True" />
            <InstructionTextStyle Font-Italic="True" ForeColor="Black" />
            <TextBoxStyle />
            <LayoutTemplate>
                <table style="min-width:300px; padding:4px">
                    <tr>
                        <td colspan="2" style="font-weight: bold; text-align:center;" >
                            <asp:Localize ID="locSignIn" runat="server" Text="<%$ Resources:LocalizedText, SignInPrompt %>"></asp:Localize>
                        </td>
                    </tr>
                    <tr runat="server" id="rowUserName" visible="false">
                        <td style="text-align:right;">
                            <asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName" Text="<% Resources:LocalizedText, SignInUserName %>"></asp:Label></td>
                        <td>
                            <asp:TextBox ID="UserName" runat="server" Width="150px"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td style="text-align:right; vertical-align:text-top;">
                            <asp:Label ID="Emaillabel" runat="server" AssociatedControlID="txtEmail" Text="<%$ Resources:LocalizedText, SignInEmailPrompt %>"></asp:Label></td>
                        <td style="vertical-align:text-top;">
                            <asp:TextBox ID="txtEmail" Width="150px"  runat="server" TextMode="Email" AutoCompleteType="Email"></asp:TextBox>
                            <asp:RegularExpressionValidator
                                ID="RegularExpressionValidator1" runat="server" 
                                ErrorMessage="<%$ Resources:LocalizedText, SignInValBadEmail %>" 
                                ValidationGroup="ctl00$Login1" ControlToValidate="txtEmail" CssClass="error" 
                                Display="Dynamic" 
                                ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"></asp:RegularExpressionValidator>
                            <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" CssClass="error" ControlToValidate="txtEmail"
                                ErrorMessage="<%$ Resources:LocalizedText, SignInValEmailRequired %>" 
                                ValidationGroup="ctl00$Login1" 
                                Display="Dynamic"></asp:RequiredFieldValidator>
                        </td>
                    </tr>
                    <tr>
                        <td style="text-align:right; vertical-align:text-top;">
                            <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password" Text="<%$ Resources:LocalizedText, SignInPasswordPrompt %>"></asp:Label></td>
                        <td style="vertical-align:text-top;">
                            <asp:TextBox ID="Password" runat="server" TextMode="Password" 
                                Width="150px"></asp:TextBox>
                            <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password"
                                ErrorMessage="<%$ Resources:LocalizedText, SignInPasswordRequired %>" ValidationGroup="ctl00$Login1" Display="Dynamic"></asp:RequiredFieldValidator>
                            <br />
                            <asp:HyperLink ID="PasswordRecoveryLink" runat="server" NavigateUrl="~/Logon/ResetPass.aspx" Text="<%$ Resources:LocalizedText, SignInForgotPasswordLink %>"></asp:HyperLink>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <asp:CheckBox ID="RememberMe" runat="server" Text="<%$ Resources:LocalizedText, SignInRememberMe %>" />
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2" style="color: red; text-align:center">
                            <asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                            <div style="float:left">
                                <asp:Localize ID="locNewUserPrompt" runat="server" Text="<%$ Resources:Profile, SignInNewUserPrompt %>"></asp:Localize><br />
                                <asp:HyperLink ID="CreateUserLink" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, SignInCreateAccountLink %>"></asp:HyperLink> 
                                <asp:Localize ID="locItIsFree" runat="server" Text="<%$ Resources:LocalizedText, SignInCreateAccountFree %>"></asp:Localize>
                            </div>
                            <div style="float:right">
                                <asp:Button ID="LoginButton" runat="server" CommandName="Login" 
                                    Text="<%$ Resources:LocalizedText, SignInButtonTitle %>" ValidationGroup="ctl00$Login1" />
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td colspan="2">
                                    
                        </td>
                    </tr>
                </table>
            </LayoutTemplate>
        </asp:Login>
    </div>
    <div style="display:inline-block; padding:5px; margin-left: 10px; background-color:#eeeeee; border: 1px solid darkgray; border-radius: 6px; box-shadow: 6px 6px 5px #888888; vertical-align:top; max-width: 300px;">
        <div class="welcomeHeader">
            <% =Branding.ReBrand(Resources.Profile.NewAccountPromo) %>
        </div>
    </div>
</asp:Panel>
