<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="ClubDetails.aspx.cs" Inherits="Member_ClubDetails" %>
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

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblClubHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <script src='<%= ResolveUrl("~/public/Scripts/daypilot-all.min.js?v=20200102") %>'></script>
    <script src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script src='<%= ResolveUrl("~/public/Scripts/jquery.json-2.4.min.js") %>'></script>
    <script src='<%= ResolveUrl("~/public/Scripts/mfbcalendar.js?v=3") %>'></script>
    <p><asp:HyperLink ID="lnkViewAll" runat="server" NavigateUrl="~/Public/Clubs.aspx?noredir=1" Text="<%$ Resources:Club, LabelViewAllClubs %>"></asp:HyperLink></p>
    <asp:MultiView ID="mvPromoStatus" runat="server">
        <asp:View ID="vwPromotional" runat="server">
            <p><asp:Label Font-Bold="true" ID="lblPromo" runat="server"></asp:Label></p>
        </asp:View>
        <asp:View ID="vwInactive" runat="server">
            <p class="error"><asp:Label ID="lblInactive" runat="server"></asp:Label></p>
        </asp:View>
    </asp:MultiView>
    <asp:MultiView ID="mvTop" runat="server">
        <asp:View ID="vwTopGuest" runat="server">
            <div class="callout">
                <p><asp:Localize ID="lblNotMember" runat="server" Text="<%$ Resources:Club, LabelNotAMember %>"></asp:Localize></p>
                <p><asp:Localize ID="locSendMessage" Text="<%$ Resources:Club, LabelSendMessage %>" runat="server"></asp:Localize> <asp:HyperLink ID="lnkSendMessage" style="cursor:pointer;" Text="<%$ Resources:Club, LinkSendMessage %>" runat="server"></asp:HyperLink></p>
            </div>
            <asp:Panel ID="pnlContact" DefaultButton="btnSendMessage" runat="server" CssClass="modalpopup" style="display:none; width: 450px;">
                <p><asp:Localize ID="locSendPrompt" runat="server" Text="<%$ Resources:Club, LabelMessagePrompt %>"></asp:Localize></p>
                <p>
                    <asp:TextBox ID="txtContact"  runat="server" TextMode="MultiLine" Rows="5" Width="400px" style="margin-left:auto; margin-right:auto;" ValidationGroup="clubContact"></asp:TextBox>
                </p>
                <p>
                    <asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ValidationGroup="clubContact" ControlToValidate="txtContact" CssClass="error" Display="Dynamic" ErrorMessage="<%$ Resources:Club, errNoContactMessageBody %>"></asp:RequiredFieldValidator>
                </p>
                <table>
                    <tr style="vertical-align:top;">
                        <td><asp:CheckBox ID="ckRequestMembership" Text="" runat="server" /></td>
                        <td><asp:Label ID="lblCkRequest" runat="server" Text="<%$ Resources:Club, LabelRequestMembership %>" AssociatedControlID="ckRequestMembership"></asp:Label></td>
                    </tr>
                    <tr>
                        <td colspan="2" style="text-align:center">
                            <asp:Button ID="btnCancel" runat="server" Text="<%$ Resources:LocalizedText, Cancel %>" />&nbsp;&nbsp;<asp:Button ID="btnSendMessage" ValidationGroup="clubContact" runat="server" Text="<%$ Resources:Club, LabelContactClub %>" OnClick="btnSendMessage_Click" />
                        </td>
                    </tr>
                </table>
            </asp:Panel>
            <asp:ModalPopupExtender ID="mpuGuestContact" runat="server" BackgroundCssClass="modalBackground" CancelControlID="btnCancel" PopupControlID="pnlContact" BehaviorID="mpuGuestContact" TargetControlID="lnkSendMessage"></asp:ModalPopupExtender>
            <p><asp:Label ID="lblMessageStatus" runat="server" CssClass="success" Text="<%$ Resources:Club, StatusMessageSent %>" EnableViewState="false" Visible="false"></asp:Label></p>
            <script>
                        /* Handle escape to dismiss */
                        function pageLoad(sender, args) {
                            if (!args.get_isPartialLoad()) {
                                $addHandler(document, "keydown", onKeyDown);
                            }
                        }

                        function onKeyDown(e) {
                            if (e && e.keyCode == Sys.UI.Key.esc)
                                $find("mpuGuestContact").hide();
                        }
            </script>
        </asp:View>
        <asp:View ID="vwTopMember" runat="server"></asp:View>
        <asp:View ID="vwTopAdmin" runat="server">
            <p class="callout"><% =Resources.Club.LabelManageClub %> <asp:HyperLink ID="lnkManageClub" runat="server" Text="<%$ Resources:Club, LabelManageNow %>"></asp:HyperLink></p>
        </asp:View>
    </asp:MultiView>
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
    <asp:Accordion ID="accClub" runat="server" HeaderCssClass="accordianHeader" HeaderSelectedCssClass="accordianHeaderSelected" ContentCssClass="accordianContent" TransitionDuration="250" SelectedIndex="2">
        <Panes>
            <asp:AccordionPane ID="acpDetails" runat="server">
                <Header>
                    <asp:Localize ID="locClubInfo" runat="server" Text="<%$ Resources:Club, TabClubInfo %>" ></asp:Localize>
                </Header>
                <Content>
                    <uc1:ViewClub ID="ViewClub1" runat="server" LinkToDetails="false" />
                    <asp:Panel ID="pnlLeaveGroup" runat="server" Visible="false">
                        <asp:LinkButton ID="lnkLeaveGroup" Text="<%$ Resources:Club, ButtonLeaveClub %>" runat="server" OnClick="lnkLeaveGroup_Click"></asp:LinkButton>
                        <asp:ConfirmButtonExtender ID="confirmLeave" runat="server" TargetControlID="lnkLeaveGroup" ConfirmText="<%$ Resources:Club, errConfirmLeaveClub %>"></asp:ConfirmButtonExtender>
                    </asp:Panel>
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
                                    <img src='<%# Eval("HeadShotHRef") %>' runat="server" class="roundedImg" style="width: 60px; height:60px;" />
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
                                    <%# Eval("DisplayRoleInClub") %>
                                </ItemTemplate>
                                <HeaderTemplate>
                                    <%# Resources.Club.LabelMemberRole %>
                                </HeaderTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="MobilePhone" HeaderText="<%$ Resources:Club, ClubStatusContact %>" HeaderStyle-HorizontalAlign="Left" ReadOnly="true" />
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
                                <h2><asp:Label ID="lblSchedules" runat="server" Text="<%$ Resources:Club, LabelAircraftSchedules %>"></asp:Label></h2>
                                <p>
                                    <asp:Label ID="lblNoteTZ" Font-Bold="true" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                                    <asp:Label ID="lblTZDisclaimer" Text="" runat="server"></asp:Label>
                                    <% =Resources.Club.TimeZoneCurrentTime %>
                                    <asp:Label ID="lblCurTime" runat="server" Text=""></asp:Label>
                                </p>
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
</asp:Content>

