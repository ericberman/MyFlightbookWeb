<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSignIn.ascx.cs" Inherits="MyFlightbook.Web.Controls.mfbSignIn" %>
<%@ Register Src="~/Controls/TwoFactorAuthVerifyCode.ascx" TagPrefix="uc1" TagName="TwoFactorAuthVerifyCode" %>

<asp:Panel ID="pnlSignIn" runat="server">
    <div style="display:inline-block; margin:5px; vertical-align:top">
        <asp:Login ID="ctlSignIn" runat="server"
            CreateUserText=""
            CreateUserUrl="~/Logon/newuser.aspx" DestinationPageUrl="~/mvc/flights"
            LoginButtonText=""
            MembershipProvider="MySqlMembershipProvider" PasswordRecoveryText=""
            PasswordRecoveryUrl="~/Logon/ResetPass.aspx" TitleText="" OnLoggedIn="ctlSignIn_LoggedIn"
            OnLoggingIn="OnLoggingIn">
            <TitleTextStyle Font-Bold="True" />
            <InstructionTextStyle Font-Italic="True" ForeColor="Black" />
            <TextBoxStyle />
            <LayoutTemplate>
                <div style="margin: 5px; width: 300px;">
                    <asp:MultiView ID="mvSignIn" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwSignIn" runat="server">
                        <h2>
                            <asp:Label ID="locSignIn" runat="server" Text="<%$ Resources:LocalizedText, SignInPrompt %>" />
                        </h2>
                        <div><asp:Label ID="Emaillabel" runat="server" AssociatedControlID="txtEmail" Text="<%$ Resources:LocalizedText, SignInEmailPrompt %>" /></div>
                        <div>
                            <asp:TextBox ID="UserName" runat="server" Visible="false" />
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" AutoCompleteType="Email" />
                            <asp:RegularExpressionValidator
                                ID="RegularExpressionValidator1" runat="server" 
                                ErrorMessage="<%$ Resources:LocalizedText, SignInValBadEmail %>" 
                                ValidationGroup="ctl00$Login1" ControlToValidate="txtEmail" CssClass="error" 
                                Display="Dynamic" 
                                ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" />
                            <asp:RequiredFieldValidator ID="UserNameRequired" runat="server" CssClass="error" ControlToValidate="txtEmail"
                                ErrorMessage="<%$ Resources:LocalizedText, SignInValEmailRequired %>" 
                                ValidationGroup="ctl00$Login1" 
                                Display="Dynamic" />
                        </div>
                        <div><asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password" Text="<%$ Resources:LocalizedText, SignInPasswordPrompt %>" /></div>
                        <div>
                            <asp:TextBox ID="Password" runat="server" TextMode="Password" />
                            <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password" CssClass="error"
                            ErrorMessage="<%$ Resources:LocalizedText, SignInPasswordRequired %>" ValidationGroup="ctl00$Login1" Display="Dynamic" />
                        </div>                                                        
                        <div>
                            <asp:HyperLink ID="PasswordRecoveryLink" runat="server" NavigateUrl="~/Logon/ResetPass.aspx" Text="<%$ Resources:LocalizedText, SignInForgotPasswordLink %>" />
                        </div>
                        <div>
                            <asp:CheckBox ID="RememberMe" runat="server" Text="<%$ Resources:LocalizedText, SignInRememberMe %>" />
                        </div>
                        <div style="text-align:right">
                            <asp:Button ID="LoginButton" runat="server" CommandName="Login" Text="<%$ Resources:LocalizedText, SignInButtonTitle %>" ValidationGroup="ctl00$Login1" />
                        </div>
                        <div class="error">
                            <asp:Literal ID="FailureText" runat="server" EnableViewState="False"></asp:Literal>
                        </div>
                        <div style="margin-top: 30px;">
                            <asp:Localize ID="locNewUserPrompt" runat="server" Text="<%$ Resources:Profile, SignInNewUserPrompt %>" />
                            <asp:HyperLink ID="CreateUserLink" runat="server" NavigateUrl="~/Logon/newuser.aspx" Text="<%$ Resources:LocalizedText, SignInCreateAccountLink %>" /> 
                            <asp:Localize ID="locItIsFree" runat="server" Text="<%$ Resources:LocalizedText, SignInCreateAccountFree %>" />
                        </div>
                    </asp:View>
                    <asp:View ID="vwTFA" runat="server">
                        <div style="max-width:300px;">
                            <h3><asp:Localize ID="locTFA" runat="server" Text="<%$ Resources:Profile, TFAIsSetUp %>"></asp:Localize></h3>
                            <p><asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>" /></p>
                            <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfavc" OnTFACodeFailed="TwoFactorAuthVerifyCode_TFACodeFailed" OnTFACodeVerified="TwoFactorAuthVerifyCode_TFACodeVerified" />
                            <div><asp:Label ID="lblCodeResult" runat="server" EnableViewState="false" /></div>
                        </div>
                    </asp:View>
                </asp:MultiView>
                </div>
            </LayoutTemplate>
        </asp:Login>
    </div>
    <div class="welcomeHeader" id="signIn"><% =Branding.ReBrand(Resources.Profile.NewAccountPromo) %></div>
</asp:Panel>
