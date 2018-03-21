<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="RequestSigs.aspx.cs" Inherits="Member_RequestSigs" culture="auto" meta:resourcekey="PageResource1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbLogbook.ascx" tagname="mfbLogbook" tagprefix="uc1" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblName" runat="server" ></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:Wizard ID="wzRequestSigs" runat="server" DisplaySideBar="False" 
        onfinishbuttonclick="wzRequestSigs_FinishButtonClick" 
        onnextbuttonclick="wzRequestSigs_NextButtonClick" 
        meta:resourcekey="wzRequestSigsResource1">
        <HeaderTemplate>
            <div style="width:100%">
                <asp:Repeater ID="SideBarList" runat="server">
                    <ItemTemplate>
                        <span class="<%# GetClassForWizardStep(Container.DataItem) %>">
                            &nbsp;
                            <%# DataBinder.Eval(Container, "DataItem.Title") %>
                        </span>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </HeaderTemplate>
        <WizardSteps>
            <asp:WizardStep ID="wsSelectFlights" runat="server" 
                Title="1. Select Flights" meta:resourcekey="wsSelectFlightsResource1">
                <h3><asp:Label ID="lblSelectFlightPrompt" runat="server" 
                        Text="Select the flight(s) that you want to have signed." 
                        meta:resourcekey="lblSelectFlightPromptResource1"></asp:Label></h3>
                <asp:Panel ID="pnlFlights" ScrollBars="Auto" Height="200" runat="server">
                    <table cellpadding="4">
                        <asp:Repeater ID="rptSelectedFlights" runat="server" >
                            <ItemTemplate>
                                <tr style="vertical-align:top">
                                    <td>
                                        <asp:CheckBox ID="ckFlight" runat="server" />
                                        <asp:HiddenField ID="hdnFlightID" runat="server" Value='<%# Eval("FlightID") %>' />
                                    </td>
                                    <td>
                                        <div style="font-weight:bold"><%# ((DateTime) Eval("Date")).ToShortDateString() %></div>
                                        <div><%# Eval("TailNumDisplay") %></div>
                                    </td>
                                    <td>
                                        <div><span style="font-style:italic"><%# Eval("Route") %></span> <%# Eval("Comment") %></div>
                                    </td>
                                </tr>
                            </ItemTemplate>
                        </asp:Repeater>
                    </table>
                </asp:Panel>
                <asp:Label ID="lblErrNoSelection" runat="server" 
                    Text="Please select at least one flight to be signed" EnableViewState="false" 
                    CssClass="error" Visible="false" meta:resourcekey="lblErrNoSelectionResource1"></asp:Label>
            </asp:WizardStep>
            <asp:WizardStep ID="wsPickInstructor" runat="server" 
                Title="2. Pick The CFI" meta:resourcekey="wsPickInstructorResource1">
                <table>
                    <tr>
                        <td><asp:Label ID="Label2" runat="server" 
                                Text="<%$ Resources:SignOff, ChooseInstructorsPrompt %>" 
                                meta:resourcekey="Label2Resource1"></asp:Label></td>
                        <td>
                            <asp:DropDownList ID="cmbInstructors" runat="server" AutoPostBack="true" 
                                AppendDataBoundItems="true" DataValueField="UserName" DataTextField="UserFullName" 
                                onselectedindexchanged="cmbInstructors_SelectedIndexChanged" 
                                meta:resourcekey="cmbInstructorsResource1">
                                <asp:ListItem Selected="True" Text="<%$ Resources:SignOff, NewInstructor %>" 
                                    Value="" meta:resourcekey="ListItemResource1"></asp:ListItem>
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr runat="server" id="rowEmail">
                        <td><asp:Label ID="lblCFIEmailPrompt" runat="server" Text="<%$ Resources:SignOff, CFIEmail %>"></asp:Label></td>
                        <td>
                            <div><asp:TextBox ID="txtCFIEmail" runat="server"></asp:TextBox></div>
                            <asp:RequiredFieldValidator ID="valEmailRequired" runat="server" ControlToValidate="txtCFIEmail"
                                ErrorMessage="You must provide a valid e-mail address to sign a flight." 
                                Display="Dynamic" ToolTip="E-mail is required." CssClass="error">
                            </asp:RequiredFieldValidator>
                            <asp:RegularExpressionValidator ID="valBadEmail" runat="server" ControlToValidate="txtCFIEmail"
                                Display="Dynamic" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                ErrorMessage="Please enter a valid e-mail address." CssClass="error">
                            </asp:RegularExpressionValidator>
                            <asp:CustomValidator OnServerValidate="VerifySeparateEmail" ID="valDifferentEmail" ControlToValidate="txtCFIEmail" runat="server"
                                Display="Dynamic" CssClass="error"
                                ErrorMessage="You can't sign your own flights!"></asp:CustomValidator>
                        </td>
                    </tr>
                </table>
            </asp:WizardStep>
            <asp:WizardStep ID="wsFinished" runat="server" Title="3. Send the request" 
                meta:resourcekey="wsFinishedResource1">
                <asp:Label ID="lblStatus" runat="server" 
                    Text="When you finish, the following request will be sent:" 
                    meta:resourcekey="lblStatusResource1"></asp:Label>
                <table>
                    <tr valign="top">
                        <td><asp:Label ID="lblInstructorPrompt" runat="server" 
                                Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>" 
                                Font-Bold="true" meta:resourcekey="lblInstructorPromptResource1"></asp:Label></td>
                        <td><asp:Label ID="lblInstructorNameSummary" runat="server" Text="" 
                                meta:resourcekey="lblInstructorNameSummaryResource1"></asp:Label></td>
                    </tr>
                    <tr valign="top">
                        <td><asp:Label ID="lblFlightsToSign" runat="server" Text="Flights to sign:" 
                                Font-Bold="true" meta:resourcekey="lblFlightsToSignResource1"></asp:Label></td>
                        <td>
                            <table cellpadding="0" cellspacing="4">
                                <asp:Repeater ID="gvFlightsToSign" runat="server" >
                                    <ItemTemplate>
                                        <tr style="vertical-align:top; padding-bottom: 5px;">
                                            <td>
                                                <div style="font-weight:bold"><%# ((DateTime) Eval("Date")).ToShortDateString() %></div>
                                                <div><%# Eval("TailNumDisplay") %></div>
                                            </td>
                                            <td>
                                                <div><span style="font-style:italic"><%# Eval("Route") %></span> <%# Eval("Comment") %></div>
                                            <div><asp:Label ID="lblExistingSigWillBeRemoved" runat="server" CssClass="error" Text="<%$ Resources:SignOff, RequestSignatureReSign %>" Visible='<%# (LogbookEntry.SignatureState) Eval("CFISignatureState") == LogbookEntry.SignatureState.Invalid %>' meta:resourcekey="lblExistingSigWillBeRemovedResource1"></asp:Label></div>
                                            </td>
                                        </tr>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </table>
                        </td>
                    </tr>
                </table>
            </asp:WizardStep>
        </WizardSteps>
    </asp:Wizard>
    <p><asp:Label ID="lblNote" Font-Bold="True" runat="server" 
            Text="<%$ Resources:LocalizedText, Note %>" meta:resourcekey="lblNoteResource1"></asp:Label> 
        <asp:Label ID="lblSignatureDisclaimer" runat="server" CssClass="fineprint" Text="MyFlightbook provides facilities for an instructor to sign a logbook on another pilot's behalf, with that pilot's permission. While every attempt is made to ensure the integrity of this process, it has not been vetted by the FAA or other similar agencies, and may therefore not be acceptible to these agencies." 
            meta:resourcekey="lblSignatureDisclaimerResource1"></asp:Label>
        <asp:HyperLink ID="lnkSignatureDescription" runat="server" Text="Learn More..." NavigateUrl="~/Public/CFISigs.aspx" meta:resourcekey="lnkSignatureDescriptionResource1"></asp:HyperLink></p>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

