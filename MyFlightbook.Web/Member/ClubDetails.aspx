<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="ClubDetails.aspx.cs" Inherits="MyFlightbook.Clubs.ClubDetails" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="../Controls/ClubControls/ViewClub.ascx" tagname="ViewClub" tagprefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbResourceSchedule.ascx" tagname="mfbResourceSchedule" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbEditAppt.ascx" tagname="mfbEditAppt" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc4" %>
<%@ Register src="../Controls/mfbHtmlEdit.ascx" tagname="mfbHtmlEdit" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc6" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register src="../Controls/ClubControls/SchedSummary.ascx" tagname="SchedSummary" tagprefix="uc7" %>
<%@ Register src="../Controls/ClubControls/ClubAircraftSchedule.ascx" tagname="ClubAircraftSchedule" tagprefix="uc8" %>
<%@ Register src="../Controls/Expando.ascx" tagname="Expando" tagprefix="uc9" %>
<%@ Register Src="~/Controls/Expando.ascx" TagPrefix="uc1" TagName="Expando" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblClubHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <script src='<%= ResolveUrl("~/public/Scripts/daypilot-all.min.js?v=20210821") %>'></script>
    <script src='<%= ResolveUrl("~/public/Scripts/mfbcalendar.js?v=3") %>'></script>
    <asp:MultiView ID="mvPromoStatus" runat="server">
        <asp:View ID="vwPromotional" runat="server">
            <p><asp:Label Font-Bold="true" ID="lblPromo" runat="server"></asp:Label></p>
        </asp:View>
        <asp:View ID="vwInactive" runat="server">
            <p class="error"><asp:Label ID="lblInactive" runat="server"></asp:Label></p>
        </asp:View>
    </asp:MultiView>
    <asp:Panel runat="server" ID="pnlGuest">
        <div class="calloutSmall calloutBackground shadowed">
            <p><asp:Localize ID="lblNotMember" runat="server" Text="<%$ Resources:Club, LabelNotAMember %>" /></p>
            <p>
                <asp:Localize ID="locSendMessage" Text="<%$ Resources:Club, LabelSendMessage %>" runat="server" />
                <a href="javascript:contactClubClick();"><% =Resources.Club.LinkSendMessage %></a>
            </p>
        </div>
        <div id="divContactClub" style="display:none; width: 450px;">
            <p>
                <textarea id="txtContactClub" rows="5" cols="20" style="width:400px; margin-left:auto; margin-right:auto;"></textarea>
            </p>
            <table>
                <tr style="vertical-align:top;">
                    <td><input type="checkbox" id="ckRequestMembership" /></td>
                    <td><label for="ckRequestMembership"><% =Resources.Club.LabelRequestMembership %></label></td>
                </tr>
                <tr>
                    <td colspan="2" style="text-align:center">
                        <asp:Button ID="btnCancel" runat="server" Text="<%$ Resources:LocalizedText, Cancel %>" OnClientClick="dismissDlg('#divContactClub'); return false;" />&nbsp;&nbsp;<asp:Button ID="btnSendMessage" runat="server" Text="<%$ Resources:Club, LabelContactClub %>" OnClientClick="javascript:contactClubSend(); return false;" />
                    </td>
                </tr>
            </table>
        </div>
        <p><asp:Label ID="lblMessageStatus" runat="server" CssClass="success" Text="<%$ Resources:Club, StatusMessageSent %>" EnableViewState="false" style="display:none;" /></p>
    </asp:Panel>
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
    <asp:Accordion ID="accClub" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" SelectedIndex="2" RequireOpenedPane="false">
        <Panes>
            <asp:AccordionPane ID="acpDetails" runat="server">
                <Header>
                    <asp:Localize ID="locClubInfo" runat="server" Text="<%$ Resources:Club, TabClubInfo %>"  />
                </Header>
                <Content>
                    <asp:Panel class="calloutSmall calloutBackground shadowed" ID="pnlManage" runat="server" style="text-align:center; font-weight:bold;">
                        <p><% =Resources.Club.LabelManageClub %> <asp:HyperLink ID="lnkManageClub" runat="server" Text="<%$ Resources:Club, LabelManageNow %>" /></p>
                    </asp:Panel>
                    <uc1:ViewClub ID="ViewClub1" runat="server" LinkToDetails="false" />
                    <asp:Panel ID="pnlLeaveGroup" runat="server" Visible="false">
                        <asp:LinkButton ID="lnkLeaveGroup" Text="<%$ Resources:Club, ButtonLeaveClub %>" runat="server" OnClick="lnkLeaveGroup_Click"></asp:LinkButton>
                        <asp:ConfirmButtonExtender ID="confirmLeave" runat="server" TargetControlID="lnkLeaveGroup" ConfirmText="<%$ Resources:Club, errConfirmLeaveClub %>"></asp:ConfirmButtonExtender>
                    </asp:Panel>
                    <p><asp:HyperLink ID="lnkViewAll" runat="server" NavigateUrl="~/Public/Clubs.aspx?noredir=1" Text="<%$ Resources:Club, LabelViewAllClubs %>" /></p>
                </Content>
            </asp:AccordionPane>
            <asp:AccordionPane ID="acpMembers" runat="server">
                <Header>
                    <asp:Localize ID="locClubMembers" runat="server" Text="<%$ Resources:Club, TabClubMembers %>" ></asp:Localize>
                </Header>
                <Content>
                    <asp:GridView ID="gvMembers" DataKeyNames="UserName" runat="server" AutoGenerateColumns="False" GridLines="None" Width="100%" CellPadding="3">
                        <RowStyle CssClass="clubMemberRow" />
                        <AlternatingRowStyle CssClass="clubMemberAlternateRow" />
                        <Columns>
                            <asp:TemplateField ItemStyle-Width="60px">
                                <ItemTemplate>
                                    <img src='<%# CurrentClub.ShowHeadshots ? Eval("HeadShotHRef") : VirtualPathUtility.ToAbsolute("~/Public/tabimages/ProfileTab.png") %>' runat="server" class="roundedImg" style="width: 60px; height:60px;" />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Club, LabelMemberName %>" HeaderStyle-HorizontalAlign="Left" ItemStyle-Font-Bold="true" ItemStyle-Font-Size="Larger">
                                <ItemTemplate>
                                    <%# Eval("UserFullName") %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:Label runat="server" CssClass="clubCFI" Visible='<%# !String.IsNullOrEmpty((string) Eval("Certificate")) %>'><%# Resources.Club.ClubStatusCFI %></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderStyle-HorizontalAlign="Left">
                                <ItemTemplate>
                                    <%# Eval("DisplayRoleInClub") %> <%# Eval("ClubOffice") %>
                                </ItemTemplate>
                                <HeaderTemplate>
                                    <%# Resources.Club.LabelMemberRole %>
                                </HeaderTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="MobilePhone" HeaderText="<%$ Resources:Club, ClubStatusContact %>" HeaderStyle-HorizontalAlign="Left" ReadOnly="true" />
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <img src="<%# "~/images/sendflight.png".ToAbsoluteURL(Page.Request) %>"
                                        onclick='javascript:sendMsgClick("<%# Eval("UserName") %>", "<%# System.Web.HttpUtility.JavaScriptStringEncode(String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Club.LabelContactMember, Eval("UserFullName"))) %>");'
                                        alt="<%# Resources.Club.LinkSendMessage %>" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </Content>
            </asp:AccordionPane>
            <asp:AccordionPane ID="acpSchedules" runat="server">
                <Header>
                    <asp:Localize ID="locClubSchedules" runat="server" Text="<%$ Resources:Club, TabClubSchedules %>" ></asp:Localize>
                </Header>
                <Content>
                    <asp:MultiView ID="mvMain" runat="server">
                            <asp:View ID="vwSchedules" runat="server">
                                <uc3:mfbEditAppt ID="mfbEditAppt1" runat="server" />
                                <script>
                                    var clubCalendars = [];
                                    var clubNavControl;

                                    function refreshAllCalendars(args) {
                                        for (i = 0; i < clubCalendars.length; i++) {
                                            var cc = clubCalendars[i];
                                            cc.dpCalendar.startDate = args.day;
                                            cc.dpCalendar.update();
                                            cc.refreshEvents();
                                        }

                                        updateAvailability(<% =CurrentClub.ID %>, cc.dpCalendar.viewType == "days" ? cc.dpCalendar.startDate : cc.dpCalendar.visibleStart(), cc.dpCalendar.viewType == "days" ? 1 : 7)
                                    }

                                    function InitClubNav(cal) {
                                        clubCalendars[clubCalendars.length] = cal;
                                        if (typeof clubNavControl == 'undefined') {
                                            clubNavControl = cal.initNav('<% =pnlCalendarNav.ClientID %>');
                                            clubNavControl.onTimeRangeSelected = refreshAllCalendars;   // override the default function
                                            clubNavControl.select(new DayPilot.Date('<% =NowUTCInClubTZ %>'));
                                        }
                                    }
                                </script>
                                <div>
                                    <h2 style="display:inline"><asp:Label ID="lblSchedules" runat="server" Text="<%$ Resources:Club, LabelAircraftSchedules %>" /></h2>
                                    <asp:Label ID="lblDownloadCSV" runat="server" style="vertical-align:middle; float:right;">
                                        <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle" />
                                        <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_sm.png" style="padding-right: 5px; vertical-align:middle;" />
                                        <span style="vertical-align:middle"><asp:Localize ID="locDownloadSchedule" runat="server" Text="<%$ Resources:Club, DownloadClubSchedulePrompt %>" /></span>
                                    </asp:Label>
                                </div>
                                <asp:CollapsiblePanelExtender ID="cpeDownload" runat="server" ExpandControlID="lblDownloadCSV" CollapseControlID="lblDownloadCSV" Collapsed="true" TargetControlID="pnlDownload" EnableViewState="false" />
                                <asp:Panel ID="pnlDownload" DefaultButton="btnDownloadSchedule" runat="server" Height="0px" CssClass="EntryBlock" style="overflow:hidden; max-width:350px; margin-left: auto; margin-right: auto;">
                                    <h3><% =Resources.Club.DownloadClubScheduleHeader %> <span style="font-weight:bold"><% = CurrentClub == null ? String.Empty : CurrentClub.Name %></span></h3>
                                    <table>
                                        <tr style="vertical-align:top;">
                                            <td><asp:Label ID="lblDownloadFrom" runat="server" Text="<%$ Resources:Club, DownloadClubScheduleFrom %>" /></td>
                                            <td><uc1:mfbTypeInDate runat="server" ID="dateDownloadFrom" /></td>
                                        </tr>
                                        <tr style="vertical-align:top;">
                                            <td><asp:Label ID="lblDownloadTo" runat="server" Text="<%$ Resources:Club, DownloadClubScheduleTo %>" /></td>
                                            <td><uc1:mfbTypeInDate runat="server" ID="dateDownloadTo" /></td>
                                        </tr>
                                        <tr>
                                            <td></td>
                                            <td><asp:Button ID="btnDownloadSchedule" runat="server" Text="<%$ Resources:Club, DownloadClubScheduleNow %>" OnClick="btnDownloadSchedule_Click" /></td>
                                        </tr>
                                    </table>
                                    <div><asp:Label ID="lblDownloadErr" runat="server" CssClass="error" EnableViewState="false" /></div>
                                </asp:Panel>
                                <asp:GridView ID="gvScheduleDownload" Visible="false" AutoGenerateColumns="false" runat="server" EnableViewState="false">
                                    <Columns>
                                        <asp:BoundField DataField="LocalStart" DataFormatString="{0:s}" HeaderText="Start Date+Time (Local)" />
                                        <asp:BoundField DataField="LocalEnd" DataFormatString="{0:s}" HeaderText="End Date+Time (Local)" />
                                        <asp:BoundField DataField="StartUtc" DataFormatString="{0:s}" HeaderText="Start Date+Time (Utc)" />
                                        <asp:BoundField DataField="EndUtc" DataFormatString="{0:s}" HeaderText="End Date+Time (Utc)" />
                                        <asp:BoundField DataField="LocalStart" DataFormatString="{0:d}" HeaderText="Start Date (Local)" />
                                        <asp:BoundField DataField="LocalEnd" DataFormatString="{0:d}" HeaderText="End Date (Local)" />
                                        <asp:BoundField DataField="LocalStart" DataFormatString="{0:t}" HeaderText="Start Time (Local)" />
                                        <asp:BoundField DataField="LocalEnd" DataFormatString="{0:t}" HeaderText="End Time (Local)" />
                                        <asp:BoundField DataField="DurationDisplay" HeaderText="Duration" />
                                        <asp:BoundField DataField="AircraftDisplay" HeaderText="Aircraft" />
                                        <asp:BoundField DataField="OwnerName" HeaderText="Club Member" />
                                        <asp:BoundField DataField="Body" HeaderText="Description" />
                                    </Columns>
                                </asp:GridView>
                                <p>
                                    <asp:Label ID="lblNoteTZ" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                                    <asp:Label ID="lblTZDisclaimer" Text="" runat="server"></asp:Label>
                                    <% =Resources.Club.TimeZoneCurrentTime %>
                                    <asp:Label ID="lblCurTime" runat="server" Text=""></asp:Label>
                                </p>
                                <asp:Panel ID="pnlAvailMap" runat="server" ScrollBars="Auto" Width="100%" />
                                <script type="text/javascript">
                                    function updateAvailability(idClub, dt, days) {
                                        if ($("#cpTopForm_pnlAvailMap")[0].style.display == "none")
                                            return;
                                        var params = new Object();
                                        params.dtStart = dt;
                                        params.clubID = idClub;
                                        params.limitAircraft = -1;
                                        params.cDays = days;
                                        $.ajax(
                                            {
                                                type: "POST",
                                                data: JSON.stringify(params),
                                                url: "<% =ResolveUrl("~/Member/Schedule.aspx") %>/AvailabilityMap",
                                                dataType: "json",
                                                contentType: "application/json",
                                                error: function (xhr, status, error) {
                                                    window.alert(xhr.responseJSON.Message);
                                                },
                                                complete: function (response) { },
                                                success: function (response) {
                                                    $("#<% =pnlAvailMap.ClientID %>")[0].innerHTML = response.d;
                                                }
                                            });
                                    }

                                    $(function () {
                                        updateAvailability(<% =CurrentClub.ID %>, clubCalendars[0].dpCalendar.visibleStart(), clubCalendars[0].displayMode == "Week" ? 7 : 1);
                                    })
                                </script>
                                <div style="padding:3px; display:inline-block; width: 220px; vertical-align:top;">
                                    <div style="margin: auto">
                                        <asp:Panel ID="pnlCalendarNav" runat="server"></asp:Panel>
                                    </div>
                                    <div>
                                        <p>
                                            <asp:RadioButtonList ID="rbScheduleMode" runat="server" AutoPostBack="True" OnSelectedIndexChanged="rbScheduleMode_SelectedIndexChanged" RepeatDirection="Horizontal">
                                                <asp:ListItem Selected="True" Text="<%$ Resources:Schedule, Day %>" Value="Day"></asp:ListItem>
                                                <asp:ListItem Selected="False" Text="<%$ Resources:Schedule, Week %>" Value="Week"></asp:ListItem>
                                            </asp:RadioButtonList>
                                        </p>
                                    </div>
                                    <p><asp:Label Font-Bold="true" ID="locSummary" Text="<%$ Resources:Club, LabelUpcomingSchedule %>" runat="server"></asp:Label></p>
                                    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
                                        <ContentTemplate>
                                            <div style="border:1px solid black; padding:3px; ">
                                                <uc7:SchedSummary ID="SchedSummary1" runat="server" />
                                                <p><asp:CheckBox ID="ckSummaryScope" Text="<%$ Resources:Club, upcomingUser %>" runat="server" OnCheckedChanged="ckSummaryScope_CheckedChanged" AutoPostBack="True" Checked="true" /></p>
                                            </div>
                                        </ContentTemplate>
                                    </asp:UpdatePanel>
                                </div>
                                <div style="padding:3px; display: inline-block;" runat="server" id="divCalendar">
                                    <asp:MultiView ID="mvClubAircraft" runat="server">
                                        <asp:View ID="vwNoAircraft" runat="server">
                                            <asp:Label ID="lblNoAircraft" runat="server" Text="<%$Resources:Club, LabelNoAircraft %>"></asp:Label>
                                        </asp:View>
                                        <asp:View ID="vwOneAircraft" runat="server">
                                            <uc8:ClubAircraftSchedule runat="server" ID="casSingleAircraft" />
                                        </asp:View>
                                        <asp:View ID="vwMultipleAircraft" runat="server">
                                            <asp:TabContainer ID="tcAircraftSchedules" runat="server" CssClass="mfbDefault" >
                                            </asp:TabContainer>
                                        </asp:View>
                                    </asp:MultiView>
                                </div>
                            </asp:View>
                            <asp:View runat="server" ID="vwMainGuest">

                            </asp:View>
                        </asp:MultiView>
                </Content>
            </asp:AccordionPane>
        </Panes>
    </asp:Accordion>
    <script type="text/javascript">
        function contactClubClick() {
            var div = $("#divContactClub");
            div.dialog({ autoOpen: false, closeOnEscape: true, modal: true, width: 450, title: '<%=Resources.Club.LabelMessagePrompt %>' });
            div.dialog("open");
        }

        function contactClubSend() {
            var params = new Object();
            params.idClub = <% =CurrentClub.ID %>;
            params.szMessage = $("#txtContactClub").val();
            params.fRequestMembership = $("#ckRequestMembership")[0].checked;
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Member/ClubDetails.aspx/ContactClub") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) {
                        hideContact();
                        $("#txtContactClub").val("");
                        $("#ckRequestMembership")[0].checked = false;
                        $("#" + "<%=lblMessageStatus.ClientID %>").show();
                    }
                });
        }

        function sendMsgClick(targetUser, szTitle) {
            $("#" + '<% =hdnTargetUser.ClientID %>').val(targetUser);
            var div = $("#divSendMsg");
            div.dialog({ autoOpen: false, closeOnEscape: true, modal: true, title: szTitle });
            div.dialog("open");
        }

        function sendMsgToUser() {
            var params = new Object();
            params.szTarget = $("#" + '<% =hdnTargetUser.ClientID %>').val();
            params.szSubject = $("#" + '<% =txtContactSubject.ClientID %>').val();
            params.szText = $("#" + '<% =txtMsg.ClientID %>').val();
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Member/ClubDetails.aspx/SendMsgToClubUser") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) {
                        dismissDlg("#divSendMsg");
                    }
                });
        }
    </script>
    <div id="divSendMsg" style="max-width: 350px; display:none;">
        <p class="fineprint"><asp:Label ID="lblDisclaimer" runat="server" Text="<%$ Resources:Club, LabelContactMemberDisclaimer %>" /></p>
        <asp:HiddenField ID="hdnTargetUser" runat="server" />
        <div><asp:Label ID="lblSubject" runat="server" Text="<%$ Resources:LocalizedText, ContactUsSubject %>" /></div>
        <div><asp:TextBox ID="txtContactSubject" runat="server" Width="100%" /></div>
        <div><asp:TextBox ID="txtMsg" runat="server" TextMode="MultiLine" Rows="4" Width="100%" /></div>
        <div style="text-align:center">
            <asp:Button ID="btnSendMsg" OnClientClick="javascript:sendMsgToUser(); return false;" runat="server" Text="<%$ Resources:LogbookEntry, SendFlightButton %>" /> 
            <asp:Button ID="btnCancelSend" runat="server" OnClientClick="dismissDlg('#divSendMsg'); return false;" Text="<%$ Resources:LogbookEntry, SendFlightCancel %>" />
        </div>
    </div>
</asp:Content>

