<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PrintOptions.ascx.cs" Inherits="Controls_PrintOptions" %>
<div><% =Resources.LogbookEntry.PrintFormatPrompt %> 
    <asp:DropDownList ID="cmbLayout" runat="server" AutoPostBack="True" OnSelectedIndexChanged="cmbLayout_SelectedIndexChanged">
        <asp:ListItem Value="Native" Selected="True" Text="<%$ Resources:LogbookEntry, PrintFormatNative %>"></asp:ListItem>
        <asp:ListItem Value="EASA" Text="<%$ Resources:LogbookEntry, PrintFormatEASA %>"></asp:ListItem>
        <asp:ListItem Value="USA" Text="<%$ Resources:LogbookEntry, PrintFormatUSA %>"></asp:ListItem>
        <asp:ListItem Value="SACAA" Text="<%$ Resources:LogbookEntry, PrintFormatSACAA %>"></asp:ListItem>
        <asp:ListItem Value="Glider" Text="<%$ Resources:LogbookEntry, PrintFormatGlider %>"></asp:ListItem>
    </asp:DropDownList>
</div>
<asp:Panel ID="pnlIncludeImages" runat="server" style="padding:3px">
    <div><asp:CheckBox ID="ckIncludeImages" runat="server" Text="<%$ Resources:LocalizedText, PrintViewIncludeImages %>" AutoPostBack="True" OnCheckedChanged="ckIncludeImages_CheckedChanged" /></div>
</asp:Panel>
<asp:Panel ID="pnlFlightsPerPage" runat="server" style="padding:3px">
<asp:Label ID="lblFlightsPerPage" runat="server" Text="<%$ Resources:LocalizedText, PrintViewFlightsPerPage %>"></asp:Label>
    <asp:DropDownList ID="cmbFlightsPerPage" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbFlightsPerPage_SelectedIndexChanged">
        <asp:ListItem Value="-1" Text="<%$ Resources:LocalizedText, PrintViewAsFit %>"></asp:ListItem>
    </asp:DropDownList>
</asp:Panel>
