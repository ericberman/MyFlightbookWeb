<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="newuser" Title="Create Account" culture="auto" meta:resourcekey="PageResource1" Codebehind="newuser.aspx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbTandC.ascx" tagname="mfbTandC" tagprefix="uc1" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locCreateAccountPageHeader" runat="server" 
                                Text="Create an account - it's Free!" 
                                meta:resourcekey="locCreateAccountPageHeaderResource1"></asp:Localize>
</asp:Content>
<asp:content id="Content1" contentplaceholderid="cpTopForm" runat="Server">
    <asp:Panel ID="pnlCreateUser" runat="server" 
        meta:resourcekey="pnlCreateUserResource1">
        <asp:CreateUserWizard ID="CreateUserWizard1" runat="server" ContinueDestinationPageUrl="~/Member/LogbookNew.aspx"
            EmailRegularExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
            MembershipProvider="MySqlMembershipProvider" 
            CreateUserButtonText="Create Account" OnCreatedUser="UserCreated"
            OnCreatingUser="CreatingUser" meta:resourcekey="CreateUserWizard1Resource1"
            >
            <WizardSteps>
                <asp:CreateUserWizardStep ID="CreateUserWizardStep1" runat="server" 
                    meta:resourcekey="CreateUserWizardStep1Resource1">
                    <ContentTemplate>
                        <h2><% =Resources.LocalizedText.TermsAndConditionsHeader %></h2>
                        <uc1:mfbTandC ID="mfbTandC1" runat="server"  />
                        <asp:Panel ID="pnlStep1" runat="server" DefaultButton="CreateUser" 
                            meta:resourcekey="pnlStep1Resource1">
                            <table border="0">
                                <tr>
                                    <td align="left" colspan="3">
                                        <h2><asp:Localize ID="locCreateAccountHeader" runat="server" 
                                                Text="Create your account" meta:resourcekey="locCreateAccountHeaderResource1"></asp:Localize></h2>
                                        <h3>
                                            <asp:Localize ID="locName" runat="server" Text="Your name (optional)" meta:resourceKey="locNameResource1"></asp:Localize></h3>
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle"><asp:Localize ID="locFirstName" runat="server" 
                                            Text="First Name:" meta:resourcekey="locFirstNameResource1"></asp:Localize></td>
                                    <td valign="middle"><asp:TextBox ID="txtFirst" runat="server" 
                                            meta:resourcekey="txtFirstResource1"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle"><asp:Localize ID="locLastName" runat="server" 
                                            Text="Last Name:" meta:resourcekey="locLastNameResource1"></asp:Localize></td>
                                    <td valign="middle">
                                        <asp:TextBox ID="txtLast" runat="server" meta:resourcekey="txtLastResource1"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="3">
                                        <h3><asp:Localize ID="locEmailAndPass" runat="server" Text="E-Mail and password" meta:resourceKey="locEmailAndPassResource1"></asp:Localize></h3>
                                        <asp:Localize ID="locWhyEmail" runat="server" Text="Your e-mail address uniquely identifies you, and enables a password reset if you forget it.  Don't worry, we don't share it!" meta:resourceKey="locWhyEmailResource1"></asp:Localize></td>
                                </tr>                            
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="EmailLabel" runat="server" AssociatedControlID="Email" 
                                            Text="E-mail:" meta:resourcekey="EmailLabelResource1"></asp:Label></td>
                                    <td valign="middle">
                                        <asp:TextBox runat="server" ID="Email" TextMode="Email" 
                                            meta:resourcekey="EmailResource1" AutoCompleteType="Email" /> * 
                                        <asp:RequiredFieldValidator ID="EmailRequired" runat="server" ControlToValidate="Email"
                                            ErrorMessage="You must provide a valid e-mail address to use this site." 
                                            Display="Dynamic" ToolTip="E-mail is required." CssClass="error" 
                                            ValidationGroup="CreateUserWizard1" meta:resourcekey="EmailRequiredResource1"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="EmailRegExp" runat="server" ControlToValidate="Email"
                                            Display="Dynamic" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                            ErrorMessage="Please enter a valid e-mail address."
                                            ValidationGroup="CreateUserWizard1"  CssClass="error"
                                            meta:resourcekey="EmailRegExpResource1"></asp:RegularExpressionValidator>                                        
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="Label1" runat="server" AssociatedControlID="txtEmail2" 
                                            Text="Confirm E-mail:" meta:resourcekey="Label1Resource1"></asp:Label></td>
                                    <td valign="middle">
                                        <asp:TextBox runat="server" ID="txtEmail2" TextMode="Email" 
                                            meta:resourcekey="txtEmail2Resource1" AutoCompleteType="Email" /> * 
                                            <asp:CompareValidator ID="valCompareEmail" runat="server"  CssClass="error"
                                            ErrorMessage="Please re-type your e-mail (helps avoid typos)" 
                                            ControlToCompare="Email" ControlToValidate="txtEmail2" Display="Dynamic" 
                                            meta:resourcekey="valCompareEmailResource1"></asp:CompareValidator>
                                        <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtEmail2"
                                            ErrorMessage="You must provide a valid e-mail address to use this site." 
                                            Display="Dynamic" ToolTip="Please re-type your e-mail." 
                                            ValidationGroup="CreateUserWizard1"  CssClass="error"
                                            meta:resourcekey="RequiredFieldValidator1Resource1"></asp:RequiredFieldValidator>
                                    </td>
                                </tr>
                                <tr runat="server" id="rowOldUsername" visible="False">
                                    <td runat="server">&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle" runat="server">
                                        <asp:Label ID="UserNameLabel" runat="server" AssociatedControlID="UserName" Text="User Name:"></asp:Label></td>
                                    <td valign="middle" runat="server">
                                        <asp:TextBox ID="UserName" runat="server"></asp:TextBox>
                                    </td>
                                </tr>
                                <tr valign="bottom">
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="PasswordLabel" runat="server" AssociatedControlID="Password" 
                                            Text="Password:" meta:resourcekey="PasswordLabelResource1"></asp:Label></td>
                                    <td valign="middle">
                                        <asp:TextBox ID="Password" runat="server" TextMode="Password" 
                                            meta:resourcekey="PasswordResource1"></asp:TextBox> * 
                                        <cc1:PasswordStrength ID="PasswordStrength2" runat="server" BehaviorID="PasswordStrength2" TargetControlID="Password" TextStrengthDescriptions="<%$ Resources:LocalizedText, PasswordStrengthStrings %>" StrengthIndicatorType="BarIndicator"
                                                TextStrengthDescriptionStyles="pwWeak;pwOK;pwGood;pwExcellent" PreferredPasswordLength="10" BarBorderCssClass="pwBorder" />
                                        <asp:RequiredFieldValidator ID="PasswordRequired" runat="server" ControlToValidate="Password"
                                            ErrorMessage="You must provide a password" Display="Dynamic"  CssClass="error"
                                            ToolTip="Password is required." ValidationGroup="CreateUserWizard1" 
                                            meta:resourcekey="PasswordRequiredResource1"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" 
                                            ControlToValidate="Password" Display="Dynamic"  CssClass="error"
                                            ErrorMessage="Password must be between 6 and 128 characters" 
                                            ValidationExpression=".{6,128}" ValidationGroup="CreateUserWizard1" 
                                            meta:resourcekey="RegularExpressionValidator1Resource1"></asp:RegularExpressionValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="ConfirmPasswordLabel" runat="server" 
                                            AssociatedControlID="ConfirmPassword" Text="Confirm Password:" 
                                            meta:resourcekey="ConfirmPasswordLabelResource1"></asp:Label></td>
                                    <td valign="middle">
                                        <asp:TextBox ID="ConfirmPassword" runat="server" TextMode="Password" 
                                            meta:resourcekey="ConfirmPasswordResource1"></asp:TextBox> * 
                                        <asp:RequiredFieldValidator ID="ConfirmPasswordRequired" runat="server" ControlToValidate="ConfirmPassword"
                                            ErrorMessage="Please re-type your password." ToolTip="Confirm Password is required."
                                            ValidationGroup="CreateUserWizard1" Display="Dynamic"  CssClass="error"
                                            meta:resourcekey="ConfirmPasswordRequiredResource1"></asp:RequiredFieldValidator>
                                        <asp:CompareValidator ID="PasswordCompare" runat="server" 
                                            ControlToCompare="Password" ControlToValidate="ConfirmPassword" 
                                            Display="Dynamic"  CssClass="error"
                                            ErrorMessage="The Password and Confirmation Password must match." 
                                            ValidationGroup="CreateUserWizard1" 
                                            meta:resourcekey="PasswordCompareResource1"></asp:CompareValidator>
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="3" valign="middle">
                                        <h3><asp:Localize ID="locSecurityQuestion" runat="server" Text="Security Question and Answer" meta:resourceKey="locSecurityQuestionResource1"></asp:Localize></h3>
                                        <% =Resources.LocalizedText.AccountQuestionHint %>
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="QuestionLabel" runat="server" AssociatedControlID="Question" 
                                            Text="Security Question:" meta:resourcekey="QuestionLabelResource1"></asp:Label></td>
                                    <td valign="middle">
                                        <uc2:AccountQuestions ID="Question" runat="server" ValidationGroup="CreateUserWizard1" Required="true" />
                                    </td>
                                </tr>
                                <tr>
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td valign="middle">
                                        <asp:Label ID="AnswerLabel" runat="server" AssociatedControlID="Answer" 
                                            Text="Security Answer:" meta:resourcekey="AnswerLabelResource1"></asp:Label></td>
                                    <td valign="middle">
                                        <asp:TextBox ID="Answer" runat="server" meta:resourcekey="AnswerResource1"></asp:TextBox> * 
                                        <asp:RequiredFieldValidator ID="AnswerRequired" runat="server" ControlToValidate="Answer"
                                             CssClass="error"
                                            ErrorMessage="Please provide an answer to your security question.  You must provide the same answer if you forget your password." ToolTip="Security answer is required."
                                            ValidationGroup="CreateUserWizard1" Display="Dynamic" 
                                            meta:resourcekey="AnswerRequiredResource1"></asp:RequiredFieldValidator>
                                        <asp:RegularExpressionValidator ID="RegularExpressionValidator3" runat="server" 
                                            ControlToValidate="Answer" Display="Dynamic"  CssClass="error"
                                            ErrorMessage="Security Answer must be shorter than 80 characters" 
                                            ValidationExpression=".{0,80}" ValidationGroup="CreateUserWizard1" 
                                            meta:resourcekey="RegularExpressionValidator3Resource1"></asp:RegularExpressionValidator>
                                    </td>
                                </tr>
                                <tr style="display:none">
                                    <td>&nbsp;&nbsp;&nbsp;</td>
                                    <td>&nbsp;</td>
                                    <td>
                                        <asp:Button ID="CreateUser" CommandName="MoveNext" 
                                            ValidationGroup="CreateUserWizard1" runat="server" Text="Button" 
                                            meta:resourcekey="CreateUserResource2" />
                                    </td>
                                </tr>
                            </table>
                        </asp:Panel>
                        <div class="error">
                            <asp:Literal ID="ErrorMessage" runat="server" EnableViewState="False" 
                                meta:resourcekey="ErrorMessageResource1"></asp:Literal>
                            <asp:Panel ID="pnlEmailCollision" runat="server" EnableViewState="False" 
                                Visible="False" meta:resourcekey="pnlEmailCollisionResource1">
                                <asp:Localize ID="locEmailInUse" runat="server" 
                                    Text="That e-mail address is already in use.  If you already have an account, you can " 
                                    meta:resourcekey="locEmailInUseResource1"></asp:Localize>
                                
                                    <asp:HyperLink ID="lnkSignIn" runat="server" 
                                    NavigateUrl="~/Secure/login.aspx" Text="sign in with that e-mail address" 
                                    meta:resourcekey="lnkSignInResource1"></asp:HyperLink>
                                    <asp:Localize ID="locNewUserAccountOption" runat="server" Text=", or " 
                                    meta:resourcekey="locNewUserAccountOptionResource1"></asp:Localize>
                                    <asp:HyperLink ID="lnkReset" runat="server" 
                                    NavigateUrl="~/Logon/ResetPass.aspx" Text="reset your password" 
                                    meta:resourcekey="lnkResetResource1"></asp:HyperLink>.</asp:Panel>
                        </div>
                    </ContentTemplate>
                    <CustomNavigationTemplate>
                        <table border="0" cellspacing="5" style="width:100%;height:100%;">
                            <tr align="center">
                                <td align="left">
                                    <asp:Button ID="StepNextButton" runat="server" CommandName="MoveNext" 
                                        Text="Create Account" ValidationGroup="CreateUserWizard1" 
                                        meta:resourcekey="StepNextButtonResource1" />
                                </td>
                            </tr>
                        </table>
                    </CustomNavigationTemplate>
                </asp:CreateUserWizardStep>
                <asp:CompleteWizardStep runat="server" 
                    meta:resourcekey="CompleteWizardStepResource1">
                    <ContentTemplate>
                    </ContentTemplate>
                </asp:CompleteWizardStep>
            </WizardSteps>
        </asp:CreateUserWizard>
    </asp:Panel>    
</asp:content>
