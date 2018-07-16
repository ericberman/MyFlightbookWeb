<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PrintOptions.ascx.cs" Inherits="Controls_PrintOptions" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<div><% =Resources.LogbookEntry.PrintFormatPrompt %> 
    <asp:DropDownList ID="cmbLayout" runat="server" AutoPostBack="True" OnSelectedIndexChanged="cmbLayout_SelectedIndexChanged">
        <asp:ListItem Value="Native" Selected="True" Text="<%$ Resources:LogbookEntry, PrintFormatNative %>"></asp:ListItem>
        <asp:ListItem Value="EASA" Text="<%$ Resources:LogbookEntry, PrintFormatEASA %>"></asp:ListItem>
        <asp:ListItem Value="USA" Text="<%$ Resources:LogbookEntry, PrintFormatUSA %>"></asp:ListItem>
        <asp:ListItem Value="SACAA" Text="<%$ Resources:LogbookEntry, PrintFormatSACAA %>"></asp:ListItem>
        <asp:ListItem Value="NZ" Text="<%$ Resources:LogbookEntry, PrintFormatNZ %>"></asp:ListItem>
        <asp:ListItem Value="Glider" Text="<%$ Resources:LogbookEntry, PrintFormatGlider %>"></asp:ListItem>
    </asp:DropDownList>
    <asp:Label ID="pnlIncludeImages" runat="server" style="padding:3px">
        <asp:CheckBox ID="ckIncludeImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeImages %>" AutoPostBack="True" OnCheckedChanged="ckIncludeImages_CheckedChanged" />
    </asp:Label>
</div>
<asp:Panel ID="pnlFlightsPerPage" runat="server">
    <br />
    <asp:Label ID="lblFlightsPerPage" runat="server" Text="<%$ Resources:LocalizedText, PrintViewFlightsPerPage %>"></asp:Label>
    <asp:DropDownList ID="cmbFlightsPerPage" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbFlightsPerPage_SelectedIndexChanged">
        <asp:ListItem Value="-1" Text="<%$ Resources:LocalizedText, PrintViewAsFit %>"></asp:ListItem>
    </asp:DropDownList>
</asp:Panel>
<asp:Panel ID="pnlOptionalColumns" runat="server">
    <p><asp:Localize ID="locOptionalColumnsHeader" runat="server" Text="<%$ Resources:LocalizedText, PrintViewOptionalColumns %>"></asp:Localize></p>
    <p><asp:DropDownList ID="cmbOptionalColumn1" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn1_SelectedIndexChanged"></asp:DropDownList></p> 
    <p><asp:DropDownList ID="cmbOptionalColumn2" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn2_SelectedIndexChanged"></asp:DropDownList></p>
    <p><asp:DropDownList ID="cmbOptionalColumn3" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn3_SelectedIndexChanged"></asp:DropDownList></p>
    <p><asp:DropDownList ID="cmbOptionalColumn4" runat="server" AutoPostBack="true" DataTextField="Text" DataValueField="Value" OnSelectedIndexChanged="cmbOptionalColumn4_SelectedIndexChanged"></asp:DropDownList></p>
</asp:Panel>
<asp:Panel ID="pnlProperties" runat="server" style="padding:3px">
    <br />
    <div>
        <asp:Label ID="lblPropSeparator" runat="server" Text="<%$ Resources:LocalizedText, PrintViewPropertySeparator %>"></asp:Label>
        <asp:RadioButtonList ID="rblPropertySeparator" runat="server" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="rblPropertySeparator_SelectedIndexChanged">
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorSpace %>" Selected="True" Value="Space"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorComma %>" Value="Comma"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorSemicolon %>" Value="Semicolon"></asp:ListItem>
            <asp:ListItem Text="<%$ Resources:LocalizedText, PrintViewPropertySeparatorNewline %>" Value="Newline"></asp:ListItem>
        </asp:RadioButtonList>
    </div>
    <p>
        <uc1:Expando runat="server" ID="expPropertiesToExclude">
            <Header>
                <asp:Label ID="lblPropertyExclusion" runat="server" Text="<%$ Resources:LocalizedText, PrintViewPropertyInclusion %>"></asp:Label>
            </Header>
            <Body>
                <p></p>
                <asp:CheckBoxList ID="cklProperties" runat="server" DataTextField="Title" DataValueField="PropTypeID" RepeatColumns="2" RepeatDirection="Horizontal" AutoPostBack="true" OnSelectedIndexChanged="cklProperties_SelectedIndexChanged" />
            </Body>
        </uc1:Expando>
    </p>
</asp:Panel>

<div>&nbsp;</div>
