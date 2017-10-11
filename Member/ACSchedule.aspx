<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeFile="ACSchedule.aspx.cs" Inherits="Member_ACSchedule" culture="auto" meta:resourcekey="PageResource1" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbEditAppt.ascx" tagname="mfbEditAppt" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbResourceSchedule.ascx" tagname="mfbResourceSchedule" tagprefix="uc2" %>
<%@ Register src="../Controls/ClubControls/SchedSummary.ascx" tagname="SchedSummary" tagprefix="uc3" %>
<%@ Register src="../Controls/ClubControls/TimeZone.ascx" tagname="TimeZone" tagprefix="uc4" %>
<%@ Register src="../Controls/popmenu.ascx" tagname="popmenu" tagprefix="uc5" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" Runat="Server">
    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;    <script type="text/javascript" src='<%= ResolveUrl("~/public/daypilot-all.min.js") %>'></script>
    <script type="text/javascript" src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script type="text/javascript" src='<%= ResolveUrl("~/public/jquery.json-2.4.min.js") %>'></script>
    <script type="text/javascript" src='<%= ResolveUrl("~/public/mfbcalendar.js") %>'></script>
    <uc1:mfbEditAppt ID="mfbEditAppt1" runat="server" />
    <asp:MultiView ID="mvStatus" runat="server">
        <asp:View ID="vwNoClub" runat="server">
            <p><asp:Label ID="lblTailNumber3" runat="server" Font-Bold="True" meta:resourcekey="lblTailNumber3Resource1"></asp:Label> - <asp:Localize ID="locNoClub" runat="server" Text="This aircraft is not in a club." meta:resourcekey="locNoClubResource1"></asp:Localize></p>
            <p><asp:HyperLink ID="lnkCreateClub" runat="server" Text="Create a club" NavigateUrl="~/Public/Clubs.aspx?noredir=1" meta:resourcekey="lnkCreateClubResource1"></asp:HyperLink></p>
        </asp:View>
        <asp:View ID="vwNotMember" runat="server">
            <p><asp:Label ID="lblTailNumber2" runat="server" Font-Bold="True" meta:resourcekey="lblTailNumber2Resource1"></asp:Label> - <asp:Localize ID="locNotMember" runat="server" Text="This aircraft belongs to the club(s) listed below, but you are not a member.  View details for the club to request membership." meta:resourcekey="locNotMemberResource1"></asp:Localize></p>
            <asp:Repeater ID="rptClubsForAircraft" runat="server">
                <ItemTemplate>
                    <h3><asp:HyperLink ID="lnkClub" NavigateUrl='<%# String.Format("~/Member/ClubDetails.aspx/{0}", Eval("ID")) %>' runat="server"><%# Eval("Name") %></asp:HyperLink></h3>
                </ItemTemplate>
            </asp:Repeater>
        </asp:View>
        <asp:View ID="vwMember" runat="server">
            <script type="text/javascript">
                var clubCalendars = [];
                var clubNavControl;

                function updateDate(d)
                {
                    // create a new date that is local but looks like this one.
                    var d2 = new Date(d.getTime() + new Date().getTimezoneOffset() * 60000);
                    document.getElementById('<% = lblDate.ClientID %>').innerHTML = d2.toDateString();
                }

                function refreshAllCalendars(args) {
                    for (i = 0; i < clubCalendars.length; i++) {
                        var cc = clubCalendars[i];
                        cc.dpCalendar.startDate = args.day;
                        cc.dpCalendar.update();
                        updateDate(args.day.d);
                        $find('mpeChooseDateBehavior').hide();
                        cc.refreshEvents(); 
                    }
                }

                function InitClubNav(cal) {
                    clubCalendars[clubCalendars.length] = cal;
                    if (typeof clubNavControl == 'undefined') {
                        clubNavControl = cal.initNav('<% =pnlDateSelector.ClientID %>');
                        clubNavControl.cellHeight = clubNavControl.cellWidth = 40;
                        clubNavControl.onTimeRangeSelected = refreshAllCalendars;   // override the default function
                        clubNavControl.select(new DayPilot.Date());
                        var dt = new DayPilot.Date();
                        if (typeof dt.d === "undefined")
                            updateDate(dt);
                        else
                            updateDate(dt.d);
                    }
                }
            </script>
            <asp:Panel ID="pnlChangeDatePop" Width="100%" runat="server" meta:resourcekey="pnlChangeDatePopResource1">
                <div style="padding:2px; width:100%; background-color:lightgray; line-height: 30px; height:30px; z-index:100; position:fixed; top: 0px; text-align:center;">
                    <asp:Label ID="lblTailNumber" Font-Bold="True" runat="server" meta:resourcekey="lblTailNumberResource1"></asp:Label> - 
                    <asp:Image ID="imgCalendar" runat="server" ImageUrl="~/images/CalendarPopup.png" style="display:inline-block; vertical-align:middle;" meta:resourcekey="imgCalendarResource1" />
                    <asp:Label ID="lblDate" runat="server" Font-Bold="True" style="vertical-align:middle; display:inline-block; line-height:normal;" meta:resourcekey="lblDateResource1"></asp:Label>
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlChangeDate" runat="server" HorizontalAlign="Center" style="padding:15px; display:none;" BackColor="White" meta:resourcekey="pnlChangeDateResource1">
                <span style="text-size-adjust: 125%">
                    <asp:Panel ID="pnlDateSelector" runat="server" meta:resourcekey="pnlDateSelectorResource1"></asp:Panel>
                </span>
                <br />
                <asp:Button ID="btnClose" runat="server" Text="Close" meta:resourcekey="btnCloseResource1" />
            </asp:Panel>
            <cc1:ModalPopupExtender ID="mpeChooseDate" runat="server" TargetControlID="pnlChangeDatePop" BackgroundCssClass="modalBackground" DropShadow="True" BehaviorID="mpeChooseDateBehavior" CancelControlID="btnClose" OkControlID="btnClose" PopupControlID="pnlChangeDate" DynamicServicePath="" >
            </cc1:ModalPopupExtender>
            <div style="margin-top: 50px;">
                <asp:Repeater ID="rptSchedules" runat="server" OnItemDataBound="rptSchedules_ItemDataBound">
                    <ItemTemplate>
                        <div style="clear:both;">
                            <h3><asp:HyperLink ID="lnkClub" NavigateUrl='<%# String.Format("~/Member/ClubDetails.aspx/{0}", Eval("ID")) %>' runat="server"><%# Eval("Name") %></asp:HyperLink></h3>
                            <uc2:mfbResourceSchedule ID="schedAircraft" NavInitClientFunction="InitClubNav" HideNavContainer="true" ShowResourceDetails="false" ClubID='<%# Eval("ID") %>' ResourceID="<%# AircraftID.ToString() %>" runat="server">
                            </uc2:mfbResourceSchedule>
                            <asp:Label ID="lblUpcoming" runat="server" Text="Upcoming scheduled items:" meta:resourcekey="lblUpcomingResource1"></asp:Label>
                            <uc3:SchedSummary ID="schedSummary" runat="server" UserName="<%# Page.User.Identity.Name %>" ResourceName="<%# AircraftID.ToString() %>" ClubID='<%# Eval("ID") %>' />
                            <br />
                            <br />
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </asp:View>
    </asp:MultiView>
</asp:Content>
