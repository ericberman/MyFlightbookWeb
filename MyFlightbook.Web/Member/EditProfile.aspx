<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master"
    Codebehind="EditProfile.aspx.cs" Inherits="MyFlightbook.MemberPages.Member_EditProfile" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc9" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbDeadlines.ascx" TagPrefix="uc1" TagName="mfbDeadlines" %>
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
    <asp:Label ID="lblName" runat="server" />
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <script type="text/javascript">
        function setLocalPref(name, value) {
            var params = new Object();
            params.prefName = name;
            params.prefValue = value;
            var d = JSON.stringify(params);
            $.ajax({
                url: '<% =ResolveUrl("~/Member/Ajax.asmx/SetLocalPref") %>',
                type: "POST", data: d, dataType: "json", contentType: "application/json",
                error: function (xhr, status, error) {
                    window.alert(xhr.responseJSON.Message);
                },
                complete: function () { },
                success: function () { }
            });
        }
        function setLocalPrefValue(name, sender) {
            setLocalPref(name, sender.value);
        }

        function setLocalPrefChecked(name, sender) {
            setLocalPref(name, sender.checked);
        }
    </script>
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwAccount">
            <h2>
                <asp:Localize ID="locAccountHeader" runat="server" Text="<%$ Resources:Profile, accountHeader %>" />
            </h2>
            <p><asp:Label ID="lblMemberSince" runat="server"></asp:Label></p>
            <cc1:Accordion ID="accordianAccount" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250"  SelectedIndex="-1" RequireOpenedPane="false">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpName">
                        <Header>
                            <asp:Localize ID="locaHeadName" runat="server" Text="<%$ Resources:Tabs, ProfileName %>" />
                        </Header>
                        <Content>
                            <asp:MultiView ID="mvNameEmail" runat="server" ActiveViewIndex="0">
                                <asp:View ID="vwStaticNameEmail" runat="server">
                                    <div><asp:Label Font-Bold="true" ID="lblFullName" runat="server"></asp:Label></div>
                                    <div><asp:Label ID="lblStaticEmail" runat="server" /> <asp:Label ID="lblPrimaryVerified" ToolTip="<%$ Resources:Profile, accountVerifyEmailValid %>" runat="server" Text="✔" ForeColor="Green" /></div>
                                    <div style="white-space:pre-wrap"><asp:Label ID="lblAddress" runat="server"></asp:Label></div>
                                    <asp:Panel ID="pnlAlternateEmails" runat="server" Visible="false">
                                        <p><asp:Label ID="lblAlternateEmails" runat="server" Text="<%$ Resources:Profile, accountEmailAliasesHeader %>" /><uc1:mfbTooltip runat="server" ID="mfbTooltip1" BodyContent="<%$ Resources:Profile, accountEmailAliasWhy %>" /></p>
                                        <ul>
                                            <asp:Repeater ID="rptAlternateEmailsRO" runat="server">
                                                <ItemTemplate>
                                                    <li><%# Container.DataItem %></li>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </ul>
                                    </asp:Panel>
                                    <div><asp:Button ID="btnEditNameEmail" runat="server" Text="<%$ Resources:Profile, ChangeNameEmail %>" OnClick="btnEditNameEmail_Click" /></div>
                                    <div><asp:Label ID="lblVerifyResult" runat="server" EnableViewState="false" /></div>
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
                                                    <asp:Label ID="lblVerifyPrimaryEmail" runat="server">
                                                        <asp:LinkButton ID="lnkVerifyEmail" runat="server" Text="<%$ Resources:Profile, accountVerifyEmailPrompt %>" OnClick="lnkVerifyEmail_Click" /><uc1:mfbTooltip runat="server" ID="mfbTooltip" BodyContent="<%$ Resources:Profile, accountVerifyWhy %>" /> 
                                                    </asp:Label>
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
                                                    <div><asp:Label ID="lblVerificationSent" runat="server" Visible="False" Text="<%$ Resources:Profile, accountVerifyEmailSent %>" EnableViewState="false" /></div>
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
                                                    <asp:TextBox ID="txtPreferredGreeting" ValidationGroup="valNameEmail" AutoCompleteType="DisplayName" runat="server" />
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
                                                <td colspan="2">&nbsp;</td>
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
                                            <tr>
                                                <td colspan="2">&nbsp;<br />&nbsp;</td>
                                            </tr>
                                            <tr>
                                                <td style="vertical-align:top;"><% = Resources.Profile.accountEmailAddAlias %><uc1:mfbTooltip runat="server" ID="mfbTooltip3" BodyContent="<%$ Resources:Profile, accountEmailAliasWhy %>" />
                                                </td>
                                                <td style="vertical-align:top;">
                                                    <asp:TextBox runat="server" ID="txtAltEmail" TextMode="Email" ValidationGroup="valAltEmail" AutoCompleteType="Email" />
                                                    <asp:Button ID="lnkAddAltEmail" runat="server" Text="<%$ Resources:Profile, accountVerifyEmailPrompt %>" OnClick="lnkAddAltEmail_Click" ValidationGroup="valAltEmail" />
                                                    <asp:RegularExpressionValidator ID="reAltEmail" runat="server" 
                                                        ControlToValidate="txtAltEmail" ValidationGroup="valAltEmail"
                                                        ErrorMessage="<%$ Resources:Profile, errEmailRequired %>" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                                        CssClass="error" SetFocusOnError="True" Display="Dynamic" />
                                                    <asp:RequiredFieldValidator ID="RequiredFieldValidator3" runat="server"  ValidationGroup="valAltEmail"
                                                    ControlToValidate="txtAltEmail" CssClass="error" Display="Dynamic" 
                                                    ErrorMessage="<%$ Resources:Profile, errEmailMissing %>" />
                                                    <div><asp:Label ID="lblAltEmailSent" runat="server" Visible="False" Text="<%$ Resources:Profile, accountVerifyEmailSent %>" EnableViewState="false" /></div>
                                                    <asp:Panel ID="pnlExistingAlternateEmails" runat="server" Visible="false">
                                                        <asp:GridView ID="gvAlternateEmails" runat="server" GridLines="None" ShowHeader="false" ShowFooter="false" AutoGenerateColumns="false" OnRowCommand="gvAlternateEmails_RowCommand">
                                                            <Columns>
                                                                <asp:TemplateField>
                                                                    <ItemTemplate>
                                                                        <asp:ImageButton ID="imgDelete" runat="server" 
                                                                            AlternateText="<%$ Resources:Profile, accountEmailAliasDelete %>" CommandArgument='<%# Container.DataItem %>' 
                                                                            CommandName="_Delete" ImageUrl="~/images/x.gif" 
                                                                            ToolTip="<%$ Resources:Currency, CustomCurrencyDeleteTooltip %>" />
                                                                        <cc1:ConfirmButtonExtender ID="cbeDelete" runat="server" 
                                                                            ConfirmOnFormSubmit="True" 
                                                                            ConfirmText="<%$ Resources:Profile, accountEmailAliasDeleteConfirm %>" 
                                                                            TargetControlID="imgDelete" />
                                                                    </ItemTemplate>
                                                                    <ItemStyle VerticalAlign="Top" />
                                                                </asp:TemplateField>
                                                                <asp:TemplateField>
                                                                    <ItemTemplate>
                                                                        <%# Container.DataItem %> <asp:Label ID="lblPrimaryVerified" ToolTip="<%$ Resources:Profile, accountVerifyEmailValid %>" runat="server" Text="✔" ForeColor="Green" />
                                                                    </ItemTemplate>
                                                                </asp:TemplateField>
                                                            </Columns>
                                                        </asp:GridView>
                                                    </asp:Panel>
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
                                                <li runat="server" id="itemLastActivity"><asp:Label ID="lblLastActivity" runat="server" /></li>
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
                                                            <asp:Label ID="CurrentPasswordLabel" runat="server" AssociatedControlID="CurrentPassword" Text="<%$ Resources:Preferences, AccountPasswordCurrent %>" />
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="CurrentPassword" runat="server" TextMode="Password" ValidationGroup="valPassword"></asp:TextBox>
                                                            <asp:CustomValidator ID="valCurrentPasswordRequired" runat="server" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountPasswordNeedPrevious %>" OnServerValidate="ValidateCurrentPassword" ValidationGroup="valPassword" />
                                                            <asp:RequiredFieldValidator ID="valCurrentPasswordPresent" runat="server" ControlToValidate="CurrentPassword" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountPasswordNeedPrevious %>" ValidationGroup="valPassword" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Label ID="lblNewPassword" runat="server" AssociatedControlID="NewPassword" Text="<%$ Resources:Preferences, AccountPasswordNew %>" />
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="NewPassword" runat="server" TextMode="Password" ValidationGroup="valPassword" />
                                                            <cc1:PasswordStrength ID="PasswordStrength2" runat="server" BehaviorID="PasswordStrength2" TargetControlID="NewPassword" TextStrengthDescriptions="<%$ Resources:LocalizedText, PasswordStrengthStrings %>" StrengthIndicatorType="BarIndicator"
                                                                StrengthStyles="pwWeak;pwOK;pwGood;pwExcellent" PreferredPasswordLength="10" BarBorderCssClass="pwBorder" />
                                                            <asp:RequiredFieldValidator ID="valNewPassword" runat="server" ControlToValidate="NewPassword" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountPasswordNewMissing %>" ValidationGroup="valPassword" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Label ID="ConfirmNewPasswordLabel" runat="server" AssociatedControlID="ConfirmNewPassword" Text="<%$ Resources:Preferences, AccountPasswordConfirmNew %>" />
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="ConfirmNewPassword" runat="server" TextMode="Password" ValidationGroup="valPassword" />
                                                            <asp:CompareValidator ID="valPassCompare" runat="server" ControlToCompare="NewPassword" ControlToValidate="ConfirmNewPassword" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountPasswordConfirmDoesNotMatch %>" ValidationGroup="valPassword" />
                                                            <asp:RequiredFieldValidator ID="valPassConfirm" runat="server" ControlToValidate="ConfirmNewPassword" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountPasswordConfirmDoesNotMatch %>" ValidationGroup="valPassword" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>&nbsp;</td>
                                                        <td>
                                                            <asp:Button ID="btnUpdatePass" runat="server" OnClick="btnUpdatePass_Click" Text="<%$ Resources:Preferences, AccountPasswordChange %>" ValidationGroup="valPassword" />
                                                            <br />
                                                            <asp:Label ID="lblPassChanged" runat="server" CssClass="success" EnableViewState="False" Text="<%$ Resources:Preferences, AccountPasswordSuccess %>" Visible="False" />
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
                    <cc1:AccordionPane ID="acpQandA" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Localize ID="locHeadQA" runat="server" Text="<%$ Resources:Tabs, ProfileQA %>" />
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlQandA" runat="server" DefaultButton="btnChangeQA">
                                <asp:UpdatePanel ID="updQA" runat="server">
                                    <ContentTemplate>
                                        <% =Resources.LocalizedText.AccountQuestionHint %>
                                        <asp:MultiView ID="mvQA" runat="server" ActiveViewIndex="0">
                                            <asp:View ID="vwStaticQA" runat="server">
                                                <div><asp:Button ID="btnChangeQA1" runat="server" Text="<%$ Resources:Profile, ChangeQA %>" OnClick="btnChangeQA_Click1" /></div>
                                            </asp:View>
                                            <asp:View ID="vwVerifyTFAQA" runat="server">
                                                <p><asp:Label ID="lblTFA2" runat="server" Text="<%$ Resources:Profile, TFARequired %>" /></p>
                                                <p><asp:Label ID="lblTFAUseApp2" runat="server" Text="<%$ Resources:Profile, TFAUseYourApp %>" /></p>
                                                <uc1:TwoFactorAuthVerifyCode runat="server" ID="tfaChangeQA" OnTFACodeFailed="tfaChangeQA_TFACodeFailed" OnTFACodeVerified="tfaChangeQA_TFACodeVerified" />
                                                <div><asp:Label ID="lblTFAErrQA" runat="server" CssClass="error" EnableViewState="false" Text="<%$ Resources:Profile, TFACodeFailed %>" Visible="false" /></div>
                                            </asp:View>
                                            <asp:View ID="vwChangeQA" runat="server">
                                                <table>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locPasswordPromptForQA" runat="server" Text="<%$ Resources:Preferences, AccountQAPasswordPrompt %>" />
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="txtPassQA" runat="server" TextMode="Password" ValidationGroup="vgNewQA" />
                                                            <asp:RequiredFieldValidator ID="valQAPassRequired" runat="server" ControlToValidate="txtPassQA" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountQAPasswordRequired %>" ValidationGroup="vgNewQA" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locCurrentQuestion" runat="server" Text="<%$ Resources:Preferences, AccountQACurrentQuestion %>" />
                                                        </td>
                                                        <td>
                                                            <asp:Label ID="lblQuestion" runat="server" Font-Bold="True" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locQuestionPrompt" runat="server" Text="<%$ Resources:Preferences, AccountQANewQuestion %>" />
                                                        </td>
                                                        <td>
                                                            <uc4:AccountQuestions ID="txtQuestion" ValidationGroup="vgNewQA" runat="server" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>
                                                            <asp:Localize ID="locNewAnswer" runat="server" Text="<%$ Resources:Preferences, AccountQANewAnswer %>" />
                                                        </td>
                                                        <td>
                                                            <asp:TextBox ID="txtAnswer" runat="server" ValidationGroup="vgNewQA" />
                                                            <asp:RequiredFieldValidator ID="reqAnswer" runat="server" ControlToValidate="txtAnswer" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Preferences, errAccountQAAnswerMissing %>" ValidationGroup="vgNewQA" />
                                                        </td>
                                                    </tr>
                                                    <tr>
                                                        <td>&nbsp;</td>
                                                        <td>
                                                            <asp:Button ID="btnChangeQA" runat="server" OnClick="btnChangeQA_Click" Text="<%$ Resources:Preferences, AccountQAChange %>" ValidationGroup="vgNewQA" />
                                                            <br />
                                                            <asp:Label ID="lblQAChangeSuccess" runat="server" CssClass="success" EnableViewState="False" Text="<%$ Resources:Preferences, AccountQAChangeSuccess %>" Visible="False" />
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
            <h2><% =Resources.Preferences.HeaderFeatures %></h2>
            <cc1:Accordion ID="accordianPrefs" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" SelectedIndex="-1" RequireOpenedPane="false">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpLocalPrefs">
                        <Header>
                            <asp:Localize ID="locFlightTimes" runat="server" Text="<%$ Resources:Preferences, PrefSectFlightEntryHeader %>" />
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <table>
                                    <tr>
                                        <td colspan="2"><h3><asp:Label ID="lblPrefTimes" runat="server" Font-Bold="True" Text="<%$ Resources:Preferences, DecimalPrefPrompt %>" /></h3></td>
                                        <td style="font-size:smaller; font-style:italic;">&nbsp;<asp:Localize ID="locSamp1" runat="server" Text="<%$ Resources:Preferences, DecimalPrefSample1 %>" /></td>
                                        <td style="font-size:smaller; font-style:italic;">&nbsp;<asp:Localize ID="locSamp2" runat="server" Text="<%$ Resources:Preferences, DecimalPrefSample2 %>" /></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimalAdaptive" runat="server" GroupName="decimalPref" onclick="setLocalPref('decimal', 'Adaptive');" /></td>
                                        <td><asp:Label ID="locAdapt" runat="server" Text="<%$ Resources:Preferences, DecimalPrefAdaptive %>" AssociatedControlID="rbDecimalAdaptive" /></td>
                                        <td style="text-align:center"><% =(70.0M / 60.0M).ToString("#,##0.0#", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.0#", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimal1" runat="server" GroupName="decimalPref" onclick="setLocalPref('decimal', 'OneDecimal');" /></td>
                                        <td><asp:Label ID="lbl1Dec" runat="server" Text="<%$ Resources:Preferences, DecimalPref1Decimal %>" AssociatedControlID="rbDecimal1" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).ToString("#,##0.0", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.0", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimal2" runat="server" GroupName="decimalPref" onclick="setLocalPref('decimal', 'TwoDecimal');" /></td>
                                        <td><asp:Label ID="lbl2Dec" runat="server" Text="<%$ Resources:Preferences, DecimalPref2Decimal %>" AssociatedControlID="rbDecimal2" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).ToString("#,##0.00", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).ToString("#,##0.00", System.Globalization.CultureInfo.CurrentCulture) %></td>
                                    </tr>
                                    <tr>
                                        <td><asp:RadioButton ID="rbDecimalHHMM" runat="server" GroupName="decimalPref" onclick="setLocalPref('decimal', 'HHMM');" /></td>
                                        <td><asp:Label ID="lblHHMM" runat="server" Text="<%$ Resources:Preferences, DecimalPrefHHMM %>" AssociatedControlID="rbDecimalHHMM" /></td>
                                        <td style="text-align:center;"><% =(70.0M / 60.0M).FormatDecimal(true) %></td>
                                        <td style="text-align:center;"><% =(72.0 / 60.0).FormatDecimal(true) %></td>
                                    </tr>
                                </table>
                                <div>
                                    <asp:Label ID="lblPrecHeader" runat="server" Text="<%$ Resources:Preferences, PrefMathPrecisionHeader %>" Font-Bold="true" />
                                    <asp:HyperLink ID="lnkQuant" runat="server" Text="<%$ Resources:Preferences, PrefMathPrecisionNote %>" NavigateUrl="~/Public/FAQ.aspx?q=72#72" />
                                </div>
                                <div>
                                    <asp:RadioButton ID="rbPrecisionMinutes" runat="server" GroupName="rbgMathPrecision" onclick="setLocalPref('rounding', '60');" Text="<%$ Resources:Preferences, PrefMathPrecisionMinutes %>" Checked="true" />
                                    <asp:RadioButton ID="rbPrecisionHundredths" runat="server" GroupName="rbgMathPrecision" onclick="setLocalPref('rounding', '100');" Text="<%$ Resources:Preferences, PrefMathPrecisionHundredths %>" />
                                </div>
                                <h3><asp:Label ID="lblPrefTimeZone" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightTimeZone %>" /></h3>
                                <div>&nbsp;&nbsp;<asp:Label ID="lblPrefTimeZoneExplanation" CssClass="fineprint" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightTimeZoneTip %>" /></div>
                                <div>&nbsp;&nbsp;<uc1:TimeZone runat="server" ID="prefTimeZone" DefaultOffset="0" AutoPostBack="false" ClientScript="javascript:setLocalPrefValue('timezone', this);" /></div>
                                <h3><asp:Label ID="lblPrefDates" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightTimeZonePrompt %>" /></h3>
                                <div><asp:RadioButton ID="rbDateLocal" runat="server" GroupName="rbgDateEntryTZ" Text="<%$ Resources:Preferences, PrefSectNewFlightTimeZoneLocal %>" onclick="setLocalPref('dateofflighttz', 'local');" /></div>
                                <div><asp:RadioButton ID="rbDateUTC" runat="server" GroupName="rbgDateEntryTZ" Text="<%$ Resources:Preferences, PrefSectNewFlightTimeZoneUTC %>" onclick="setLocalPref('dateofflighttz', 'utc');" /></div>
                                <h3><asp:Label ID="lblFieldsToShow" runat="server" Text="<%$ Resources:Preferences, PrefSectFlightEntryDataToInclude %>" /></h3>
                                <table> <!-- table here is to match layout of radiobuttonlist above -->
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckTrackCFITime" runat="server" onclick="setLocalPrefChecked('usecfi', this);" Text="<%$ Resources:Preferences, PrefSectNewFlightShowCFI %>" ValidationGroup="valPrefs" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckSIC" runat="server" onclick="setLocalPrefChecked('usesic', this);" Text="<%$ Resources:Preferences, PrefSectNewFlightShowSIC %>" ValidationGroup="valPrefs" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:CheckBox ID="ckShowTimes" runat="server" onclick="setLocalPrefChecked('tracktimes', this);" Text="<%$ Resources:Preferences, PrefSectNewFlightShowTimes %>" ValidationGroup="valPrefs" />
                                        </td>
                                    </tr>
                                </table>
                                <h3><% =Resources.Preferences.PrefSectFlightMisc %></h3>
                                <div><asp:CheckBox ID="ckPreserveOriginal" runat="server" Text="<%$ Resources:Preferences, PrefSaveOriginalFlight %>" onclick="setLocalPrefChecked('trackoriginal', this);" /><uc1:mfbTooltip runat="server" ID="ttTrackChanges" BodyContent="<%$ Resources:Preferences, PrefSaveOriginalFlightDesc %>" />
                                </div>
                            </div>
                            <script type="text/javascript">
                                function startDrag(event, index) {
                                    event.dataTransfer.setData("ID", event.target.id);
                                    event.dataTransfer.setData("Index", index);
                                }

                                function reorder(event, index) {
                                    var idx = parseInt(event.dataTransfer.getData("Index"));
                                    var id = event.dataTransfer.getData("ID");
                                    var ele = document.getElementById(id);
                                    var divUnused = document.getElementById('<% =divUnusedFields.ClientID %>');
                                    event.target.parentNode.insertBefore(ele, event.target);
                                    if (event.target.parentNode === divUnused)
                                        move(event, false);
                                    else
                                        updatePermuation(idx, index);
                                    event.preventDefault();
                                    event.stopPropagation();
                                    return false;
                                }

                                function move(event, append) {
                                    var idx = parseInt(event.dataTransfer.getData("Index"));
                                    var id = event.dataTransfer.getData("ID");
                                    var ele = document.getElementById(id);
                                    var targ = document.getElementById(append ? '<% =divCoreFields.ClientID %>' : '<% =divUnusedFields.ClientID %>');
                                    targ.insertBefore(ele, targ.firstElementChild);
                                    var perms = getPermutations();

                                    if (append && perms.indexOf(idx) < 0) {
                                        perms.splice(0, 0, idx);
                                        setPermutations(perms);
                                    }
                                    else {
                                        perms = perms.filter(function (s) { return s != idx; });
                                        setPermutations(perms);
                                    }

                                    event.preventDefault();
                                    event.stopPropagation();
                                    return false;
                                }

                                function allowDrop(event) {
                                    event.preventDefault();
                                }

                                function getPermutations() {
                                    var elePerms = document.getElementById('<% =hdnPermutation.ClientID %>');
                                    return (elePerms.value === "") ? [] : JSON.parse(elePerms.value);
                                }

                                function setPermutations(perms) {
                                    var elePerms = document.getElementById('<% =hdnPermutation.ClientID %>');
                                    elePerms.value = JSON.stringify(perms);
                                    setLocalPref("FIELDDISPLAY", elePerms.value);
                                }

                                function updatePermuation(insert, before) {
                                    var perms = getPermutations();

                                    // Remove "insert" if it's in the list
                                    var permsFiltered = perms.filter(function (s) { return s != insert; });

                                    if (before !== null) {
                                        // Now add it before "before", if before is in the arrawy
                                        var i = permsFiltered.indexOf(before);
                                        permsFiltered.splice(i >= 0 ? i : 0, 0, insert);
                                    }
                                    setPermutations(permsFiltered);
                                }

                                function resetPermutations() {
                                    setPermutations([]);
                                }
                            </script>
                            <h3><asp:Label ID="lblCustCore" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightCustomization %>" /></h3>
                            <p><asp:Label ID="lblCustCoreTip" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightCustomizationTip %>" /></p>
                            <div><asp:Label ID="lblcustInst" runat="server" Text="<%$ Resources:Preferences, PrefBlockListInstructions %>" /></div>
                            <table style="margin-left: auto; margin-right: auto;">
                                <tr>
                                    <td><asp:Label ID="lblshowfields" runat="server" Text="<%$ Resources:Preferences, PrefBlockListShow %>" /></td>
                                    <td><asp:Label ID="Label1" runat="server" Text="<%$ Resources:Preferences, PrefBlockListHide %>" /></td>
                                </tr>
                                <tr>
                                    <td style="width:50%">
                                        <asp:Panel runat="server" ID="divCoreFields" ondragover="allowDrop(event)" ondrop="move(event, true)" CssClass="dragTarget">
                                            <asp:Label CssClass="draggableItem" ID="lblDragXC" runat="server" Text="<%$ Resources:LogbookEntry, FieldCrossCountry %>" draggable="true" ondragstart="startDrag(event, 0)" ondrop="reorder(event, 0)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragNight" runat="server" Text="<%$ Resources:LogbookEntry, FieldNight %>" draggable="true" ondragstart="startDrag(event, 1)" ondrop="reorder(event, 1)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragSimIMC" runat="server" Text="<%$ Resources:LogbookEntry, FieldSimIMCFull %>" draggable="true" ondragstart="startDrag(event, 2)" ondrop="reorder(event, 2)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragIMC" runat="server" Text="<%$ Resources:LogbookEntry, FieldIMC %>" draggable="true" ondragstart="startDrag(event, 3)" ondrop="reorder(event, 3)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragGrndSim" runat="server" Text="<%$ Resources:LogbookEntry, FieldGroundSimFull %>" draggable="true" ondragstart="startDrag(event, 4)" ondrop="reorder(event, 4)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragDual" runat="server" Text="<%$ Resources:LogbookEntry, FieldDual %>" draggable="true" ondragstart="startDrag(event, 5)" ondrop="reorder(event, 5)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragInst" runat="server" Text="<%$ Resources:LogbookEntry, FieldCFI %>" draggable="true" ondragstart="startDrag(event, 6)" ondrop="reorder(event, 6)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragSIC" runat="server" Text="<%$ Resources:LogbookEntry, FieldSIC %>" draggable="true" ondragstart="startDrag(event, 7)" ondrop="reorder(event, 7)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragPIC" runat="server" Text="<%$ Resources:LogbookEntry, FieldPIC %>" draggable="true" ondragstart="startDrag(event, 8)" ondrop="reorder(event, 8)" ondragover="allowDrop(event)" />
                                            <asp:Label CssClass="draggableItem" ID="lblDragTotal" runat="server" Text="<%$ Resources:LogbookEntry, FieldTotalFull %>" draggable="true" ondragstart="startDrag(event, 9)" ondrop="reorder(event, 9)" ondragover="allowDrop(event)" />
                                        </asp:Panel>
                                    </td>
                                    <td style="width:50%">
                                        <asp:Panel ondragover="allowDrop(event)" ondrop="move(event, false)" class="dragTarget" runat="server" id="divUnusedFields" />
                                    </td>
                                </tr>
                                <tr>
                                    <td colspan="2">
                                        <asp:Button ID="btnResetPermutations" runat="server" Text="<%$ Resources:Preferences, PrefSectNewFlightCustReset %>" OnClientClick="javascript:resetPermutations();" />
                                    </td>
                                </tr>
                            </table>
                            <asp:HiddenField ID="hdnPermutation" runat="server" Value="0,1,2,3,4,5,6,7,8,9" />
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
                    <cc1:AccordionPane runat="server" ID="acpProperties" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <% =Resources.Preferences.HeaderPropsAndTemplates %>
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
                    <cc1:AccordionPane ID="acpCurrency" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Label ID="lblCurrencyPrefs" runat="server" Text="<%$ Resources:Preferences, PrefCurrencyTotalsSectionHeader %>" />
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <h3><%=Resources.Currency.CurrencyTotalsDisplayHeader %></h3>
                                <div><asp:RadioButton ID="rbTotalsModeCatClass" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsGroupCatClass %>" GroupName="rbTotalsMode" onclick="setLocalPref('TotalsMode', 'CatClass');" /></div>
                                <div><asp:RadioButton ID="rbTotalsModeModel" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsGroupModel %>" GroupName="rbTotalsMode" onclick="setLocalPref('TotalsMode', 'Model');" /></div>
                                <div><asp:RadioButton ID="rbTotalsModeICAO" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsGroupICAO %>" GroupName="rbTotalsMode" onclick="setLocalPref('TotalsMode', 'Family');" /></div>
                                <div><asp:CheckBox ID="ckIncludeModelFeatureTotals" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsTotalsModelFeatures %>" onclick="setLocalPrefChecked('totalsIncludeMF', this);" /></div>
                                <h3><%=Resources.Currency.CurrencyPrefsHeader %></h3>
                                <div>
                                    <asp:Label ID="lblJurisdictionHeader" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsJurisdiction %>" />
                                    <div>
                                        <asp:RadioButton ID="rbFAARules" runat="server" GroupName="currJurisd" onclick="setLocalPref('CurrencyJurisdiction', 'FAA');" />
                                        <asp:Label ID="lblFAARules" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseFAA %>" AssociatedControlID="rbFAARules" />
                                    </div>
                                    <div>
                                        <asp:RadioButton ID="rbCanadianRules" runat="server" GroupName="currJurisd" onclick="setLocalPref('CurrencyJurisdiction', 'Canada');" />
                                        <asp:Label ID="lblCanadaRules" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseCanada %>" AssociatedControlID="rbCanadianRules" />
                                    </div>
                                    <div>
                                        <asp:RadioButton ID="rbEASARules" runat="server" GroupName="currJurisd" onclick="setLocalPref('CurrencyJurisdiction', 'EASA');" />
                                        <asp:Label ID="lblEASARules" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseEASA %>" AssociatedControlID="rbEASARules" />
                                            <span class="fineprint"><asp:HyperLink ID="lnkCurrencyNotes2" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsNotesRef %>" Target="_blank" NavigateUrl="~/Public/CurrencyDisclaimer.aspx#instrument" /></span>
                                        
                                    </div>
                                    <div>
                                        <asp:RadioButton ID="rbAustraliaRules" runat="server" GroupName="currJurisd" onclick="setLocalPref('CurrencyJurisdiction', 'Australia');" />
                                        <asp:Label ID="lblAustraliaRules" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsUseCASA %>" AssociatedControlID="rbAustraliaRules" />
                                    </div>
                                </div>
                                <br />
                                <div><asp:RadioButton ID="rbCurrencyModeCatClass" GroupName="currencyMode" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsByCatClass %>" onclick="setLocalPref('usePerModelCurrency', 'false');" Checked="true" /></div>
                                <div><asp:RadioButton ID="rbCurrencyModeModel" GroupName="currencyMode" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsByModel %>" onclick="setLocalPref('usePerModelCurrency', 'true');" /></div>
                                <br />
                                <div><asp:CheckBox ID="ckAllowNightTouchAndGo" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsNightTouchAndGo %>" onclick="setLocalPrefChecked('allowNightTouchAndGo', this);" /> <uc1:mfbTooltip runat="server" ID="ttNightTG" BodyContent="<%$ Resources:Currency, CurrencyOptionNoteNightTouchAndGo %>" /></div>
                                <div><asp:CheckBox ID="ckDayLandingsForDayCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsDayLandings %>" onclick="setLocalPrefChecked('onlyDayLandingsForDayCurrency', this);" /> <uc1:mfbTooltip runat="server" ID="ttDayLandings" BodyContent="<%$ Resources:Currency, CurrencyOptionNoteDayLandings %>" /></div>

                                <h3><% =Resources.Preferences.PrefCurrencyDisplay %></h3>
                                <div><asp:CheckBox ID="ckUseArmyCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsArmyCurreny %>" onclick="setLocalPrefChecked('useArmyCurrency', this);" /></div>
                                <div><asp:CheckBox ID="ckUse117DutyTime" runat="server" Text="<%$ Resources:Currency, CurrencyOptionsFAR117DutyTime %>" onclick="setLocalPrefChecked('use117DutyTime', this);" /></div>
                                <div style="margin-left:2em;">
                                    <div><asp:RadioButton ID="rb117OnlyDutyTimeFlights" GroupName="117Flights" runat="server" Text="<%$ Resources:Currency, Currency117OnlyDutyTimeFlights %>" onclick="setLocalPref('use117DutyAllFlights', 'false');" /></div>
                                    <div><asp:RadioButton ID="rb117AllFlights" GroupName="117Flights" runat="server" Text="<%$ Resources:Currency, Currency117AllFlights %>" onclick="setLocalPref('use117DutyAllFlights', 'true');" /></div>
                                </div>
                                <div runat="server" id="div135DutyTime" visible="False">
                                    <asp:CheckBox ID="ckUse135DutyTime" runat="server" Text="<%$ Resources:Currency, CurrencyOptions135DutyTime %>" onclick="setLocalPrefChecked('use135DutyTime', this);" />
                                </div>
                                <div><asp:CheckBox ID="ckUse1252xxCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions1252xx %>" onclick="setLocalPrefChecked('use1252Currency', this);" /></div>
                                <div><asp:CheckBox ID="ckUse13529xCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions13529x %>" onclick="setLocalPrefChecked('useFAR13529xCurrency', this);" /></div>
                                <div><asp:CheckBox ID="ckUse13526xCurrency" runat="server" Text="<%$ Resources:Currency, CurrencyOptions13526x %>" onclick="setLocalPrefChecked('useFAR13526xCurrency', this);" /></div>
                                <div><asp:CheckBox ID="ckUse61217Currency" runat="server" Text="<%$ Resources:Currency, Part61217Option %>" onclick="setLocalPrefChecked('useFAR61217Currency', this);" /></div>
                                <h3><% =Resources.Preferences.PrefCurrencyClutterControl %></h3>
                                <p>
                                    <asp:Localize ID="locExpireCurrency" Text="<%$ Resources:Currency, CurrencyOptionsExpiredCurrency %>" runat="server" />
                                    <asp:DropDownList ID="cmbExpiredCurrency" runat="server" onchange="setLocalPrefValue('currencyExpiration', this);" />
                                </p>
                                <p>
                                    <asp:Localize ID="Localize1" Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenance %>" runat="server" />
                                    <asp:DropDownList ID="cmbAircraftMaintWindow" runat="server" onchange="setLocalPrefValue('maintWindow', this);">
                                        <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenanceAlways %>" Value="-1" />
                                        <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenance180Days %>" Value="180" />
                                        <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenance120Days %>" Value="120" />
                                        <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenance90Days %>" Value="90" Selected="True" />
                                        <asp:ListItem Text="<%$ Resources:Currency, CurrencyOptionsAircraftMaintenance30Days %>" Value="30" />
                                    </asp:DropDownList> 
                                </p>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpEmail" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <% =Resources.Preferences.HeaderEmail %>
                        </Header>
                        <Content>
                            <uc1:mfbSubscriptionManager runat="server" id="mfbSubscriptionManager" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpCustomCurrencies" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <% =Resources.Preferences.HeaderCustomCurrency %>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                                    <ContentTemplate>
										<asp:Label ID="lblAddCustomCurrency" runat="server" Text="<%$ Resources:Preferences, CustCurrencyDescription %>" />
                                        <asp:Label ID="lblShowcurrency" runat="server" style="font-weight:bold" />
                                        <uc1:mfbCustomCurrencyList runat="server" ID="mfbCustomCurrencyList1" />
                                    </ContentTemplate>
                                </asp:UpdatePanel>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpCustomDeadlines" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <% =Resources.Preferences.HeaderCustomDeadlines %>
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
                                <h2><% =Resources.Preferences.HeaderSharePublicFlights %></h2>
                                <p><%=Resources.LocalizedText.SharePublicFlightsDescription %></p>
                                <p><%=Resources.Preferences.SharingShareFlightsDisclaimer %></p>
                                <p>
                                    <asp:TextBox ID="lnkMyFlights" runat="server" ReadOnly="true" Width="200px" />
                                    <asp:ImageButton ID="imgCopyMyFlights" style="vertical-align:text-bottom" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LocalizedText, CopyToClipboard %>" ToolTip="<%$ Resources:LocalizedText, CopyToClipboard %>" runat="server" />
                                    <asp:Label ID="lblMyFlightsCopied" runat="server" Text="<%$ Resources:LocalizedText, CopiedToClipboard %>" CssClass="hintPopup" style="display:none; font-weight:bold; font-size: 10pt; color:black; " />
                                </p>
                            </div>
                            <div class="prefSectionRow">
                                <h2><asp:Localize ID="locShareLogbook" runat="server" Text="<%$ Resources:LocalizedText, ShareLogbookPrompt1 %>" /></h2>
                                <p><asp:Localize ID="locCreateShareLinksPrompt" runat="server" Text="<%$ Resources:LocalizedText, ShareLogbookPrompt2 %>" /></p>
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
                    <cc1:AccordionPane ID="acpBackup" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Localize ID="locCloudStorage" runat="server" Text="<%$ Resources:Preferences, CloudStorageHeader %>" />
                        </Header>
                        <Content>
                            <uc1:mfbCloudStorage runat="server" id="mfbCloudStorage" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpCloudAhoy" runat="server">
                        <Header>
                            <asp:Localize ID="locCloudAhoy" runat="server" Text="<%$ Resources:Preferences, CloudAhoyName %>" />
                        </Header>
                        <Content>
                            <uc1:mfbCloudAhoy runat="server" id="mfbCloudAhoy" />
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpSchedulers" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Localize ID="locSchedulers" runat="server" Text="<%$ Resources:Preferences, ScheduleServiceHeader %>" />
                        </Header>
                        <Content>
                            <table style="border-spacing: 10px; border-collapse: separate;">
                                <tr>
                                    <td style="width:100px;"><img src='<% =VirtualPathUtility.ToAbsolute("~/images/LeonLogo.svg") %>' /></td>
                                    <td style="vertical-align:top">
                                        <h2><%=Resources.Preferences.ScheduleServiceLeonName %></h2>
                                        <p><%=Resources.Preferences.ScheduleServiceLeonDesc %></p>
                                        <asp:HyperLink ID="lnkSetUpLeon" runat="server" NavigateUrl="~/Public/LeonRedir.aspx"><% =Branding.ReBrand(Resources.Preferences.ScheduleServiceLeonManage) %></asp:HyperLink>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="width:100px;"><img style="width:100px" src='<% =VirtualPathUtility.ToAbsolute("~/images/rb_logo.png") %>' /></td>
                                    <td style="vertical-align:top;">
                                        <h2><%=Resources.Preferences.ScheduleServiceRBName %></h2>
                                        <p><%=Resources.Preferences.ScheduleServiceRBDesc %></p>
                                        <asp:HyperLink ID="lnkSetUpRB" runat="server" NavigateUrl="~/mvc/rbredirect"><% =Branding.ReBrand(Resources.Preferences.ScheduleServiceRBManage) %></asp:HyperLink>
                                    </td>
                                </tr>
                            </table>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpoAuthApps" runat="server" Visible="False" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <% =Resources.Preferences.HeaderOAuthApps %>
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
