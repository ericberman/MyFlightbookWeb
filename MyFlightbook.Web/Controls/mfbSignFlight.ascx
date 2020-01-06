<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSignFlight" Codebehind="mfbSignFlight.ascx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc3" %>
<%@ Register src="mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc2" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbScribbleSignature.ascx" TagPrefix="uc1" TagName="mfbScribbleSignature" %>
<asp:Panel ID="pnlMain" runat="server" meta:resourcekey="pnlMainResource1">
    <p><asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label> 
        <asp:Label ID="lblSignatureDisclaimer" runat="server" CssClass="fineprint" Text=""></asp:Label></p>
        <asp:MultiView ID="mvFlightToSign" ActiveViewIndex="0" runat="server">
            <asp:View ID="vwEntrySummary" runat="server">
                <div class="signFlightFlightToSign">
                    <asp:FormView ID="fvEntryToSign" runat="server" EnableModelValidation="True" meta:resourcekey="fvEntryToSignResource1" OnDataBound="fvEntryToSign_OnDataBound">
                        <ItemTemplate>
                            <table>
                                <tr style="vertical-align: text-top">
                                    <td colspan="4">
                                        <div><span style="font-weight: bold; font-size:larger"><%# ((DateTime) Eval("Date")).ToShortDateString() %> <%# Eval("TailNumDisplay") %></span> (<%# Eval("CatClassDisplay") %> <%# Eval("ModelDisplay") %>)</div>
                                        <div><span style="font-weight:bold;"><%# Eval("Route") %></span> <%# Eval("Comment") %></div>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="lblLandingPrompt" runat="server" Font-Bold="True" meta:resourcekey="lblLandingPromptResource1" Text="<%$ Resources:LogbookEntry, FieldLanding %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("Landings").FormatInt() %></td>
                                    <td>
                                        <asp:Label ID="lblNightLandingsPrompt" runat="server" Font-Bold="True" meta:resourcekey="lblNightLandingsPromptResource1" Text="<%$ Resources:LogbookEntry, FieldNightLandings %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("NightLandings").FormatInt() %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="lblApproachesPrompt" runat="server" Font-Bold="True" meta:resourcekey="lblApproachesPromptResource1" Text="<%$ Resources:LogbookEntry, FieldApproaches %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("Approaches").FormatInt() %></td>
                                    <td>
                                        <asp:Label ID="lblHoldPrompt" runat="server" Font-Bold="True" meta:resourcekey="lblHoldPromptResource1" Text="<%$ Resources:LogbookEntry, FieldHold %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("fHoldingProcedures").FormatBoolean() %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label7" runat="server" Font-Bold="True" meta:resourcekey="Label7Resource1" Text="<%$ Resources:LogbookEntry, FieldXCountry %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label9" runat="server" Font-Bold="True" meta:resourcekey="Label9Resource1" Text="<%$ Resources:LogbookEntry, FieldNight %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label11" runat="server" Font-Bold="True" meta:resourcekey="Label11Resource1" Text="<%$ Resources:LogbookEntry, FieldSimIMCFull %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label13" runat="server" Font-Bold="True" meta:resourcekey="Label13Resource1" Text="<%$ Resources:LogbookEntry, FieldIMC %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label1" runat="server" Font-Bold="True" meta:resourcekey="Label1Resource1" Text="<%$ Resources:LogbookEntry, FieldGroundSimFull %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label4" runat="server" Font-Bold="True" meta:resourcekey="Label4Resource1" Text="<%$ Resources:LogbookEntry, FieldDual %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label6" runat="server" Font-Bold="True" meta:resourcekey="Label6Resource1" Text="<%$ Resources:LogbookEntry, FieldCFI %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label16" runat="server" Font-Bold="True" meta:resourcekey="Label16Resource1" Text="<%$ Resources:LogbookEntry, FieldSIC %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label18" runat="server" Font-Bold="True" meta:resourcekey="Label18Resource1" Text="<%$ Resources:LogbookEntry, FieldPIC %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label20" runat="server" Font-Bold="True" meta:resourcekey="Label20Resource1" Text="<%$ Resources:LogbookEntry, FieldTotal %>"></asp:Label>
                                    </td>
                                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td colspan="4">
                                        <asp:Repeater ID="rptProps" runat="server">
                                            <ItemTemplate>
                                                <%# Eval("DisplayString") %>
                                            </ItemTemplate>
                                            <SeparatorTemplate>
                                                <br />
                                            </SeparatorTemplate>
                                        </asp:Repeater>
                                    </td>
                                </tr>
                                <%--
                                    // Only allow editing if the CFI is:
                                    // a) authenticated (i.e., not ad-hoc signing)
                                    // b) signed in (no need for a password)
                                    // c) named on the flight (i.e., the flight is awaiting this CFI's signature or has previously signed it)
                                --%>
                                <tr runat="server" visible='<%# SigningMode == SignMode.Authenticated && Page.User.Identity.IsAuthenticated && ((LogbookEntry) Container.DataItem).CanEditThisFlight(Page.User.Identity.Name) %>'>
                                    <td colspan="4">
                                        <asp:LinkButton ID="lnkEditFlightToSign" runat="server" OnClick="lnkEditFlightToSign_Click" Text="<%$ Resources:SignOff, InstructorEditFlightPrompt %>"></asp:LinkButton>
                                    </td>
                                </tr>
                            </table>
                        </ItemTemplate>
                    </asp:FormView>
                </div>
                <div class="signFlightSignatureBlock">
                    <h2><asp:Image ID="imgSig" runat="server" ImageUrl="~/images/sigok.png" ImageAlign="AbsMiddle" /> <% =Resources.SignOff.SignFlightAffirmation %> <asp:Label ID="lblCFIName" runat="server" Text=""></asp:Label></h2>
                    <asp:Panel ID="rowEmail" runat="server" meta:resourcekey="rowEmailResource1">
                        <div><asp:Label ID="lblInstructorNamePrompt" runat="server" Font-Bold="True" 
                                Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>" 
                                meta:resourcekey="lblInstructorNamePromptResource1"></asp:Label></div>
                        <div>
                            <asp:TextBox ID="txtCFIName" runat="server" Width="280px" 
                                meta:resourcekey="txtCFINameResource1"></asp:TextBox>
                            <div>
                            <asp:RequiredFieldValidator ID="valNameRequired" runat="server" ControlToValidate="txtCFIName"
                                    ErrorMessage="You must provide your name to sign a flight." 
                                    Display="Dynamic" ToolTip="Name is required." CssClass="error" 
                                meta:resourcekey="RequiredFieldValidator1Resource1"></asp:RequiredFieldValidator>
                            </div>
                        </div>
                        <div><asp:Label ID="lblCFIEmailPrompt" runat="server" 
                                Text="<%$ Resources:Signoff, CFIEmail %>" Font-Bold="True" 
                                meta:resourcekey="lblCFIEmailPromptResource1"></asp:Label></div>
                        <div>
                            <asp:TextBox ID="txtCFIEmail" TextMode="Email" runat="server" Width="280px" meta:resourcekey="txtCFIEmailResource1"></asp:TextBox>
                            <div>
                            <asp:RequiredFieldValidator ID="valEmailRequired" runat="server" ControlToValidate="txtCFIEmail"
                                ErrorMessage="You must provide a valid e-mail address to sign a flight." 
                                Display="Dynamic" ToolTip="E-mail is required." CssClass="error" 
                                meta:resourcekey="EmailRequiredResource1"></asp:RequiredFieldValidator>
                            <asp:RegularExpressionValidator ID="valBadEmail" runat="server" ControlToValidate="txtCFIEmail"
                                Display="Dynamic" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                ErrorMessage="Please enter a valid e-mail address." CssClass="error" 
                                meta:resourcekey="EmailRegExpResource1"></asp:RegularExpressionValidator>
                            </div>
                        </div>
                    </asp:Panel>
                    <div>
                        <asp:Label ID="lblCFICertificatePrompt" runat="server" 
                            Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>" 
                            Font-Bold="True" meta:resourcekey="lblCFICertificatePromptResource1"></asp:Label>
                            <asp:Label ID="lblCFICertificate" runat="server" 
                            meta:resourcekey="lblCFICertificateResource1"></asp:Label>
                    </div>
                    <div>
                        <asp:TextBox ID="txtCFICertificate" runat="server" Visible="False" 
                            Width="280px" meta:resourcekey="txtCFICertificateResource1"></asp:TextBox>
                        <div>
                        <asp:RequiredFieldValidator ID="valCertificateRequired" runat="server" ControlToValidate="txtCFICertificate"
                                ErrorMessage="You must provide a valid CFI certificate." 
                                Display="Dynamic" ToolTip="Certificate is required." CssClass="error" 
                            meta:resourcekey="RequiredFieldValidator2Resource1"></asp:RequiredFieldValidator>
                        </div>
                    </div>
                    <div>
                        <asp:Label ID="lblCFIDatePrompt" runat="server" 
                            Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>" 
                            Font-Bold="True" meta:resourcekey="lblCFIDatePromptResource1"></asp:Label>
                            <asp:Label ID="lblCFIDate" runat="server" 
                            meta:resourcekey="lblCFIDateResource1"></asp:Label>
                    </div>
                    <asp:UpdatePanel ID="updPanelComments" runat="server">
                        <ContentTemplate>
                            <div>
                                <uc2:mfbTypeInDate runat="server" ID="dropDateCFIExpiration" Visible="false" Width="280px" DefaultType="Today" />
                                <div>
                                <asp:CustomValidator ID="valCFIExpiration" runat="server" 
                                    ErrorMessage="To sign, your CFI certificate must not be expired." 
                                    CssClass="error" Display="Dynamic" 
                                    onservervalidate="valCFIExpiration_ServerValidate" 
                                    meta:resourcekey="valCFIExpirationResource1"></asp:CustomValidator>
                                </div>
                            </div>
                            <asp:Panel ID="pnlRowPassword" runat="server" Visible="False"
                                meta:resourcekey="pnlRowPasswordResource1">
                                <div>
                                    <asp:Label ID="lblPassPrompt" runat="server" Text="<%$ Resources:SignOff, SignReEnterPassword %>" 
                                        Font-Bold="True" meta:resourcekey="lblPassPromptResource2"></asp:Label></div>
                                <div>
                                    <asp:TextBox ID="txtPassConfirm" runat="server" TextMode="Password" Width="280px"   
                                        meta:resourcekey="txtPassConfirmResource1"></asp:TextBox><br />
                                    <asp:RequiredFieldValidator ID="valPassword" runat="server" 
                                        ErrorMessage="The instructor must provide a password to sign this flight." Enabled="False"
                                        ControlToValidate="txtPassConfirm" CssClass="error" 
                                        meta:resourcekey="valPasswordResource1" Display="Dynamic"></asp:RequiredFieldValidator>
                                    <asp:CustomValidator Enabled="False"
                                        ID="valCorrectPassword" runat="server" CssClass="error" 
                                        ErrorMessage="Please enter the correct password for this account" 
                                        onservervalidate="valCorrectPassword_ServerValidate" Display="Dynamic" 
                                        meta:resourcekey="valCorrectPasswordResource1"></asp:CustomValidator>
                                </div>
                            </asp:Panel>
                            <div>
                                <asp:Label ID="lblCFICommentsPrompt" runat="server" 
                                    Text="<%$ Resources:Signoff, CFIComments %>" Font-Bold="True" 
                                    meta:resourcekey="lblCFICommentsPromptResource1"></asp:Label>
                            </div>
                            <div><asp:CheckBox ID="ckSignSICEndorsement" runat="server" Text="Use SIC Endorsement from AC 135-43" Visible="false" AutoPostBack="true" meta:resourcekey="ckSignSICEndorsementResource1" OnCheckedChanged="ckSignSICEndorsement_CheckedChanged" /></div>
                            <div>
                                <asp:MultiView ID="mvComments" runat="server" ActiveViewIndex="0">
                                    <asp:View ID="vwEdit" runat="server">
                                        <asp:TextBox ID="txtComments" runat="server" Rows="3" Width="280px" 
                                            TextMode="MultiLine" meta:resourcekey="txtCommentsResource1"></asp:TextBox>
                                    </asp:View>
                                    <asp:View ID="vwTemplate" runat="server">
                                        <asp:Label ID="lblSICTemplate" runat="server" Width="280px" BorderColor="Gray" BorderStyle="Solid" BorderWidth="1px" style="padding: 4px"></asp:Label>
                                    </asp:View>
                                </asp:MultiView>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <asp:Panel ID="rowSignature" Visible="False" runat="server" 
                        meta:resourcekey="rowSignatureResource1">
                        <div><asp:Label ID="lblSignaturePrompt" runat="server" Text="Signature" 
                                Font-Bold="True" meta:resourcekey="lblSignaturePromptResource1"></asp:Label></div>
                        <uc1:mfbScribbleSignature runat="server" id="mfbScribbleSignature" />
                    </asp:Panel>
                    <asp:Panel ID="pnlCopyFlight" runat="server">
                        <asp:CheckBox ID="ckCopyFlight" runat="server" />
                    </asp:Panel>
                    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False" 
                        meta:resourcekey="lblErrResource1"></asp:Label></div>
                    <div style="text-align:center">
                        <asp:Button ID="btnCancel" runat="server" 
                            Text="<%$ Resources:SignOff, CancelSignFlight %>" Visible="False" 
                            onclick="btnCancel_Click" meta:resourcekey="btnCancelResource1" />
                        &nbsp;&nbsp;
                        <asp:Button ID="btnSign" runat="server" 
                            Text="<%$ Resources:SignOff, SignFlight %>" onclick="btnSign_Click" 
                            meta:resourcekey="btnSignResource1" />
                    </div>
                </div>
            </asp:View>
            <asp:View ID="vwEntryEdit" runat="server">
                <uc4:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="mfbEditFlight1_FlightUpdated" />
            </asp:View>
        </asp:MultiView>
    <div>
    <div><br />&nbsp;<br />&nbsp;<br />&nbsp;</div>
</asp:Panel>
