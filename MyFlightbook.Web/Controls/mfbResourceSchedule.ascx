<%@ Control Language="C#" AutoEventWireup="true" Inherits="Controls_mfbResourceSchedule" Codebehind="mfbResourceSchedule.ascx.cs" %>
<div><asp:Label ID="lblResourceHeader" Font-Bold="true" runat="server" Text=""></asp:Label></div>
<div class="calContainer">
    <div class="calPlaceholder" id="divResourceDetails" runat="server">
        <asp:PlaceHolder ID="plcResource" runat="server"></asp:PlaceHolder>
    </div>
    <asp:Panel ID="pnlNavContainer" CssClass="calNavContainer" runat="server">
        <asp:Panel ID="pnlCalendarNav" Width="100%" runat="server"></asp:Panel>
        <asp:PlaceHolder ID="plcSubNav" runat="server"></asp:PlaceHolder>
    </asp:Panel>
    <div class="calCalContainer">
        <asp:Panel ID="pnlCalendar" runat="server"></asp:Panel>
    </div>
</div>
<asp:HiddenField ID="hdnResourceName" runat="server" />
<asp:HiddenField ID="hdnClubID" runat="server" />
<script>
    var cal = new mfbCalendar('<% =ResolveUrl("~/Member/Schedule.aspx") %>', '<% =ResourceID %>', '<% =ClubID %>', '<% =Mode %>', '<% =pnlCalendar.ClientID %>', newAppt, editAppt, getAppointment);
    <% if (String.IsNullOrEmpty(NavInitClientFunction))
           Response.Write(String.Format("cal.initNav('{0}').select(new DayPilot.Date('{1}'));", pnlCalendarNav.ClientID, NowUTCInClubTZ));
       else
           Response.Write(String.Format("{0}(cal);", NavInitClientFunction)); 
    %>
    cal.refreshEvents();
</script>
