<%@ Page Language="C#" AutoEventWireup="true" Codebehind="Default.aspx.cs" MasterPageFile="~/MasterPage.master" Inherits="PlayPen_Default" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    Playpen
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <p>Not everything makes the cut for functionality.  Sometimes there's a need for a quick-and-dirty tool, or a need for a testbed, or just a need for a place to try stuff out.</p>
    <p>The playpen is where these things live.  These tools are NOT ready for "prime time", they lack fit-and-finish, they often don't have good error reporting, and so forth.  But they may be useful or interesting.  So they are offered here AS-IS.</p>
    <ul>
        <li><asp:HyperLink ID="lnkChecklist" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/Checklist.aspx">Checklist</asp:HyperLink> - Create interactive checklists for your aircraft.</li>
        <li><asp:HyperLink ID="lnkDayNight" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/DayNight.aspx">Day/Night</asp:HyperLink> - See the angle of the sun (and thus sunset/night time) at various points around the world.</li>
        <li><asp:HyperLink ID="lnkICalConvert" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/iCalConvert.aspx">iCal Convert</asp:HyperLink> - Take a spreadsheet of appointments and create an iCal file (that you can load into Outlook).</li>
        <li><asp:HyperLink ID="lnkImgDebug" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/ImgDbg.aspx">Image Debugger</asp:HyperLink> - See metadata about images.</li>
        <li><asp:HyperLink ID="lnkMergeFlights" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/MergeFlights.aspx">Merge Flights</asp:HyperLink> - Merge multiple flights into one.  USE WITH CARE - THIS ACTS ON YOUR LOGBOOK DATA!!!!</li>
        <li><asp:HyperLink ID="lnkMergeTelemetry" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/MergeTelemetry.aspx">Merge Telemetry</asp:HyperLink> - Merge multiple telemetry files into one.</li>
        <li><asp:HyperLink ID="lnkImportFromTelemetry" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/BulkImportFromTelemetry.aspx">Bulk Create Flights from Telemetry</asp:HyperLink> - Load multiple telemetry files and create pending flights from each one.</li>
        <li><asp:HyperLink ID="lnkAnalyzeDutyPeriods" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/DutyPeriodAnalyzer.aspx">Duty Period Analyzer</asp:HyperLink> - view duty periods and do simple analysis.</li>
        <li><asp:HyperLink ID="lnkOAuthTest" runat="server" Font-Bold="true" NavigateUrl="~/PlayPen/oAuthClientTest.aspx">oAuth Client Tester</asp:HyperLink> - Tool for testing oAuth access to MyFlightbook; see the <asp:HyperLink ID="lnkDev" NavigateUrl="~/Public/Developer.aspx" runat="server">Developer</asp:HyperLink> page for more information.</li>
    </ul>
</asp:Content>
