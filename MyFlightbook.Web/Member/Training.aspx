<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Training.aspx.cs" Inherits="MyFlightbook.Instruction.TrainingPage" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc2" %>
<%@ Register src="../Controls/mfbEndorsementList.ascx" tagname="mfbEndorsementList" tagprefix="uc6" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc9" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbMultiFileUpload.ascx" TagPrefix="uc2" TagName="mfbMultiFileUpload" %>
<%@ Register Src="~/Controls/mfbImageList.ascx" TagPrefix="uc2" TagName="mfbImageList" %>
<%@ Register Src="~/Controls/mfbScribbleSignature.ascx" TagPrefix="uc2" TagName="mfbScribbleSignature" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblName" runat="server" />
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwEndorsements">
            <div style="float:right"><asp:HyperLink ID="lnkPrintFriendly" Target="_blank" runat="server" Text="<%$ Resources:SignOff, EndorsementsPrintView %>" /></div>
            <h2><asp:Localize ID="locEndorsementsPrompt" runat="server" Text="<%$ Resources:SignOff, EndorsementsYourEndorsements %>" /></h2>
            <p><asp:HyperLink ID="lnkDisclaimer" Text="<%$ Resources:SignOff, DigitalEndorsementDisclaimer %>" Target="_blank" NavigateUrl="~/Public/FAQ.aspx?q=23#23" runat="server"></asp:HyperLink></p>
            <uc6:mfbEndorsementList ID="mfbEndorsementList1" runat="server" />
            <p><asp:HyperLink ID="lnkAddEndorsement" runat="server" Text="<%$ Resources:SignOff, EndorsementsAddEndorsement %>" NavigateUrl="~/Member/AddEndorsement.aspx" /></p>
            <div class="noprint">
                <h2><asp:Localize ID="locScannedEndorsementsHeader" runat="server" Text="<%$ Resources:SignOff, EndorsementScannedImagesHeader %>" /></h2>
                <p><asp:Localize ID="locScannedEndorsementDesc" runat="server" Text="<%$ Resources:SignOff, EndorsementsScannedImagesDescription %>" /></p>
                <div>
                    <asp:Label ID="lblNote" runat="server" Font-Bold="True" Text="<%$ Resources:LocalizedText, Note %>" /> <asp:Label ID="lblPreviousEndorsements" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDisclaimer %>" />
                </div>
                <uc7:mfbMultiFileUpload ID="mfbMultiFileUpload1" Class="Endorsement" OnUploadComplete="mfbMultiFileUpload1_OnUploadComplete"
                        runat="server" Mode="Legacy" RefreshOnUpload="true" IncludeDocs="true" />
                <asp:Button ID="btnUploadImages" runat="server" CausesValidation="False" 
                     onclick="btnUploadImages_Click" Text="<%$ Resources:LocalizedText, UploadImages %>" />
            </div>
            <uc8:mfbImageList ID="mfbIlEndorsements" runat="server" AltText="<%$ Resources:LocalizedText, EndorsementAltText %>" 
                CanEdit="true" Columns="3" ImageClass="Endorsement" IncludeDocs="true" MaxImage="-1" />
            <asp:Repeater ID="rptEndorsementImages" runat="server">
                <ItemTemplate>
                    <div>
                        <asp:Image style="max-width: 100%" ID="imgEndorsement" AlternateText='<%#: Eval("Comment") %>' ImageUrl='<%# Eval("URLFullImage") %>' runat="server" />
                        <p style="text-align:center"><%#: Eval("Comment") %></p>
                    </div>
                </ItemTemplate>
            </asp:Repeater>
        </asp:View>
        <asp:View runat="server" ID="vwSignFlights">
            <h2><asp:Localize ID="locRequestSignatureHeading" runat="server" Text="<%$ Resources:SignOff, RequestSignatures %>" /></h2>
            <p><asp:Localize ID="locRequestSigDescription" runat="server" 
                    Text="<%$ Resources:SignOff, RequestSignaturesPrompt %>" />&nbsp; <asp:HyperLink ID="lnkRequestSignatures" runat="server" 
                    Text="<%$ Resources:SignOff, RequestSignatures %>" NavigateUrl="~/Member/RequestSigs.aspx" /></p>
            <asp:GridView ID="gvFlightsAwaitingSignatures" GridLines="None"  OnRowCommand="DeletePendingFlightSignature" 
                OnRowDataBound="LinkToPendingFlightDataBound" runat="server" CellPadding="3" AutoGenerateColumns="False">
                <Columns>
                    <asp:TemplateField HeaderText="<%$ Resources:SignOff, SignFlightsToBeSignedHeader %>">
                        <ItemTemplate>
                            <asp:ImageButton ID="btnIgnore" CommandName="Ignore" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, SignFlightIgnore %>"
                            ToolTip="<%$ Resources:SignOff, SignFlightIgnore %>" runat="server" />
                            &nbsp;
                            <asp:Label ID="lblFlight" runat="server" Text='<%#: Eval("DisplayString") %>' />
                        </ItemTemplate>
                        <HeaderStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                    <asp:TemplateField>
                        <ItemTemplate>
                            (<%# Eval("CFIName") %>)
                        </ItemTemplate>
                        <ItemStyle Font-Italic="true" />
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
        <asp:View runat="server" ID="vwStudents">
            <h2><% =Branding.ReBrand(Resources.LocalizedText.StudentsMemberPrompt) %></h2>
            <asp:GridView ID="gvStudents" GridLines="None" runat="server" 
                AutoGenerateColumns="False" ShowHeader="False"
                    OnRowCommand="gvStudents_Delete" EnableViewState="False" CellPadding="3"
                onrowdatabound="gvStudents_RowDataBound">
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                                    AlternateText="<%$ Resources:Profile, EditProfileDeleteStudentTooltip %>" 
                                    ToolTip="<%$ Resources:Profile, EditProfileDeleteStudentTooltip %>" CommandName="_Delete"
                                    CommandArgument='<%# Bind("username") %>' runat="server"  />
                                <cc1:ConfirmButtonExtender ID="cbeDeleteStudent" runat="server" TargetControlID="imgDelete"
                                    ConfirmOnFormSubmit="True" 
                                    ConfirmText="<%$ Resources:Profile, EditProfileDeleteStudentConfirmation %>" />
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:BoundField DataField="UserFullName" ShowHeader="False">
                            <ItemStyle VerticalAlign="Top" Font-Bold="True" />
                        </asp:BoundField>
                        <asp:TemplateField><ItemTemplate>&nbsp;&nbsp;&nbsp;</ItemTemplate></asp:TemplateField>
                        <asp:HyperLinkField DataNavigateUrlFormatString="~/Member/EndorseStudent.aspx/{0}" 
                            DataNavigateUrlFields="username" Text="<%$ Resources:SignOff, EndorsementsViewAdd %>">
                            <ItemStyle VerticalAlign="Top" />
                        </asp:HyperLinkField>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:Panel ID="pnlViewLogbook" runat="server" style="border-left: 1px solid black; padding-left:8px; margin-left:8px;" Visible='<%# Eval("CanViewLogbook") %>' >
                                    <asp:HyperLink ID="lnkViewStudentLogbook" 
                                        runat="server" Text="<%$ Resources:SignOff, ViewStudentLogbook %>" NavigateUrl='<%# String.Format("~/Member/StudentLogbook.aspx?student={0}", Eval("Username")) %>' />
                                </asp:Panel>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:Panel ID="pnlViewProgress" runat="server" style="border-left: 1px solid black; padding-left:8px; margin-left:8px;" Visible='<%# Eval("CanViewLogbook") %>' >
                                    <asp:HyperLink ID="lnkViewProgress" runat="server"  Text="<%$ Resources:MilestoneProgress, ViewStudentProgress %>"
                                       NavigateUrl='<%# String.Format("~/Member/RatingProgress.aspx?user={0}", Eval("Username")) %>' />
                                </asp:Panel>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:TemplateField><ItemTemplate>&nbsp;&nbsp;&nbsp;</ItemTemplate></asp:TemplateField>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:GridView GridLines="None" ID="gvPendingFlightsToSign" OnRowCommand="DeletePendingFlightSignatureForStudent" 
                                    OnRowDataBound="LinkToPendingFlightDataBound" runat="server" AutoGenerateColumns="False">
                                    <Columns>
                                        <asp:TemplateField HeaderText="<%$ Resources:SignOff, SignFlightsToBeSignedHeader %>">
                                            <ItemTemplate>
                                                <asp:ImageButton ID="btnIgnore" CommandName="Ignore" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, SignFlightIgnore %>"
                                                ToolTip="<%$ Resources:SignOff, SignFlightIgnore %>" runat="server" />
                                                &nbsp;
                                                <asp:HyperLink ID="lnkFlightToSign" runat="server" 
                                                    Text='<%# Eval("DisplayString") %>' />
                                            </ItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <ul>
                            <li style="font-weight:bold;"><%# Branding.ReBrand(Resources.LocalizedText.StudentsMemberNoneFound) %></li>
                        </ul>
                    </EmptyDataTemplate>
            </asp:GridView>
            <asp:Panel ID="pnlViewAllEndorsements" runat="server">
                <asp:HyperLink ID="lnkViewAllEndorsements" runat="server" Text="<%$ Resources:SignOff, EndorsementsViewAll %>" NavigateUrl="~/Member/EndorseStudent.aspx/" />
            </asp:Panel>
            <asp:Panel ID="pnlAddStudent" runat="server" DefaultButton="btnAddStudent">
                <p>
                <asp:Localize ID="locAddStudentPrmopt" runat="server" Text="<%$ Resources:SignOff, RoleAddStudentPrompt %>" />
                <br />
                <asp:Label ID="lblEmailDisclaimer" runat="server" Text="<%$ Resources:SignOff, RoleAddEmailDisclaimer %>" />
                </p>
                <asp:Panel ID="pnlCertificate" runat="server">
                        <asp:TextBox ID="txtCertificate" runat="server" ValidationGroup="valPilotInfo" /> &nbsp;
                        <cc1:TextBoxWatermarkExtender ID="wmeCertificate" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, PilotInfoCertificateCFIWatermark %>" TargetControlID="txtCertificate" runat="server" BehaviorID="wmeCertificate" />
                        <asp:Localize ID="locExpiration" runat="server" Text="<%$ Resources:Preferences, PilotInfoCFIExpiration %>" />
                        <uc2:mfbTypeInDate ID="mfbTypeInDateCFIExpiration" runat="server" DefaultType="None" />
                </asp:Panel>
                <p>
                <asp:TextBox runat="server" ID="txtStudentEmail" TextMode="Email"
                            AutoCompleteType="Email" ValidationGroup="vgAddStudent"  />
                <asp:Button ID="btnAddStudent" runat="server" Text="<%$ Resources:SignOff, RoleAddStudent %>" ValidationGroup="vgAddStudent"
                        onclick="btnAddStudent_Click" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator4" 
                    ControlToValidate="txtStudentEmail" runat="server" 
                    ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailRequired %>" CssClass="error" 
                    ValidationGroup="vgAddStudent" Display="Dynamic" />
                <asp:RegularExpressionValidator ID="RegularExpressionValidator3" runat="server" 
                    ControlToValidate="txtStudentEmail" 
                    ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailFormat %>" CssClass="error" 
                    ValidationGroup="vgAddStudent" Display="Dynamic" 
                    ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" />
                </p>
                <div>
                <asp:Label ID="lblAddStudentSuccess" runat="server" EnableViewState="False" />
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlSignature" runat="server" style="margin-top: 6pt; margin-bottom: 18pt;">
                <h2><% =Resources.LocalizedText.StudentSigningDefaultScribbleHeader %></h2>
                <p><% =Resources.LocalizedText.StudentSigningDefaultScribblePrompt %></p>
                <asp:MultiView ID="mvDefaultSig" runat="server">
                    <asp:View ID="vwCurrentSig" runat="server">
                        <img runat="server" id="imgCurrSig" src="~/images/signature.png" />
                        <div><asp:LinkButton ID="lnkEditDefaultSig" runat="server" Text="<%$ Resources:LocalizedText, StudentSigningDefaultScribbleEdit %>" OnClick="lnkEditDefaultSig_Click"></asp:LinkButton></div>
                    </asp:View>
                    <asp:View ID="vwNewSig" runat="server">
                        <div style="display:inline-block;">
                            <uc2:mfbScribbleSignature runat="server" Enabled="false" ColorRef="#888888" ID="mfbScribbleSignature" ShowCancel="true" ShowSave="true" OnSaveClicked="btnSaveDefaultSig_Click" OnCancelClicked="btnCancel_Click" />
                        </div>
                        <div style="display:inline-block; vertical-align:top; padding: 4px;">
                            <asp:Image ID="imgStamp" ImageUrl="~/images/rubberstamp.png" runat="server" ToolTip="<%$ Resources:LocalizedText, StudentSigningDefaultScribbleIcon %>" />
                        </div>
                        <div style="display:inline-block; vertical-align:top; max-width:300px; padding: 4px">
                            <% =Resources.LocalizedText.StudentSigningDefaultScribblePrompt2 %>
                        </div>
                    </asp:View>
                </asp:MultiView>
            </asp:Panel>
            <h2><% =Branding.ReBrand(Resources.LocalizedText.StudentsNonMemberPrompt) %></h2>
            <p><% =Branding.ReBrand(Resources.LocalizedText.StudentsNonMemberDescription) %></p>
            <p><asp:HyperLink ID="lnkAddOfflineEndorsement" Text="<%$ Resources:SignOff, EndorseAddOfflineHeader %>" NavigateUrl="~/Member/EndorseStudent.aspx/?extern=1" runat="server" /></p>
            <uc2:mfbMultiFileUpload runat="server" ID="mfuOfflineEndorsements" IncludeDocs="true" IncludeVideos="false" Mode="Ajax" Class="OfflineEndorsement" RefreshOnUpload="true" OnUploadComplete="mfuOfflineEndorsements_UploadComplete" />
            <asp:Button ID="btnUploadOfflineImages" runat="server" CausesValidation="False" 
                     onclick="btnUploadOfflineImages_Click" 
                    Text="<%$ Resources:LocalizedText, UploadImages %>" />
            <uc2:mfbImageList runat="server" ID="mfbIlOfflineEndorsements" ImageClass="OfflineEndorsement" CanEdit="true" Columns="3" MaxImage="-1" IncludeDocs="true" AltText="<%$ Resources:LocalizedText, StudentEndorsementAltText %>" />
        </asp:View>
        <asp:View runat="server" ID="vwInstructors">
            <h2><asp:Localize ID="locInstructorsPrompt" runat="server" Text="<%$ Resources:SignOff, RoleYourInstructors %>" /></h2>
            <asp:GridView ID="gvInstructors" GridLines="None" runat="server" 
                AutoGenerateColumns="False" EnableViewState="False" ShowHeader="False" CellPadding="3" OnRowCommand="gvInstructors_Delete" >
                    <Columns>
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                                    AlternateText="<%$ Resources:Profile, EditProfileDeleteCFITooltip %>"
                                    ToolTip="<%$ Resources:Profile, EditProfileDeleteCFITooltip %>" CommandName="_Delete"
                                    CommandArgument='<%# Bind("username") %>' runat="server" />
                                <cc1:ConfirmButtonExtender ID="cbeDeleteInstructor" runat="server" TargetControlID="imgDelete"
                                        ConfirmOnFormSubmit="True"
                                        ConfirmText="<%$ Resources:Profile, EditProfileDeleteCFIConfirmation %>" BehaviorID="cbeDeleteInstructor" />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="UserFullName" ShowHeader="False" ItemStyle-Font-Bold="true" />
                        <asp:TemplateField>
                            <ItemTemplate>
                                <asp:CheckBox ID="ckCanViewLogbook" runat="server"
                                    Text="<%$ Resources:SignOff, RoleAllowLogbook %>" AutoPostBack="True" Checked="<%# ((InstructorStudent)Container.DataItem).CanViewLogbook %>"
                                    OnCheckedChanged="ckCanViewLogbook_CheckedChanged" />
                                <asp:CheckBox ID="ckCanAddLogbook" runat="server"
                                    Enabled="<%# ((InstructorStudent)Container.DataItem).CanViewLogbook %>"
                                    Checked="<%# ((InstructorStudent)Container.DataItem).CanAddLogbook %>"
                                    Text="<%$ Resources:SignOff, RoleAllowAddFlights %>" AutoPostBack="True"
                                    OnCheckedChanged="ckCanAddLogbook_CheckedChanged" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <asp:Label ID="lblNoInstructors" runat="server" Text="<%$ Resources:SignOff, RoleNoInstructors %>"/>
                    </EmptyDataTemplate>
            </asp:GridView>
            <asp:Panel ID="pnlAddInstructor" runat="server" DefaultButton="btnAddInstructor">
                <br />
                <asp:Localize ID="locAddInstructorPrompt" runat="server" Text="<%$ Resources:SignOff, RoleAddInstructorPrompt %>" />
                <br />
                <asp:Label ID="lblEmailDisclaimer2" Font-Bold="True" runat="server" Text="<%$ Resources:SignOff, RoleAddEmailDisclaimer %>" />
                <br />
                <asp:TextBox ID="txtInstructorEmail" runat="server" 
                    ValidationGroup="vgAddInstructor" TextMode="Email" AutoCompleteType="Email" />
                <asp:Button ID="btnAddInstructor" runat="server" Text="<%$ Resources:SignOff, RoleAddInstructor %>" ValidationGroup="vgAddInstructor"
                        onclick="btnAddInstructor_Click" />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator3" 
                    ControlToValidate="txtInstructorEmail" runat="server" 
                    ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailRequired %>" CssClass="error" 
                    ValidationGroup="vgAddInstructor" Display="Dynamic" />
                <asp:RegularExpressionValidator ID="RegularExpressionValidator2" runat="server" 
                    ControlToValidate="txtInstructorEmail" 
                    ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailFormat %>" CssClass="error" 
                    ValidationGroup="vgAddInstructor" Display="Dynamic" 
                    ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" />
                <br />
                <asp:Label ID="lblAddInstructorSuccess" runat="server" EnableViewState="False" />
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
</asp:Content>

