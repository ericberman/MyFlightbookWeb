<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSendFlight" Codebehind="mfbSendFlight.ascx.cs" %>
<asp:Panel ID="pnlSendFlight" runat="server" Width="480px" DefaultButton="btnSendFlight" CssClass="modalpopup" style="display:none">
    <h2><asp:Label ID="lblSendPrompt" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightPrompt %>"></asp:Label></h2>
    <asp:HiddenField ID="hdnFlightToSend" runat="server" />
    <asp:HiddenField ID="hdnFlightSendToTarget" runat="server" Value="~/Member/LogbookNew.aspx" />
    <table>
        <tr>
            <td>
                <asp:Localize ID="locEmailRecipient" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightEmailPrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ValidationGroup="valSendFlight" ID="txtSendFlightEmail" Text="" runat="server"></asp:TextBox>
                <asp:RequiredFieldValidator ValidationGroup="valSendFlight" ID="RequiredFieldValidator2" runat="server" CssClass="error" ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailRequired %>" 
                    ControlToValidate="txtSendFlightEmail" SetFocusOnError="True" Display="Dynamic"></asp:RequiredFieldValidator>
                <asp:RegularExpressionValidator ValidationGroup="valSendFlight" ID="RegularExpressionValidator2" runat="server" CssClass="error"
                    ErrorMessage="<%$ Resources:LocalizedText, ValidationEmailFormat %>" 
                    ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*" 
                    ControlToValidate="txtSendFlightEmail" SetFocusOnError="True" Display="Dynamic"></asp:RegularExpressionValidator>
            </td>
        </tr>
        <tr>
            <td style="vertical-align:top">
                <asp:Localize ID="locSendFlightMessage" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightMessagePrompt %>"></asp:Localize>
            </td>
            <td>
                <asp:TextBox ValidationGroup="valSendFlight" ID="txtSendFlightMessage" TextMode="MultiLine" Rows="3" runat="server"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td></td>
            <td>
                <asp:FormView ID="fmvFlight" runat="server">
                    <ItemTemplate>
                        <table>
                            <tr style="vertical-align:top">
                                <td><asp:Label Font-Bold="true" ID="lblDates" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightDate %>"></asp:Label></td>
                                <td><%# ((DateTime) Eval("Date")).ToShortDateString() %></td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:Label Font-Bold="true" ID="lblAircraft" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightAircraft %>"></asp:Label></td>
                                <td><%#: Eval("TailNumDisplay") %></td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:Label Font-Bold="true" ID="lblRoute" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightRoute %>"></asp:Label></td>
                                <td><%#: Eval("Route") %></td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td><asp:Label Font-Bold="true" ID="lblComments" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightComments %>"></asp:Label></td>
                                <td><%#: Eval("Comment") %></td>
                            </tr>
                        </table>
                    </ItemTemplate>
                </asp:FormView>
            </td>
        </tr>
    </table>
    <div style="text-align:center">
        <asp:Button ValidationGroup="valSendFlight" ID="btnSendFlight" OnClick="btnSendFlight_Click" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightButton %>" /> <asp:Button ID="btnCancelSend" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightCancel %>" />
    </div>
</asp:Panel>
<asp:HyperLink ID="lnkPopSendFlight" runat="server" style="display:none"></asp:HyperLink>
<ajaxToolkit:ModalPopupExtender ID="modalPopupSendFlight" runat="server" 
    PopupControlID="pnlSendFlight" TargetControlID="lnkPopSendFlight"
    BackgroundCssClass="modalBackground"
    CancelControlID="btnCancelSend" BehaviorID="modalPopupSendFlight"
    Enabled="true">
</ajaxToolkit:ModalPopupExtender>

<script>
/* Handle escape to dismiss */
function pageLoad(sender, args) {
    if (!args.get_isPartialLoad()) {
        $addHandler(document, "keydown", onKeyDown);
    }
}

function onKeyDown(e) {
    if (e && e.keyCode == Sys.UI.Key.esc)
        $find("modalPopupSendFlight").hide();
}
</script>