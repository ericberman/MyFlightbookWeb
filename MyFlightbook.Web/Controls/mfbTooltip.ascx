<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbTooltip.ascx.cs" Inherits="Controls_mfbTooltip" %>
<asp:Label ID="lblTip" runat="server" Text="[?]" CssClass="hint" />
<script type="text/javascript">
    $(function () {
        $("<% =HoverControlSelector %>").tooltip({
            items: "<% =HoverControlSelector %>, .hint",
            classes: { "ui-tooltip" : "hintPopup itemlabel" },
            content: function () {
                return $("#" + "<% =pnlTip.ClientID %>").html();
            }
        });
    });
</script>
<div style="display:none">
    <asp:Panel ID="pnlTip" runat="server" CssClass="hintPopup">
        <asp:Literal ID="litPop" runat="server"></asp:Literal>
        <asp:PlaceHolder ID="plcCustom" runat="server"></asp:PlaceHolder>
    </asp:Panel>
</div>
