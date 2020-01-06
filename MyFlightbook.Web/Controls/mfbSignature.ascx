<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbSignature" Codebehind="mfbSignature.ascx.cs" %>
<asp:Repeater ID="rptSignature" runat="server">
    <ItemTemplate>
        <asp:Panel ID="pnlSignature" runat="server" CssClass="signatureBlock" Visible='<%# ((LogbookEntry.SignatureState) Eval("CFISignatureState")) != LogbookEntry.SignatureState.None %>'>
            <div style="display:inline-block; vertical-align:middle;">
                <table>
                    <tr>
                        <td style="vertical-align:middle">
                            <asp:Image ID="imgSigState" ToolTip='<%# Eval("SignatureStateDescription") %>' AlternateText='<%# Eval("SignatureStateDescription") %>' ImageUrl='<%# ((bool) Eval("HasValidSig")) ? "~/Images/sigok.png" : "~/Images/siginvalid.png" %>' ImageAlign="Middle" runat="server" />
                        </td>
                        <td style="vertical-align:middle">
                            <asp:Panel ID="lblSigData" runat="server" CssClass='<%# ((bool) Eval("HasValidSig")) ? "signatureValid" : "signatureInvalid" %>'>
                                <div><%#: ((string) Eval("SignatureMainLine")) %></div>
                                <div><%#: Eval("SignatureCommentLine") %></div>
                            </asp:Panel>
                            <asp:Panel ID="pnlInvalidSig" runat="server" Visible='<%# !(bool) Eval("HasValidSig") %>'>
                                <asp:Label ID="lblSigInvalid" runat="server" Font-Bold="true" CssClass="signatureInvalid" Text="<%$ Resources:SignOff, FlightSignatureInvalid %>"></asp:Label>
                            </asp:Panel>
                        </td>
                    </tr>
                </table>
            </div>
            <div class="signatureBlockSig">
                <asp:Panel ID="pnlSigState" runat="server" Visible='<%# Convert.ToBoolean(Eval("HasDigitizedSig")) %>'>
                    <asp:Image ID="imgDigitizedSig" runat="server" Width="140px" ImageUrl='<%# String.Format("~/Public/ViewSig.aspx?id={0}", Eval("FlightID")) %>' />
                </asp:Panel>
            </div>
        </asp:Panel>
    </ItemTemplate>
</asp:Repeater>
