<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbEditEndorsement.ascx.cs" Inherits="Controls_mfbEditEndorsement" %>
<%@ Register src="mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc1" %>
<%@ Register assembly="AjaxControlToolkit" namespace="AjaxControlToolkit" tagprefix="asp" %>
<asp:Panel ID="pnlEditEndorsement" runat="server" BorderStyle="Solid" Style="padding:5px;" BorderWidth="1px" BorderColor="Black" Width="600px" BackColor="#CCCCCC">
    <table style="width: 100%">
        <tr>
            <td style="font-weight: bold; width:25%">
                <asp:Literal runat="server" Text="<%$ Resources:SignOff, EditEndorsementEndorsementPrompt %>" /></td>
            <td>
                <asp:TextBox ID="txtTitle" runat="server" Width="100%"></asp:TextBox>
                <asp:TextBoxWatermarkExtender ID="wmeCustomTitle" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Signoff, EndorsementTitleWatermark %>" TargetControlID="txtTitle" runat="server" />
                <div><asp:RequiredFieldValidator ID="valTitleRequired" runat="server" Display="Dynamic" EnableClientScript="true" ErrorMessage="<%$ Resources:Signoff, errTitleRequired %>" ControlToValidate="txtTitle" CssClass="error"></asp:RequiredFieldValidator></div>
            </td>
        </tr>
        <tr>
            <td style="font-weight: bold; width:25%">
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
                <uc1:mfbTypeInDate ID="mfbTypeInDate1" runat="server" />
                <asp:CustomValidator ID="valNoBackDate" CssClass="error" Display="Dynamic" runat="server" OnServerValidate="valNoBackDate_ServerValidate" ErrorMessage="<%$ Resources:SignOff, errNoBackDating %>"></asp:CustomValidator>
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
        <tr>
            <td>
                <b><asp:Literal ID="Literal4" runat="server" Text="<%$ Resources:SignOff, EditEndorsementInstructorPrompt %>" /></b>
            </td>
            <td>
                <asp:Label ID="lblCFI" runat="server" Text=""></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal5" runat="server" Text="<%$ Resources:SignOff, EditEndorsementCFIPrompt %>" /></b>
            </td>
            <td>
                <asp:Label ID="lblCFICert" runat="server" Text=""></asp:Label>
            </td>
        </tr>
        <tr>
            <td>
                <b><asp:Literal ID="Literal6" runat="server" Text="<%$ Resources:SignOff, EditEndorsementExpirationPrompt %>" /></b>
            </td>
            <td>
                <asp:Label ID="lblCFIExp" runat="server" Text=""></asp:Label>
            </td>
        </tr>
        <tr>
            <td></td><td style="text-align:right"><asp:Button ID="btnAddEndorsement" runat="server" 
                Text="<%$ Resources:SignOff, EditEndorsementAddEndorsement %>" onclick="btnAddEndorsement_Click" /></td>
        </tr>
    </table>
    <asp:HiddenField ID="hdnEndorsementID" Value="-1" runat="server" />
</asp:Panel>
