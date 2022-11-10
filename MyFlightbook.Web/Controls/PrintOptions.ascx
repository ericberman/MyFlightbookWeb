<%@ Control Language="C#" AutoEventWireup="true" Codebehind="PrintOptions.ascx.cs" Inherits="MyFlightbook.Printing.PrintOptions" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>


<div>
    <p><asp:Label ID="lblLayoutPrompt" runat="server" Font-Bold="true" Text="<%$ Resources:LogbookEntry, PrintFormatPrompt %>"></asp:Label></p>
    <asp:DropDownList ID="cmbLayout" runat="server" AutoPostBack="True" OnSelectedIndexChanged="cmbLayout_SelectedIndexChanged">
        <asp:ListItem Value="Native" Selected="True" Text="<%$ Resources:LogbookEntry, PrintFormatNative %>"></asp:ListItem>
        <asp:ListItem Value="Portrait" Text="<%$ Resources:LogbookEntry, PrintFormatPortrait %>"></asp:ListItem>
        <asp:ListItem Value="EASA" Text="<%$ Resources:LogbookEntry, PrintFormatEASA %>"></asp:ListItem>
        <asp:ListItem Value="USA" Text="<%$ Resources:LogbookEntry, PrintFormatUSA %>"></asp:ListItem>
        <asp:ListItem Value="Canada" Text="<%$ Resources:LogbookEntry, PrintFormatCanada %>"></asp:ListItem>
        <asp:ListItem Value="SACAA" Text="<%$ Resources:LogbookEntry, PrintFormatSACAA %>"></asp:ListItem>
        <asp:ListItem Value="CASA" Text="<%$ Resources:LogbookEntry, PrintFormatCASA %>"></asp:ListItem>
        <asp:ListItem Value="PCAA" Text="<%$ Resources:LogbookEntry, PrintFormatPCAA %>"></asp:ListItem>
        <asp:ListItem Value="NZ" Text="<%$ Resources:LogbookEntry, PrintFormatNZ %>"></asp:ListItem>
        <asp:ListItem Value="Glider" Text="<%$ Resources:LogbookEntry, PrintFormatGlider %>"></asp:ListItem>
        <asp:ListItem Value="Condensed" Text="<%$ Resources:LogbookEntry, PrintFormatCondensed %>"></asp:ListItem>
        <asp:ListItem Value="UASCivi" Text="<%$ Resources:LogbookEntry, PrintFormatUASCivi %>"></asp:ListItem>
        <asp:ListItem Value="TwoPage" Text="<%$ Resources:LogbookEntry, PrintFormat2Page %>"></asp:ListItem>
        <asp:ListItem Value="Navy" Text="<%$ Resources:LogbookEntry, PrintFormatNavy %>"></asp:ListItem>
    </asp:DropDownList>
    <asp:Panel ID="pnlIncludeImages" runat="server">
        <asp:CheckBox ID="ckIncludeImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeImages %>" AutoPostBack="True" OnCheckedChanged="ckIncludeImages_CheckedChanged" />
    </asp:Panel>
    <div><asp:CheckBox ID="ckIncludeSignatures" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeSignatures %>" Checked="true" AutoPostBack="true" OnCheckedChanged="ckIncludeSignatures_CheckedChanged" /></div>
    <div>
        <asp:CheckBox ID="ckFlightColoring" runat="server" Text="<%$ Resources:LocalizedText, PrintViewFlightColoring %>" AutoPostBack="true" OnCheckedChanged="ckFlightColoring_CheckedChanged" /><uc1:mfbTooltip runat="server" ID="ttFlightCol" BodyContent="<%$ Resources:LocalizedText, PrintViewFlightColoringTip %>" />
    </div>
    <div>
        <asp:RadioButton ID="rbSmallFont" runat="server" Text="<%$ Resources:LocalizedText, PrintFontSizeSmaller %>" Font-Size="7pt" GroupName="rbFontSize" AutoPostBack="true" OnCheckedChanged="rbFont_CheckedChanged" />
        <asp:RadioButton ID="rbMediumFont" runat="server" Text="<%$ Resources:LocalizedText, PrintFontSizeNormal %>" GroupName="rbFontSize" AutoPostBack="true" OnCheckedChanged="rbFont_CheckedChanged" />
        <asp:RadioButton ID="rbLargeFont" runat="server" Text="<%$ Resources:LocalizedText, PrintFontSizeLarge %>" Font-Size="11pt" GroupName="rbFontSize" AutoPostBack="true" OnCheckedChanged="rbFont_CheckedChanged" />
    </div>
</div>
<asp:Panel ID="pnlFlightsPerPage" runat="server">
    <p><asp:Label ID="lblFlightsPerPage" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, PrintViewFlightsPerPage %>"></asp:Label>
        <uc1:mfbTooltip runat="server" id="mfbTooltip">
            <TooltipBody>
                <asp:Localize ID="locApproximatePageDesc" runat="server" Text="<%$ Resources:LocalizedText, PrintOptionsEntriesPerPageDescription %>"></asp:Localize>
            </TooltipBody>
        </uc1:mfbTooltip>
    </p>
    <asp:DropDownList ID="cmbFlightsPerPage" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbFlightsPerPage_SelectedIndexChanged">
        <asp:ListItem Value="-1" Text="<%$ Resources:LocalizedText, PrintViewAsFit %>" Selected="True"></asp:ListItem>
    </asp:DropDownList>
    <div><asp:CheckBox ID="ckPullForwardTotals" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludePreviousPageTotals %>" Checked="true" AutoPostBack="true" OnCheckedChanged="ckPullForwardTotals_CheckedChanged" /></div>
    <div><asp:CheckBox ID="ckSeparateCategoryClass" runat="server" Text="<%$ Resources:LocalizedText, PrintViewSubtotalsByCategoryClass %>" Checked="true" AutoPostBack="true" OnCheckedChanged="ckSeparateCategoryClass_CheckedChanged" /></div>
    <asp:Panel runat="server" ID="pnlForceBreak"><asp:Label ID="lblForceBreak" runat="server" Text="<%$ Resources:LocalizedText, PrintViewBreakPeriodPrompt %>" />
        <asp:RadioButtonList ID="rblForceBreak" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rblForceBreak_SelectedIndexChanged" RepeatLayout="Flow" RepeatDirection="Horizontal">
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewBreakNone %>" Value="0" Selected="True" />
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewBreakMonth %>" Value="1" />
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewBreakYear %>" Value="2" />
        </asp:RadioButtonList>
    </asp:Panel>
</asp:Panel>
<asp:Panel ID="pnlModelDisplay" runat="server">
    <p><asp:Label ID="lblModelDisplay" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, PrintViewModelDisplay %>"></asp:Label></p>
    <asp:RadioButtonList ID="rblModelDisplay" runat="server" AutoPostBack="true" OnSelectedIndexChanged="rblModelDisplay_SelectedIndexChanged">
        <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewModelDisplayFull %>" Value="Full" Selected="True"></asp:ListItem>
        <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewModelDisplayShort %>" Value="Short"></asp:ListItem>
        <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewModelDisplayICAO %>" Value="ICAO"></asp:ListItem>
    </asp:RadioButtonList>
</asp:Panel>
<asp:Panel ID="pnlOptionalColumns" runat="server">
    <p><asp:Label ID="lblOptionalColumnsHeader" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, PrintViewOptionalColumns %>"></asp:Label></p>
    <div style="display:inline-block">
        <p><asp:DropDownList ID="cmbOptionalColumn1" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn1_SelectedIndexChanged"></asp:DropDownList></p> 
        <p><asp:DropDownList ID="cmbOptionalColumn2" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn2_SelectedIndexChanged"></asp:DropDownList></p>
    </div>
    <div style="display:inline-block">
        <p><asp:DropDownList ID="cmbOptionalColumn3" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn3_SelectedIndexChanged"></asp:DropDownList></p>
        <p><asp:DropDownList ID="cmbOptionalColumn4" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn4_SelectedIndexChanged"></asp:DropDownList></p>
    </div>
</asp:Panel>
<asp:Panel ID="pnlProperties" runat="server" style="padding:3px">
    <p>
        <asp:Label ID="lblPropSeparator" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, PrintViewPropertySeparator %>"></asp:Label>
    </p>
    <div>
        <asp:RadioButtonList ID="rblPropertySeparator" runat="server" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="rblPropertySeparator_SelectedIndexChanged">
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorSpace %>" Selected="True" Value="Space"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorComma %>" Value="Comma"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorSemicolon %>" Value="Semicolon"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorNewline %>" Value="Newline"></asp:ListItem>
        </asp:RadioButtonList>
    </div>
    <div>&nbsp;</div>
    <uc1:Expando runat="server" ID="expPropertiesToExclude">
        <Header>
            <asp:Label ID="lblPropertyExclusion" runat="server" Font-Bold="true" Text="<%$ Resources:LocalizedText, PrintViewPropertyInclusion %>" />
        </Header>
        <Body>
            <asp:CheckBoxList ID="cklProperties" runat="server" DataTextField="Title" DataValueField="PropTypeID" RepeatColumns="2" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="cklProperties_SelectedIndexChanged" />
            <table>
                <tr>
                    <td><asp:CheckBox ID="ckCheckAll" runat="server" AutoPostBack="true" OnCheckedChanged="ckCheckAll_CheckedChanged" Text="<%$ Resources:LocalizedText, SelectAll %>" /></td>
                </tr>
            </table>
        </Body>
    </uc1:Expando>
</asp:Panel>

<div>&nbsp;</div>
