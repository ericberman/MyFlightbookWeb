<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="ResetPass.aspx.cs" Inherits="MyFlightbook.LogonPages.ResetPass" Title="MyFlightbook: Reset Password" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/TwoFactorAuthVerifyCode.ascx" TagPrefix="uc1" TagName="TwoFactorAuthVerifyCode" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Label ID="lblResetPWMain" runat="server" Text="Reset Password" meta:resourcekey="lblResetPWMainResource1"></asp:Label></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvResetPass" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwEmail" runat="server">
            <asp:Panel ID="pnlEmail" runat="server" DefaultButton="btnSendEmail" meta:resourcekey="pnlEmailResource1">
                <div><asp:Label ID="lblEmailPrompt" runat="server" AssociatedControlID="txtEmail" Text="Please enter the e-mail address associated with your account:" meta:resourcekey="lblEmailPromptResource1"></asp:Label></div>
                <div>
                    <asp:TextBox ID="txtEmail" ValidationGroup="resetPassEmail" runat="server" Width="200px" meta:resourcekey="txtEmailResource1"></asp:TextBox>&nbsp;<asp:Button ID="btnSendEmail" ValidationGroup="resetPassEmail" runat="server" Text="Send E-mail" OnClick="btnSendEmail_Click" meta:resourcekey="btnSendEmailResource1" />
                    &nbsp;<asp:RequiredFieldValidator ID="RequiredFieldValidator1" ValidationGroup="resetPassEmail" runat="server" CssClass="error" Display="Dynamic"
                        ControlToValidate="txtEmail" ErrorMessage="<%$ Resources:LocalizedText, SignInValEmailRequired %>" meta:resourcekey="RequiredFieldValidator1Resource1"></asp:RequiredFieldValidator>
                    &nbsp;<asp:RegularExpressionValidator ValidationGroup="resetPassEmail" ID="RegularExpressionValidator2" runat="server" ControlToValidate="txtEmail" ErrorMessage="<%$ Resources:LocalizedText, SignInValBadEmail %>" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" CssClass="error" Display="Dynamic" meta:resourcekey="RegularExpressionValidator2Resource1"></asp:RegularExpressionValidator>
                </div>
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwEmailSent" runat="server">
            <h2><asp:Label ID="lblEmailSentHeader" runat="server" Text="Reset Password: E-mail sent." meta:resourcekey="lblEmailSentHeaderResource1"></asp:Label></h2>
            <asp:Label ID="lblEmailSent" runat="server" meta:resourcekey="lblEmailSentResource1"></asp:Label>
            <p><asp:Label ID="lblNotReceived" runat="server" Text="If you don't receive it momentarily..." meta:resourcekey="lblNotReceivedResource1"></asp:Label></p>
            <ul>
                <li><asp:Label ID="lblNoAccount" runat="server" Text="Verify that the email address you used is one for which an account exists.  For security, we don't tell you whether or not an account exists." meta:resourcekey="lblNoAccountResource1"></asp:Label></li>
                <li><asp:Label ID="lblJunk" runat="server" Text="Check your junk mail folder" meta:resourcekey="lblJunkResource1"></asp:Label></li>
                <li><asp:Hyperlink ID="lnkContactUs" runat="server" Text="Contact Us" NavigateUrl="~/mvc/pub/contact" meta:resourcekey="lnkContactUsResource1"></asp:Hyperlink></li>
            </ul>
        </asp:View>
        <asp:View ID="vwVerifyTFAPass" runat="server">
            <p><asp:Label ID="lblTFAReq" runat="server" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
            <p><asp:Label ID="lblUseApp" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
            <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaReset" OnTFACodeFailed="tfaReset_TFACodeFailed" OnTFACodeVerified="tfaReset_TFACodeVerified" />
            <div><asp:Label ID="lblTFAReset" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false"></asp:Label></div>
        </asp:View>
        <asp:View ID="vwVerify" runat="server">
            <h2><asp:Label ID="lblVerify" runat="server" Text="Reset Password: verify your identity" meta:resourcekey="lblVerifyResource1"></asp:Label></h2>
            <p><asp:Label ID="lblPleaseAnswer" runat="server" Text="Please provide the answer to the following question, which you provided when you created your account" meta:resourcekey="lblPleaseAnswerResource1"></asp:Label></p>
            <p><asp:Label ID="lblNoteExact" Font-Bold="True" runat="server" Text="<%$ Resources:LocalizedText, Note %>" meta:resourcekey="lblNoteExactResource1"></asp:Label> <asp:Label ID="lblExactPrompt" runat="server" Text="The answer must match EXACTLY, including capitalization, punctuation, and spaces." meta:resourcekey="lblExactPromptResource1"></asp:Label></p>
            <p>
                <asp:Label ID="lblQuestionPrompt" runat="server" Text="Your question:" meta:resourcekey="lblQuestionPromptResource1"></asp:Label>
            </p>
            <p><asp:Label ID="lblQuestion" runat="server" Font-Bold="True" meta:resourcekey="lblQuestionResource1"></asp:Label></p>
            <asp:Panel ID="pnlAnswer" runat="server" DefaultButton="btnSubmitAnswer">
                <p><asp:TextBox ID="txtAnswer" runat="server" ValidationGroup="vgAnswer" meta:resourcekey="txtAnswerResource1" ></asp:TextBox> &nbsp;<asp:Button ValidationGroup="vgAnswer" ID="btnSubmitAnswer" runat="server" Text="Next" OnClick="btnSubmitAnswer_Click" meta:resourcekey="btnSubmitAnswerResource1" />
                    &nbsp;<asp:RequiredFieldValidator ID="RequiredFieldValidator2" ValidationGroup="vgAnswer" runat="server" CssClass="error" Display="Dynamic" ControlToValidate="txtAnswer" ErrorMessage="<%$ Resources:LocalizedText, ResetPasswordAnswerRequired %>" meta:resourcekey="RequiredFieldValidator2Resource1"></asp:RequiredFieldValidator>
                </p>
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwNewPass" runat="server">
            <h2><asp:Label ID="lblChangePassword" runat="server" Text="Reset Password: Choose a new password" meta:resourcekey="lblChangePasswordResource1"></asp:Label></h2>
            <asp:Panel ID="pnlNewPass" runat="server" DefaultButton="btnUpdatePass">
                <table style="padding:2px;">
                    <tr>
                        <td>
                            <asp:Label ID="lblNewPass" runat="server" AssociatedControlID="txtNewPass" Text="New Password" meta:resourcekey="lblNewPassResource1"></asp:Label>
                        </td>
                        <td>
                            <asp:TextBox ID="txtNewPass" runat="server" TextMode="Password" ValidationGroup="valPassword" meta:resourcekey="txtNewPassResource1"></asp:TextBox>
                            <ajaxToolkit:PasswordStrength ID="PasswordStrength2" runat="server" BehaviorID="PasswordStrength2" TargetControlID="txtNewPass" TextStrengthDescriptions="<%$ Resources:LocalizedText, PasswordStrengthStrings %>" StrengthIndicatorType="BarIndicator"
                                                    StrengthStyles="pwWeak;pwOK;pwGood;pwExcellent" PreferredPasswordLength="10" BarBorderCssClass="pwBorder" />
                            &nbsp;<asp:RequiredFieldValidator ID="valPassRequired" runat="server" ControlToValidate="txtNewPass" CssClass="error" Display="Dynamic" ErrorMessage="Please provide a new password" ValidationGroup="valPassword" meta:resourcekey="valPassRequiredResource1"></asp:RequiredFieldValidator>
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:Label ID="lblConfirmPass" runat="server" AssociatedControlID="txtConfirmPass" Text="Confirm New Password" meta:resourcekey="lblConfirmPassResource1"></asp:Label>
                        </td>
                        <td>
                            <asp:TextBox ID="txtConfirmPass" runat="server" TextMode="Password" ValidationGroup="valPassword" meta:resourcekey="txtConfirmPassResource1"></asp:TextBox>
                            &nbsp;<asp:CompareValidator ID="NewPasswordCompare" runat="server" ValidationGroup="valPassword" ControlToCompare="txtNewPass" ControlToValidate="txtConfirmPass" CssClass="error" Display="Dynamic" ErrorMessage="The two passwords must match." meta:resourcekey="NewPasswordCompareResource1"></asp:CompareValidator>
                            &nbsp;<asp:RequiredFieldValidator ID="valConfirmRequired" runat="server" ValidationGroup="valPassword" ControlToValidate="txtConfirmPass" CssClass="error" Display="Dynamic" ErrorMessage="Please retype your new password.  This reduces the likelihood of a typo." meta:resourcekey="valConfirmRequiredResource1"></asp:RequiredFieldValidator>
                        </td>
                    </tr>
                    <tr>
                        <td>&nbsp;</td>
                        <td>
                            <asp:Button ID="btnUpdatePass" runat="server" ValidationGroup="valPassword" Text="Update Password" OnClick="btnUpdatePass_Click" meta:resourcekey="btnUpdatePassResource1" />
                        </td>
                    </tr>
                </table>
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
    <asp:Label ID="lblErr" EnableViewState="False" CssClass="error" runat="server" meta:resourcekey="lblErrResource1"></asp:Label>
    <div>&nbsp;</div>
</asp:Content>

