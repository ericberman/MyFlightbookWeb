<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master"
    Codebehind="EditProfile.aspx.cs" Inherits="Member_EditProfile" Title="Edit Profile" culture="auto" meta:resourcekey="PageResource1" %>
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

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <asp:Label ID="lblName" runat="server" meta:resourcekey="lblNameResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwAccount">
            <h2>
                <asp:Localize ID="locAccountHeader" runat="server" meta:resourcekey="locAccountHeaderResource1" Text="Account Settings"></asp:Localize>
            </h2>
            <p><asp:Label ID="lblMemberSince" runat="server"></asp:Label></p>
            <cc1:Accordion ID="accordianAccount" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" meta:resourcekey="accordianAccountResource1">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpName" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpNameResource1">
                        <Header>
                            <asp:Localize ID="locaHeadName" runat="server" Text="<%$ Resources:Tabs, ProfileName %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlNameAndEmail" runat="server" DefaultButton="btnUpdatename" 
                            meta:resourcekey="pnlNameAndEmailResource1">
                                <table>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locEmailPrompt" runat="server" Text="Email" 
                                                meta:resourcekey="locEmailPromptResource1"></asp:Localize>
                                            </td>
                                        <td>
                                            <asp:TextBox runat="server" ID="txtEmail" TextMode="Email"
                                                AutoCompleteType="Email" ValidationGroup="valNameEmail" 
                                                meta:resourcekey="txtEmailResource1" />
                                            <asp:RegularExpressionValidator ID="RegularExpressionValidator1" runat="server" 
                                                ControlToValidate="txtEmail" ValidationGroup="valNameEmail"
                                                ErrorMessage="Please enter a valid email address" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                                CssClass="error" SetFocusOnError="True" Display="Dynamic" 
                                                meta:resourcekey="RegularExpressionValidator1Resource1"></asp:RegularExpressionValidator>
                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server"  ValidationGroup="valNameEmail"
                                            ControlToValidate="txtEmail" CssClass="error" Display="Dynamic" 
                                            ErrorMessage="An email address is required" 
                                                meta:resourcekey="RequiredFieldValidator1Resource1"></asp:RequiredFieldValidator>
                                            <asp:CustomValidator ID="ValidateEmailOK" runat="server" 
                                                ErrorMessage="That email address is already in use by another account" ValidationGroup="valNameEmail"
                                                ControlToValidate="txtEmail" CssClass="error" Display="Dynamic" 
                                                OnServerValidate="VerifyEmailAvailable" 
                                                meta:resourcekey="ValidateEmailOKResource1"></asp:CustomValidator>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td><asp:Localize ID="locRetypeEmailPrompt" runat="server" 
                                                Text="Retype Email" meta:resourcekey="locRetypeEmailPromptResource1"></asp:Localize>
                                            </td>
                                        <td>
                                            <asp:TextBox runat="server" ID="txtEmail2" TextMode="Email"  
                                                AutoCompleteType="Email" ValidationGroup="valNameEmail" 
                                                meta:resourcekey="txtEmail2Resource1" />
                                            <asp:CompareValidator ID="valCompareEmail" ControlToValidate="txtEmail2" 
                                                ControlToCompare="txtEmail" ValidationGroup="valNameEmail"
                                                Display="Dynamic" runat="server" 
                                                ErrorMessage="Please type your e-mail twice (avoids typos)." 
                                                meta:resourcekey="valCompareEmailResource1"></asp:CompareValidator>
                                            <asp:RequiredFieldValidator ID="RequiredFieldValidator2" 
                                                ControlToValidate="txtEmail2" Display="Dynamic" runat="server" 
                                                ValidationGroup="valNameEmail" 
                                                ErrorMessage="Please type your e-mail twice (avoids typos)" 
                                                meta:resourcekey="RequiredFieldValidator2Resource1"></asp:RequiredFieldValidator>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td><asp:Localize ID="locFirstNamePrompt" runat="server" 
                                                Text="First Name" meta:resourcekey="locFirstNamePromptResource1"></asp:Localize>
                                            </td>
                                        <td>
                                            <asp:TextBox ID="txtFirst" runat="server" AutoCompleteType="FirstName" Wrap="False"
                                                ValidationGroup="valNameEmail" meta:resourcekey="txtFirstResource1"></asp:TextBox></td>
                                    </tr>
                                    <tr>
                                        <td><asp:Localize ID="locLastNamePrompt" runat="server" 
                                                Text="Last Name" meta:resourcekey="locLastNamePromptResource1"></asp:Localize>
                                            </td>
                                        <td>
                                            <asp:TextBox ID="txtLast" runat="server" AutoCompleteType="LastName" 
                                                Wrap="False" ValidationGroup="valNameEmail" 
                                                meta:resourcekey="txtLastResource1"></asp:TextBox></td>
                                    </tr>
                                    <tr>
                                        <td style="vertical-align: text-top">
                                            <asp:Localize ID="locAddress" runat="server" Text="Mailing Address" meta:resourcekey="locAddressResource1"></asp:Localize>
                                        </td>
                                        <td>
                                            <asp:TextBox ID="txtAddress" TextMode="MultiLine" Rows="4" runat="server" ValidationGroup="valPilotInfo" meta:resourcekey="txtAddressResource1" Width="300px"></asp:TextBox>
                                            <br />
                                            <asp:Label ID="lblAddressFinePrint" runat="server" CssClass="fineprint" 
                                                Text="(Only used when printing your logbook)" meta:resourcekey="lblAddressFinePrintResource1"></asp:Label>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            &nbsp;</td>
                                        <td>
                                            <asp:Button ID="btnUpdatename" runat="server" Text="Update Name and Email" 
                                                ValidationGroup="valNameEmail" onclick="btnUpdatename_Click" 
                                                meta:resourcekey="btnUpdatenameResource1" />
                                            <br />
                                            <asp:Label ID="lblNameUpdated" runat="server" CssClass="success" EnableViewState="False"
                                                Text="Name and Email successfully updated" Visible="False" 
                                                meta:resourcekey="lblNameUpdatedResource1"></asp:Label>
                                        </td>
                                    </tr>
                                </table>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpPassword" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpPasswordResource1">
                        <Header>
                            <asp:Localize ID="locHeadPass" runat="server" Text="<%$ Resources:Tabs, ProfilePassword %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <ul>
                                <li><asp:Label ID="lblLastLogin" runat="server"></asp:Label></li>
                                <li runat="server" id="itemLastActivity"><asp:Label ID="lblLastActivity" runat="server"></asp:Label></li>
                                <li><asp:Label ID="lblPasswordStatus" runat="server"></asp:Label></li>
                            </ul>
                            <asp:Panel ID="pnlPassword" runat="server" DefaultButton="btnUpdatePass" meta:resourceKey="pnlPasswordResource1">
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
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpQandA" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpQandAResource1">
                        <Header>
                            <asp:Localize ID="locHeadQA" runat="server" Text="<%$ Resources:Tabs, ProfileQA %>" ></asp:Localize>
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlQandA" runat="server" DefaultButton="btnChangeQA" meta:resourceKey="pnlQandAResource1">
                                <% =Resources.LocalizedText.AccountQuestionHint %>
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
                                            <asp:Button ID="btnChangeQA" runat="server" meta:resourceKey="btnChangeQAResource1" OnClick="btnChangeQA_Click" Text="Change Security Question" ValidationGroup="vgNewQA" />
                                            <br />
                                            <asp:Label ID="lblQAChangeSuccess" runat="server" CssClass="success" EnableViewState="False" meta:resourceKey="lblQAChangeSuccessResource1" Text="Security question and answer successfully changed" Visible="False"></asp:Label>
                                        </td>
                                    </tr>
                                </table>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpDeletion" runat="server" ContentCssClass="" HeaderCssClass="">
                        <Header>
                            <asp:Localize ID="locHeadDeletion" runat="server" Text="<%$ Resources:Profile, ProfileDeleteHeader %>"></asp:Localize>
                        </Header>
                        <Content>
                            <p><asp:Localize ID="locDeleteUnusedAircraft" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteAircraftPrompt %>"></asp:Localize></p>
                            <div><asp:Button ID="btnDeleteUnusedAircraft" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteAircraft %>" OnClick="btnDeleteUnusedAircraft_Click" /></div>
                            <p><asp:Localize ID="locDeleteFlights" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteFlightsPrompt %>"></asp:Localize></p>
                            <div><asp:Button ID="btnDeleteFlights" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileBulkDeleteFlights %>" OnClick="btnDeleteFlights_Click" /></div>
                            <div><asp:Label ID="lblDeleteFlightsCompleted" runat="server" Text="<%$ Resources:Profile, ProfileDeleteFlightsCompleted %>" CssClass="success" Font-Bold="true" Visible="false" EnableViewState="false"></asp:Label></div>
                            <cc1:ConfirmButtonExtender ID="confirmDeleteFlights" ConfirmText="<%$ Resources:Profile, ProfileBulkDeleteConfirm %>" TargetControlID="btnDeleteFlights" runat="server" />
                            <p><asp:Localize ID="locCloseAccount" runat="server" Text="<%$ Resources:Profile, ProfileDeleteAccountPrompt %>"></asp:Localize></p>
                            <div><asp:Button ID="btnCloseAccount" Font-Bold="true" ForeColor="Red" runat="server" Text="<%$ Resources:Profile, ProfileDeleteAccount %>" OnClick="btnCloseAccount_Click" /></div>
                            <cc1:ConfirmButtonExtender ID="ConfirmButtonExtender1" ConfirmText="<%$ Resources:Profile, ProfileDeleteAccountConfirm %>" TargetControlID="btnCloseAccount" runat="server" />
                            <asp:Label ID="lblDeleteErr" runat="server" EnableViewState="false" CssClass="error"></asp:Label>
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
                                <div><asp:Label ID="lblPrefTimes" runat="server" Font-Bold="True" Text="Format for times:" meta:resourcekey="lblPrefTimesResource1"></asp:Label></div>
                                <asp:RadioButtonList ID="rblTimeEntryPreference" runat="server" 
                                    ValidationGroup="valPrefs" meta:resourcekey="rblTimeEntryPreferenceResource1">
                                    <asp:ListItem Text="Use decimal" Value="1"
                                        meta:resourcekey="ListItemResource1"></asp:ListItem>
                                    <asp:ListItem Text="Use hours and minutes (HH:MM)" 
                                        Value="0" meta:resourcekey="ListItemResource2"></asp:ListItem>
                                </asp:RadioButtonList>
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
                                            <asp:CheckBox ID="ckTrackCFITime" runat="server" Text="CFI Time" 
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
                    <cc1:AccordionPane runat="server" ID="acpProperties" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpPropertiesResource1">
                        <Header>
                            <asp:Localize ID="locPropertiesHeader" runat="server" Text="Flight Properties and Templates" meta:resourcekey="locPropertiesHeaderResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <h2><asp:Localize ID="locPropHeader" runat="server" Text="<%$ Resources:LogbookEntry, PropertiesHeader %>"></asp:Localize></h2>
                                <p><asp:Localize ID="lblPropertyDesc" runat="server" Text="Properties that you have used on previous flights are automatically shown for new flights.  To reduce clutter, though, you can choose to not display some by default." meta:resourcekey="lblPropertyDescResource1"></asp:Localize></p>
                                <p><asp:Localize ID="locInstructions" runat="server" Text="Drag and drop between the two lists below if using a mouse; if using touch, press-and-hold to move an item between lists." meta:resourcekey="locInstructionsResource1"></asp:Localize></p>
                                <asp:UpdatePanel runat="server" ID="UpdatePanel1">
                                    <ContentTemplate>
                                        <script>
                                            var listDrop = new listDragger('<% =txtPropID.ClientID %>', '<% =btnWhiteList.ClientID %>', '<% =btnBlackList.ClientID %>');
                                        </script>
                                        <div style="display:none">
                                            <asp:TextBox ID="txtPropID" runat="server" EnableViewState="False" meta:resourcekey="txtPropIDResource1"></asp:TextBox>
                                            <asp:Button ID="btnBlackList" runat="server" OnClick="btnBlackList_Click" meta:resourcekey="btnBlackListResource1" />
                                            <asp:Button ID="btnWhiteList" runat="server" OnClick="btnWhiteList_Click" meta:resourcekey="btnWhiteListResource1" />
                                        </div>
                                        <table>
                                            <tr>
                                                <td style="width:50%"><asp:Localize ID="locPrevUsed" runat="server" Text="Show these..." meta:resourcekey="locPrevUsedResource1"></asp:Localize></td>
                                                <td style="width:50%"><asp:Localize ID="locBlackListed" runat="server" Text="...but not these" meta:resourcekey="locBlackListedResource1"></asp:Localize></td>
                                            </tr>
                                            <tr>
                                                <td style="width:50%">
                                                    <div id="divPropsToShow" ondrop="javascript:listDrop.leftListDrop(event)" ondragover="javascript:listDrop.allowDrop(event)" class="dragTarget">
                                                        <asp:Repeater ID="rptUsedProps" runat="server">
                                                            <ItemTemplate>
                                                                <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:listDrop.drag(event, <%# Eval("PropTypeID") %>)" >
                                                                    <%# Eval("Title") %>
                                                                    <script>
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { listDrop.startLeftTouch('<%# Eval("PropTypeID") %>'); });
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { listDrop.resetTouch(); });
                                                                    </script>
                                                                </div>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                </td>
                                                <td style="width:50%">
                                                    <div id="divPropsToBlacklist" ondrop="javascript:listDrop.rightListDrop(event)" ondragover="javascript:listDrop.allowDrop(event)" class="dragTarget">
                                                        <asp:Repeater ID="rptBlackList" runat="server">
                                                            <ItemTemplate>
                                                                <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="javascript:listDrop.drag(event, <%# Eval("PropTypeID") %>)">
                                                                    <%# Eval("Title") %>
                                                                    <script>
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { listDrop.startRightTouch('<%# Eval("PropTypeID") %>'); });
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { listDrop.resetTouch(); });
                                                                    </script>
                                                                </div>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>
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
                                <asp:RadioButtonList ID="rblTotalsOptions" runat="server" meta:resourcekey="rblTotalsOptionsResource1">
                                    <asp:ListItem Selected="True" Text="Show totals by category/class (and type, for models that require a type rating)" Value="CatClass" meta:resourcekey="ListItemResource30" />
                                    <asp:ListItem Text="Show totals by model" Value="Model" meta:resourcekey="ListItemResource31" />
                                    <asp:ListItem Text="Show totals by model with common models grouped by family/ICAO Designator" Value="Family" meta:resourcekey="ListItemResource32" />
                                </asp:RadioButtonList>
                                <div><asp:CheckBox ID="ckIncludeModelFeatureTotals" runat="server" Text="Include subtotals for model features like complex, turbine, tailwheel, etc." meta:resourcekey="ckIncludeModelFeatureTotalsResource1" /></div>
                                <div><asp:Localize ID="locExpireCurrency" Text="When a currency is expired, display it:" runat="server" meta:resourcekey="locExpireCurrencyResource1"></asp:Localize> <asp:DropDownList ID="cmbExpiredCurrency" runat="server" meta:resourcekey="cmbExpiredCurrencyResource1"></asp:DropDownList></div>
                                <h3><%=Resources.Currency.CurrencyPrefsHeader %></h3>
                                <div><asp:CheckBox ID="ckUseArmyCurrency" runat="server" Text="Show AR 95-1 (US Army) Currency" meta:resourcekey="ckUseArmyCurrencyResource1" /></div>
                                <div><asp:CheckBox ID="ckUse117DutyTime" runat="server" Text="Show FAR 117 Duty Time Status" meta:resourcekey="ckUse117DutyTimeResource1" /></div>
                                <div style="margin-left:2em;">
                                    <asp:RadioButtonList ID="rbl117Rules" runat="server">
                                        <asp:ListItem Value="0" Text="<%$ Resources:Currency, Currency117OnlyDutyTimeFlights %>"></asp:ListItem>
                                        <asp:ListItem Selected="True" Value="1" Text="<%$ Resources:Currency, Currency117AllFlights %>"></asp:ListItem>
                                    </asp:RadioButtonList>
                                </div>
                                <div runat="server" id="div135DutyTime" visible="False">
                                    <asp:CheckBox ID="ckUse135DutyTime" runat="server" Text="Show FAR 135 Duty Time Status" meta:resourcekey="ckUse135DutyTimeResource1" />
                                </div>
                                <div><asp:CheckBox ID="ckUse13529xCurrency" runat="server" Text="Show FAR 135.293, 297, 299 Status" meta:resourcekey="ckUse135CurrencyResource1" /></div>
                                <div><asp:CheckBox ID="ckUse13526xCurrency" runat="server" Text="Show FAR 135.265/135.267 Progress" meta:resourcekey="ckUse13526xCurrencyResource1" /></div>
                                <div><asp:CheckBox ID="ckUse61217Currency" runat="server" Text="<%$ Resources:Currency, Part61217Option %>" meta:resourcekey="ckUse61217CurrencyResource1" /></div>
                                <asp:Panel runat="server" ID="pnlLoose6157" meta:resourcekey="pnlLoose6157Resource1">
                                    <asp:CheckBox ID="ck6157c4Pref" runat="server" meta:resourcekey="ck6157c4PrefResource1" Text="Use loose interpretation of 61.57(c)(4)" /> 
                                    <span class="fineprint"><asp:HyperLink ID="lnkCurrencyNotes" meta:resourcekey="lnkCurrencyNotesResource1" runat="server" Text="(See notes on currency computations for details)" Target="_blank" NavigateUrl="~/Public/CurrencyDisclaimer.aspx#instrument"></asp:HyperLink></span>
                                </asp:Panel>
                                <div><asp:CheckBox ID="ckCanadianCurrency" runat="server" meta:resourcekey="ckCanadianCurrencyResource1" Text="Use Canadian currency rules" /></div>
                                <div>
                                    <asp:CheckBox ID="ckLAPLCurrency" runat="server" Text="Use EASA/LAPL currency rules" meta:resourcekey="ckLAPLCurrencyResource1" />
                                    <span class="fineprint"><asp:HyperLink ID="lnkCurrencyNotes2" meta:resourcekey="lnkCurrencyNotesResource1" runat="server" Text="(See notes on currency computations for details)" Target="_blank" NavigateUrl="~/Public/CurrencyDisclaimer.aspx#instrument"></asp:HyperLink></span>
                                </div>
                                <div>
                                    <asp:RadioButtonList ID="rblCurrencyPref" runat="server" 
                                        meta:resourcekey="rblCurrencyPrefResource1">
                                        <asp:ListItem Selected="True" Value="0" 
                                            Text="Show currency by Category/class/type" 
                                            meta:resourcekey="ListItemResource3"></asp:ListItem>
                                        <asp:ListItem Value="1" Text="Show currency for each make/model I have flown" 
                                            meta:resourcekey="ListItemResource4"></asp:ListItem>
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
                    <cc1:AccordionPane ID="acpSocialNetworking" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpSocialNetworkingResource1">
                        <Header>
                            <asp:Label ID="lblSocialNetworkingPrompt" runat="server" 
                                Text="Sharing" meta:resourcekey="lblSocialNetworkingPromptResource1"></asp:Label>
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
                            <div class="prefSectionRow">
                                <asp:Label ID="lblSocNetworkPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="Preferences successfully updated" Visible="False" meta:resourcekey="lblSocNetworkPrefsUpdatedResource1"></asp:Label>
                            </div>
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
