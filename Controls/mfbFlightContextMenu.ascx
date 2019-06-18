<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbFlightContextMenu.ascx.cs" Inherits="Controls_mfbFlightContextMenu" %>
<%@ Register Src="~/Controls/mfbTweetThis.ascx" TagPrefix="uc1" TagName="mfbTweetThis" %>
<%@ Register Src="~/Controls/mfbMiniFacebook.ascx" TagPrefix="uc1" TagName="mfbMiniFacebook" %>
<div style="line-height: 26px;">
    <asp:HyperLink ID="lnkEditThisFlight" runat="server" OnClick="lnkEditThisFlight_Click">
        <asp:Image ID="imgPencil" runat="server" style="padding-right: 4px;" ImageUrl="~/images/pencilsm.png" />
        <asp:Label ID="lblEditFlight" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightEditThisFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px;">
    <asp:LinkButton ID="lnkDelete" OnClick="lnkDelete_Click" runat="server">
        <asp:Image ID="imgDelete" style="padding-right: 10px" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" runat="server" />
        <asp:Label ID="lblDeleteThis" runat="server" Text="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>"></asp:Label>
    </asp:LinkButton>
    <ajaxToolkit:ConfirmButtonExtender ID="ConfirmButtonExtender1" runat="server" TargetControlID="lnkDelete" ConfirmOnFormSubmit="True" ConfirmText="<%$ Resources:LogbookEntry, LogbookConfirmDelete %>">
    </ajaxToolkit:ConfirmButtonExtender>
</div>
<div style="line-height: 26px"><uc1:mfbMiniFacebook ID="mfbMiniFacebook" runat="server" /></div>
<div style="line-height: 26px"><uc1:mfbTweetThis ID="mfbTweetThis" runat="server" /></div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkRequestSignature" runat="server" OnClick="lnkRequestSignature_Click">
        <asp:Image ID="imgSignature" runat="server" style="padding-right: 4px" ImageUrl="~/images/signaturesm.png" />
        <asp:Label ID="lblRequestSignature" runat="server" Text="<%$ Resources:SignOff, RequestSignature %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkClone" runat="server" OnClick="lnkClone_Click">
        <asp:Image ID="imgClone" runat="server" style="padding-right:4px;" ImageUrl="~/images/copyflight.png" />
        <asp:Label ID="lblClone" runat="server" Text="<%$ Resources:LogbookEntry, RepeatFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:HyperLink ID="lnkReverse" runat="server" OnClick="lnkReverse_Click">
        <asp:Image ID="imgReverse" runat="server" style="padding-right:4px;" ImageUrl="~/images/copyflightreverse.png" />
        <asp:Label ID="lblReverse" runat="server" Text="<%$ Resources:LogbookEntry, RepeatReverseFlight %>"></asp:Label>
    </asp:HyperLink>
</div>
<div style="line-height: 26px">
    <asp:LinkButton ID="lnkSendFlight" runat="server" OnClick="lnkSendFlight_Click">
        <asp:Image ID="imgSendFlight" style="padding-right:4px" ImageUrl="~/images/sendflight.png" runat="server" />
        <asp:Label ID="lblSendFlight" runat="server" Text="<%$ Resources:LogbookEntry, SendFlight %>"></asp:Label>
    </asp:LinkButton>
</div>
<asp:HiddenField ID="hdnID" runat="server" />
