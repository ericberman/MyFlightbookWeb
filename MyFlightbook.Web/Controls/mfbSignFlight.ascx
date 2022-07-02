<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbSignFlight.ascx.cs" Inherits="MyFlightbook.Instruction.mfbSignFlight" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register src="mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc3" %>
<%@ Register src="mfbEditFlight.ascx" tagname="mfbEditFlight" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc2" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbScribbleSignature.ascx" TagPrefix="uc1" TagName="mfbScribbleSignature" %>
<asp:Panel ID="pnlMain" runat="server">
    <p><asp:Label ID="lblNote" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>" /> 
        <asp:Label ID="lblSignatureDisclaimer" runat="server" CssClass="fineprint" Text="" /></p>
        <asp:MultiView ID="mvFlightToSign" ActiveViewIndex="0" runat="server">
            <asp:View ID="vwEntrySummary" runat="server">
                <div class="signFlightFlightToSign">
                    <asp:FormView ID="fvEntryToSign" runat="server" EnableModelValidation="True" OnDataBound="fvEntryToSign_OnDataBound">
                        <ItemTemplate>
                            <table>
                                <tr style="vertical-align: text-top">
                                    <td colspan="4">
                                        <div><span style="font-weight: bold; font-size:larger"><%# ((DateTime) Eval("Date")).ToShortDateString() %> <%#: Eval("TailNumDisplay") %></span> (<%#: Eval("CatClassDisplay") %>&nbsp;<%#: Eval("ModelDisplay") %>)</div>
                                        <div><span style="font-weight:bold;"><%#: Eval("Route") %></span> <%#: Eval("Comment") %></div>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="lblLandingPrompt" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldLanding %>" />
                                    </td>
                                    <td><%# Eval("Landings").FormatInt() %></td>
                                    <td>
                                        <asp:Label ID="lblNightLandingsPrompt" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldNightLandings %>" />
                                    </td>
                                    <td><%# Eval("NightLandings").FormatInt() %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="lblApproachesPrompt" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldApproaches %>" />
                                    </td>
                                    <td><%# Eval("Approaches").FormatInt() %></td>
                                    <td>
                                        <asp:Label ID="lblHoldPrompt" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldHold %>" />
                                    </td>
                                    <td><%# Eval("fHoldingProcedures").FormatBoolean() %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label7" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldXCountry %>" />
                                    </td>
                                    <td><%# Eval("CrossCountry").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label9" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldNight %>" />
                                    </td>
                                    <td><%# Eval("Nighttime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label11" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldSimIMCFull %>" />
                                    </td>
                                    <td><%# Eval("SimulatedIFR").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label13" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldIMC %>" />
                                    </td>
                                    <td><%# Eval("IMC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label1" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldGroundSimFull %>" />
                                    </td>
                                    <td><%# Eval("GroundSim").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label4" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldDual %>" />
                                    </td>
                                    <td><%# Eval("Dual").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label6" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldCFI %>" />
                                    </td>
                                    <td><%# Eval("CFI").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label16" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldSIC %>" />
                                    </td>
                                    <td><%# Eval("SIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td>
                                        <asp:Label ID="Label18" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldPIC %>" />
                                    </td>
                                    <td><%# Eval("PIC").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                    <td>
                                        <asp:Label ID="Label20" runat="server" Font-Bold="True" Text="<%$ Resources:LogbookEntry, FieldTotal %>" />
                                    </td>
                                    <td><%# Eval("TotalFlightTime").FormatDecimal(CurrentUser.UsesHHMM) %></td>
                                </tr>
                                <tr style="vertical-align: text-top">
                                    <td colspan="4">
                                        <asp:Repeater ID="rptProps" runat="server">
                                            <ItemTemplate>
                                                <%#: Eval("DisplayString") %>
                                            </ItemTemplate>
                                            <SeparatorTemplate>
                                                <br />
                                            </SeparatorTemplate>
                                        </asp:Repeater>
                                    </td>
                                </tr>
                            </table>
                            <%--
                                // Only allow editing if the CFI is:
                                // a) authenticated (i.e., not ad-hoc signing)
                                // b) signed in (no need for a password)
                                // c) named on the flight (i.e., the flight is awaiting this CFI's signature or has previously signed it)
                            --%>
                            <asp:Panel ID="pnlEdit" runat="server" Visible='<%# SigningMode == SignMode.Authenticated && Page.User.Identity.IsAuthenticated && ((LogbookEntry) Container.DataItem).CanEditThisFlight(Page.User.Identity.Name) %>'>
                                <asp:LinkButton ID="lnkEditFlightToSign" runat="server" OnClick="lnkEditFlightToSign_Click">
                                    <asp:Image ID="imgPencil" runat="server" style="padding-right: 4px;" ImageUrl="~/images/pencilsm.png" />
                                    <asp:Label ID="lblEdit" runat="server"  Text="<%$ Resources:SignOff, InstructorEditFlightPrompt %>" AssociatedControlID="imgPencil" />
                                </asp:LinkButton>
                            </asp:Panel>
                        </ItemTemplate>
                    </asp:FormView>
                </div>
                <div class="signFlightSignatureBlock">
                    <h2><asp:Image ID="imgSig" runat="server" ImageUrl="~/images/sigok.png" ImageAlign="AbsMiddle" /> <% =Resources.SignOff.SignFlightAffirmation %> <asp:Label ID="lblCFIName" runat="server" Text=""></asp:Label></h2>
                    <asp:Panel ID="rowEmail" runat="server">
                        <div><asp:Label ID="lblInstructorNamePrompt" runat="server" Font-Bold="True" 
                                Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>" /></div>
                        <div>
                            <asp:TextBox ID="txtCFIName" runat="server" Width="280px" />
                            <div>
                            <asp:RequiredFieldValidator ID="valNameRequired" runat="server" ControlToValidate="txtCFIName"
                                    ErrorMessage="<%$ Resources:SignOff, errProvideNameToSign %>" 
                                    Display="Dynamic" ToolTip="<%$ Resources:SignOff, errNameRequired %>" CssClass="error" />
                            </div>
                        </div>
                        <div><asp:Label ID="lblCFIEmailPrompt" runat="server" 
                                Text="<%$ Resources:Signoff, CFIEmail %>" Font-Bold="True" /></div>
                        <div>
                            <asp:TextBox ID="txtCFIEmail" TextMode="Email" runat="server" Width="280px" />
                            <div>
                            <asp:RequiredFieldValidator ID="valEmailRequired" runat="server" ControlToValidate="txtCFIEmail"
                                ErrorMessage="<%$ Resources:SignOff, errEmailMissing %>" 
                                Display="Dynamic" ToolTip="<%$ Resources:SignOff, errEmailMissing %>" CssClass="error" />
                            <asp:RegularExpressionValidator ID="valBadEmail" runat="server" ControlToValidate="txtCFIEmail"
                                Display="Dynamic" ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"
                                ErrorMessage="<%$ Resources:SignOff, errInvalidEmail %>" CssClass="error" />
                            </div>
                        </div>
                    </asp:Panel>
                    <div>
                        <asp:Label ID="lblCFICertificatePrompt" runat="server" 
                            Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>" Font-Bold="True" />
                            <asp:Label ID="lblCFICertificate" runat="server" />
                    </div>
                    <div>
                        <asp:TextBox ID="txtCFICertificate" runat="server" Visible="False" Width="280px" />
                        <div>
                        <asp:RequiredFieldValidator ID="valCertificateRequired" runat="server" ControlToValidate="txtCFICertificate"
                                ErrorMessage="<%$ Resources:SignOff, errMissingCertificate %>" 
                                Display="Dynamic" ToolTip="<%$ Resources:SignOff, errMissingCertificate %>" CssClass="error" />
                        </div>
                    </div>
                    <asp:UpdatePanel ID="updPanelComments" runat="server">
                        <ContentTemplate>
                            <div>
                                <asp:Label ID="lblCFIDatePrompt" runat="server" 
                                    Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>" 
                                    Font-Bold="True" />
                                <asp:CheckBox ID="ckATP" runat="server" Text="<%$ Resources:Signoff, SignFlightATP %>" AutoPostBack="true" OnCheckedChanged="ckATP_CheckedChanged" />
                                <asp:Label ID="lblCFIDate" runat="server" />
                            </div>
                            <div>
                                <uc2:mfbTypeInDate runat="server" ID="dropDateCFIExpiration" Visible="false" Width="280px" DefaultType="Today" />
                                <div>
                                <asp:CustomValidator ID="valCFIExpiration" runat="server" 
                                    ErrorMessage="<%$ Resources:SignOff, errCertificateExpired %>" 
                                    CssClass="error" Display="Dynamic" 
                                    onservervalidate="valCFIExpiration_ServerValidate" />
                                </div>
                            </div>
                            <asp:Panel ID="pnlRowPassword" runat="server" Visible="False">
                                <div>
                                    <asp:Label ID="lblPassPrompt" runat="server" Text="<%$ Resources:SignOff, SignReEnterPassword %>" 
                                        Font-Bold="True" />
                                </div>
                                <div>
                                    <asp:TextBox ID="txtPassConfirm" runat="server" TextMode="Password" Width="280px" /><br />
                                    <asp:RequiredFieldValidator ID="valPassword" runat="server" 
                                        ErrorMessage="<%$ Resources:SignOff, errPasswordRequiredForSigning %>" Enabled="False"
                                        ControlToValidate="txtPassConfirm" CssClass="error" Display="Dynamic" />
                                    <asp:CustomValidator Enabled="False"
                                        ID="valCorrectPassword" runat="server" CssClass="error" 
                                        ErrorMessage="<%$ Resources:SignOff, errIncorrectPasswordForSigning %>" 
                                        onservervalidate="valCorrectPassword_ServerValidate" Display="Dynamic" />
                                </div>
                            </asp:Panel>
                            <div style="width:280px">
                                <asp:Label ID="lblCFICommentsPrompt" runat="server" 
                                    Text="<%$ Resources:Signoff, CFIComments %>" Font-Bold="True" />
                                <span class="fineprint" style="float:right" id="lblCharCount">0/250</span>
                            </div>
                            <div><asp:CheckBox ID="ckSignSICEndorsement" runat="server" Text="<%$ Resources:SignOff, PromptUseAC13543ForSIC %>" Visible="false" AutoPostBack="true" OnCheckedChanged="ckSignSICEndorsement_CheckedChanged" /></div>
                            <div>
                                <asp:MultiView ID="mvComments" runat="server" ActiveViewIndex="0">
                                    <asp:View ID="vwEdit" runat="server">
                                        <asp:TextBox ID="txtComments" runat="server" Rows="3" Width="280px" TextMode="MultiLine" oninput="updateCount(this)" />
                                        <script type="text/javascript">
                                            function updateCount(sender) {
                                                document.getElementById("lblCharCount").innerText = sender.value.length + "/250";
                                            }
                                        </script>
                                    </asp:View>
                                    <asp:View ID="vwTemplate" runat="server">
                                        <asp:Label ID="lblSICTemplate" runat="server" Width="280px" BorderColor="Gray" BorderStyle="Solid" BorderWidth="1px" style="padding: 4px"></asp:Label>
                                    </asp:View>
                                </asp:MultiView>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                    <asp:Panel ID="rowSignature" Visible="False" runat="server">
                        <div><asp:Label ID="lblSignaturePrompt" runat="server" Text="<%$ Resources:SignOff, PromptSignature %>" Font-Bold="True" /></div>
                        <uc1:mfbScribbleSignature runat="server" id="mfbScribbleSignature" />
                    </asp:Panel>
                    <asp:Panel ID="pnlCopyFlight" runat="server">
                        <asp:CheckBox ID="ckCopyFlight" runat="server" />
                    </asp:Panel>
                    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False" />
                </div>
                <div style="text-align:center">
                    <asp:Button ID="btnCancel" runat="server" 
                        Text="<%$ Resources:SignOff, CancelSignFlight %>" Visible="False" 
                        onclick="btnCancel_Click" />
                    &nbsp;&nbsp;
                    <asp:Button ID="btnSign" runat="server" 
                        Text="<%$ Resources:SignOff, SignFlight %>" onclick="btnSign_Click" />
                    <asp:Button ID="btnSignAndNext" runat="server" 
                        Text="<%$ Resources:SignOff, SignFlightAndNext %>" onclick="btnSignAndNext_Click" Visible="false" />
                </div>
            </asp:View>
            <asp:View ID="vwEntryEdit" runat="server">
                <uc4:mfbEditFlight ID="mfbEditFlight1" runat="server" OnFlightUpdated="mfbEditFlight1_FlightUpdated" />
            </asp:View>
        </asp:MultiView>
    <div><br />&nbsp;<br />&nbsp;<br />&nbsp;</div>
</asp:Panel>
