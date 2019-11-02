<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="ClubManage.aspx.cs" Inherits="Member_ClubManage" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register src="../Controls/ClubControls/ViewClub.ascx" tagname="ViewClub" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbResourceSchedule.ascx" tagname="mfbResourceSchedule" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbImageList.ascx" tagname="mfbImageList" tagprefix="uc4" %>
<%@ Register src="../Controls/mfbHtmlEdit.ascx" tagname="mfbHtmlEdit" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbTypeInDate.ascx" tagname="mfbTypeInDate" tagprefix="uc6" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register src="../Controls/ClubControls/SchedSummary.ascx" tagname="SchedSummary" tagprefix="uc7" %>
<%@ Register src="../Controls/ClubControls/ClubAircraftSchedule.ascx" tagname="ClubAircraftSchedule" tagprefix="uc8" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblClubHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <h2><asp:Label ID="lblManageheader" runat="server"></asp:Label></h2>
    <asp:HyperLink ID="lnkReturnToClub" runat="server" Text="<%$ Resources:Club, LabelReturnToClub %>"></asp:HyperLink>
    <asp:TabContainer ID="tabManage" runat="server" CssClass="mfbDefault">
        <asp:TabPanel ID="tabpanelMembers" runat="server" HeaderText="<%$ Resources:Club, TabClubMembers %>">
            <ContentTemplate>
                <asp:Panel ID="pnlManageMembers" runat="server">
                    <asp:GridView ID="gvMembers" DataKeyNames="UserName" runat="server" AutoGenerateColumns="False" GridLines="None" AutoGenerateEditButton="True" OnRowCancelingEdit="gvMembers_RowCancelingEdit" 
                        OnRowDataBound="gvMembers_RowDataBound" CellPadding="3" OnRowCommand="gvMembers_RowCommand" OnRowUpdating="gvMembers_RowUpdating" OnRowEditing="gvMembers_RowEditing">
                        <Columns>
                            <asp:BoundField HeaderText="<%$ Resources:Club, LabelMemberName %>" DataField="UserFullName" ReadOnly="True" />
                            <asp:BoundField HeaderText="<%$ Resources:Club, LabelMemberJoinDate %>" DataField="JoinedDate" ReadOnly="True" DataFormatString="{0:d}" />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, LabelMemberRole %>">
                                <ItemTemplate>
                                    <%# Eval("RoleInClub") %>
                                </ItemTemplate>
                                <EditItemTemplate>
                                    <asp:RadioButtonList ID="rblRole" RepeatDirection="Horizontal" runat="server">
                                        <asp:ListItem Text="<%$ Resources:Club, RoleMember %>" Value="Member"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:Club, RoleManager %>" Value="Admin"></asp:ListItem>
                                        <asp:ListItem Text="<%$ Resources:Club, RoleOwner %>" Value="Owner"></asp:ListItem>
                                    </asp:RadioButtonList></EditItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:ConfirmButtonExtender ID="confirmDeleteMember" TargetControlID="lnkDelete" ConfirmText="<%$ Resources:Club, confirmMemberDelete %>" runat="server"></asp:ConfirmButtonExtender>
                                    <asp:LinkButton ID="lnkDelete" CommandName="_Delete" CommandArgument='<%# Eval("UserName") %>' runat="server">
                                        <asp:Image ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" runat="server" />
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                    <asp:Label ID="lblManageMemberError" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
                    <hr />
                    <asp:Panel ID="pnlAddMember" runat="server" DefaultButton="btnAddMember">
                        <asp:Localize ID="locAddMemberPrompt" runat="server" Text="<%$ Resources:Club, LabelInviteMember %>"></asp:Localize>
                        <br />
                        <asp:Label ID="lblEmailDisclaimer2" Font-Bold="True" runat="server" 
                            Text="<%$ Resources:Club, LabelEmailDisclaimer %>"></asp:Label>
                        <br />
                        <br />
                        <asp:TextBox ID="txtMemberEmail" runat="server" Width="300px" ValidationGroup="vgAddMember" AutoCompleteType="Email"></asp:TextBox>
                        <asp:TextBoxWatermarkExtender ID="txtMemberEmail_TextBoxWatermarkExtender" WatermarkText="<%$ Resources:Club, WatermarkInviteMember %>" WatermarkCssClass="watermark" runat="server" TargetControlID="txtMemberEmail" BehaviorID="_content_txtMemberEmail_TextBoxWatermarkExtender">
                        </asp:TextBoxWatermarkExtender>
                        &nbsp;&nbsp;&nbsp; <asp:Button ID="btnAddMember" runat="server" Text="<%$ Resources:Club, ButtonInviteMember %>" ValidationGroup="vgAddMember" onclick="btnAddMember_Click"  />
                        <asp:RequiredFieldValidator ID="RequiredFieldValidator3" 
                            ControlToValidate="txtMemberEmail" runat="server" 
                            ErrorMessage="<%$ Resources:Club, errValidEmailRequired %>" CssClass="error" 
                            ValidationGroup="vgAddMember" Display="Dynamic"></asp:RequiredFieldValidator>
                        <asp:RegularExpressionValidator ID="RegularExpressionValidator2" runat="server" 
                            ControlToValidate="txtMemberEmail" 
                            ErrorMessage="<%$ Resources:Club, errValidEmailRequired %>" CssClass="error" 
                            ValidationGroup="vgAddMember" Display="Dynamic" 
                                ValidationExpression="\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*"></asp:RegularExpressionValidator>
                    </asp:Panel>
                    <asp:Label ID="lblAddMemberSuccess" runat="server" EnableViewState="False"></asp:Label>
                </asp:Panel>
            </ContentTemplate>
        </asp:TabPanel>
        <asp:TabPanel ID="tabpanelAircraft" runat="server" HeaderText="<%$ Resources:Club, TabClubAircraft %>">
            <ContentTemplate>
                <asp:Panel ID="pnlManageAircraft" runat="server">
                    <asp:GridView ID="gvAircraft" runat="server" Width="100%" AutoGenerateColumns="false" AutoGenerateEditButton="true" GridLines="None" OnRowDataBound="gvAircraft_RowDataBound" 
                        OnRowCancelingEdit="gvAircraft_RowCancelingEdit" OnRowCommand="gvAircraft_RowCommand" OnRowUpdating="gvAircraft_RowUpdating" OnRowEditing="gvAircraft_RowEditing" 
                        DataKeyNames="AircraftID">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <table>
                                        <tr style="vertical-align: top;">
                                            <td style="text-align: center">
                                                <asp:Panel ID="pnlSampleImage" Width="200px" Visible='<%# Eval("HasSampleImage") %>' runat="server">
                                                    <asp:HyperLink ID="lnkImage" runat="server" NavigateUrl='<%# Eval("SampleImageFull") %>' Target="_blank">
                                                        <asp:Image ID="imgSample" ImageUrl='<%# Eval("SampleImageThumbnail") %>' ImageAlign="Middle" runat="server" />
                                                    </asp:HyperLink>
                                                    <div><%# Eval("SampleImageComment") %></div>
                                                </asp:Panel>
                                            </td>
                                            <td>
                                                <div style="font-weight: bold;"><%# Eval("DisplayTailnumber") %></div>
                                                <asp:Literal ID="litDesc" Text='<%# Eval("ClubDescription") %>' runat="server"></asp:Literal></td>
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <EditItemTemplate>
                                    <h3 style="font-weight: bold"><%# Eval("DisplayTailnumber") %></h3>
                                    <uc5:mfbHtmlEdit ID="txtDescription" Width="100%" Text='<%# Eval("ClubDescription") %>' runat="server" />
                                    <table>
                                        <tr>
                                            <td>
                                                <div style="font-weight:bold"><% =Resources.Club.ClubAircraftTime %></div>
                                                <div class="fineprint"><%=Resources.Club.ClubAircraftTimeDesc %></div>
                                            </td>
                                            <td>
                                                <uc1:mfbDecimalEdit runat="server" ID="decEditTime" Value='<%# Eval("HighWater") %>' />
                                            </td>
                                            <td>
                                                <asp:Panel ID="pnlHighHobbs" runat="server"><asp:Image ID="imgXFillHobbs" runat="server" ImageUrl="~/images/cross-fill.png" AlternateText="<%$ Resources:Club, ClubAircraftTimeCrossFill %>" ToolTip="<%$ Resources:Club, ClubAircraftTimeCrossFill %>" /> <asp:Label ID="lnkCopyHobbs" runat="server"></asp:Label></asp:Panel>
                                                <asp:Panel ID="pnlHighTach" runat="server"><asp:Image ID="imgXFillTach" runat="server" ImageUrl="~/images/cross-fill.png" AlternateText="<%$ Resources:Club, ClubAircraftTimeCrossFill %>" ToolTip="<%$ Resources:Club, ClubAircraftTimeCrossFill %>" /> <asp:Label ID="lnkCopyTach" runat="server"></asp:Label></asp:Panel>
                                            </td>
                                        </tr>
                                    </table>
                                </EditItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:ConfirmButtonExtender ID="confirmDeleteAircraft" TargetControlID="lnkDelete" ConfirmText="<%$ Resources:Club, confirmAircraftDelete %>" runat="server"></asp:ConfirmButtonExtender>
                                    <asp:LinkButton ID="lnkDelete" CommandName="_Delete" CommandArgument='<%# Eval("AircraftID") %>' runat="server">
                                        <asp:Image ID="imgDelete" ImageUrl="~/images/x.gif" AlternateText="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" ToolTip="<%$ Resources:LogbookEntry, LogbookDeleteTooltip %>" runat="server" />
                                    </asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <asp:Label ID="lblNoAircraft" runat="server" Text="<%$Resources:Club, LabelNoAircraft %>"></asp:Label>
                        </EmptyDataTemplate>
                    </asp:GridView>
                    <hr />
                    <asp:Panel ID="pnlAddAircraft" runat="server">
                        <p><asp:Label ID="lblAddAircraft" runat="server" Text="<%$ Resources:Club, LabelAddAircraft %>"></asp:Label><asp:DropDownList ID="cmbAircraftToAdd" DataTextField="DisplayTailnumber" DataValueField="AircraftID" runat="server"></asp:DropDownList></p>
                        <p>
                            <asp:Label ID="lblDescPrompt" runat="server" Text="<%$ Resources:Club, LabelAircraftDescriptionHeader %>"></asp:Label>
                            <uc5:mfbHtmlEdit ID="txtDescription" Width="500px" runat="server" />
                        </p>
                        <p style="text-align:center"><asp:Button ID="btnAddAircraft" runat="server" Text="<%$ Resources:Club, ButtonAddAircraft %>" OnClick="btnAddAircraft_Click" /></p>
                    </asp:Panel>
                    <asp:Label ID="lblManageAircraftError" runat="server" Text="" CssClass="error" EnableViewState="false"></asp:Label>
                </asp:Panel>
            </ContentTemplate>
        </asp:TabPanel>
        <asp:TabPanel ID="tabpanelClubDetails" runat="server" HeaderText="<%$ Resources:Club, TabClubInfo %>">
            <ContentTemplate>
                <uc1:ViewClub ID="vcEdit" runat="server" DefaultMode="Edit" OnClubChanged="vcEdit_ClubChanged" OnClubDeleted="btnDeleteClub_Click" ShowCancel="false" ShowDelete="true" OnClubChangeCanceled="vcEdit_ClubChangeCanceled" />
            </ContentTemplate>
        </asp:TabPanel>
        <asp:TabPanel ID="tabpanelClubReports" runat="server" HeaderText="<%$ Resources:Club, TabClubReports %>">
            <ContentTemplate>
                <h2><asp:Literal ID="litReportsPrompt" runat="server" Text="<%$ Resources:Club, ReportPrompt %>"></asp:Literal></h2>
                <h3><% = Resources.Club.ReportHeaderFlying %></h3>
                <asp:Panel ID="pnlFlyingReport" DefaultButton="btnUpdate" runat="server">
                    <p>
                        <asp:Label ID="lblReportDisclaimer" Font-Bold="True" runat="server" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                        <asp:Literal ID="litReportsDisclaimer" runat="server" Text="<%$ Resources:Club, ReportPromptDisclaimer %>"></asp:Literal>
                    </p>
                    <table>
                        <tr>
                            <td>
                                <asp:Literal ID="litStart" runat="server" Text="<%$ Resources:Club, ReportStartDate %>"></asp:Literal>
                            </td>
                            <td>
                                <uc1:mfbTypeInDate runat="server" ID="dateStart" DefaultType="Today" />
                            </td>
                            <td rowspan="2" style="padding-left: 50px">
                                <asp:LinkButton ID="lnkViewKML" runat="server" onclick="lnkViewKML_Click">
                                    <asp:Image ID="imgDownloadKML" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                                    <asp:Image ID="imgKMLIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/kmlicon_med.png" style="padding-right: 5px;" />
                                    <div style="display:inline-block;vertical-align:middle"><asp:Localize ID="locViewGoogleEarth" runat="server" Text="<%$ Resources:Airports, DownloadKML %>"></asp:Localize><br /><asp:Label ID="locKMLSlow" runat="server" Text="<%$ Resources:Airports, WarningSlow %>"></asp:Label></div> 
                                </asp:LinkButton>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <asp:Literal ID="litEnd" runat="server" Text="<%$ Resources:Club, ReportEndDate %>"></asp:Literal>
                            </td>
                            <td>
                                <uc1:mfbTypeInDate runat="server" ID="dateEnd" DefaultType="Today" />
                            </td>
                        </tr>

                        <tr>
                            <td colspan="3">
                                <asp:Button ID="btnUpdate" OnClick="btnUpdate_Click" runat="server" Text="<%$ Resources:Club, ReportUpdate %>" />
                                <asp:Button ID="btnDownload" OnClick="btnDownload_Click" runat="server" Text="<%$ Resources:Club, ReportDownload %>" Visible="false" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <asp:Panel ID="pnlReport" runat="server" ScrollBars="Auto">
                    <asp:GridView ID="gvClubReports" AutoGenerateColumns="false" ShowFooter="false" CellPadding="4" GridLines="None" runat="server">
                        <Columns>
                            <asp:BoundField DataField="Date" DataFormatString="{0:d}" HeaderText="<%$ Resources:Club, ReportHeaderDate %>" />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderMonth %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblMonth" runat="server" Text='<%# MonthForDate(Convert.ToDateTime(Eval("Date"))) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Aircraft" HeaderText="<%$ Resources:Club, ReportHeaderAircraft %>"  />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderPilotName %>" >
                                <ItemTemplate>
                                    <asp:Label ID="lblName" runat="server" Text='<%# FullName(Eval("Firstname").ToString(), Eval("Lastname").ToString(), Eval("Email").ToString()) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Route" HeaderText="<%$ Resources:Club, ReportHeaderRoute %>" />
                            <asp:BoundField DataField="Total Time" HeaderText="<%$ Resources:Club, ReportHeaderTotalTime %>" DataFormatString="{0:0.0#}" />
                            <asp:BoundField DataField="Hobbs Start" HeaderText="<%$ Resources:Club, ReportHeaderHobbsStart %>" />
                            <asp:BoundField DataField="Hobbs End" HeaderText="<%$ Resources:Club, ReportHeaderHobbsEnd %>" />
                            <asp:BoundField DataField="Total Hobbs" HeaderText="<%$ Resources:Club, ReportHeaderTotalHobbs %>" DataFormatString="{0:0.0#}" />
                            <asp:BoundField DataField="Tach Start" HeaderText="<%$ Resources:Club, ReportHeaderTachStart %>" />
                            <asp:BoundField DataField="Tach End" HeaderText="<%$ Resources:Club, ReportHeaderTachEnd %>" />
                            <asp:BoundField DataField="Total Tach" HeaderText="<%$ Resources:Club, ReportHeaderTotalTach %>" DataFormatString="{0:0.0#}" />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightStart %>" >
                                <ItemTemplate>
                                    <asp:Label ID="lblFlightStart" runat="server" Text='<%# FormattedUTCDate(Eval("Flight Start")) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderFlightEnd %>" >
                                <ItemTemplate>
                                    <asp:Label ID="lblflightEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Flight End")) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Total Flight" HeaderText="<%$ Resources:Club, ReportHeaderTotalFlight %>" DataFormatString="{0:0.0#}"  />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineStart %>" >
                                <ItemTemplate>
                                    <asp:Label ID="lblEngineStart" runat="server" Text='<%# FormattedUTCDate(Eval("Engine Start")) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderEngineEnd %>" >
                                <ItemTemplate>
                                    <asp:Label ID="lblEngineEnd" runat="server" Text='<%# FormattedUTCDate(Eval("Engine End")) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="Total Engine" HeaderText="<%$ Resources:Club, ReportHeaderTotalEngine %>" DataFormatString="{0:0.0#}" />
                            <asp:BoundField DataField="Oil Added" HeaderText="<%$ Resources:Club, ReportHeaderOilAdded %>" DataFormatString="{0:0.0#}" />
                            <asp:BoundField DataField="Fuel Added" HeaderText="<%$ Resources:Club, ReportHeaderFuelAdded %>" DataFormatString="{0:0.0#}" />
                            <asp:BoundField DataField="Fuel Cost" HeaderText="<%$ Resources:Club, ReportHeaderFuelCost %>" DataFormatString="{0:C}" />
                        </Columns>
                        <EmptyDataTemplate>
                            <p><% =Resources.Club.ReportNoData %></p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:Panel>
                <asp:SqlDataSource ID="sqlDSReports" runat="server" 
                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                    SelectCommand="SELECT f.idflight, f.date AS Date, f.TotalFlightTime AS 'Total Time', f.Route, f.HobbsStart AS 'Hobbs Start', f.HobbsEnd AS 'Hobbs End', u.username AS 'Username', u.Firstname, u.LastName, u.Email, ac.Tailnumber AS 'Aircraft',  
                    fp.decValue AS 'Tach Start', fp2.decValue AS 'Tach End',
                    f.dtFlightStart AS 'Flight Start', f.dtFlightEnd AS 'Flight End', 
                    f.dtEngineStart AS 'Engine Start', f.dtEngineEnd AS 'Engine End',
                    IF (YEAR(f.dtFlightEnd) &gt; 1 AND YEAR(f.dtFlightStart) &gt; 1, (UNIX_TIMESTAMP(f.dtFlightEnd)-UNIX_TIMESTAMP(f.dtFlightStart))/3600, 0) AS 'Total Flight',
                    IF (YEAR(f.dtEngineEnd) &gt; 1 AND YEAR(f.dtEngineStart) &gt; 1, (UNIX_TIMESTAMP(f.dtEngineEnd)-UNIX_TIMESTAMP(f.dtEngineStart))/3600, 0) AS 'Total Engine',
                    f.HobbsEnd - f.HobbsStart AS 'Total Hobbs', 
                    fp2.decValue - fp.decValue AS 'Total Tach',
                    fp3.decValue AS 'Oil Added',
                    fp4.decValue AS 'Fuel Added',
                    fp5.decValue AS 'Fuel Cost'
FROM flights f 
INNER JOIN clubmembers cm ON f.username = cm.username
INNER JOIN users u ON u.username=cm.username
INNER JOIN clubs c ON c.idclub=cm.idclub
INNER JOIN clubaircraft ca ON ca.idaircraft=f.idaircraft
INNER JOIN aircraft ac ON ca.idaircraft=ac.idaircraft
LEFT JOIN flightproperties fp on (fp.idflight=f.idflight AND fp.idproptype=95)
LEFT JOIN flightproperties fp2 on (fp2.idflight=f.idflight AND fp2.idproptype=96)
LEFT JOIN flightproperties fp3 on (fp3.idflight=f.idflight AND fp3.idproptype=365)
LEFT JOIN flightproperties fp4 on (fp4.idflight=f.idflight AND fp4.idproptype=94)
LEFT JOIN flightproperties fp5 on (fp5.idflight=f.idflight AND fp5.idproptype=159)
WHERE
c.idClub = ?idClub AND
f.date &gt;= GREATEST(?startDate, cm.joindate, c.creationDate) AND
f.date &lt;= ?endDate
ORDER BY f.DATE ASC
">
                </asp:SqlDataSource>
                <h3><% =Resources.Club.ReportHeaderMaintenance %></h3>
                <div>
                    <asp:Button ID="btnUpdateMaintenance" runat="server" Text="<%$ Resources:Club, ReportUpdate %>" OnClick="btnUpdateMaintenance_Click" />
                    <asp:Button ID="btnDownloadMaintenance" runat="server" Text="<%$ Resources:Club, ReportDownload %>" Visible="false" OnClick="btnDownloadMaintenance_Click" />
                </div>
                <asp:Panel ID="pnlMaintenanceReport" runat="server" ScrollBars="Auto" >
                    <asp:GridView ID="gvMaintenance" AutoGenerateColumns="false" ShowFooter="false" CellPadding="4" GridLines="None" runat="server">
                        <Columns>
                            <asp:BoundField DataField="DisplayTailnumber" ItemStyle-Font-Bold="true" HeaderText="<%$ Resources:Aircraft, AircraftHeader %>" />
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ClubAircraftTime %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastAnnual" runat="server" Text='<%# ValueString (Eval("HighWater")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnual %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastAnnual" runat="server" Text='<%# ValueString (Eval("LastAnnual")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnualDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblAnnualDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextAnnual")) %>' Text='<%# ValueString (Eval("Maintenance.NextAnnual")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponder %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastXponder" runat="server" Text='<%# ValueString (Eval("LastTransponder")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponderDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblXPonderDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextTransponder")) %>' Text='<%# ValueString (Eval("Maintenance.NextTransponder")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStatic %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastPitot" runat="server" Text='<%# ValueString (Eval("LastStatic")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStaticDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblPitotDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextStatic")) %>' Text='<%# ValueString (Eval("Maintenance.NextStatic")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeter %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastAltimeter" runat="server" Text='<%# ValueString (Eval("LastAltimeter")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeterDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblAltimeterDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextAltimeter")) %>' Text='<%# ValueString (Eval("Maintenance.NextAltimeter")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELT %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastELT" runat="server" Text='<%# ValueString (Eval("LastELT")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELTDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblELTDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextELT")) %>' Text='<%# ValueString (Eval("Maintenance.NextELT")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVOR %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastVOR" runat="server" Text='<%# ValueString (Eval("LastVOR")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVORDue %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblVORDue" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("Maintenance.NextVOR")) %>' Text='<%# ValueString (Eval("Maintenance.NextVOR")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100 %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLast100" runat="server" Text='<%# ValueString (Eval("Last100")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100Due %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblNext100" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("Maintenance.Next100"), 10) %>' Text='<%# ValueString (Eval("Maintenance.Next100")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOil %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastOil" runat="server" Text='<%# ValueString (Eval("LastOilChange")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue25 %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblOil25" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 5, 25) %>' Text='<%# ValueString (Eval("LastOilChange"), 25) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue50 %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblOil50" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 10, 50) %>' Text='<%# ValueString (Eval("LastOilChange"), 50) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue100 %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblOil100" runat="server" CssClass='<%# CSSForValue((decimal) Eval("HighWater"), (decimal) Eval("LastOilChange"), 15, 100) %>' Text='<%# ValueString (Eval("LastOilChange"), 100) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceEngine %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblLastEngine" runat="server" Text='<%# ValueString (Eval("LastNewEngine")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceRegistration %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblRegistration" runat="server" CssClass='<%# CSSForDate((DateTime) Eval("RegistrationDue")) %>' Text='<%# ValueString (Eval("RegistrationDue")) %>' /></ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Currency, deadlinesHeaderDeadlines %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblDeadlines" runat="server" Text='<%# MyFlightbook.FlightCurrency.DeadlineCurrency.CoalescedDeadlinesForAircraft(null, (int) Eval("AircraftID")) %>' />
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderNotes %>">
                                <ItemTemplate>
                                    <asp:Label ID="lblNotes" runat="server" Text='<%# Eval("PublicNotes") %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p><% =Resources.Club.ReportNoData %></p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:Panel>
                <h3><% =Resources.Club.ReportHeaderInsurance %></h3>
                <div>
                    <asp:Label ID="lblMonthsForInsurance" runat="server" Text="<%$ Resources:Club, ReportInsuranceMonths %>"></asp:Label>
                    <asp:DropDownList ID="cmbMonthsInsurance" runat="server">
                        <asp:ListItem Text="1" Value="1"></asp:ListItem>
                        <asp:ListItem Text="2" Value="2"></asp:ListItem>
                        <asp:ListItem Text="3" Value="3"></asp:ListItem>
                        <asp:ListItem Text="4" Value="4"></asp:ListItem>
                        <asp:ListItem Text="5" Value="5"></asp:ListItem>
                        <asp:ListItem Text="6" Value="6" Selected="True"></asp:ListItem>
                        <asp:ListItem Text="12" Value="12"></asp:ListItem>
                        <asp:ListItem Text="18" Value="18"></asp:ListItem>
                        <asp:ListItem Text="24" Value="24"></asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div>
                    <asp:Button ID="btnInsuranceReport" runat="server" Text="<%$ Resources:Club, ReportUpdate %>" OnClick="btnInsuranceReport_Click" />
                </div>
                <asp:GridView ID="gvInsuranceReport" runat="server" AutoGenerateColumns="false" AlternatingRowStyle-BackColor="#EEEEEE" ShowFooter="false" CellPadding="4" GridLines="None" OnRowDataBound="gvInsuranceReport_RowDataBound">
                    <Columns>
                        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderPilotName %>" ItemStyle-VerticalAlign="Top" >
                            <ItemTemplate>
                                <asp:Label ID="lblName" runat="server" Font-Bold="true" Text='<%# ((MyFlightbook.Clubs.ClubInsuranceReportItem) Container.DataItem).User.UserFullName %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportHeaderInsurancePilotStatus %>" ItemStyle-VerticalAlign="Top" >
                            <ItemTemplate>
                                <asp:Repeater ID="rptPilotStatus" runat="server">
                                    <ItemTemplate>
                                        <div><asp:Label ID="lblTitle" runat="server" CssClass="currencylabel" Text='<%# Eval("Attribute") %>'></asp:Label>: <asp:Label ID="lblStatus" runat="server" CssClass='<%# CSSForItem((MyFlightbook.FlightCurrency.CurrencyState) Eval("Status")) %>' Text='<%# Eval("Value") %>'></asp:Label></div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceFlightsInPeriod %>" ItemStyle-Width="1.5cm" DataField="FlightsInInterval" ItemStyle-VerticalAlign="Top"  />
                        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceLastFlightInClubPlane %>" ItemStyle-Width="2cm" DataField="MostRecentFlight" DataFormatString="{0:d}" ItemStyle-VerticalAlign="Top"  />
                        <asp:BoundField HeaderText="<%$ Resources:Club, ReportHeaderInsuranceTotalTime %>" DataField="TotalTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
                        <asp:BoundField HeaderText="<%$ Resources:Club, ReportheaderInsuranceComplexTime %>" DataField="ComplexTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
                        <asp:BoundField HeaderText="<%$ Resources:Club, ReportheaderInsuranceHighPerformance %>" DataField="HighPerformanceTime" ItemStyle-Width="1.5cm" DataFormatString="{0:#,##0.0}" ItemStyle-VerticalAlign="Top" />
                        <asp:TemplateField HeaderText="<%$ Resources:Club, ReportheaderInsuranceTimeInClubAircraft %>" ItemStyle-VerticalAlign="Top" >
                            <ItemTemplate>
                                <asp:GridView ID="gvAircraftTime" runat="server" AutoGenerateColumns="false" ShowHeader="false" ShowFooter="false" GridLines="None" CellPadding="4">
                                    <Columns>
                                        <asp:BoundField DataField="key" ItemStyle-Font-Bold="true" />
                                        <asp:BoundField DataField="value" DataFormatString="{0:#,##0.0}" />
                                    </Columns>
                                </asp:GridView>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </ContentTemplate>
        </asp:TabPanel>
    </asp:TabContainer>
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
    <uc1:ViewClub ID="ViewClub1" runat="server" LinkToDetails="false" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

