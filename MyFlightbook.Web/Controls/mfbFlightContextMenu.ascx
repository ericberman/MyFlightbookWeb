<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbFlightContextMenu" Codebehind="mfbFlightContextMenu.ascx.cs" %>
<%@ Register Src="~/Controls/mfbTweetThis.ascx" TagPrefix="uc1" TagName="mfbTweetThis" %>
<%@ Register Src="~/Controls/mfbMiniFacebook.ascx" TagPrefix="uc1" TagName="mfbMiniFacebook" %>
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
<div style="line-height: 26px"><uc1:mfbMiniFacebook ID="mfbMiniFacebook" runat="server" /></div>
<div style="line-height: 26px"><uc1:mfbTweetThis ID="mfbTweetThis" runat="server" /></div>
<div style="line-height: 26px">
    <asp:LinkButton ID="lnkSendFlight" runat="server" OnClick="lnkSendFlight_Click">
        <asp:Image ID="imgSendFlight" style="padding-right:4px" ImageUrl="~/images/sendflight.png" runat="server" AlternateText="<%$ Resources:LogbookEntry, SendFlight %>" />
        <asp:Label ID="lblSendFlight" runat="server" Text="<%$ Resources:LogbookEntry, SendFlight %>"></asp:Label>
    </asp:LinkButton>
</div>
<asp:HiddenField ID="hdnID" runat="server" />
