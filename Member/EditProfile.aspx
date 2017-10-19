<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master"
    CodeFile="EditProfile.aspx.cs" Inherits="Member_EditProfile" Title="Edit Profile" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc2" %>
<%@ Register Src="../Controls/mfbTwitter.ascx" TagName="mfbTwitter" TagPrefix="uc3" %>
<%@ Register src="../Controls/mfbFacebook.ascx" tagname="mfbFacebook" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbEndorsementList.ascx" tagname="mfbEndorsementList" tagprefix="uc6" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbGooglePlus.ascx" tagname="mfbGooglePlus" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc9" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbDeadlines.ascx" TagPrefix="uc1" TagName="mfbDeadlines" %>
<%@ Register Src="~/Controls/mfbCustCurrency.ascx" TagPrefix="uc1" TagName="mfbCustCurrency" %>
<%@ Register Src="~/Controls/oAuthAuthorizationManager.ascx" TagPrefix="uc1" TagName="oAuthAuthorizationManager" %>
<%@ Register Src="~/Controls/mfbSubscriptionManager.ascx" TagPrefix="uc1" TagName="mfbSubscriptionManager" %>
<%@ Register Src="~/Controls/mfbCustomCurrencyList.ascx" TagPrefix="uc1" TagName="mfbCustomCurrencyList" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <script type="text/javascript" src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script type="text/javascript" src='<%= ResolveUrl("~/public/jquery.json-2.4.min.js") %>'></script>
    <asp:Label ID="lblName" runat="server" meta:resourcekey="lblNameResource1"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwAccount">
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
                                            <cc1:PasswordStrength ID="PasswordStrength2" runat="server" BehaviorID="PasswordStrength2" TargetControlID="NewPassword" TextStrengthDescriptions="<%$ Resources:LocalizedText, PasswordStrengthStrings %>" />
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
                                <table cellpadding="4">
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
                                    ValidationGroup="valPrefs" onclick="btnUpdateLocalPrefs_Click" />
                                <br />
                                <asp:Label ID="lblLocalPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False"></asp:Label>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpProperties" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpPropertiesResource1">
                        <Header>
                            <asp:Localize ID="locPropertiesHeader" runat="server" Text="Flight Properties" meta:resourcekey="locPropertiesHeaderResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <script type="text/javascript">
                                    var longTouchTimer = null;

                                    function allowDrop(ev) {
                                        ev.preventDefault();
                                    }

                                    function drag(ev, id) {
                                        ev.dataTransfer.setData("Text", id.toString());
                                    }

                                    function blackList(id) {
                                        document.getElementById('<% = txtPropID.ClientID %>').value = id;;
                                        document.getElementById('<% = btnBlackList.ClientID %>').click();
                                    }

                                    function whiteList(id) {
                                        document.getElementById('<% = txtPropID.ClientID %>').value = id;
                                        document.getElementById('<% = btnWhiteList.ClientID %>').click();
                                    }

                                    function blackListDrop(ev) {
                                        ev.preventDefault();
                                        blackList(ev.dataTransfer.getData("Text"));
                                    }

                                    function whiteListDrop(ev) {
                                        ev.preventDefault();
                                        whiteList(ev.dataTransfer.getData("Text"));
                                    }
                                </script>
                                <p><asp:Localize ID="lblPropertyDesc" runat="server" Text="Properties that you have used on previous flights are automatically shown for new flights.  To reduce clutter, though, you can choose to not display some by default." meta:resourcekey="lblPropertyDescResource1"></asp:Localize></p>
                                <p><asp:Localize ID="locInstructions" runat="server" Text="Drag and drop between the two lists below if using a mouse; if using touch, press-and-hold to move an item between lists." meta:resourcekey="locInstructionsResource1"></asp:Localize></p>
                                <asp:UpdatePanel runat="server" ID="UpdatePanel1">
                                    <ContentTemplate>
                                        <div style="display:none">
                                            <asp:TextBox ID="txtPropID" runat="server" EnableViewState="False" meta:resourcekey="txtPropIDResource1"></asp:TextBox>
                                            <asp:Button ID="btnBlackList" runat="server" OnClick="btnBlackList_Click" meta:resourcekey="btnBlackListResource1" />
                                            <asp:Button ID="btnWhiteList" runat="server" OnClick="btnWhiteList_Click" meta:resourcekey="btnWhiteListResource1" />
                                        </div>
                                        <table>
                                            <tr>
                                                <td><asp:Localize ID="locPrevUsed" runat="server" Text="Show these..." meta:resourcekey="locPrevUsedResource1"></asp:Localize></td>
                                                <td><asp:Localize ID="locBlackListed" runat="server" Text="...but not these" meta:resourcekey="locBlackListedResource1"></asp:Localize></td>
                                            </tr>
                                            <tr>
                                                <td>
                                                    <div id="divPropsToShow" ondrop="whiteListDrop(event)" ondragover="allowDrop(event)" class="dragTarget">
                                                        <asp:Repeater ID="rptUsedProps" runat="server">
                                                            <ItemTemplate>
                                                                <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="drag(event, <%# Eval("PropTypeID") %>)" >
                                                                    <%# Eval("Title") %>
                                                                    <script type="text/javascript">
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { longTouchTimer = setTimeout(function () { blackList(<%# Eval("PropTypeID") %>); }, 1000); });
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { clearTimeout(longTouchTimer); });
                                                                    </script>
                                                                </div>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                </td>
                                                <td>
                                                    <div id="divPropsToBlacklist" ondrop="blackListDrop(event)" ondragover="allowDrop(event)" class="dragTarget">
                                                        <asp:Repeater ID="rptBlackList" runat="server">
                                                            <ItemTemplate>
                                                                <div draggable="true" id="cpt<%# Eval("PropTypeID") %>" class="draggableItem" ondragstart="drag(event, <%# Eval("PropTypeID") %>)">
                                                                    <%# Eval("Title") %>
                                                                    <script type="text/javascript">
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchstart", function () { longTouchTimer = setTimeout(function () { whiteList(<%# Eval("PropTypeID") %>); }, 1000); });
                                                                        document.getElementById('cpt<%# Eval("PropTypeID") %>').addEventListener("touchend", function () { clearTimeout(longTouchTimer); });
                                                                    </script>
                                                                </div>
                                                            </ItemTemplate>
                                                        </asp:Repeater>
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>
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
                                <div><asp:CheckBox ID="ckPerModelTotals" Text="Show totals by model" runat="server" meta:resourcekey="ckPerModelTotalsResource2" /></div>
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
                                <div>
                                    <asp:CheckBox ID="ck6157c4Pref" runat="server" meta:resourcekey="ck6157c4PrefResource1" Text="Use loose interpretation of 61.57(c)(4)" /> 
                                    <span class="fineprint"><asp:HyperLink ID="lnkCurrencyNotes" meta:resourcekey="lnkCurrencyNotesResource1" runat="server" Text="(See notes on currency computations for details)" Target="_blank" NavigateUrl="~/Public/CurrencyDisclaimer.aspx#instrument"></asp:HyperLink></span>
                                </div>
                                <div><asp:CheckBox ID="ckCanadianCurrency" runat="server" meta:resourcekey="ckCanadianCurrencyResource1" Text="Use Canadian currency rules" /></div>
                                <div>
                                    <asp:CheckBox ID="ckLAPLCurrency" runat="server" Text="Use EASA LAPL currency rules" meta:resourcekey="ckLAPLCurrencyResource1" />
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
                                    ValidationGroup="valPrefs" onclick="btnUpdateCurrencyPrefs_Click" />
                                <br />
                                <asp:Label ID="lblCurrencyPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False"></asp:Label>
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
                                <asp:UpdatePanel runat="server" ID="updDeadlines">
                                    <ContentTemplate>
                                        <div id="divDeadlinesHeader" runat="server">
                                            <p>
                                                <asp:Label ID="lblDeadlinesHeader" runat="server" meta:resourceKey="lblDeadlinesHeaderResource1" Text="You can define custom deadlines that will appear in your currency.  The difference from other currencies is that a deadline isn't impacted by your flying."></asp:Label>
                                            </p>
                                            <p>
                                                <asp:Label ID="lblDeadlinesheader2" runat="server" meta:resourceKey="lblDeadlinesheader2Resource1" Text="Examples include certificate/registration expirations, inspections mandated by airworthiness directives, employer-imposed deadlines, and such"></asp:Label>
                                            </p>
                                            <p>
                                                <asp:Label ID="lblDeadlinesHeader3" runat="server" meta:resourcekey="lblDeadlinesHeader3Resource1" Text="A deadline may be due based on a calendar date, or, if associated with an aircraft, by the hours on that aircraft (typically hobbs or tachometer)."></asp:Label>
                                            </p>
                                            <asp:Label ID="lblAddDeadlines" runat="server" Font-Bold="True" meta:resourceKey="lblAddDeadlinesResource1"></asp:Label>
                                        </div>
                                        <asp:Panel ID="pnlAddDeadlines" runat="server" DefaultButton="btnAddDeadline" meta:resourceKey="pnlAddDeadlinesResource1" style="overflow:hidden">
                                            <table style="border-spacing: 5px;">
                                                <tr valign="middle">
                                                    <td>
                                                        <asp:Label ID="lblDeadlineName" runat="server" meta:resourceKey="lblDeadlineNameResource1" Text="Name"></asp:Label>
                                                    </td>
                                                    <td>
                                                        <asp:TextBox ID="txtDeadlineName" runat="server" meta:resourceKey="txtDeadlineNameResource1"></asp:TextBox>
                                                        <asp:RequiredFieldValidator ID="valDeadlineName" runat="server" ControlToValidate="txtDeadlineName" CssClass="error" ErrorMessage="Please give this deadline a name." meta:resourceKey="valDeadlineNameResource1" ValidationGroup="vgAddDeadlines"></asp:RequiredFieldValidator>
                                                    </td>
                                                </tr>
                                                <tr valign="top">
                                                    <td>
                                                        <asp:Label ID="lblDeadlineAircraft" runat="server" meta:resourcekey="lblDeadlineAircraftResource1" Text="Associated Aircraft:"></asp:Label>
                                                        <div class="fineprint">
                                                            <asp:Label ID="lblDeadlineAircraftOptional" runat="server" meta:resourcekey="lblDeadlineAircraftOptionalResource1" Text="(Optional)"></asp:Label>
                                                        </div>
                                                    </td>
                                                    <td>
                                                        <asp:DropDownList ID="cmbDeadlineAircraft" runat="server" AppendDataBoundItems="True" AutoPostBack="True" DataTextField="TailNumber" DataValueField="AircraftID" meta:resourcekey="cmbDeadlineAircraftResource1" OnSelectedIndexChanged="cmbDeadlineAircraft_SelectedIndexChanged">
                                                            <asp:ListItem meta:resourcekey="ListItemResource20" Selected="True" Text="(None)" Value=""></asp:ListItem>
                                                        </asp:DropDownList>
                                                        <asp:CheckBox ID="ckDeadlineUseHours" runat="server" AutoPostBack="True" meta:resourcekey="ckDeadlineUseHoursResource1" OnCheckedChanged="ckDeadlineUseHours_CheckedChanged" Text="Deadline is determined using aircraft hours, not a date" Visible="False" />
                                                    </td>
                                                </tr>
                                                <tr valign="middle">
                                                    <td>
                                                        <asp:Label ID="lblInitialDueDate" runat="server" meta:resourcekey="lblInitialDueDateResource2" Text="Deadline is due:"></asp:Label>
                                                    </td>
                                                    <td>
                                                        <asp:MultiView ID="mvDeadlineDue" runat="server" ActiveViewIndex="0">
                                                            <asp:View ID="vwDeadlineDueDate" runat="server">
                                                                <uc2:mfbTypeInDate ID="mfbDeadlineDate" runat="server" />
                                                            </asp:View>
                                                            <asp:View ID="vwDeadlineDueHours" runat="server">
                                                                <uc9:mfbDecimalEdit ID="decDueHours" runat="server" EditingMode="Decimal" Width="40" />
                                                            </asp:View>
                                                        </asp:MultiView>
                                                    </td>
                                                </tr>
                                                <tr valign="top">
                                                    <td>
                                                        <asp:Label ID="lblRegen" runat="server" meta:resourceKey="lblRegenResource1" Text="Regeneration:"></asp:Label>
                                                    </td>
                                                    <td>
                                                        <asp:Label ID="lblRegenExplain" runat="server" meta:resourceKey="lblRegenExplainResource1" Text="When the deadline has passed and you've done whatever is required:"></asp:Label>
                                                        <br />
                                                        <table>
                                                            <tr valign="middle">
                                                                <td valign="top">
                                                                    <asp:RadioButton ID="rbRegenManual" runat="server" Checked="True" GroupName="regenType" meta:resourcekey="rbRegenManualResource1" />
                                                                </td>
                                                                <td valign="top">
                                                                    <asp:Label ID="lblDeadlineManual" runat="server" AssociatedControlID="rbRegenManual" meta:resourceKey="lblDeadlineManualResource1" Text="Manually update or delete the deadline"></asp:Label>
                                                                </td>
                                                            </tr>
                                                            <tr valign="middle">
                                                                <td valign="top">
                                                                    <asp:RadioButton ID="rbRegenInterval" runat="server" GroupName="regenType" meta:resourcekey="rbRegenIntervalResource1" />
                                                                </td>
                                                                <td valign="top">
                                                                    <asp:Label ID="lblDeadlineDays" runat="server" AssociatedControlID="rbRegenInterval" meta:resourcekey="lblDeadlineDaysResource2" Text="Extend the deadline by:"></asp:Label>
                                                                    <uc9:mfbDecimalEdit ID="decRegenInterval" runat="server" EditingMode="Integer" Width="40" />
                                                                    <asp:MultiView ID="mvRegenInterval" runat="server" ActiveViewIndex="0">
                                                                        <asp:View ID="vwDeadlineCalendarRange" runat="server">
                                                                            <asp:DropDownList ID="cmbRegenRange" runat="server" meta:resourcekey="cmbRegenRangeResource1">
                                                                                <asp:ListItem meta:resourcekey="ListItemResource21" Selected="True" Text="Days" Value="Days"></asp:ListItem>
                                                                                <asp:ListItem meta:resourcekey="ListItemResource22" Text="Calendar Months" Value="CalendarMonths"></asp:ListItem>
                                                                            </asp:DropDownList>
                                                                        </asp:View>
                                                                        <asp:View ID="vwDeadlineHours" runat="server">
                                                                            <asp:Label ID="lblHours" runat="server" meta:resourcekey="lblHoursResource1" Text="Hours"></asp:Label>
                                                                            <asp:Label ID="lblHoursTip" runat="server" CssClass="fineprint" meta:resourcekey="lblHoursTipResource1" Text="(Typically tach or hobbs)"></asp:Label>
                                                                        </asp:View>
                                                                    </asp:MultiView>
                                                                </td>
                                                            </tr>
                                                        </table>
                                                    </td>
                                                </tr>
                                                <tr valign="middle">
                                                    <td>&nbsp;</td>
                                                    <td>
                                                        <asp:Button ID="btnAddDeadline" runat="server" meta:resourceKey="btnAddDeadlineResource1" OnClick="btnAddDeadline_Click" Text="Add Deadline" ValidationGroup="vgAddDeadlines" />
                                                        <asp:Label ID="lblErrDeadline" runat="server" CssClass="error" EnableViewState="False" meta:resourceKey="lblErrResource1"></asp:Label>
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:Panel>
                                        <cc1:CollapsiblePanelExtender ID="cpeDeadlines" runat="server" BehaviorID="cpeDeadlines" CollapseControlID="divDeadlinesHeader" Collapsed="True" CollapsedSize="0" CollapsedText="(Click to create a new deadline)" ExpandControlID="divDeadlinesHeader" ExpandedText="(Click to hide)" TargetControlID="pnlAddDeadlines" TextLabelID="lblAddDeadlines" />
                                        <uc1:mfbDeadlines ID="mfbDeadlines1" runat="server" />
                                    </ContentTemplate>
                                    <Triggers>
                                        <asp:AsyncPostBackTrigger ControlID="btnAddDeadline" />
                                        <asp:AsyncPostBackTrigger ControlID="cmbDeadlineAircraft" />
                                        <asp:AsyncPostBackTrigger ControlID="ckDeadlineUseHours" />
                                    </Triggers>
                                </asp:UpdatePanel>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpSocialNetworking" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpSocialNetworkingResource1">
                        <Header>
                            <asp:Label ID="lblSocialNetworkingPrompt" runat="server" 
                                Text="Sharing and Social Networking" meta:resourcekey="lblSocialNetworkingPromptResource1"></asp:Label>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <table>
                                    <tr valign="top">
                                        <td style="width:150px">
                                            <p><asp:Image ID="imgTwitter" runat="server" AlternateText="Twitter" Height="18px" 
                                            ImageUrl="~/images/twitter_logo_header.png" 
                                            ToolTip="Twitter" Width="82px" meta:resourcekey="imgTwitterResource1" /></p>
                                        </td>
                                        <td>
                                            <asp:Panel ID="pnlCanTweet" runat="server" 
                                                meta:resourcekey="pnlCanTweetResource1">
                                                <p><asp:Label ID="lblCanTweet" runat="server" 
                                                    meta:resourcekey="lblCanTweetResource1"></asp:Label></p>
                                                <p><asp:LinkButton ID="lnkNoTweet" runat="server" OnClick="lnkNoTweet_Click" 
                                                    meta:resourcekey="lnkNoTweetResource1"></asp:LinkButton></p>
                                            </asp:Panel>
                                            <asp:LinkButton ID="lnkSetUpTwitter" runat="server" 
                                                OnClick="lnkSetUpTwitter_Click" 
                                                meta:resourcekey="lnkSetUpTwitterResource1"></asp:LinkButton>
                                            <uc3:mfbTwitter ID="mfbTwitter1" runat="server" />
                                        </td>
                                    </tr>
                                    <tr valign="top">
                                        <td style="width:150px">
                                            <p><asp:Image ID="imgFacebook" AlternateText="Facebook" ToolTip="Facebook" ImageUrl="~/images/facebooklogo.png" runat="server" meta:resourcekey="imgFacebookResource1" /></p>
                                        </td>
                                        <td>
                                            <asp:Panel ID="pnlFacebookAuthorized" runat="server" 
                                                meta:resourcekey="pnlFacebookAuthorizedResource1">
                                                <p><asp:Localize ID="locFBIsAuthorized" runat="server" 
                                                            meta:resourcekey="locFBIsAuthorizedResource1"></asp:Localize></p>
                                                <p><asp:LinkButton
                                                            ID="lnkNoFacebook" runat="server" onclick="lnkNoFacebook_Click" 
                                                            meta:resourcekey="lnkNoFacebookResource1"></asp:LinkButton></p>
                                            </asp:Panel>
                                            <asp:LinkButton ID="lnkSetUpFacebook" runat="server" 
                                                onclick="lnkSetUpFacebook_Click" 
                                                meta:resourcekey="lnkSetUpFacebookResource1"></asp:LinkButton>
                                            <uc5:mfbFacebook ID="mfbFacebook1" runat="server" />
                                        </td>
                                    </tr>
                                    <!-- Google Plus is not yet ready for prime-time -->
                                    <tr runat="server" id="rowGPlus" visible="False" valign="top">
                                        <td style="width:150px" runat="server">
                                            <p><asp:Image ID="Image1" runat="server" 
                                                ImageUrl="https://ssl.gstatic.com/s2/oz/images/google-logo-plus-0fbe8f0119f4a902429a5991af5db563.png" /></p>
                                        </td>
                                        <td runat="server">
                                            <asp:Panel ID="pnlGooglePlus" runat="server">
                                            <asp:Localize ID="locGPIsAuthorized" runat="server" Text="You have authorized MyFlightbook to post the flights you select on Google+."></asp:Localize>
                                            <asp:LinkButton
                                                    ID="lnkDeAuthGooglePlus" runat="server" 
                                                    onclick="lnkDeAuthGooglePlus_Click" Text="Click here to de-authorize MyFlightbook on Google+"></asp:LinkButton>
                                            </asp:Panel>
                                            <asp:LinkButton ID="lnkAuthorizeGooglePlus" runat="server" 
                                                onclick="lnkAuthorizeGooglePlus_Click" Text="Authorize MyFlightbook to post selected flights to your account on Google+"></asp:LinkButton>
                                            <uc1:mfbGooglePlus ID="mfbGooglePlus1" runat="server" />
                                        </td>
                                    </tr>
                                    <tr valign="top">
                                        <td style="width:150px">
                                            <p><asp:Image ID="imgMyFlightbook" Width="100px" runat="server" ImageAlign="Middle" meta:resourcekey="imgMyFlightbookResource1" />
                                            </p>
                                        </td>
                                        <td>
                                            <p><asp:Localize ID="locShareAllFlightsPrompt" runat="server" 
                                                Text="Share your public flights with this link:" 
                                                meta:resourcekey="locShareAllFlightsPromptResource1"></asp:Localize></p>
                                            <p><asp:HyperLink ID="lnkMyFlights" runat="server" Target="_blank" 
                                                meta:resourcekey="lnkMyFlightsResource1"></asp:HyperLink></p>
                                            <p>
                                            <asp:Localize ID="locShareAllFlightsDisclaimer" runat="server" 
                                                Text="This will ONLY show flights that you have designated to be shared." 
                                                meta:resourcekey="locShareAllFlightsDisclaimerResource1"></asp:Localize></p>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <div class="prefSectionRow">
                                <asp:Label ID="lblSocNetworkPrefsUpdated" runat="server" CssClass="success" EnableViewState="False"
                                    Text="Preferences successfully updated" Visible="False" meta:resourcekey="lblSocNetworkPrefsUpdatedResource1"></asp:Label>
                            </div>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane ID="acpBackup" runat="server" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpBackupResource1">
                        <Header>
                            <asp:Localize ID="locCloudStorage" runat="server" Text="Cloud Backup" 
                                meta:resourceKey="locCloudStorageResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <div class="prefSectionRow">
                                <p><asp:Localize ID="locAboutCloudStorage" runat="server" 
                                    Text="" meta:resourcekey="locAboutCloudStorageResource1"></asp:Localize></p>
                                <table>
                                    <tr valign="top">
                                        <td style="width:180px">
                                            <!-- This comes from https://www.dropbox.com/developers/reference/branding, per their guidelines -->
                                            <asp:Image ID="DropboxLogo" 
                                                ImageUrl="~/images/dropbox-logos_dropbox-logotype-blue.png" runat="server" 
                                                AlternateText="Dropbox" meta:resourcekey="DropboxLogoResource1" Width="180px" />
                                        </td>
                                        <td>
                                            <asp:MultiView ID="mvDropBoxState" runat="server">
                                                <asp:View ID="vwAuthDropBox" runat="server">
                                                    <p><asp:LinkButton ID="lnkAuthDropbox" runat="server" 
                                                        onclick="lnkAuthDropbox_Click" meta:resourceKey="lnkAuthDropboxResource1"></asp:LinkButton></p>
                                                </asp:View>
                                                <asp:View ID="vwDeAuthDropbox" runat="server">
                                                    <p><asp:Localize ID="locDropboxIsAuthed" 
                                                        runat="server" meta:resourcekey="locDropboxIsAuthedResource1"></asp:Localize></p>
                                                    <p>
                                                    <asp:LinkButton ID="lnkDeAuthDropbox" runat="server" 
                                                        onclick="lnkDeAuthDropbox_Click" 
                                                        meta:resourceKey="lnkDeAuthDropboxResource1"></asp:LinkButton></p>
                                                </asp:View>
                                            </asp:MultiView>
                                        </td>
                                    </tr>
                                    <tr valign="top" runat="server" id="rowGDrive">
                                        <td style="width:180px" runat="server">
                                            <!-- This comes from https://developers.google.com/drive/v2/web/branding, per their guidelines -->
                                            <asp:Image ID="GDriveLogo" 
                                                ImageUrl="~/images/google-drive-logo-lockup.png" runat="server" 
                                                AlternateText="GoogleDrive" Width="180px" />
                                        </td>
                                        <td runat="server">
                                            <asp:MultiView ID="mvGDriveState" runat="server">
                                                <asp:View ID="vwAuthGDrive" runat="server">
                                                    <p><asp:LinkButton ID="lnkAuthorizeGDrive" runat="server" OnClick="lnkAuthorizeGDrive_Click"></asp:LinkButton></p>
                                                </asp:View>
                                                <asp:View ID="vwDeAuthGDrive" runat="server">
                                                    <p><asp:Localize ID="locGoogleDriveIsAuthed" 
                                                        runat="server"></asp:Localize></p>
                                                    <p><asp:LinkButton ID="lnkDeAuthGDrive" runat="server" OnClick="lnkDeAuthGDrive_Click"></asp:LinkButton></p>
                                                </asp:View>
                                            </asp:MultiView>
                                        </td>
                                    </tr>
                                    <tr valign="top" runat="server" id="rowOneDrive">
                                        <td style="width:180px" runat="server">
                                            <!-- This comes from https://msdn.microsoft.com/en-us/onedrive/dn673556.aspx, per their guidelines -->
                                            <asp:Image ID="Image3" 
                                                ImageUrl="~/images/OneDrive_rgb_Blue2728.png" runat="server" 
                                                AlternateText="OneDrive" Width="180px" />
                                        </td>
                                        <td runat="server">
                                            <asp:MultiView ID="mvOneDriveState" runat="server">
                                                <asp:View ID="vwAuthOneDrive" runat="server">
                                                    <p><asp:LinkButton ID="lnkAuthorizeOneDrive" runat="server" OnClick="lnkAuthorizeOneDrive_Click"></asp:LinkButton></p>
                                                </asp:View>
                                                <asp:View ID="vwDeAuthOneDrive" runat="server">
                                                    <p><asp:Localize ID="locOneDriveIsAuthed" 
                                                        runat="server"></asp:Localize></p>
                                                    <p><asp:LinkButton ID="lnkDeAuthOneDrive" runat="server" OnClick="lnkDeAuthOneDrive_Click"></asp:LinkButton></p>
                                                </asp:View>
                                            </asp:MultiView>
                                        </td>
                                    </tr>
                                </table>
                            </div>
                            <asp:Panel ID="pnlDefaultCloud" Visible="False" runat="server" meta:resourceKey="pnlDefaultCloudResource1">
                                <hr />
                                <asp:Label ID="lblPickDefault" runat="server" Text="<%$ Resources:LocalizedText, CloudStoragePickDefault %>" meta:resourceKey="lblPickDefaultResource1"></asp:Label>
                                <asp:DropDownList ID="cmbDefaultCloud" AutoPostBack="True" OnSelectedIndexChanged="cmbDefaultCloud_SelectedIndexChanged" runat="server" meta:resourceKey="cmbDefaultCloudResource1">
                                </asp:DropDownList>
                            </asp:Panel>
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
            <cc1:Accordion ID="accordianPilotInfo" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" meta:resourcekey="accordianPrefsResource1" TransitionDuration="250">
                <Panes>
                    <cc1:AccordionPane runat="server" ID="acpMedical" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpMedicalResource1" >
                        <Header>
                            <asp:Localize ID="locHeaderMedical" runat="server" Text="<%$ Resources:Profile, ProfilePilotInfoMedical %>"></asp:Localize>
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlMedical" runat="server" DefaultButton="btnUpdateMedical" meta:resourcekey="pnlMedicalResource1">
                                <table cellpadding="3px">
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locLastMedicalPrompt" runat="server"
                                                Text="Date of Last Medical" meta:resourcekey="locLastMedicalPromptResource1"></asp:Localize></td>
                                        <td>
                                            <uc2:mfbTypeInDate ID="dateMedical" ValidationGroup="valPilotInfo" runat="server" />
                                        </td>
                                    </tr>
                                    <tr>
                                        <td>
                                            <asp:Localize ID="locMedicalDurationPrompt" runat="server"
                                                Text="Duration"
                                                meta:resourcekey="locMedicalDurationPromptResource1"></asp:Localize></td>
                                        <td>
                                            <asp:DropDownList ID="cmbMonthsMedical" runat="server"
                                                ValidationGroup="valPilotInfo"
                                                meta:resourcekey="cmbMonthsMedicalResource1">
                                                <asp:ListItem Selected="True" Value="0" Text="(Unspecified)"
                                                    meta:resourcekey="ListItemResource11"></asp:ListItem>
                                                <asp:ListItem Value="6" Text="6 Months" meta:resourcekey="ListItemResource12"></asp:ListItem>
                                                <asp:ListItem Value="12" Text="12 Months" meta:resourcekey="ListItemResource13"></asp:ListItem>
                                                <asp:ListItem Value="24" Text="24 Months" meta:resourcekey="ListItemResource14"></asp:ListItem>
                                                <asp:ListItem Value="36" Text="36 Months" meta:resourcekey="ListItemResource15"></asp:ListItem>
                                                <asp:ListItem Value="48" Text="48 Months" meta:resourcekey="ListItemResource16"></asp:ListItem>
                                                <asp:ListItem Value="60" Text="60 Months" meta:resourcekey="ListItemResource17"></asp:ListItem>
                                            </asp:DropDownList>
                                            <asp:CustomValidator ID="CustomValidator1" runat="server"
                                                ErrorMessage="Please specify the duration of your medical."
                                                ControlToValidate="cmbMonthsMedical" CssClass="error"
                                                OnServerValidate="DurationIsValid" ValidationGroup="valPilotInfo"
                                                meta:resourcekey="CustomValidator1Resource1"></asp:CustomValidator>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td></td>
                                        <td>
                                            <asp:RadioButtonList ID="rblMedicalDurationType" runat="server" meta:resourcekey="rblMedicalDurationTypeResource1">
                                                <asp:ListItem Value="0" Text="Use FAA rules (calendar months)" meta:resourcekey="ListItemResource7" Selected="True"></asp:ListItem>
                                                <asp:ListItem Value="1" Text="Use ICAO rules (day for day)" meta:resourcekey="ListItemResource8"></asp:ListItem>
                                            </asp:RadioButtonList></td>
                                    </tr>
                                </table>

                                    <asp:Panel ID="pnlNextMedical" runat="server" Visible="False" 
                                        meta:resourcekey="pnlNextMedicalResource1"><br />
                                        <asp:Localize ID="locNextMedicalDuePrompt" runat="server" Text="<%$ Resources:Currency, NextMedical %>"></asp:Localize>
                                        <asp:Label ID="lblNextMedical" runat="server" Style="font-weight: bold;" 
                                            meta:resourcekey="lblNextMedicalResource1"></asp:Label>.<br /><br /></asp:Panel>
                                    <div>
                                        <asp:Button ID="btnUpdateMedical" runat="server"  
                                            Text="<%$ Resources:Profile, ProfilePilotInfoMedicalUpdate %>" ValidationGroup="valMedical" 
                                            onclick="btnUpdateMedical_Click" meta:resourcekey="btnUpdateMedicalResource1" />
                                    </div>
                                    <div>
                                        <asp:Label ID="lblMedicalInfo" runat="server" CssClass="success" EnableViewState="False"
                                            Text="<%$ Resources:Profile, ProfilePilotInfoMedicalUpdated %>" Visible="False" meta:resourcekey="lblMedicalInfoResource1"></asp:Label>
                                    </div>
                                    <asp:Panel ID="pnlBasicMed" runat="server" meta:resourcekey="pnlBasicMedResource1">
                                        <h3><% =Resources.Profile.BasicMedHeader %></h3>
                                        <p><% =Resources.Profile.BasicMedDescription %></p>
                                        <ol style="list-style-type:lower-alpha">
                                            <li><%=Resources.Profile.BasicMedDescriptionA %></li>
                                            <li><%=Resources.Profile.BasicMedDescriptionB %></li>
                                            <li><%=Resources.Profile.BasicMedDescriptionC %></li>
                                        </ol>
                                        <p><a href="javascript:void(0);"><asp:Label ID="lblAddBaiscMedEvent" runat="server" Font-Bold="True" Text="<%$ Resources:Profile, BasicMedAddEventPrompt %>" meta:resourcekey="lblAddBaiscMedEventResource1"></asp:Label></a></p>
                                        <asp:Panel ID="pnlAddBasicMedEvent" runat="server" meta:resourcekey="pnlAddBasicMedEventResource1">
                                            <table>
                                                <tr>
                                                    <td><% =Resources.Profile.BasicMedEventDate %></td>
                                                    <td><uc2:mfbTypeInDate runat="server" ID="mfbBasicMedEventDate" /></td>
                                                </tr>
                                                <tr>
                                                    <td><% =Resources.Profile.BasicMedEventActivity %></td>
                                                    <td>
                                                        <asp:RadioButtonList ID="rblBasicMedAction" runat="server">
                                                            <asp:ListItem Text="<%$ Resources:Profile, BasicMedMedicalCourse %>" Selected="True" Value="AeromedicalCourse"></asp:ListItem>
                                                            <asp:ListItem Text="<%$ Resources:Profile, BasicMedPhysicianVisit %>" Value="PhysicianVisit"></asp:ListItem>
                                                        </asp:RadioButtonList>
                                                    </td>
                                                </tr>
                                                <tr style="vertical-align:top">
                                                    <td><% =Resources.Profile.BasicMedEventDescription %></td>
                                                    <td><asp:TextBox ID="txtBasicMedNotes" TextMode="MultiLine" runat="server" meta:resourcekey="txtBasicMedNotesResource1"></asp:TextBox></td>
                                                </tr>
                                                <tr style="vertical-align:top">
                                                    <td></td>
                                                    <td>
                                                        <p><% =Resources.Profile.BasicMedAttachDocumentation %></p>
                                                        <uc7:mfbMultiFileUpload runat="server" ID="mfuBasicMedImages" Mode="Legacy" Class="BasicMed" IncludeDocs="true" IncludeVideos="false" RefreshOnUpload="true" />
                                                    </td>
                                                </tr>
                                                <tr style="vertical-align:top">
                                                    <td></td>
                                                    <td>
                                                        <br />
                                                        <asp:Button ID="btnAddBasicMedEvent" runat="server" Text="<%$ Resources:Profile, BasicMedAddEvent %>" OnClick="btnAddBasicMedEvent_Click" meta:resourcekey="btnAddBasicMedEventResource1" />
                                                        <asp:Label ID="lblBasicMedErr" CssClass="error" runat="server" EnableViewState="False" meta:resourcekey="lblBasicMedErrResource1"></asp:Label>
                                                    </td>
                                                </tr>
                                            </table>
                                        </asp:Panel>
                                        <cc1:CollapsiblePanelExtender ID="cpeBasicMedEvents" runat="server" CollapseControlID="lblAddBaiscMedEvent" Collapsed="True" 
                                                CollapsedSize="0" 
                                            CollapsedText="<%$ Resources:Profile, BasicMedAddEventPrompt %>" 
                                                ExpandControlID="lblAddBaiscMedEvent" ExpandedText="<%$ Resources:LocalizedText, ClickToHide %>" 
                                                TargetControlID="pnlAddBasicMedEvent" TextLabelID="lblAddBaiscMedEvent" BehaviorID="cpeBasicMed"></cc1:CollapsiblePanelExtender>
                                        <asp:GridView ID="gvBasicMedEvents" runat="server" OnRowCommand="gvBasicMedEvents_RowCommand" OnRowEditing="gvBasicMedEvents_RowEditing" BorderStyle="None" BorderWidth="0px"
                                            CellPadding="3" GridLines="None"
                                            OnRowCancelingEdit="gvBasicMedEvents_RowCancelingEdit" OnRowUpdating="gvBasicMedEvents_RowUpdating" OnRowDataBound="gvBasicMedEvents_RowDataBound" AutoGenerateColumns="False" ShowHeader="False" meta:resourcekey="gvBasicMedEventsResource1">
                                            <Columns>
                                                <asp:TemplateField meta:resourcekey="TemplateFieldResource2">
                                                    <ItemTemplate>
                                                        <asp:ImageButton ID="imgDelete" runat="server" 
                                                            AlternateText="<%$ Resources:Profile, BasicMedDeleteTooltip %>" CommandArgument='<%# Bind("ID") %>' 
                                                            CommandName="_Delete" ImageUrl="~/images/x.gif" 
                                                            ToolTip="<%$ Resources:Profile, BasicMedDeleteTooltip %>" />
                                                        <cc1:ConfirmButtonExtender ID="confirmDeleteBasicMed" runat="server" 
                                                            ConfirmOnFormSubmit="True" 
                                                            ConfirmText="<%$ Resources:Profile, BasicMedConfirmDelete %>" 
                                                            TargetControlID="imgDelete"></cc1:ConfirmButtonExtender>
                                                    </ItemTemplate>
                                                    <ItemStyle VerticalAlign="Top" />
                                                </asp:TemplateField>
                                                <asp:TemplateField meta:resourcekey="TemplateFieldResource3">
                                                    <ItemTemplate>
                                                        <div style="font-weight:bold"><%# ((DateTime) Eval("EventDate")).ToShortDateString() %> - <%# Eval("EventTypeDescription") %></div>
                                                        <div><% =Resources.Profile.BasicMedExpiration  %><span style="font-weight:bold"><%# ((DateTime) Eval("ExpirationDate")).ToShortDateString() %></span></div>
                                                        <div><%#: Eval("Description") %></div>
                                                    </ItemTemplate>
                                                    <ItemStyle VerticalAlign="Top" />
                                                </asp:TemplateField>
                                                <asp:TemplateField>
                                                    <ItemTemplate>
                                                        <uc8:mfbImageList runat="server" ID="ilBasicMed" CanEdit="false" ImageClass="BasicMed" IncludeDocs="true" />
                                                    </ItemTemplate>
                                                    <EditItemTemplate>
                                                        <div><uc8:mfbImageList runat="server" ID="ilBasicMed" CanEdit="true" ImageClass="BasicMed" IncludeDocs="true" /></div>
                                                        <div><uc7:mfbMultiFileUpload runat="server" ID="mfuBasicMedImages" Mode="Legacy" Class="BasicMed" IncludeDocs="true" ImageKey='<%# Bind("ID") %>' IncludeVideos="false" RefreshOnUpload="true" /></div>
                                                    </EditItemTemplate>
                                                    <ItemStyle VerticalAlign="Top" />
                                                </asp:TemplateField>
                                                <asp:CommandField ShowEditButton="True" meta:resourcekey="CommandFieldResource3" >
                                                <ItemStyle VerticalAlign="Top" />
                                                </asp:CommandField>
                                            </Columns>
                                            <EmptyDataTemplate>
                                                <ul>
                                                    <li>
                                                        <asp:Label ID="lblNoDeadlines" runat="server" Font-Italic="True" 
                                                            Text="<%$ Resources:Profile, BasicMedNoEvents %>" meta:resourcekey="lblNoDeadlinesResource2"></asp:Label>
                                                    </li>
                                                </ul>
                                            </EmptyDataTemplate>
                                        </asp:GridView>
                                    </asp:Panel>
                                </div>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpCertificates" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpCertificatesResource1" >
                        <Header>
                            <asp:Localize ID="locheaderCertificates" runat="server" Text="<%$ Resources:Profile, ProfilePilotInfoCertificates %>"></asp:Localize>
                        </Header>
                        <Content>
                            <asp:Panel ID="pnlCertificates" runat="server" DefaultButton="btnUpdatePilotInfo" meta:resourcekey="pnlCertificatesResource1">
                                <h3>
                                    <asp:Localize ID="locLicenseHeader" runat="server" Text="Pilot License" meta:resourcekey="locLicenseHeaderResource1"></asp:Localize>
                                </h3>
                                <div>
                                    <asp:TextBox ID="txtLicense" dir="auto" runat="server" ValidationGroup="valPilotInfo" meta:resourcekey="txtLicenseResource1"></asp:TextBox>
                                    <cc1:TextBoxWatermarkExtender ID="wmeLicense" WatermarkCssClass="watermark" WatermarkText="License #" TargetControlID="txtLicense" runat="server" BehaviorID="wmeLicense" />
                                    <br />
                                    <asp:Label ID="lblLicenseFineprint" runat="server" CssClass="fineprint" 
                                        Text="(Only used when printing your logbook)" meta:resourcekey="lblLicenseFineprintResource1"></asp:Label>
                                </div>
                                <h3>
                                    <asp:Localize ID="locCertPrompt" runat="server" Text="Instructor Certificate #" 
                                        meta:resourcekey="locCertPromptResource1"></asp:Localize>
                                </h3>
                                <div>
                                    <asp:TextBox ID="txtCertificate" dir="auto" runat="server" ValidationGroup="valPilotInfo" 
                                        meta:resourcekey="txtCertificateResource1"></asp:TextBox> &nbsp;
                                    <cc1:TextBoxWatermarkExtender ID="wmeCertificate" WatermarkCssClass="watermark" WatermarkText="Instructor #" TargetControlID="txtCertificate" runat="server" BehaviorID="wmeCertificate" />
                                    <asp:Localize ID="locExpiration" runat="server" Text="Expiration" meta:resourcekey="locExpirationResource1"></asp:Localize>
                                    <uc2:mfbTypeInDate ID="mfbTypeInDateCFIExpiration" runat="server" />
                                    <br />
                                    <asp:Label ID="lblCertFineprint" runat="server" CssClass="fineprint" 
                                        Text="(Only necessary if you want to have records of student endorsements)" 
                                        meta:resourcekey="lblCertFineprintResource1"></asp:Label>
                                </div>
                                <h3>
                                    <asp:Localize ID="locLangProficiency" runat="server" Text="English Proficiency Check Expiration"
                                        meta:resourcekey="locLangProficiencyResource1"></asp:Localize>
                                </h3>
                                <div>
                                    <uc2:mfbTypeInDate ID="mfbDateEnglishCheck" runat="server" />
                                </div>
                                <div>
                                    <br />
                                    <asp:Button ID="btnUpdatePilotInfo" runat="server"  
                                        Text="<%$ Resources:Profile, ProfilePilotInfoCertificatesUpdate %>" ValidationGroup="valPilotInfo" 
                                        onclick="btnUpdatePilotInfo_Click" meta:resourcekey="btnUpdatePilotInfoResource1" />
                                    <br />
                                    <asp:Label ID="lblPilotInfoUpdated" runat="server" CssClass="success" EnableViewState="False"
                                        Text="<%$ Resources:Profile, ProfilePilotInfoCertificatesUpdated %>" Visible="False" meta:resourcekey="lblPilotInfoUpdatedResource1" />
                                    <br />
                                </div>
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpFlightReviews" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpFlightReviewsResource1">
                        <Header>
                            <asp:Localize ID="locBFRPrompt" runat="server" Text="Flight Reviews or Checkrides" meta:resourcekey="locBFRPromptResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <p><asp:Label ID="lblBFRHelpText" runat="server" 
                                Text="When you enter a flight, you can attach a variety of properties such as flight reviews or checkrides.  Most - but not all - checkrides count as reviews." 
                                meta:resourcekey="lblBFRHelpTextResource1"></asp:Label></p>
                            <asp:GridView ID="gvBFR" runat="server" AutoGenerateColumns="False" 
                                GridLines="None" ShowHeader="False" CellPadding="5"
                                meta:resourcekey="gvBFRResource1">
                                <Columns>
                                    <asp:HyperLinkField DataNavigateUrlFields="FlightID" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}" DataTextField="Date" DataTextFormatString="{0:d}" meta:resourcekey="HyperLinkFieldResource3" />
                                    <asp:BoundField DataField="DisplayString" meta:resourcekey="BoundFieldResource2" />
                                </Columns>
                                <EmptyDataTemplate>
                                    <p><asp:Localize ID="locNoBFRFound" runat="server" Text="(No flight reviews or checkrides found)" meta:resourcekey="locNoBFRFoundResource1"></asp:Localize></p>
                                </EmptyDataTemplate>
                            </asp:GridView>
                            <asp:Panel ID="pnlNextBFR" runat="server" Visible="False" 
                                meta:resourcekey="pnlNextBFRResource1">
                                <asp:Localize ID="locNextBFRDue" runat="server" Text="<%$ Resources:Currency, NextFlightReview %>"></asp:Localize>
                                <asp:Label ID="lblNextBFR" runat="server" style="font-weight: bold;" 
                                    meta:resourcekey="lblNextBFRResource1"></asp:Label>.<br /><br />
                            </asp:Panel>
                        </Content>
                    </cc1:AccordionPane>
                    <cc1:AccordionPane runat="server" ID="acpIPCs" ContentCssClass="" HeaderCssClass="" meta:resourcekey="acpIPCsResource1">
                        <Header>
                            <asp:Localize ID="locIPCPrompt" runat="server" Text="Instrument Proficiency Checks"  meta:resourcekey="locIPCPromptResource1"></asp:Localize>
                        </Header>
                        <Content>
                            <p><asp:Label ID="lblIPCHelpText" runat="server" 
                                Text="When you enter a flight, attach a property for an IPC or an Instrument Checkride and it will count towards your instrument currency." 
                                meta:resourcekey="lblIPCHelpTextResource1"></asp:Label></p>
                            <asp:GridView ID="gvIPC" runat="server" AutoGenerateColumns="False" 
                                GridLines="None" ShowHeader="False" CellPadding="5"
                                meta:resourcekey="gvIPCResource1">
                                <Columns>
                                    <asp:HyperLinkField DataNavigateUrlFields="FlightID" 
                                        DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}" 
                                        DataTextField="Date" DataTextFormatString="{0:d}" 
                                        meta:resourcekey="HyperLinkFieldResource1" />
                                    <asp:BoundField DataField="DisplayString" DataFormatString="{0}" 
                                        meta:resourcekey="BoundFieldResource4" />
                                </Columns>
                                <EmptyDataTemplate>
                                    <p><asp:Localize ID="Localize1" runat="server" Text="(No Instrument Proficiency Checks were found)" meta:resourcekey="Localize1Resource1"></asp:Localize></p>
                                </EmptyDataTemplate>
                            </asp:GridView>
                        </Content>
                    </cc1:AccordionPane>
                </Panes>
            </cc1:Accordion>
        </asp:View>
        <asp:View ID="vwDonate" runat="server">
            <h2><asp:Label ID="lblDonateHeader" runat="server" Text="Donations" 
                    meta:resourcekey="lblDonateHeaderResource1"></asp:Label> </h2>
            <asp:Panel ID="pnlPaypalSuccess" runat="server" Visible="False" 
                EnableViewState="False" meta:resourcekey="pnlPaypalSuccessResource1">
                <asp:Label ID="Label1" runat="server" 
                    Text="Thank-you - your payment has been successfully applied!" Font-Bold="True" 
                    CssClass="success" meta:resourcekey="Label1Resource1"></asp:Label>
            </asp:Panel>
            <asp:Panel ID="pnlPaypalCanceled" runat="server" Visible="False" 
                EnableViewState="False" meta:resourcekey="pnlPaypalCanceledResource1">
                <asp:Label ID="Label2" runat="server" Text="Your payment was canceled." 
                    CssClass="error" meta:resourcekey="Label2Resource1"></asp:Label>
            </asp:Panel>
            <p>
                <asp:Label ID="lblDonatePrompt" runat="server" 
                    meta:resourcekey="lblDonatePromptResource1"></asp:Label>
            </p>
            <p>
                <asp:Label ID="lblDonatePromptGratuity" runat="server" 
                    Text="<%$ Resources:LocalizedText, DonatePromptGratuity %>" meta:resourcekey="lblDonatePromptGratuityResource8"></asp:Label>
            </p>
            <ul>
                <asp:Repeater ID="rptAvailableGratuities" runat="server">
                    <ItemTemplate>
                        <li><span style="font-weight:bold">US$<%# String.Format("{0:F0}", Eval("Threshold")) %></span>: <%# Eval("Name") %></li>
                    </ItemTemplate>
                </asp:Repeater>
            </ul>
            <iframe id="iframeDonate" src="../Donate.aspx" style="border:none;" width="300" height="120"></iframe>
            <h2><asp:Label ID="lblDonationHistory" runat="server" Text="Your donation history"
                    meta:resourcekey="lblDonationHistoryResource1" ></asp:Label>
            </h2>
            <asp:Panel ID="pnlEarnedGratuities" runat="server" CssClass="callout" meta:resourcekey="pnlFreeDropboxGratuityResource1" Visible="False">
                <asp:Label ID="lblThankYou" runat="server" Font-Bold="True" meta:resourcekey="lblThankYouResource1" Text="Thank-you for your support!"></asp:Label>
                <br />
                <asp:Localize ID="locEarnedGratuities" runat="server" Text="<%$ Resources:LocalizedText, GratuityEarnedHeader %>"></asp:Localize>
                <ul>
                    <asp:Repeater ID="rptEarnedGratuities" runat="server">
                        <ItemTemplate>
                            <li><%# Eval("GratuityEarned.ThankYou") %></li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </asp:Panel>
            <asp:GridView ID="gvDonations" runat="server" CellPadding="4" 
                ShowHeader="False" AutoGenerateColumns="False" GridLines="None" meta:resourcekey="gvDonationsResource1">
                <Columns>
                    <asp:BoundField DataField="TimeStamp" DataFormatString="{0:d}" 
                        meta:resourcekey="BoundFieldResource7" >
                        <ItemStyle Font-Bold="True" />
                        </asp:BoundField>
                    <asp:BoundField DataField="Amount" DataFormatString="{0:C}" 
                        meta:resourcekey="BoundFieldResource8" />
                        <asp:BoundField DataField="Notes" meta:resourcekey="BoundFieldResource9" />
                </Columns>
                <EmptyDataTemplate>
                    <p><asp:Label ID="lblNoDonations" runat="server" 
                            Text="You have not made any donations." 
                            meta:resourcekey="lblNoDonationsResource1"></asp:Label></p>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">

</asp:Content>
