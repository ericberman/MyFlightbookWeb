<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Checklist.aspx.cs" Inherits="PlayPen_Checklist" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/ChecklistControls/ChecklistItem.ascx" TagPrefix="uc1" TagName="ChecklistItem" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:Panel ID="pnlEditor" runat="server">
        <h2>Cheat sheet:</h2>
        <ul>
            <li>Tab xxx - The following sections goes in a tab with title xxx</li>
            <li>- XXX - Expandable/Collapsible header; subsequent items grouped in it</li>
            <li>-- XXX - Non-expandable header</li>
            <li>* Emphasize this line (boldface)</li>
            <li>** Really emphasize this line (e.g., for emergency)</li>
            <li>XXX - Plain text</li>
            <li>[] xxx...yyy: Checkbox with challenge x and optional response y</li>
            <li>[E+]([E-]) - Checkbox denoting engine start (stop)</li>
            <li>[B+]([B-]) - Checkbox denoting block out (in)</li>
        </ul>
        <div><asp:TextBox ID="txtChecklistSrc" runat="server" TextMode="MultiLine" Rows="20" Width="100%"></asp:TextBox></div>
        <div><asp:Button ID="btnLoadSample" runat="server" Text="Load Sample" OnClick="btnLoadSample_Click" /><asp:Button ID="btnPreview" runat="server" Text="Preview" OnClick="btnPreview_Click" /></div>
    </asp:Panel>
    <div>
        <uc1:ChecklistItem runat="server" id="ChecklistItem1" />
    </div>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

