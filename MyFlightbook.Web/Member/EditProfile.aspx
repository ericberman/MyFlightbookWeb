<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master"
    Codebehind="EditProfile.aspx.cs" Inherits="MyFlightbook.MemberPages.Member_EditProfile" Title="Edit Profile" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbEndorsementList.ascx" tagname="mfbEndorsementList" tagprefix="uc6" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc9" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbDeadlines.ascx" TagPrefix="uc1" TagName="mfbDeadlines" %>
<%@ Register Src="~/Controls/mfbCustCurrency.ascx" TagPrefix="uc1" TagName="mfbCustCurrency" %>
<%@ Register Src="~/Controls/oAuthAuthorizationManager.ascx" TagPrefix="uc1" TagName="oAuthAuthorizationManager" %>
<%@ Register Src="~/Controls/mfbSubscriptionManager.ascx" TagPrefix="uc1" TagName="mfbSubscriptionManager" %>
<%@ Register Src="~/Controls/mfbCustomCurrencyList.ascx" TagPrefix="uc1" TagName="mfbCustomCurrencyList" %>
<%@ Register Src="~/Controls/mfbEditPropTemplate.ascx" TagPrefix="uc1" TagName="mfbEditPropTemplate" %>
<%@ Register Src="~/Controls/ClubControls/TimeZone.ascx" TagPrefix="uc1" TagName="TimeZone" %>
<%@ Register Src="~/Controls/mfbShareKeys.ascx" TagPrefix="uc1" TagName="mfbShareKeys" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/Prefs/mfbDonate.ascx" TagPrefix="uc1" TagName="mfbDonate" %>
<%@ Register Src="~/Controls/Prefs/mfbCloudAhoy.ascx" TagPrefix="uc1" TagName="mfbCloudAhoy" %>
<%@ Register Src="~/Controls/Prefs/mfbCloudStorage.ascx" TagPrefix="uc1" TagName="mfbCloudStorage" %>
<%@ Register Src="~/Controls/Prefs/mfbPilotInfo.ascx" TagPrefix="uc1" TagName="mfbPilotInfo" %>
<%@ Register Src="~/Controls/TwoFactorAuth.ascx" TagPrefix="uc1" TagName="FactorAuth" %>
<%@ Register Src="~/Controls/TwoFactorAuthVerifyCode.ascx" TagPrefix="uc1" TagName="TwoFactorAuthVerifyCode" %>
<%@ Register Src="~/Controls/Prefs/mfbFlightColoring.ascx" TagPrefix="uc1" TagName="mfbFlightColoring" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/Prefs/mfbBigRedButtons.ascx" TagPrefix="uc1" TagName="mfbBigRedButtons" %>
<%@ Register Src="~/Controls/Prefs/mfbPropertyBlocklist.ascx" TagPrefix="uc1" TagName="mfbPropertyBlocklist" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <asp:Label ID="lblName" runat="server" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwAccount">
            <h2>
                <asp:Localize ID="locAccountHeader" runat="server" Text="<%$ Resources:Profile, accountHeader %>"></asp:Localize>
            </h2>
            <p><asp:Label ID="lblMemberSince" runat="server"></asp:Label></p>
            <cc1:Accordion ID="accordianAccount" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpName">
                        <Header>
                            <asp:Localize ID="locaHeadName" runat="server" Text="<%$ Resources:Tabs, ProfileName %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <asp:MultiView ID="mvNameEmail" runat="server" ActiveViewIndex="0">
                                <asp:View ID="vwStaticNameEmail" runat="server">
                                    <div><asp:Label Font-Bold="true" ID="lblFullName" runat="server"></asp:Label></div>
                                    <div><asp:Label ID="lblStaticEmail" runat="server"></asp:Label></div>
                                    <div style="white-space:pre-wrap"><asp:Label ID="lblAddress" runat="server"></asp:Label></div>
                                    <div><asp:Button ID="btnEditNameEmail" runat="server" Text="<%$ Resources:Profile, ChangeNameEmail %>" OnClick="btnEditNameEmail_Click" /></div>
                                </asp:View>
                                <asp:View ID="vwVerifyTFAEmail" runat="server">
                                    <p><asp:Label ID="lblChangeEmailTFA" runat="server" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
                                    <p><asp:Label ID="lblChangeEmailUseApp" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
                                    <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaEmail" OnTFACodeFailed="tfaEmail_TFACodeFailed" OnTFACodeVerified="tfaEmail_TFACodeVerified" />
                                    <div><asp:Label ID="lblInvalidTFAEmail" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false"></asp:Label></div>
                                </asp:View>
                                <asp:View ID="vwChangeNameEmail" runat="server">
                                    <asp:Panel ID="pnlNameAndEmail" runat="server" DefaultButton="btnUpdatename">
                                        <table>
                                            <tr>
                                                <td>
                                                    <asp:Localize ID="locEmailPrompt" runat="server" Text="<%$ Resources:Profile, accountEmailPrompt %>" />
                                                    </td>
                                                <td>
                                                    <asp:TextBox runat="server" ID="txtEmail" TextMode="Email"
                                                        AutoCompleteType="Email" ValidationGroup="valNameEmail" />
                                                    <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" 
                                                        ControlToValidate="txtEmail" ValidationGroup="valNameEmail"
                                                        ErrorMessage="<%$ Resources:Profile, errEmailRequired %>" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                                        CssClass="error" SetFocusOnError="True" Display="Dynamic" />
                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"  ValidationGroup="valNameEmail"
                                                    ControlToValidate="txtEmail" CssClass="error" Display="Dynamic" 
                                                    ErrorMessage="<%$ Resources:Profile, errEmailMissing %>" />
                                                    <asp:CustomValidator ID="ValidateEmailOK" runat="server" 
                                                        ErrorMessage="<%$ Resources:Profile, errEmailInUse2 %>" ValidationGroup="valNameEmail"
                                                        ControlToValidate="txtEmail" CssClass="error" Display="Dynamic" 
                                                        OnServerValidate="VerifyEmailAvailable" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td><asp:Localize ID="locRetypeEmailPrompt" runat="server" Text="<%$ Resources:Profile, accountRetypeEmailPrompt %>" />
                                                    </td>
                                                <td>
                                                    <asp:TextBox runat="server" ID="txtEmail2" TextMode="Email"  
                                                        AutoCompleteType="Email" ValidationGroup="valNameEmail"  />
                                                    <asp:CompareValidator ID="valCompareEmail" ControlToValidate="txtEmail2" 
                                                        ControlToCompare="txtEmail" ValidationGroup="valNameEmail"
                                                        Display="Dynamic" runat="server" CssClass="error"
                                                        ErrorMessage="<%$ Resources:Profile, err2ndEmailRequired %>" />
                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator2" 
                                                        ControlToValidate="txtEmail2" Display="Dynamic" runat="server" 
                                                        ValidationGroup="valNameEmail" CssClass="error"
                                                        ErrorMessage="<%$ Resources:Profile, err2ndEmailRequired %>" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td><asp:Localize ID="locFirstNamePrompt" runat="server" Text="<%$ Resources:Profile, accountFirstNamePrompt %>" />
                                                    </td>
                                                <td>
                                                    <script type="text/javascript">
                                                        function updateGreeting(sender) {
                                                            $find('wmeGreet').set_watermarkText(sender.value);
                                                        }
                                                    </script>
                                                    <asp:TextBox ID="txtFirst" runat="server" AutoCompleteType="FirstName" Wrap="False"
                                                        ValidationGroup="valNameEmail" /></td>
                                            </tr>
                                            <tr>
                                                <td><asp:Localize ID="locLastNamePrompt" runat="server" Text="<%$ Resources:Profile, accountLastNamePrompt %>" />
                                                    </td>
                                                <td>
                                                    <asp:TextBox ID="txtLast" runat="server" AutoCompleteType="LastName" 
                                                        Wrap="False" ValidationGroup="valNameEmail" /></td>
                                            </tr>
                                            <tr style="vertical-align:top;">
                                                <td>
                                                    <asp:Localize ID="locPreferredGreeting" runat="server" Text=" <%$Resources:Profile, accountPreferredGreetingPrompt %>" /><br />
                                                    <span class="fineprint">
                                                        <asp:Localize ID="locPrefGreetingNote" runat="server" Text="<%$ Resources:Profile, accountPreferredGreetingNote %>" /></span>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txtPreferredGreeting" ValidationGroup="valNameEmail" AutoCompleteType="DisplayName" runat="server"></asp:TextBox>
                                                    <cc1:TextBoxWatermarkExtender ID="wmeGreeting" TargetControlID="txtPreferredGreeting" WatermarkCssClass="watermark" BehaviorID="wmeGreet" runat="server" />
                                                </td>
                                            </tr>
                                            <tr><td>&nbsp;</td><td>&nbsp;</td></tr>
                                            <tr>
                                                <td style="vertical-align: text-top">
                                                    <asp:Localize ID="locAddress" runat="server" Text="<%$ Resources:Profile, accountMailingAddressPrompt %>" />
                                                    <br />
                                                    <asp:Label ID="lblAddressFinePrint" runat="server" CssClass="fineprint" 
                                                        Text="<%$ Resources:Profile, accountMailingAddressPromptNote %>" />
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txtAddress" TextMode="MultiLine" Rows="4" runat="server" ValidationGroup="valPilotInfo" Width="300px"></asp:TextBox>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align: text-top; max-width: 200px">
                                                    <asp:Localize ID="locDOB" runat="server" Text="<%$ Resources:Profile, accountDateOfBirth %>" />
                                                    <br />
                                                    <asp:Label ID="lblDOBNote" runat="server" CssClass="fineprint" 
                                                        Text="<%$ Resources:Profile, accountDateOfBirthPromptNote %>" />
                                                </td>
                                                <td>
                                                    <uc1:mfbTypeInDate runat="server" ID="dateDOB" DefaultType="None" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align:text-top; max-width: 200px">
                                                    <div><asp:Localize ID="locCell" runat="server" Text="<%$ Resources:Profile, accountCellPhone %>" /></div>
                                                    <div class="fineprint"><% =Resources.Profile.accountCellPhoneHint %></div>
                                                </td>
                                                <td>
                                                    <asp:TextBox ID="txtCell" runat="server" ValidationGroup="valPilotInfo" Width="300px" TextMode="Phone" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align:text-top; max-width: 200px">
                                                    <div><asp:Localize ID="locHeadShot" runat="server" Text="<%$ Resources:Profile, accountHeadShot %>" /></div>
                                                    <div class="fineprint"><asp:Localize ID="locHeadShtHint" runat="server" Text="<%$ Resources:Profile, accountHeadShotHint %>" /></div>
                                                </td>
                                                <td>
                                                    <div>
                                                        <img runat="server" class="roundedImg" id="imgHdSht" style="width:40px; height:40px;" src="~/Public/tabimages/ProfileTab.png" />&nbsp;<asp:ImageButton ID="imgDelHdSht" style="vertical-align:middle;" ImageUrl="~/images/x.gif" OnClick="imgDelHdSht_Click" runat="server" />
                                                        <asp:FileUpload ID="fuHdSht" runat="server" />
                                                    </div>
                                                    <div>
                                                        <asp:Image ID="afuThrb" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
                                                        <script>
                                                            function hdshtUpdated() {
                                                                document.getElementById('<% =afuThrb.ClientID %>').style.display = "block";
                                                                document.getElementById('<% =btnUpdHdSht.ClientID %>').click();
                                                            }
                                                        </script>
                                                    </div>
                                                    <asp:Button ID="btnUpdHdSht" runat="server" Text="(Refresh)" OnClick="btnUpdHdSht_Click" style="display:none" />
                                                </td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    &nbsp;</td>
                                                <td>
                                                    <asp:Button ID="btnUpdatename" runat="server" Text="<%$ Resources:Profile, saveChanges %>" 
                                                        ValidationGroup="valNameEmail" onclick="btnUpdatename_Click" />
                                                    <br />
                                                    <asp:Label ID="lblNameUpdated" runat="server" CssClass="success" EnableViewState="False"
                                                        Text="<%$ Resources:Profile, accountPersonalInfoSuccess %>" Visible="False" />
                                                </td>
                                            </tr>
                                        </table>
                                    </asp:Panel>
                                </asp:View>
                            </asp:MultiView>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpPassword" runat="server">
                        <Header>
                            <asp:Localize ID="locHeadPass" runat="server" Text="<%$ Resources:Tabs, ProfilePassword %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <asp:UpdatePanel ID="updPass" runat="server">
                                <ContentTemplate>
                                    <asp:MultiView ID="mvChangePass" runat="server" ActiveViewIndex="0">
                                        <asp:View ID="vwStaticPass" runat="server">
                                            <ul>
                                                <li><asp:Label ID="lblLastLogin" runat="server"></asp:Label></li>
                                                <li runat="server" id="itemLastActivity"><asp:Label ID="lblLastActivity" runat="server"></asp:Label></li>
                                                <li><asp:Label ID="lblPasswordStatus" runat="server"></asp:Label></li>
                                            </ul>
                                            <div><asp:Button ID="btnChangePass" runat="server" Text="<%$ Resources:Profile, ChangePassword %>" OnClick="btnChangePass_Click" /></div>
                                        </asp:View>
                                        <asp:View ID="vwVerifyTFAPass" runat="server">
                                            <p><asp:Label ID="lblTFAReq" runat="server" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
                                            <p><asp:Label ID="lblUseApp" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
                                            <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaChangePass" OnTFACodeFailed="tfaChangePass_TFACodeFailed" OnTFACodeVerified="tfaChangePass_TFACodeVerified" />
                                            <div><asp:Label ID="lblTFACheckPass" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false"></asp:Label></div>
                                        </asp:View>
                                        <asp:View ID="vwChangePass" runat="server">
                                            <asp:Panel ID="pnlPassword" runat="server" DefaultButton="btnUpdatePass">
                                                <table>
                                                    <tr>
                                                        <td>
                                                            <asp:Label ID="CurrentPasswordLabel" runat="server" AssociatedControlID="CurrentPassword" meta:resourceKey="CurrentPasswordLabelResource1" Text="Current Password"></asp:Label>
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="CurrentPassword" runat="server" meta:resourceKey="CurrentPasswordResource1" TextMode="Password" ValidationGroup="valPassword"></asp:TextBox>
                                                            <asp:CustomValidator ID="valCurrentPasswordRequired" runat="server" CssClass="error" Display="Dynamic" ErrorMessage="To change your password, you must first correctly provide your current password" meta:resourceKey="valCurrentPasswordRequiredResource1" OnServerValidate="ValidateCurrentPassword" ValidationGroup="valPassword"></asp:CustomValidator>
                                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator5" runat="server" ControlToValidate="CurrentPassword" CssClass="error" Display="Dynamic" ErrorMessage="To change your password you must first correctly provide your current password." meta:resourceKey="RequiredFieldValidator5Resource1" ValidationGroup="valPassword"></asp:RequiredFieldValidator>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Label ID="NewPasswordLabel" runat="server" AssociatedControlID="NewPassword" meta:resourceKey="NewPasswordLabelResource1" Text="New Password"></asp:Label>
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="NewPassword" runat="server" meta:resourceKey="NewPasswordResource1" TextMode="Password" ValidationGroup="valPassword"></asp:TextBox>
                                                            <cc1:PasswordStrength ID="PasswordStrength2" runat="server" BehaviorID="PasswordStrength2" TargetControlID="NewPassword" TextStrengthDescriptions="<%$ Resources:LocalizedText, PasswordStrengthStrings %>" StrengthIndicatorType="BarIndicator"
                                                                StrengthStyles="pwWeak;pwOK;pwGood;pwExcellent" PreferredPasswordLength="10" BarBorderCssClass="pwBorder" />
                                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator6" runat="server" ControlToValidate="NewPassword" CssClass="error" Display="Dynamic" ErrorMessage="Please provide a new password" meta:resourceKey="RequiredFieldValidator6Resource1" ValidationGroup="valPassword"></asp:RequiredFieldValidator>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Label ID="ConfirmNewPasswordLabel" runat="server" AssociatedControlID="ConfirmNewPassword" meta:resourceKey="ConfirmNewPasswordLabelResource1" Text="Confirm New Password"></asp:Label>
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="ConfirmNewPassword" runat="server" meta:resourceKey="ConfirmNewPasswordResource1" TextMode="Password" ValidationGroup="valPassword"></asp:TextBox>
                                                            <asp:CompareValidator ID="NewPasswordCompare" runat="server" ControlToCompare="NewPassword" ControlToValidate="ConfirmNewPassword" CssClass="error" Display="Dynamic" ErrorMessage="The Confirm New Password must match the New Password entry." meta:resourceKey="NewPasswordCompareResource1" ValidationGroup="valPassword"></asp:CompareValidator>
                                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator7" runat="server" ControlToValidate="ConfirmNewPassword" CssClass="error" Display="Dynamic" ErrorMessage="Please retype your new password.  This reduces the likelihood of a typo." meta:resourceKey="RequiredFieldValidator7Resource1" ValidationGroup="valPassword"></asp:RequiredFieldValidator>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>&nbsp;</td>
                                                        <td>
                                                            <asp:Button ID="btnUpdatePass" runat="server" meta:resourceKey="btnUpdatePassResource1" OnClick="btnUpdatePass_Click" Text="Change Password" ValidationGroup="valPassword" />
                                                            <br />
                                                            <asp:Label ID="lblPassChanged" runat="server" CssClass="success" EnableViewState="False" meta:resourceKey="lblPassChangedResource1" Text="Password successfully changed" Visible="False"></asp:Label>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </asp:Panel>
                                        </asp:View>
                                    </asp:MultiView>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpQandA" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpQandAResource1">
                        <Header>
                            <asp:Localize ID="locHeadQA" runat="server" Text="<%$ Resources:Tabs, ProfileQA %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlQandA" runat="server" DefaultButton="btnChangeQA" meta:resourceKey="pnlQandAResource1">
                                <asp:UpdatePanel ID="updQA" runat="server">
                                    <ContentTemplate>
                                        <% =Resources.LocalizedText.AccountQuestionHint %>
                                        <asp:MultiView ID="mvQA" runat="server" ActiveViewIndex="0">
                                            <asp:View ID="vwStaticQA" runat="server">
                                                <div><asp:Button ID="btnChangeQA" runat="server" Text="<%$ Resources:Profile, ChangeQA %>" OnClick="btnChangeQA_Click1" /></div>
                                            </asp:View>
                                            <asp:View ID="vwVerifyTFAQA" runat="server">
                                                <p><asp:Label ID="lblTFA2" runat="server" Text="<%$ Resources:Profile, TFARequired %>"></asp:Label></p>
                                                <p><asp:Label ID="lblTFAUseApp2" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>"></asp:Label></p>
                                                <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaChangeQA" OnTFACodeFailed="tfaChangeQA_TFACodeFailed" OnTFACodeVerified="tfaChangeQA_TFACodeVerified" />
                                                <div><asp:Label ID="lblTFAErrQA" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false"></asp:Label></div>
                                            </asp:View>
                                            <asp:View ID="vwChangeQA" runat="server">
                                                <table>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locPasswordPromptForQA" runat="server" meta:resourceKey="locPasswordPromptForQAResource1" Text="Password"></asp:Localize>
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="txtPassQA" runat="server" meta:resourceKey="txtPassQAResource1" TextMode="Password" ValidationGroup="vgNewQA"></asp:TextBox>
                                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator8" runat="server" ControlToValidate="txtPassQA" CssClass="error" Display="Dynamic" ErrorMessage="Please type your password" meta:resourceKey="RequiredFieldValidator8Resource1" ValidationGroup="vgNewQA"></asp:RequiredFieldValidator>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locCurrentQuestion" runat="server" meta:resourceKey="locCurrentQuestionResource1" Text="Current question: "></asp:Localize>
                                                        </td>
                                                        <td>
                                                            <asp:Label ID="lblQuestion" runat="server" Font-Bold="True" meta:resourceKey="lblQuestionResource1"></asp:Label>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locQuestionPrompt" runat="server" meta:resourceKey="locQuestionPromptResource1" Text="New Security Question"></asp:Localize>
                                                        </td>
                                                        <td>
                                                            <uc4:AccountQuestions ID="txtQuestion" ValidationGroup="vgNewQA" runat="server" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locNewAnswer" runat="server" meta:resourceKey="locNewAnswerResource1" Text="New Security Answer"></asp:Localize>
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="txtAnswer" runat="server" meta:resourceKey="txtAnswerResource1" ValidationGroup="vgNewQA"></asp:TextBox>
                                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator10" runat="server" ControlToValidate="txtAnswer" CssClass="error" Display="Dynamic" ErrorMessage="Please type an answer for your question" meta:resourceKey="RequiredFieldValidator10Resource1" ValidationGroup="vgNewQA"></asp:RequiredFieldValidator>
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>&nbsp;</td>
                                                        <td>
                                                            <asp:Button ID="Button1" runat="server" meta:resourceKey="btnChangeQAResource1" OnClick="btnChangeQA_Click" Text="Change Security Question" ValidationGroup="vgNewQA" />
                                                            <br />
                                                            <asp:Label ID="lblQAChangeSuccess" runat="server" CssClass="success" EnableViewState="False" meta:resourceKey="lblQAChangeSuccessResource1" Text="Security question and answer successfully changed" Visible="False"></asp:Label>
                                                        </td>
                                                    </tr>
                                                </table>
                                            </asp:View>
                                        </asp:MultiView>
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acp2fa" runat="server">
                        <Header>
                            <asp:Localize ID="loc2fa" Text="<%$ Resources:Profile, TFAHeader %>" runat="server"></asp:Localize>
                        </Header>
                        <Content>
                            <asp:UpdatePanel ID="UpdatePanel4" runat="server">
                                <ContentTemplate>
                                    <uc1:FactorAuth runat="server" id="TwoFactorAuth" />
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpDeletion" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Localize ID="locHeadDeletion" runat="server" Text="<%$ Resources:Profile, ProfileDeleteHeader %>"></asp:Localize>
                        </Header>
                        <Content>
                            <uc1:mfbBigRedButtons runat="server" id="mfbBigRedButtons" />
                        </Content>
                    </cc1:AccordionPane>
                </Panes>
            </cc1:Accordion>
        </asp:View>
        <asp:View runat="server" ID="vwPrefs">
            <h2>
                <asp:Localize ID="locPrefsheader" runat="server" 
                    Text="Features and preferences" 
                    meta:resourcekey="locPrefsheaderResource1"></asp:Localize>
            </h2>
            <cc1:Accordion ID="accordianPrefs" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" meta:resourcekey="accordianPrefsResource1" TransitionDuration="250">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpLocalPrefs" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpLocalPrefsResource1">
                        <Header>
                            <asp:Localize ID="locFlightTimes" runat="server" Text="Flight Entry" 
                                meta:resourcekey="locFlightTimesResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <table>
                                    <tr>
                                        <td colspan="2"><asp:Label ID="lblPrefTimes" runat="server" Font-Bold="True" Text="<%$ Resources:Preferences, DecimalPrefPrompt %>" /></td>
                                        <td style="font-size:smaller; font-style:italic;">&nbsp;<asp:Localize ID="locSamp1" runat="server" Text="<%$ Resources:Preferences, DecimalPrefSample1 %>" /></td>
                                        <td style="font-size:smaller; font-style:italic;">&nbsp;<asp:Localize ID="locSamp2" runat="server" Text="<%$ Resources:Preferences, DecimalPrefSample2 %>" /></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimalAdaptive" runat="server" GroupName="decimalPref" /></td>
                                        <td><asp:Label ID="locAdapt" runat="server" Text="<%$ Resources:Preferences, DecimalPrefAdaptive %>" AssociatedControlID="rbDecimalAdaptive" /></td>
                                        <td style="text-align:center"><% =(70.0M / 60.0M).ToString("#,##0.0#", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.0#", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimal1" runat="server" GroupName="decimalPref" /></td>
                                        <td><asp:Label ID="lbl1Dec" runat="server" Text="<%$ Resources:Preferences, DecimalPref1Decimal %>" AssociatedControlID="rbDecimal1" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).ToString("#,##0.0", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.0", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimal2" runat="server" GroupName="decimalPref" /></td>
                                        <td><asp:Label ID="lbl2Dec" runat="server" Text="<%$ Resources:Preferences, DecimalPref2Decimal %>" AssociatedControlID="rbDecimal2" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).ToString("#,##0.00", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.00", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimalHHMM" runat="server" GroupName="decimalPref" /></td>
                                        <td><asp:Label ID="lblHHMM" runat="server" Text="<%$ Resources:Preferences, DecimalPrefHHMM %>" AssociatedControlID="rbDecimalHHMM" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).FormatDecimal(true) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).FormatDecimal(true) %></td>
                                    </tr>
                                </table>
                                <div><asp:Label ID="lblPrefTimeZone" runat="server" Text="Preferred time zone:" Font-Bold="true" meta:resourcekey="lblPrefTimeZoneResource1"></asp:Label></div>
                                <div>&nbsp;&nbsp;<asp:Label ID="lblPrefTimeZoneExplanation" CssClass="fineprint" runat="server" meta:resourcekey="lblPrefTimeZoneExplanationResource1" Text="Use this if you prefer to enter times in your local timezone; all times will be converted to and displayed as UTC"></asp:Label></div>
                                <div>&nbsp;&nbsp;<uc1:TimeZone runat="server" ID="prefTimeZone" DefaultOffset="0" /></div>
                                <div><asp:Label ID="lblPrefDates" runat="server" Font-Bold="True" Text="Interpret the date of flight as:" meta:resourcekey="lblPrefDatesResource1"></asp:Label></div>
                                <asp:RadioButtonList ID="rblDateEntryPreferences" runat="server" 
                                    ValidationGroup="valPrefs" meta:resourcekey="rblDateEntryPreferencesResource1">
                                    <asp:ListItem Text="The local date at the point/time of departure" Value="1" meta:resourcekey="ListItemResource18" 
                                        ></asp:ListItem>
                                    <asp:ListItem Text="The UTC date at the time of departure" 
                                        Value="0" meta:resourcekey="ListItemResource19"></asp:ListItem>
                                </asp:RadioButtonList>
                                <div><asp:Label ID="lblFieldsToShow" runat="server" Font-Bold="True" Text="Show the following for flights:" meta:resourcekey="lblFieldsToShowResource2"></asp:Label></div>
                                <table> <!-- table here is to match layout of radiobuttonlist above -->
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckTrackCFITime" runat="server" Text="Instructor Time" 
                                                ValidationGroup="valPrefs" meta:resourcekey="ckTrackCFITimeResource1" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckSIC" runat="server" Text="Second in Command (SIC) time" 
                                                ValidationGroup="valPrefs" meta:resourcekey="ckSICResource1" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckShowTimes" runat="server" 
                                                Text="Hobbs time, flight times, and engine times for flights" 
                                                ValidationGroup="valPrefs" meta:resourcekey="ckShowTimesResource1" />
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div class="prefSectionRow">
                                <asp:Button ID="btnUpdateLocalPrefs" runat="server"  
                                    Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" 
                                    ValidationGroup="valPrefs" onclick="btnUpdateLocalPrefs_Click" meta:resourcekey="btnUpdateLocalPrefsResource2" />
                                <br />
                                <asp:Label ID="lblLocalPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False" meta:resourcekey="lblLocalPrefsUpdatedResource2"></asp:Label>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="accColoring" runat="server" HeaderCssClass="accordianHeader" ContentCssClass="accordianContent">
                        <Header>
                            <asp:Localize ID="locFlightColoring" runat="server" Text="<%$ Resources:Preferences, FlightColoringHeader %>" />
                        </Header>
                        <Content>
                            <asp:UpdatePanel ID="updColor" runat="server">
                                <ContentTemplate>
                                    <uc1:mfbFlightColoring runat="server" id="mfbFlightColoring" />
                                </ContentTemplate>
                            </asp:UpdatePanel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpProperties" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpPropertiesResource1">
                        <Header>
                            <asp:Localize ID="locPropertiesHeader" runat="server" Text="Flight Properties and Templates" meta:resourcekey="locPropertiesHeaderResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <asp:UpdatePanel runat="server" ID="UpdatePanel1">
                                    <ContentTemplate>
                                        <uc1:mfbPropertyBlocklist runat="server" id="mfbPropertyBlocklist" />
                                        <uc1:mfbEditPropTemplate runat="server" ID="mfbEditPropTemplate" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpCurrency" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpCurrencyResource1">
                        <Header>
                            <asp:Label ID="lblCurrencyPrefs" runat="server" Text="Currency/Totals" 
                                meta:resourcekey="lblCurrencyPrefsResource1"></asp:Label>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <h3><%=Resources.Currency.CurrencyTotalsDisplayHeader %></h3>
                                <asp:RadioButtonList ID="rblTotalsOptions" runat="server">
                                    <asp:ListItem Selected="True" Text="<%$ Resources:Currency, CurrencyOptionsGroupCatClass %>" Value="CatClass" />
                                    <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsGroupModel %>" Value="Model" />
                                    <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsGroupICAO %>" Value="Family" />
                                </asp:RadioButtonList>
                                <div><asp:CheckBox ID="ckIncludeModelFeatureTotals" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsTotalsModelFeatures %>" /></div>
                                <div><asp:Localize ID="locExpireCurrency" Text="<%$ Resources:Currency, CurrencyOptionsExpiredCurrency %>" runat="server" /> <asp:DropDownList ID="cmbExpiredCurrency" runat="server" /></div>
                                <h3><%=Resources.Currency.CurrencyPrefsHeader %></h3>
                                <div><asp:CheckBox ID="ckUseArmyCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsArmyCurreny %>" /></div>
                                <div><asp:CheckBox ID="ckUse117DutyTime" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsFAR117DutyTime %>" /></div>
                                <div style="margin-left:2em;">
                                    <asp:RadioButtonList ID="rbl117Rules" runat="server">
                                        <asp:ListItem Value="0" Text="<%$ Resources:Currency, Currency117OnlyDutyTimeFlights %>"></asp:ListItem>
                                        <asp:ListItem Selected="True" Value="1" Text="<%$ Resources:Currency, Currency117AllFlights %>"></asp:ListItem>
                                    </asp:RadioButtonList>
                                </div>
                                <div runat="server" id="div135DutyTime" visible="False">
                                    <asp:CheckBox ID="ckUse135DutyTime" runat="server" Text="Show FAR 135 Duty Time Status" />
                                </div>
                                <div><asp:CheckBox ID="ckUse1252xxCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions1252xx %>" /></div>
                                <div><asp:CheckBox ID="ckUse13529xCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions13529x %>" /></div>
                                <div><asp:CheckBox ID="ckUse13526xCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions13526x %>" /></div>
                                <div><asp:CheckBox ID="ckUse61217Currency" runat="server" Text="<%$ Resources:Currency, Part61217Option %>" /></div>
                                <div><asp:CheckBox ID="ckAllowNightTouchAndGo" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsNightTouchAndGo %>" /> <uc1:mfbTooltip runat="server" ID="ttNightTG" BodyContent="<%$ Resources:Currency, CurrencyOptionNoteNightTouchAndGo %>" /></div>
                                <div><asp:CheckBox ID="ckDayLandingsForDayCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsDayLandings %>" /> <uc1:mfbTooltip runat="server" ID="ttDayLandings" BodyContent="<%$ Resources:Currency, CurrencyOptionNoteDayLandings %>" /></div>
                                <div><asp:CheckBox ID="ckCanadianCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseCanada %>" /></div>
                                <div>
                                    <asp:CheckBox ID="ckLAPLCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseEASA %>" />
                                    <span class="fineprint"><asp:HyperLink ID="lnkCurrencyNotes2" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsNotesRef %>" Target="_blank" NavigateUrl="~/Public/CurrencyDisclaimer.aspx#instrument"></asp:HyperLink></span>
                                </div>
                                <div>
                                    <asp:RadioButtonList ID="rblCurrencyPref" runat="server">
                                        <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:Currency, CurrencyOptionsByCatClass %>" />
                                        <asp:ListItem Value="1" Text="<%$ Resources:Currency, CurrencyOptionsByModel %>" />
                                    </asp:RadioButtonList>
                                </div>
                            </div>
                            <div class="prefSectionRow">
                                <asp:Button ID="btnUpdateCurrencyPrefs" runat="server"  
                                    Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" 
                                    ValidationGroup="valPrefs" onclick="btnUpdateCurrencyPrefs_Click" meta:resourcekey="btnUpdateCurrencyPrefsResource3" />
                                <br />
                                <asp:Label ID="lblCurrencyPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False" meta:resourcekey="lblCurrencyPrefsUpdatedResource3"></asp:Label>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpEmail" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpEmailResource1">
                        <Header>
                            <asp:Label ID="lblEmailNotifications" runat="server"
                                Text="Email Notifications" meta:resourcekey="lblEmailNotifications1"></asp:Label>
                        </Header>
                        <Content>
                            <uc1:mfbSubscriptionManager runat="server" id="mfbSubscriptionManager" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpCustomCurrencies" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpCustomCurrenciesResource1">
                        <Header>
                            <asp:Localize ID="locCustomCurrencyHeader" Text="Custom Currency Rules" 
                                    runat="server" meta:resourcekey="locCustomCurrencyHeaderResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                                    <ContentTemplate>
										<asp:Label ID="lblAddCustomCurrency" runat="server"  
											Text="You can define your own currency rules (can be useful for FBO or insurance rules)" 
                                            meta:resourceKey="locCustCurrencyDescResource1"></asp:Label>&nbsp;
                                        <asp:Label ID="lblShowcurrency" runat="server" style="font-weight:bold" 
                                            meta:resourcekey="lblShowcurrencyResource1"></asp:Label>
                                        <uc1:mfbCustomCurrencyList runat="server" ID="mfbCustomCurrencyList1" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpCustomDeadlines" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpCustomDeadlinesResource1">
                        <Header>
                            <asp:Label ID="lblDeadlinesSection" runat="server" Text="Custom Deadlines" 
                                meta:resourcekey="lblDeadlinesSectionResource1"></asp:Label>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <% =Resources.Currency.DeadlineDescription %>
                                <uc1:mfbDeadlines ID="mfbDeadlines1" runat="server" />
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpSocialNetworking" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Label ID="lblSocialNetworkingPrompt" runat="server" Text="<%$ Resources:LocalizedText, PrefSharingHeader %>" />
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <h2><asp:Localize ID="locShareAllFlightsPrompt" runat="server" 
                                    Text="Share your public flights" 
                                    meta:resourcekey="locShareAllFlightsPromptResource1"></asp:Localize></h2>
                                <p>
                                    <asp:Localize ID="locSharePublicDesc" runat="server" Text="<%$ Resources:LocalizedText, SharePublicFlightsDescription %>"></asp:Localize></p>
                                <asp:Localize ID="locShareAllFlightsDisclaimer" runat="server" 
                                    Text="This will ONLY show flights for which you have allowed details to be visible." 
                                    meta:resourcekey="locShareAllFlightsDisclaimerResource1"></asp:Localize></p>
                                <p>
                                    <asp:TextBox ID="lnkMyFlights" runat="server" ReadOnly="true" Width="200px" meta:resourcekey="lnkMyFlightsResource1"></asp:TextBox>
                                    <asp:ImageButton ID="imgCopyMyFlights" style="vertical-align:text-bottom" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LocalizedText, CopyToClipboard %>" ToolTip="<%$ Resources:LocalizedText, CopyToClipboard %>" runat="server" />
                                    <asp:Label ID="lblMyFlightsCopied" runat="server" Text="<%$ Resources:LocalizedText, CopiedToClipboard %>" CssClass="hintPopup" style="display:none; font-weight:bold; font-size: 10pt; color:black; "></asp:Label>
                                </p>
                                <p>
                            </div>
                            <div class="prefSectionRow">
                                <h2><asp:Localize ID="locShareLogbook" runat="server" Text="<%$ Resources:LocalizedText, ShareLogbookPrompt1 %>"></asp:Localize></h2>
                                <p><asp:Localize ID="locCreateShareLinksPrompt" runat="server" Text="<%$ Resources:LocalizedText, ShareLogbookPrompt2 %>"></asp:Localize></p>
                                <asp:UpdatePanel ID="UpdatePanel3" runat="server">
                                    <ContentTemplate>
                                        <uc1:mfbShareKeys runat="server" id="mfbShareKeys" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                            <asp:Panel ID="pnlGPhotos" runat="server" CssClass="prefSectionRow">
                                <h2><asp:Localize ID="locShareGooglePhotos" runat="server" Text="<%$ Resources:LocalizedText, PrefSharingGooglePhotos %>"></asp:Localize></h2>
                                <p><asp:Label ID="lblGPhotosDesc" runat="server" /></p>
                                <div>
                                    <img src="https://ssl.gstatic.com/social/photosui/images/logo/favicon_alldp.ico" style="float:left; margin-right: 5px; max-width: 30px;" />
                                    <asp:MultiView ID="mvGPhotos" runat="server" ActiveViewIndex="0">
                                        <asp:View ID="vwGPhotosDisabled" runat="server">
                                            <asp:LinkButton ID="lnkAuthGPhotos" runat="server" OnClick="lnkAuthGPhotos_Click" />
                                        </asp:View>
                                        <asp:View ID="vwGPhotosEnabled" runat="server">
                                            <div><asp:Label ID="lblGPhotosEnabled" runat="server" /></div>
                                            <div><asp:LinkButton ID="lnkDeAuthGPhotos" runat="server" OnClick="lnkDeAuthGPhotos_Click" /></div>
                                        </asp:View>
                                    </asp:MultiView>
                                </div>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpBackup" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpBackupResource1">
                        <Header>
                            <asp:Localize ID="locCloudStorage" runat="server" Text="<%$ Resources:Preferences, CloudStorageHeader %>" />
                        </Header>
                        <Content>
                            <uc1:mfbCloudStorage runat="server" id="mfbCloudStorage" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpCloudAhoy" runat="server" meta:resourcekey="acpCloudAhoyResource1">
                        <Header>
                            <asp:Localize ID="locCloudAhoy" runat="server" Text="<%$ Resources:Preferences, CloudAhoyName %>" />
                        </Header>
                        <Content>
                            <uc1:mfbCloudAhoy runat="server" id="mfbCloudAhoy" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpoAuthApps" runat="server" Visible="False" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpoAuthAppsResource1">
                        <Header>
                            <asp:Localize ID="locPrefOAuthApps" Text="Authorized Applications" runat="server" meta:resourcekey="locPrefOAuthAppsResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <uc1:oAuthAuthorizationManager runat="server" id="oAuthAuthorizationManager" />
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                </Panes>
            </cc1:Accordion>
        </asp:View>
        <asp:View runat="server" ID="vwPilotInfo">
            <h2>
                <asp:Localize ID="locPilotHeader" runat="server" Text="<%$ Resources:Preferences, PilotInfoHeader %>" />
            </h2>
            <uc1:mfbPilotInfo runat="server" id="mfbPilotInfo" />
        </asp:View>
        <asp:View ID="vwDonate" runat="server">
            <uc1:mfbDonate runat="server" id="mfbDonate" />
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">

</asp:Content>
