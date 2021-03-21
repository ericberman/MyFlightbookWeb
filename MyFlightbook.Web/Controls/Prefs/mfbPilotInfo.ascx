<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbPilotInfo.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbPilotInfo" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagName="mfbTypeInDate" TagPrefix="uc2" %>
<%@ Register Src="~/Controls/mfbBasicMedManager.ascx" TagPrefix="uc1" TagName="mfbBasicMedManager" %>
<cc1:Accordion ID="accordianPilotInfo" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250">
    <Panes>
        <cc1:AccordionPane runat="server" ID="acpMedical" ContentCssClass="" HeaderCssClass="">
            <Header>
                <asp:Localize ID="locHeaderMedical" runat="server" Text="<%$ Resources:Preferences, PilotInfoMedical %>"></asp:Localize>
            </Header>
            <Content>
                <asp:Panel ID="pnlMedical" runat="server" DefaultButton="btnUpdateMedical">
                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                        <ContentTemplate>
                            <table>
                                <tr>
                                    <td><asp:Localize ID="locLastMedicalPrompt" runat="server" Text="<%$ Resources:Preferences, PilotInfoLastMedical %>" /></td>
                                    <td>
                                        <uc2:mfbTypeInDate ID="dateMedical" runat="server" DefaultType="None" />
                                    </td>
                                </tr>
                                <tr>
                                    <td>
                                        <asp:Localize ID="locMedicalDurationPrompt" runat="server"
                                            Text="<%$ Resources:Preferences, MedicalTypePrompt %>" /></td>
                                    <td>
                                        <asp:DropDownList ID="cmbMedicalType" runat="server" ValidationGroup="valPilotInfo" AutoPostBack="true" OnSelectedIndexChanged="cmbMedicalType_SelectedIndexChanged">
                                            <asp:ListItem Selected="True" Text="<%$ Resources:Preferences, MedicalTypeOther %>" Value="Other" />
                                            <asp:ListItem Text="<%$ Resources:Preferences, MedicalTypeFAA1stClass %>" Value="FAA1stClass" />
                                            <asp:ListItem Text="<%$ Resources:Preferences, MedicalTypeFAA2ndClass %>" Value="FAA2ndClass" />
                                            <asp:ListItem Text="<%$ Resources:Preferences, MedicalTypeFAA3rdClass %>" Value="FAA3rdClass" />
                                            <asp:ListItem Text="<%$ Resources:Preferences, MedicalTypeEASA %>" Value="EASA" />
                                        </asp:DropDownList>
                                    </td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td>
                                        <div>
                                            <asp:DropDownList ID="cmbMonthsMedical" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbMonthsMedical_SelectedIndexChanged"
                                                ValidationGroup="valPilotInfo">
                                                <asp:ListItem Selected="True" Value="0" Text="<%$ Resources:Preferences, PilotInfoMedicalUnspecified %>"></asp:ListItem>
                                                <asp:ListItem Value="6" Text="<%$ Resources:Preferences, PilotInfoMedical6Months %>"></asp:ListItem>
                                                <asp:ListItem Value="12" Text="<%$ Resources:Preferences, PilotInfoMedical12Months %>"></asp:ListItem>
                                                <asp:ListItem Value="24" Text="<%$ Resources:Preferences, PilotInfoMedical24Months %>"></asp:ListItem>
                                                <asp:ListItem Value="36" Text="<%$ Resources:Preferences, PilotInfoMedical36Months %>"></asp:ListItem>
                                                <asp:ListItem Value="48" Text="<%$ Resources:Preferences, PilotInfoMedical48Months %>"></asp:ListItem>
                                                <asp:ListItem Value="60" Text="<%$ Resources:Preferences, PilotInfoMedical60Months %>"></asp:ListItem>
                                            </asp:DropDownList>
                                            <asp:CustomValidator ID="valMonthsMedical" runat="server"
                                                ErrorMessage="<%$ Resources:Preferences, PilotInfoMedicalDurationRequired %>"
                                                ControlToValidate="cmbMonthsMedical" CssClass="error" Display="Dynamic"
                                                OnServerValidate="DurationIsValid" ValidationGroup="valPilotInfo"></asp:CustomValidator>
                                        </div>
                                        <div>
                                            <asp:RadioButtonList ID="rblMedicalDurationType" runat="server" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="rblMedicalDurationType_SelectedIndexChanged">
                                                <asp:ListItem Value="0" Text="<%$ Resources:Preferences, PilotInfoMedicalFAARules %>" Selected="True"></asp:ListItem>
                                                <asp:ListItem Value="1" Text="<%$ Resources:Preferences, PilotInfoMedicalICAORules %>"></asp:ListItem>
                                            </asp:RadioButtonList>
                                        </div>
                                    </td>
                                </tr>
                                <tr runat="server" id="rowDOB">
                                    <td>
                                        <asp:Localize ID="locDOB" runat="server" Text="<%$ Resources:Profile, accountDateOfBirth %>" />
                                    </td>
                                    <td>
                                        <uc2:mfbTypeInDate ID="dateDOB" runat="server" DefaultType="None" />
                                        <asp:CustomValidator ID="valDOBRequired" runat="server"
                                            ErrorMessage="<%$ Resources:Preferences, MedicalDOBRequired %>"
                                            CssClass="error"
                                            OnServerValidate="valDOBRequired_ServerValidate" ValidationGroup="valPilotInfo" />
                                        <div><asp:Label ID="lblDOBNote" runat="server" CssClass="fineprint" 
                                                    Text="<%$ Resources:Preferences, PIlotInfoMedicalDOBNote %>" /></div>
                                    </td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td>
                                        <asp:Panel ID="pnlNextMedical" runat="server" Visible="False">
                                            <asp:Repeater ID="rptNextMedical" runat="server">
                                                <ItemTemplate>
                                                    <div><asp:Label ID="lblTitle" runat="server" Text='<%#: Eval("Attribute") %>' /> <asp:Label ID="lblStatus" runat="server" CssClass='<%# CSSForItem((MyFlightbook.Currency.CurrencyState) Eval("Status")) %>' Text='<%#: Eval("Value") %>' Font-Bold="true" /></div>
                                                </ItemTemplate>
                                            </asp:Repeater>
                                        </asp:Panel>
                                    </td>
                                </tr>
                                <tr style="vertical-align:top;">
                                    <td>
                                        <asp:Localize ID="locMedicalNotes" runat="server" Text="<%$ Resources:Preferences, PilotInfoMedicalNotes %>" />
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtMedicalNotes" runat="server" TextMode="MultiLine" Rows="4" style="min-width: 300px" />
                                        <div>
                                            <asp:Label ID="lblNotesHint" runat="server" Text="<%$ Resources:Preferences, PilotInfoMedicalNotesDescription %>" CssClass="fineprint" />
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td></td>
                                    <td>
                                        <asp:Button ID="btnUpdateMedical" runat="server"  
                                            Text="<%$ Resources:Preferences, PilotInfoMedicalUpdate %>" ValidationGroup="valMedical" 
                                            onclick="btnUpdateMedical_Click" />
                                        <div>
                                            <asp:Label ID="lblMedicalInfo" runat="server" CssClass="success" EnableViewState="False"
                                                Text="<%$ Resources:Preferences, PilotInfoMedicalUpdated %>" Visible="False"></asp:Label>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            <div>
                                <uc1:mfbBasicMedManager runat="server" id="BasicMedManager" />
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </asp:Panel>
            </Content>
        </cc1:AccordionPane>
        <cc1:AccordionPane runat="server" ID="acpCertificates" ContentCssClass="" HeaderCssClass="">
            <Header>
                <asp:Localize ID="locheaderCertificates" runat="server" Text="<%$ Resources:Profile, ProfilePilotInfoCertificates %>"></asp:Localize>
            </Header>
            <Content>
                <asp:Panel ID="pnlCertificates" runat="server" DefaultButton="btnUpdatePilotInfo">
                    <h3>
                        <asp:Localize ID="locRatings" runat="server" Text="<%$ Resources:Preferences, PilotInfoRatings %>"></asp:Localize></h3>
                    <p>
                        <asp:Localize ID="locRatingsPrompt" runat="server" Text="<%$ Resources:Preferences, PilotInfoRatingsPrompt %>"></asp:Localize></p>
                    <div>
                        <asp:GridView ID="gvRatings" CellPadding="4" runat="server" GridLines="None" AutoGenerateColumns="False" ShowHeader="False">
                            <Columns>
                                <asp:BoundField DataField="LicenseName">
                                <ItemStyle Font-Bold="True" VerticalAlign="Top" />
                                </asp:BoundField>
                                <asp:TemplateField>
                                    <ItemTemplate>
                                        <asp:Repeater ID="rptPrivs" runat="server" DataSource='<%# Eval("Privileges") %>'>
                                            <ItemTemplate>
                                                <div><%#: Container.DataItem %></div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ItemTemplate>
                                    <ItemStyle VerticalAlign="Top" />
                                </asp:TemplateField>
                            </Columns>
                            <EmptyDataTemplate>
                                <asp:Localize ID="locNoRatingsFound" runat="server" Text="<%$ Resources:Preferences, PilotInfoNoCheckrides %>" />
                            </EmptyDataTemplate>
                        </asp:GridView>
                    </div>
                    <h3>
                        <asp:Localize ID="locLicenseHeader" runat="server" Text="<%$ Resources:Preferences, PilotInfoCertificatePrompt %>" />
                    </h3>
                    <div>
                        <asp:TextBox ID="txtLicense" runat="server" ValidationGroup="valPilotInfo"></asp:TextBox>
                        <cc1:TextBoxWatermarkExtender ID="wmeLicense" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, PilotInfoCertificateWatermark %>" TargetControlID="txtLicense" runat="server" BehaviorID="wmeLicense" />
                        <br />
                        <asp:Label ID="lblLicenseFineprint" runat="server" CssClass="fineprint" 
                            Text="<%$ Resources:Preferences, PilotInfoLicenseFinePrint %>"></asp:Label>
                    </div>
                    <h3>
                        <asp:Localize ID="locCertPrompt" runat="server" Text="<%$ Resources:Preferences, PilotInfoInstructorCertificatePrompt %>" />
                    </h3>
                    <div>
                        <asp:TextBox ID="txtCertificate" runat="server" ValidationGroup="valPilotInfo" /> &nbsp;
                        <cc1:TextBoxWatermarkExtender ID="wmeCertificate" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, PilotInfoCertificateCFIWatermark %>" TargetControlID="txtCertificate" runat="server" BehaviorID="wmeCertificate" />
                        <asp:Localize ID="locExpiration" runat="server" Text="<%$ Resources:Preferences, PilotInfoCFIExpiration %>" />
                        <uc2:mfbTypeInDate ID="mfbTypeInDateCFIExpiration" runat="server" DefaultType="None" />
                        <br />
                        <asp:Label ID="lblCertFineprint" runat="server" CssClass="fineprint" 
                            Text="<%$ Resources:Preferences, PilotInfoCFIExpirationNote %>" />
                    </div>
                    <h3>
                        <asp:Localize ID="locLangProficiency" runat="server" Text="<%$ Resources:Preferences, PilotInfoEnglishProficiencyExpiration %>" />
                    </h3>
                    <div>
                        <uc2:mfbTypeInDate ID="mfbDateEnglishCheck" runat="server" DefaultType="None" />
                    </div>
                    <div>
                        <br />
                        <asp:Button ID="btnUpdatePilotInfo" runat="server"  
                            Text="<%$ Resources:Profile, ProfilePilotInfoCertificatesUpdate %>" ValidationGroup="valPilotInfo" 
                            onclick="btnUpdatePilotInfo_Click" />
                        <br />
                        <asp:Label ID="lblPilotInfoUpdated" runat="server" CssClass="success" EnableViewState="False"
                            Text="<%$ Resources:Profile, ProfilePilotInfoCertificatesUpdated %>" Visible="False" />
                        <br />
                    </div>
                </asp:Panel>
            </Content>
        </cc1:AccordionPane>
        <cc1:AccordionPane runat="server" ID="acpFlightReviews" ContentCssClass="" HeaderCssClass="">
            <Header>
                <asp:Localize ID="locBFRPrompt" runat="server" Text="<%$ Resources:Preferences, PilotInfoBFRs %>" />
            </Header>
            <Content>
                <p><asp:Label ID="lblBFRHelpText" runat="server" Text="<%$ Resources:Preferences, PilotInfoBFRNotes %>" /></p>
                <asp:GridView ID="gvBFR" runat="server" AutoGenerateColumns="False" 
                    GridLines="None" ShowHeader="False" CellPadding="5">
                    <Columns>
                        <asp:HyperLinkField DataNavigateUrlFields="FlightID" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}" DataTextField="Date" DataTextFormatString="{0:d}" />
                        <asp:BoundField DataField="DisplayString" />
                    </Columns>
                    <EmptyDataTemplate>
                        <p><asp:Localize ID="locNoBFRFound" runat="server" Text="<%$ Resources:Preferences, PilotInfoNoBFRFound %>" /></p>
                    </EmptyDataTemplate>
                </asp:GridView>
                <asp:Panel ID="pnlNextBFR" runat="server" Visible="False">
                    <asp:Localize ID="locNextBFRDue" runat="server" Text="<%$ Resources:Currency, NextFlightReview %>"></asp:Localize>
                    <asp:Label ID="lblNextBFR" runat="server" style="font-weight: bold;" />.<br /><br />
                </asp:Panel>
            </Content>
        </cc1:AccordionPane>
        <cc1:AccordionPane runat="server" ID="acpIPCs" ContentCssClass="" HeaderCssClass="">
            <Header>
                <asp:Localize ID="locIPCPrompt" runat="server" Text="<%$ Resources:Preferences, PilotInfoIPCHeader %>" />
            </Header>
            <Content>
                <p><asp:Label ID="lblIPCHelpText" runat="server" Text="<%$ Resources:Preferences, PilotInfoIPCHelp %>" /></p>
                <asp:GridView ID="gvIPC" runat="server" AutoGenerateColumns="False" 
                    GridLines="None" ShowHeader="False" CellPadding="5">
                    <Columns>
                        <asp:HyperLinkField DataNavigateUrlFields="FlightID" DataTextField="Date" DataTextFormatString="{0:d}" 
                            DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}" />
                        <asp:BoundField DataField="DisplayString" DataFormatString="{0}" />
                    </Columns>
                    <EmptyDataTemplate>
                        <p><asp:Localize ID="Localize1" runat="server" Text="<%$ Resources:Preferences, PilotInfoIPCNoneFound %>" /></p>
                    </EmptyDataTemplate>
                </asp:GridView>
            </Content>
        </cc1:AccordionPane>
    </Panes>
</cc1:Accordion>
