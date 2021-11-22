<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="mfbFlightColoring.ascx.cs" Inherits="MyFlightbook.Web.Controls.Prefs.mfbFlightColoring" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<p>
    <asp:Localize ID="locFlightColoringDesc" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescription %>" />
    <asp:HyperLink ID="lnkLearnMore" runat="server" Text="<%$ Resources:Preferences, FlightColoringDescriptionLearnMore %>" NavigateUrl="https://myflightbookblog.blogspot.com/2021/03/saved-searches-and-flight-coloring.html" Target="_blank" />
</p>
<div style="margin-left: 5px; margin-bottom: 10px">
    <asp:GridView ID="gvCanned" runat="server" ShowHeader="false" ShowFooter="false" GridLines="None" CellPadding="3" AutoGenerateColumns="false" OnRowCommand="gvCanned_RowCommand" OnRowDataBound="gvCanned_RowDataBound">
        <Columns>
            <asp:TemplateField>
                <ItemTemplate>
                    <div><asp:Label ID="lblName" runat="server" Text='<%#: Eval("QueryName") %>' Font-Bold="true" /></div>
                    <div><asp:Label ID="lblDesc" runat="server" Text='<%#: Eval("Description") %>' CssClass="fineprint" /></div>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <div><asp:Label ID="lblQSamp" runat="server" style="padding: 3px" Text="<%$ Resources:Preferences, FlightColoringSample %>" /><asp:TextBox ID="txtQsamp" runat="server" style="visibility:hidden; width: 4px;" /></div>
                    <cc1:ColorPickerExtender ID="cpeQ" runat="server" TargetControlID="txtQsamp" PopupButtonID="lblQSamp" SampleControlID="lblQSamp" PaletteStyle="Continuous" />
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField>
                <ItemTemplate>
                    <asp:LinkButton ID="lnkRemove" runat="server" Text="<%$ Resources:Preferences, FlightColoringNoColor %>" CommandName="_RemoveColor" CommandArgument='<%# Eval("QueryName") %>' />
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p>
                <asp:Localize ID="locNoQueries" runat="server" Text="<%$ Resources:Preferences, FlightColoringNoSaveQueries %>" />
                <asp:HyperLink ID="lnkLearnMore" NavigateUrl="https://myflightbookblog.blogspot.com/2021/03/saved-searches-and-flight-coloring.html" runat="server" Text="<%$ Resources:Preferences, FlightColoringLearnMore %>" Target="_blank" />
            </p>
        </EmptyDataTemplate>
    </asp:GridView>
</div>
<asp:Button ID="btnUpdateColors" runat="server" Text="<%$ Resources:LocalizedText, profileUpdatePreferences %>" onclick="btnUpdateColors_Click" />
    <div><asp:Label ID="lblColorsUpdated" runat="server" CssClass="success" EnableViewState="False"
        Text="<%$ Resources:LocalizedText, profilePreferencesUpdated %>" Visible="False" /></div>
