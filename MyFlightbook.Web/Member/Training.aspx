<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_Training" culture="auto" meta:resourcekey="PageResource1" Codebehind="Training.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc2" %>
<%@ Register src="../Controls/mfbEndorsementList.ascx" tagname="mfbEndorsementList" tagprefix="uc6" %>
<%@ Register src="../Controls/mfbMultiFileUpload.ascx" tagname="mfbMultiFileUpload" tagprefix="uc7" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc8" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc9" %>
<%@ Register src="../Controls/AccountQuestions.ascx" tagname="AccountQuestions" tagprefix="uc4" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <asp:Label ID="lblName" runat="server" meta:resourcekey="lblNameResource1" ></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:MultiView ID="mvProfile" runat="server">
        <asp:View runat="server" ID="vwEndorsements">
            <div style="float:right"><asp:HyperLink ID="lnkPrintFriendly" Target="_blank" runat="server" Text="Printer Friendly View" meta:resourcekey="lnkPrintFriendlyResource1" ></asp:HyperLink></div>
            <h2>
                <asp:Localize ID="locEndorsementsPrompt" runat="server" 
                     Text="Your Endorsements" meta:resourcekey="locEndorsementsPromptResource1"></asp:Localize>
            </h2>
            <uc6:mfbEndorsementList ID="mfbEndorsementList1" runat="server" />
            <p><asp:HyperLink ID="lnkAddEndorsement" runat="server" Text="Add an endorsement..." NavigateUrl="~/Member/AddEndorsement.aspx" meta:resourcekey="lnkAddEndorsementResource1"></asp:HyperLink></p>
            <div class="noprint">
                <h2>
                    <asp:Localize ID="locScannedEndorsementsHeader" runat="server" 
                         
                        Text="Images of physical endorsements" meta:resourcekey="locScannedEndorsementsHeaderResource1"></asp:Localize>
                </h2>
                <p>
                    <asp:Localize ID="locScannedEndorsementDesc" runat="server" 
                         
                        Text="You can scan your paper logbook endorsements and upload them here so that you always have access to them." meta:resourcekey="locScannedEndorsementDescResource1"></asp:Localize>
                </p>
                <div>
                    <asp:Label ID="lblNote" runat="server" Font-Bold="True" Text="<%$ Resources:LocalizedText, Note %>" meta:resourcekey="lblNoteResource1"></asp:Label> <asp:Label ID="lblPreviousEndorsements" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDisclaimer %>" meta:resourcekey="lblPreviousEndorsementsResource1"></asp:Label>
                </div>
                <uc7:mfbMultiFileUpload ID="mfbMultiFileUpload1" Class="Endorsement" OnUploadComplete="mfbMultiFileUpload1_OnUploadComplete"
                        runat="server" Mode="Legacy" RefreshOnUpload="true" IncludeDocs="true" />
                <asp:Button ID="btnUploadImages" runat="server" CausesValidation="False" 
                     onclick="btnUploadImages_Click" 
                    Text="Upload Images" meta:resourcekey="btnUploadImagesResource1" />
            </div>
            <uc8:mfbImageList ID="mfbIlEndorsements" runat="server" AltText="Endorsements" 
                CanEdit="true" Columns="3" ImageClass="Endorsement" IncludeDocs="true" MaxImage="-1" />
        </asp:View>
        <asp:View runat="server" ID="vwSignFlights">
            <h2><asp:Localize ID="locRequestSignatureHeading" runat="server" 
                    Text="Request Signatures" meta:resourcekey="locRequestSignatureHeadingResource1" 
                    ></asp:Localize></h2>
            <p><asp:Localize ID="locRequestSigDescription" runat="server" 
                    Text="Take a lesson, flight-review, or checkout and want the CFI to sign flights that you've already entered?" meta:resourcekey="locRequestSigDescriptionResource1" 
                    ></asp:Localize>&nbsp; <asp:HyperLink ID="lnkRequestSignatures" runat="server" 
                    Text="Request Signatures"  NavigateUrl="~/Member/RequestSigs.aspx" meta:resourcekey="lnkRequestSignaturesResource1"></asp:HyperLink></p>
            <asp:GridView ID="gvFlightsAwaitingSignatures" GridLines="None"  OnRowCommand="DeletePendingFlightSignature" 
                OnRowDataBound="LinkToPendingFlightDataBound" runat="server" 
                AutoGenerateColumns="False" meta:resourcekey="gvFlightsAwaitingSignaturesResource1" 
                >
                <Columns>
                    <asp:TemplateField HeaderText="Flights Awaiting Signature" meta:resourcekey="TemplateFieldResource1" 
                        >
                        <ItemTemplate>
                            <asp:ImageButton ID="btnIgnore" CommandName="Ignore" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, SignFlightIgnore %>"
                            ToolTip="<%$ Resources:SignOff, SignFlightIgnore %>" runat="server" />
                            &nbsp;
                            <asp:Label ID="lblFlight" runat="server" Text='<%# Eval("DisplayString") %>' meta:resourcekey="lblFlightResource1"  
                                ></asp:Label>
                        </ItemTemplate>
                        <HeaderStyle HorizontalAlign="Left" />
                    </asp:TemplateField>
                </Columns>
                <EmptyDataTemplate>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
        <asp:View runat="server" ID="vwStudents">
            <h2><asp:Localize ID="locStudentsPrompt" runat="server" Text="Your Students" meta:resourcekey="locStudentsPromptResource1" 
                    ></asp:Localize></h2>
            <asp:GridView ID="gvStudents" GridLines="None" runat="server" 
                AutoGenerateColumns="False" ShowHeader="False"
                    OnRowCommand="gvStudents_Delete" EnableViewState="False" CellPadding="3"
                onrowdatabound="gvStudents_RowDataBound" meta:resourcekey="gvStudentsResource1" >
                    <Columns>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource2" ><ItemTemplate><asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                                    AlternateText="Delete this student" 
                                    ToolTip="<%$ Resources:Profile, EditProfileDeleteStudentTooltip %>" CommandName="_Delete"
                                    CommandArgument='<%# Bind("username") %>' runat="server" meta:resourcekey="imgDeleteResource1" 
                                     /><cc1:ConfirmButtonExtender ID="cbeDeleteStudent" runat="server" TargetControlID="imgDelete"
                                    ConfirmOnFormSubmit="True" 
                                    ConfirmText="<%$ Resources:Profile, EditProfileDeleteStudentConfirmation %>" BehaviorID="cbeDeleteStudent"></cc1:ConfirmButtonExtender></ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:BoundField DataField="UserFullName" ShowHeader="False" meta:resourcekey="BoundFieldResource1">
                            <ItemStyle VerticalAlign="Top" Font-Bold="True" />
                        </asp:BoundField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource3" ><ItemTemplate>&nbsp;&nbsp;&nbsp;</ItemTemplate></asp:TemplateField>
                        <asp:HyperLinkField DataNavigateUrlFormatString="~/Member/EndorseStudent.aspx/{0}" 
                            DataNavigateUrlFields="username" Text="View/Add Endorsements" meta:resourcekey="HyperLinkFieldResource1" 
                             >
                        <ItemStyle VerticalAlign="Top" />
                        </asp:HyperLinkField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource4" >
                            <ItemTemplate>
                                <asp:Panel ID="pnlViewLogbook" runat="server" style="border-left: 1px solid black; padding-left:8px; margin-left:8px;" Visible='<%# Eval("CanViewLogbook") %>' meta:resourcekey="pnlViewLogbookResource1" >
                                    <asp:HyperLink ID="lnkViewStudentLogbook" 
                                        runat="server" Text="View logbook" NavigateUrl='<%# String.Format("~/Member/StudentLogbook.aspx?student={0}", Eval("Username")) %>' meta:resourcekey="lnkViewStudentLogbookResource1" 
                                        ></asp:HyperLink>
                                </asp:Panel>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource5" >
                            <ItemTemplate>
                                <asp:Panel ID="pnlViewProgress" runat="server" style="border-left: 1px solid black; padding-left:8px; margin-left:8px;" Visible='<%# Eval("CanViewLogbook") %>' meta:resourcekey="pnlViewProgressResource1" >
                                    <asp:HyperLink ID="lnkViewProgress" runat="server"  Text="<%$ Resources:MilestoneProgress, ViewStudentProgress %>"
                                       NavigateUrl='<%# String.Format("~/Member/RatingProgress.aspx?user={0}", Eval("Username")) %>' meta:resourcekey="lnkViewProgressResource1" ></asp:HyperLink>
                                </asp:Panel>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource6" ><ItemTemplate>&nbsp;&nbsp;&nbsp;</ItemTemplate></asp:TemplateField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource8" >
                            <ItemTemplate>
                                <asp:GridView GridLines="None" ID="gvPendingFlightsToSign" OnRowCommand="DeletePendingFlightSignatureForStudent" 
                                    OnRowDataBound="LinkToPendingFlightDataBound" runat="server" 
                                    AutoGenerateColumns="False" meta:resourcekey="gvPendingFlightsToSignResource1" 
                                    >
                                    <Columns>
                                        <asp:TemplateField HeaderText="Flights Awaiting Signature" meta:resourcekey="TemplateFieldResource7"
                                            >
                                            <ItemTemplate>
                                                <asp:ImageButton ID="btnIgnore" CommandName="Ignore" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:SignOff, SignFlightIgnore %>"
                                                ToolTip="<%$ Resources:SignOff, SignFlightIgnore %>" runat="server" />
                                                &nbsp;
                                                <asp:HyperLink ID="lnkFlightToSign" runat="server" 
                                                    Text='<%# Eval("DisplayString") %>' meta:resourcekey="lnkFlightToSignResource1" ></asp:HyperLink>
                                            </ItemTemplate>
                                            <HeaderStyle HorizontalAlign="Left" />
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </ItemTemplate>
                            <ItemStyle VerticalAlign="Top" />
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <asp:Label ID="lblNoStudents" runat="server" Text="You have no students." meta:resourcekey="lblNoStudentsResource1" 
                    ></asp:Label>
                    </EmptyDataTemplate>
            </asp:GridView>
            <asp:Panel ID="pnlViewAllEndorsements" runat="server" meta:resourcekey="pnlViewAllEndorsementsResource1" >
                <asp:HyperLink ID="lnkViewAllEndorsements" runat="server" Text="View all endorsements" NavigateUrl="~/Member/EndorseStudent.aspx/" meta:resourcekey="lnkViewAllEndorsementsResource1" ></asp:HyperLink>
            </asp:Panel>
            <asp:Panel ID="pnlAddStudent" runat="server" DefaultButton="btnAddStudent" meta:resourcekey="pnlAddStudentResource1" 
                >
                <br />
                <asp:Localize ID="locAddStudentPrmopt" runat="server" 
                    Text="If you have a student and would like to track the endorsements you give to them,
                enter the student&#39;s email address below. They will confirm that they know you." meta:resourcekey="locAddStudentPrmoptResource1" 
                    ></asp:Localize>
                <br />
                <asp:Label ID="lblEmailDisclaimer" Font-Bold="True" runat="server" 
                    Text="This e-mail address will NOT be stored or used for any other purpose." meta:resourcekey="lblEmailDisclaimerResource1" 
                    ></asp:Label>
                <br />
                <asp:TextBox runat="server" ID="txtStudentEmail" TextMode="Email"
                            AutoCompleteType="Email" ValidationGroup="vgAddStudent" meta:resourcekey="txtStudentEmailResource1" 
                             />
                <asp:Button ID="btnAddStudent" runat="server" Text="Add Student" ValidationGroup="vgAddStudent"
                        onclick="btnAddStudent_Click" meta:resourcekey="btnAddStudentResource1" 
                     />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator4" 
                    ControlToValidate="txtStudentEmail" runat="server" 
                    ErrorMessage="Please provide a valid email address" CssClass="error" 
                    ValidationGroup="vgAddStudent" Display="Dynamic" meta:resourcekey="RequiredFieldValidator4Resource1" 
                    ></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="RegularExpressionValidator3" runat="server" 
                    ControlToValidate="txtStudentEmail" 
                    ErrorMessage="Please provide a valid email address" CssClass="error" 
                    ValidationGroup="vgAddStudent" Display="Dynamic" 
                    ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" meta:resourcekey="RegularExpressionValidator3Resource1" 
                    ></asp:RegularExpressionValidator>
                <br />
                <asp:Label ID="lblAddStudentSuccess" runat="server" EnableViewState="False" meta:resourcekey="lblAddStudentSuccessResource1" 
                    ></asp:Label>
                <br />
            </asp:Panel>
            <p><asp:HyperLink ID="lnkAddOfflineEndorsement" Text="Record an endorsement given to a student off-line" NavigateUrl="~/Member/EndorseStudent.aspx/?extern=1" runat="server" meta:resourcekey="lnkAddOfflineEndorsementResource1" ></asp:HyperLink></p>
        </asp:View>
        <asp:View runat="server" ID="vwInstructors">
            <h2><asp:Localize ID="locInstructorsPrompt" runat="server" Text="Your Instructors" meta:resourcekey="locInstructorsPromptResource1" 
                    ></asp:Localize></h2>
            <asp:GridView ID="gvInstructors" GridLines="None" runat="server" 
                AutoGenerateColumns="False" EnableViewState="False" ShowHeader="False" CellPadding="3"
                    OnRowCommand="gvInstructors_Delete" meta:resourcekey="gvInstructorsResource1">
                    <Columns>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource9">
                            <ItemTemplate>
                                <asp:ImageButton ID="imgDelete" ImageUrl="~/images/x.gif" CausesValidation="False"
                                    AlternateText="Delete this instructor"
                                    ToolTip="<%$ Resources:Profile, EditProfileDeleteCFITooltip %>" CommandName="_Delete"
                                    CommandArgument='<%# Bind("username") %>' runat="server" meta:resourcekey="imgDeleteResource2" /><cc1:ConfirmButtonExtender ID="cbeDeleteInstructor" runat="server" TargetControlID="imgDelete"
                                        ConfirmOnFormSubmit="True"
                                        ConfirmText="<%$ Resources:Profile, EditProfileDeleteCFIConfirmation %>" BehaviorID="cbeDeleteInstructor"></cc1:ConfirmButtonExtender>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="UserFullName" ShowHeader="False" meta:resourcekey="BoundFieldResource2" >
                        <ItemStyle Font-Bold="True" />
                        </asp:BoundField>
                        <asp:TemplateField meta:resourcekey="TemplateFieldResource10">
                            <ItemTemplate>
                                <asp:CheckBox ID="ckCanViewLogbook" runat="server"
                                    Text="Can view my logbook" AutoPostBack="True" Checked="<%# ((InstructorStudent)Container.DataItem).CanViewLogbook %>"
                                    OnCheckedChanged="ckCanViewLogbook_CheckedChanged" meta:resourcekey="ckCanViewLogbookResource1"></asp:CheckBox>
                                <asp:CheckBox ID="ckCanAddLogbook" runat="server"
                                    Enabled="<%# ((InstructorStudent)Container.DataItem).CanViewLogbook %>"
                                    Checked="<%# ((InstructorStudent)Container.DataItem).CanAddLogbook %>"
                                    Text="Can add flights to my logbook" AutoPostBack="True"
                                    OnCheckedChanged="ckCanAddLogbook_CheckedChanged" meta:resourcekey="ckCanAddLogbookResource1"></asp:CheckBox>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                    <EmptyDataTemplate>
                        <asp:Label ID="lblNoInstructors" runat="server" Text="You have no instructors." meta:resourcekey="lblNoInstructorsResource1" 
                ></asp:Label>
                    </EmptyDataTemplate>
            </asp:GridView>
            <asp:Panel ID="pnlAddInstructor" runat="server" 
                DefaultButton="btnAddInstructor" meta:resourcekey="pnlAddInstructorResource1" >
                <br />
                <asp:Localize ID="locAddInstructorPrompt" runat="server" Text="If you would like to track endorsements from your 
                instructor, enter their email address below. They will confirm that they know 
                you." meta:resourcekey="locAddInstructorPromptResource1" ></asp:Localize>
                <br />
                <asp:Label ID="lblEmailDisclaimer2" Font-Bold="True" runat="server" 
                    Text="This e-mail address will NOT be stored or used for any other purpose." meta:resourcekey="lblEmailDisclaimer2Resource1" 
                    ></asp:Label>
                <br />
                <asp:TextBox ID="txtInstructorEmail" runat="server" 
                    ValidationGroup="vgAddInstructor" TextMode="Email"
                            AutoCompleteType="Email" meta:resourcekey="txtInstructorEmailResource1" 
                    ></asp:TextBox>
                <asp:Button ID="btnAddInstructor" runat="server" Text="Add Instructor" ValidationGroup="vgAddInstructor"
                        onclick="btnAddInstructor_Click" meta:resourcekey="btnAddInstructorResource1" 
                     />
                <asp:RequiredFieldValidator ID="RequiredFieldValidator3" 
                    ControlToValidate="txtInstructorEmail" runat="server" 
                    ErrorMessage="Please provide a valid email address" CssClass="error" 
                    ValidationGroup="vgAddInstructor" Display="Dynamic" meta:resourcekey="RequiredFieldValidator3Resource1" 
                    ></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ID="RegularExpressionValidator2" runat="server" 
                    ControlToValidate="txtInstructorEmail" 
                    ErrorMessage="Please provide a valid email address" CssClass="error" 
                    ValidationGroup="vgAddInstructor" Display="Dynamic" 
                    ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" meta:resourcekey="RegularExpressionValidator2Resource1" 
                    ></asp:RegularExpressionValidator>
                <br />
                <asp:Label ID="lblAddInstructorSuccess" runat="server" EnableViewState="False" meta:resourcekey="lblAddInstructorSuccessResource1" 
                     ></asp:Label>
            </asp:Panel>
        </asp:View>
    </asp:MultiView>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:Repeater ID="rptEndorsementImages" runat="server">
        <ItemTemplate>
            <div>
                <asp:Image style="max-width: 100%" ID="imgEndorsement" AlternateText='<%# Eval("Comment") %>' ImageUrl='<%# Eval("URLFullImage") %>' runat="server" meta:resourcekey="imgEndorsementResource1"  />
                <p style="text-align:center"><%# Eval("Comment") %></p>
            </div>
        </ItemTemplate>
    </asp:Repeater>
</asp:Content>

