<%@ Control Language="C#" AutoEventWireup="true" Codebehind="mfbFlightContextMenu.ascx.cs" Inherits="MyFlightbook.Controls.mfbFlightContextMenu" %>
<div style="line-height: 26px;">
    <asp:HyperLink ID="lnkEditThisFlight" runat="server">
        <asp:Image ID="imgPencil" runat="server" style="padding-right: 4px;" ImageUrl="~/images/pencilsm.png" />
        <asp:Label ID="lblEditFlight" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightEditThisFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px;">
    <asp:LinkButton ID="lnkDelete" OnClick="lnkDelete_Click" runat="server">
        <asp:Image ID="imgDelete" style="padding-right: 10px" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" runat="server" />
        <asp:Label ID="lblDeleteThis" runat="server" Text="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>"></asp:Label>
    </asp:LinkButton>
    <ajaxToolkit:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="lnkDelete" ConfirmOnFormSubmit="True">
    </ajaxToolkit:ConfirmButtonExtender>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkClone" runat="server">
        <asp:Image ID="imgClone" runat="server" style="padding-right:4px;" ImageUrl="~/images/copyflight.png" AlternateText="<%$ Resources:LogbookEntry, RepeatFlight %>" />
        <asp:Label ID="lblClone" runat="server" Text="<%$ Resources:LogbookEntry, RepeatFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkReverse" runat="server">
        <asp:Image ID="imgReverse" runat="server" style="padding-right:4px;" ImageUrl="~/images/copyflightreverse.png" AlternateText="<%$ Resources:LogbookEntry, RepeatReverseFlight %>" />
        <asp:Label ID="lblReverse" runat="server" Text="<%$ Resources:LogbookEntry, RepeatReverseFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkRequestSignature" runat="server">
        <asp:Image ID="imgSignature" runat="server" style="padding-right: 4px" ImageUrl="~/images/signaturesm.png" AlternateText="<%$ Resources:SignOff, RequestSignature %>" />
        <asp:Label ID="lblRequestSignature" runat="server" Text="<%$ Resources:SignOff, RequestSignature %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkCopyFlightLink" runat="server" ToolTip="<%$ Resources:LogbookEntry, CopyFlightLinkTip %>">
        <asp:Image ID="imgCopy" style="padding-right:4px" ImageUrl="~/images/copyflight.png" runat="server" AlternateText="<%$ Resources:LogbookEntry, CopyFlightLinkTip %>" />
        <asp:Label ID="lblCopyFlight" runat="server" Text="<%$ Resources:LogbookEntry, CopyFlightLink %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:LinkButton ID="lnkSendFlight" runat="server">
        <asp:Image ID="imgSendFlight" style="padding-right:4px" ImageUrl="~/images/sendflight.png" runat="server" AlternateText="<%$ Resources:LogbookEntry, SendFlight %>" />
        <asp:Label ID="lblSendFlight" runat="server" Text="<%$ Resources:LogbookEntry, SendFlight %>"></asp:Label>
    </asp:LinkButton>
</div>
<div style="line-height: 26px;">
    <asp:HyperLink ID="lnkCheck" runat="server">
        <asp:Image ID="iChk" runat="server" style="padding-right: 4px;" ImageUrl="~/images/CheckFlights.png" Width="20px" ImageAlign="AbsMiddle" />
        <asp:Label ID="lChk" runat="server" Text="<%$ Resources:FlightLint, TitleCheckThisFlightShort %>"></asp:Label>
    </asp:HyperLink>
</div>
<asp:Label ID="lblFlightCopied" runat="server" Text="<%$ Resources:LocalizedText, CopiedToClipboard %>" CssClass="hintPopup" style="display:none; font-weight:bold; font-size: 10pt; color:black; " />
<asp:HiddenField ID="hdnCopyLink" runat="server" />
<asp:HiddenField ID="hdnID" runat="server" />
