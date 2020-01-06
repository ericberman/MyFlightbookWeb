<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbEditEndorsement" Codebehind="mfbEditEndorsement.ascx.cs" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc2" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbScribbleSignature.ascx" TagPrefix="uc1" TagName="mfbScribbleSignature" %>



<asp:Panel ID="pnlEditEndorsement" runat="server" BorderStyle="Solid" Style="padding:5px; max-width: 480px;" BorderWidth="1px" BorderColor="Black" BackColor="#CCCCCC">
    <table style="width: 100%">
        <tr>
            <td colspan="2">
                <asp:TextBox ID="txtTitle" runat="server" Width="100%"></asp:TextBox>
                <asp:TextBoxWatermarkExtender ID="wmeCustomTitle" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Signoff, EndorsementTitleWatermark %>" TargetControlID="txtTitle" runat="server" />
                <div><asp:RequiredFieldValidator ID="valTitleRequired" runat="server" Display="Dynamic" EnableClientScript="true" ErrorMessage="<%$ Resources:Signoff, errTitleRequired %>" ControlToValidate="txtTitle" CssClass="error"></asp:RequiredFieldValidator></div>
            </td>
        </tr>
        <tr>
            <td style="font-weight: bold;">
                <asp:Literal ID="Literal1" runat="server" Text="<%$ Resources:SignOff, EditEndorsementFARPrompt %>" /></td>
            <td>
                <asp:Label ID="lblEndorsementFAR" runat="server" Text=""></asp:Label></td>
        </tr>
        <tr>
            <td colspan="2">
                <hr />
                <div style="line-height: 180%">
                    <asp:PlaceHolder ID="plcTemplateForm" runat="server"></asp:PlaceHolder>
                </div>
                <div>
                    <asp:PlaceHolder ID="plcValidations" runat="server"></asp:PlaceHolder>
                </div>
                <hr />
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal2" runat="server" Text="<%$ Resources:SignOff, EditEndorsementDatePrompt %>" /></b>
            </td>
            <td>
                <uc1:mfbTypeInDate ID="mfbTypeInDate1" runat="server" DefaultType="Today" />
                <asp:CustomValidator ID="valNoPostDate" CssClass="error" Display="Dynamic" runat="server" OnServerValidate="valNoPostDate_ServerValidate" ErrorMessage="<%$ Resources:SignOff, errNoPostDating %>"></asp:CustomValidator>
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal3" runat="server" Text="<%$ Resources:SignOff, EditEndorsementStudentPrompt %>" /></b></td>
            <td>
                <asp:MultiView ID="mvStudent" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwStudentAuthenticated" runat="server">
                        <asp:Label ID="lblStudent" runat="server"></asp:Label>
                    </asp:View>
                    <asp:View ID="vwStudentOffline" runat="server">
                        <asp:TextBox ID="txtOfflineStudent" runat="server"></asp:TextBox>
                        <asp:TextBoxWatermarkExtender ID="TextBox1_TextBoxWatermarkExtender" runat="server" Enabled="True" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:SignOff, EditEndorsementStudentNamePrompt %>" TargetControlID="txtOfflineStudent"></asp:TextBoxWatermarkExtender>
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
        <tr runat="server" visible="false" id="rowPassword">
            <td>
                <asp:Label ID="lblPassPrompt" runat="server" Text="<%$ Resources:SignOff, SignReEnterPassword %>" 
                                Font-Bold="True"></asp:Label>
            </td>
            <td>
                <asp:TextBox ID="txtPassConfirm" runat="server" TextMode="Password"></asp:TextBox><br />
                <div><asp:RequiredFieldValidator ID="valPassword" runat="server" 
                    ErrorMessage="<%$ Resources:SignOff, errInstructorPasswordRequiredToEndorse %>" Enabled="False"
                    ControlToValidate="txtPassConfirm" CssClass="error" 
                    Display="Dynamic"></asp:RequiredFieldValidator></div>
                <div><asp:CustomValidator Enabled="False"
                    ID="valCorrectPassword" runat="server" CssClass="error" 
                    ErrorMessage="<%$ Resources:Signoff, errInstructorBadPassword %>" 
                    onservervalidate="valCorrectPassword_ServerValidate" Display="Dynamic"></asp:CustomValidator></div>
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>" /></b>
            </td>
            <td>
                <asp:MultiView ID="mvCFI" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwStaticCFI" runat="server">
                        <asp:Label ID="lblCFI" runat="server"></asp:Label>
                    </asp:View>
                    <asp:View ID="vwAdhocCFI" runat="server">
                        <asp:TextBox ID="txtCFI" runat="server"></asp:TextBox>
                        <div>
                            <asp:RequiredFieldValidator ID="valRequiredCFI" runat="server" ErrorMessage="<%$ Resources:SignOff, errNoInstructor %>" ControlToValidate="txtCFI"
                                Display="Dynamic" CssClass="error"></asp:RequiredFieldValidator>
                        </div>
                    </asp:View>
                </asp:MultiView>             
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>" /></b>
            </td>
            <td>
                <asp:MultiView ID="mvCFICert" runat="server" ActiveViewIndex="0">
                    <asp:View ID="vwStaticCert" runat="server">
                        <asp:Label ID="lblCFICert" runat="server"></asp:Label>
                    </asp:View>
                    <asp:View ID="vwAdhocCert" runat="server">
                        <asp:TextBox runat="server" ID="txtCFICert"></asp:TextBox>
                        <div>
                            <asp:RequiredFieldValidator ID="valRequiredCert" runat="server" ErrorMessage="<%$ Resources:SignOff, errNeedCertificate %>" ControlToValidate="txtCFICert"
                                Display="Dynamic" CssClass="error"></asp:RequiredFieldValidator>
                        </div>
                    </asp:View>
                </asp:MultiView>
                
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal6" runat="server" Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>" /></b>
            </td>
            <td>
                <asp:MultiView ID="mvCertExpiration" runat="server">
                    <asp:View ID="vwStaticCertExpiration" runat="server">
                        <asp:Label ID="lblCFIExp" runat="server"></asp:Label>
                    </asp:View>
                    <asp:View ID="vwAdhocCertExpiration" runat="server">
                        <uc2:mfbTypeInDate runat="server" ID="mfbDateCertExpiration" DefaultType="None" />
                    </asp:View>
                </asp:MultiView>
            </td>
        </tr>
    </table>
    <div runat="server" id="rowScribble" visible="false">
        <div><b><asp:Localize ID="locSignPrompt" runat="server" Text="<%$ Resources:Signoff, SignFlightAffirmation %>"></asp:Localize></b></div>
        <uc1:mfbScribbleSignature runat="server" id="mfbScribbleSignature" />
    </div>
    <div style="text-align:right;">
        <asp:Button ID="btnAddEndorsement" runat="server" 
                Text="<%$ Resources:SignOff, EditEndorsementAddEndorsement %>" onclick="btnAddEndorsement_Click" />
    </div>
    <asp:HiddenField ID="hdnEndorsementID" Value="-1" runat="server" />
</asp:Panel>
