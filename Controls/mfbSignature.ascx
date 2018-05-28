<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbSignature.ascx.cs" Inherits="Controls_mfbSignature" %>
<asp:Repeater ID="rptSignature" runat="server">
    <ItemTemplate>
        <asp:Panel ID="pnlSignature" runat="server" Visible='<%# ((LogbookEntry.SignatureState) Eval("CFISignatureState")) != LogbookEntry.SignatureState.None %>'>
            <table style="border-collapse:collapse;">
                <tr>
                    <td style="border:none; vertical-align:middle;">
                        <asp:Image ID="imgSigState" ToolTip='<%# Eval("SignatureStateDescription") %>' AlternateText='<%# Eval("SignatureStateDescription") %>' ImageUrl='<%# ((bool) Eval("HasValidSig")) ? "~/Images/sigok.png" : "~/Images/siginvalid.png" %>' ImageAlign="Middle" runat="server" />
                    </td>
                    <td style="border:none;">
                        <asp:Panel ID="lblSigData" runat="server" CssClass='<%# ((bool) Eval("HasValidSig")) ? "signatureValid" : "signatureInvalid" %>'>
                            <div><%#: ((string) Eval("SignatureMainLine")) %></div>
                            <div><%#: Eval("SignatureCommentLine") %></div>
                        </asp:Panel>
                        <asp:Panel ID="pnlInvalidSig" runat="server" Visible='<%# !(bool) Eval("HasValidSig") %>'>
                            <asp:Label ID="lblSigInvalid" runat="server" CssClass="boldface signatureInvalid" Text="<%$ Resources:SignOff, FlightSignatureInvalid %>"></asp:Label>
                        </asp:Panel>
                    </td>
                    <td style="border:none;">
                        <asp:Panel ID="pnlSigState" runat="server" Visible='<%# Convert.ToBoolean(Eval("HasDigitizedSig")) %>'>
                            <asp:Image ID="imgDigitizedSig" runat="server" Width="140px" ImageUrl='<%# String.Format("~/Public/ViewSig.aspx?id={0}", Eval("FlightID")) %>' />
                        </asp:Panel>
                    </td>
                </tr>
            </table>
        </asp:Panel>
    </ItemTemplate>
</asp:Repeater>
