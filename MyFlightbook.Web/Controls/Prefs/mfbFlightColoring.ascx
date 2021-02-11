<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbFlightColoring.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbFlightColoring" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<p>
    <asp:Localize ID="locFlightColoringDesc" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescription %>" />
    <asp:HyperLink ID="lnkLearnMore" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescriptionLearnMore %>" NavigateUrl="~/Public/FAQ.aspx?q=63#63" />
</p>
<div style="margin-left: 5px; margin-bottom: 10px">
    <asp:GridView ID="gvCanned" runat="server" ShowHeader="false" ShowFooter="false" GridLines="None" CellPadding="3" AutoGenerateColumns="false" OnRowCommand="gvCanned_RowCommand" OnRowDataBound="gvCanned_RowDataBound">
        <Columns>
            <asp:BoundField DataField="QueryName" ItemStyle-Font-Bold="true" />
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:HiddenField ID="hdnKey" runat="server" Value='<%#: Eval("QueryName") %>' />
                    <div><asp:Label ID="lblQSamp" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" /><asp:TextBox ID="txtQsamp" runat="server" style="visibility:hidden; width: 4px;" /></div>
                    <cc1:ColorPickerExtender ID="cpeQ" runat="server" TargetControlID="txtQsamp" PopupButtonID="lblQSamp" SampleControlID="lblQSamp" PaletteStyle="Continuous" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:LinkButton ID="lnkRemove" runat="server" Text="<%$ Resources:Preferences, FlightColoringNoColor %>" CommandName="_RemoveColor" CommandArgument='<%# Eval("QueryName") %>' />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p><asp:Localize ID="locNoQueries" runat="server" Text="<%$ Resources:Preferences, FlightColoringNoSaveQueries %>" /></p>
            <p>
                <asp:Panel ID="pnlNewQ" runat="server" DefaultButton="btnAddQuery">
                    <asp:Localize ID="locKeyHeader" runat="server" Text="<%$ Resources:Preferences, FlightColoringKeywordHeader %>" /> <asp:TextBox ID="txtKey1" runat="server" />
                    <cc1:TextBoxWatermarkExtender ID="tweKey1" runat="server" TargetControlID="txtKey1" WatermarkCssClass="watermark" WatermarkText="<%$ Resources:Preferences, FlightColoringKeywordPrompt %>" />
                    <asp:Button ID="btnAddQuery" runat="server" Text="<%$ Resources:Preferences, FlightColoringQuickAddQuery %>" OnClick="btnAddQuery_Click" />
                </asp:Panel>
            </p>
        </EmptyDataTemplate>
    </asp:GridView>
</div>
<asp:Button ID="btnUpdateColors" runat="server" Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" onclick="btnUpdateColors_Click" />
    <div><asp:Label ID="lblColorsUpdated" runat="server" CssClass="success" EnableViewState="False"
        Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False" /></div>
