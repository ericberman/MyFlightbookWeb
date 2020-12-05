<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="ClubManage.aspx.cs" Inherits="Member_ClubManage" %>
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
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<%@ Register Src="~/Controls/ClubControls/FlyingReport.ascx" TagPrefix="uc1" TagName="FlyingReport" %>
<%@ Register Src="~/Controls/ClubControls/MaintenanceReport.ascx" TagPrefix="uc1" TagName="MaintenanceReport" %>
<%@ Register Src="~/Controls/ClubControls/InsuranceReport.ascx" TagPrefix="uc1" TagName="InsuranceReport" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblClubHeader" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <h2><asp:Label ID="lblManageheader" runat="server"></asp:Label></h2>
    <asp:HyperLink ID="lnkReturnToClub" runat="server" Text="<%$ Resources:Club, LabelReturnToClub %>"></asp:HyperLink>
    <asp:TabContainer ID="tabManage" runat="server" CssClass="mfbDefault">
        <asp:TabPanel ID="tabpanelMembers" runat="server" HeaderText="<%$ Resources:Club, TabClubMembers %>">
            <ContentTemplate>
                <asp:UpdatePanel ID="updMembers" runat="server">
                    <ContentTemplate>
                        <asp:Panel ID="pnlManageMembers" runat="server">
                            <asp:GridView ID="gvMembers" DataKeyNames="UserName" runat="server" AutoGenerateColumns="False" GridLines="None" AutoGenerateEditButton="True" OnRowCancelingEdit="gvMembers_RowCancelingEdit" 
                                OnRowDataBound="gvMembers_RowDataBound" Width="100%" CellPadding="3" OnRowCommand="gvMembers_RowCommand" OnRowUpdating="gvMembers_RowUpdating" OnRowEditing="gvMembers_RowEditing">
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
                                    <asp:TemplateField HeaderStyle-HorizontalAlign="Left">
                                        <ItemTemplate>
                                            <asp:Label runat="server" Visible='<%# !String.IsNullOrEmpty((string) Eval("Certificate")) %>' style="background-color:lightgrey; font-weight:bold; border-radius: 3px; padding: 3px; margin: 2px;"><%# Resources.Club.ClubStatusCFI %></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField HeaderText="<%$ Resources:Club, LabelMemberJoinDate %>" DataField="JoinedDate" HeaderStyle-HorizontalAlign="Left" ReadOnly="True" DataFormatString="{0:d}" />
                                    <asp:TemplateField HeaderStyle-HorizontalAlign="Left">
                                        <ItemTemplate>
                                            <%# Eval("DisplayRoleInClub") %> <%# Eval("ClubOffice") %>
                                        </ItemTemplate>
                                        <HeaderTemplate>
                                            <%# Resources.Club.LabelMemberRole %>
                                            <uc1:mfbTooltip runat="server" ID="mfbTooltip">
                                                <TooltipBody>
                                                    <div style="font-weight:normal; text-align:left">
                                                        <% =Resources.Club.ClubRolesDescription %>
                                                    </div>
                                                </TooltipBody>
                                            </uc1:mfbTooltip>
                                        </HeaderTemplate>
                                        <EditItemTemplate>
                                            <div>
                                                <asp:RadioButtonList ID="rblRole" RepeatDirection="Vertical" runat="server">
                                                    <asp:ListItem Text="<%$ Resources:Club, RoleMember %>" Value="Member"></asp:ListItem>
                                                    <asp:ListItem Text="<%$ Resources:Club, RoleManager %>" Value="Admin"></asp:ListItem>
                                                    <asp:ListItem Text="<%$ Resources:Club, RoleOwner %>" Value="Owner"></asp:ListItem>
                                                </asp:RadioButtonList>
                                            </div>
                                            <div><asp:CheckBox ID="ckMaintenanceOfficer" runat="server" Text="<%$ Resources:Club, RoleMaintenanceOfficer %>" /></div>
                                            <div><asp:CheckBox ID="ckTreasurer" runat="server" Text="<%$ Resources:Club, RoleTreasurer %>" /></div>
                                            <div><asp:CheckBox ID="ckInsuranceOfficer" runat="server" Text="<%$ Resources:Club, RoleInsuranceOfficer %>" /></div>
                                            <div><asp:Label ID="lblOffice" runat="server" Text="<%$ Resources:Club, RoleOfficeHeld %>" /> <asp:TextBox ID="txtOffice" runat="server" /></div>
                                        </EditItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="MobilePhone" HeaderText="<%$ Resources:Club, ClubStatusContact %>" HeaderStyle-HorizontalAlign="Left" ReadOnly="true" />
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
                </asp:UpdatePanel>
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
                <uc1:ViewClub ID="vcEdit" runat="server" DefaultMode="Edit" OnClubChanged="vcEdit_ClubChanged" OnClubDeleted="btnDeleteClub_Click" ShowCancel="false" OnClubChangeCanceled="vcEdit_ClubChangeCanceled" />
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
                            <td><% =Resources.Club.ClubMembersForReport %></td>
                            <td>
                                <asp:DropDownList ID="cmbClubMembers" runat="server" AppendDataBoundItems="true" DataTextField="UserFullName" DataValueField="UserName">
                                    <asp:ListItem Selected="True" Text="<%$ Resources:Club, AllClubMembers %>" Value=""></asp:ListItem>
                                </asp:DropDownList>
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
                            <td><% =Resources.Club.ClubAircraftForReport %></td>
                            <td>
                                <asp:DropDownList ID="cmbClubAircraft" runat="server" AppendDataBoundItems="true" DataTextField="DisplayTailnumber" DataValueField="AircraftID">
                                    <asp:ListItem Selected="True" Text="<%$ Resources:Club, AllClubAircraft %>" Value="-1"></asp:ListItem>
                                </asp:DropDownList>
                            </td>
                        </tr>

                        <tr>
                            <td colspan="5">
                                <asp:Button ID="btnUpdate" OnClick="btnUpdate_Click" runat="server" Text="<%$ Resources:Club, ReportUpdate %>" />
                                <asp:Button ID="btnDownload" OnClick="btnDownload_Click" runat="server" Text="<%$ Resources:Club, ReportDownload %>" Visible="false" />
                            </td>
                        </tr>
                    </table>
                </asp:Panel>
                <asp:Panel ID="pnlReport" runat="server" ScrollBars="Auto">
                    <uc1:FlyingReport runat="server" ID="FlyingReport" />
                </asp:Panel>
                <h3><% =Resources.Club.ReportHeaderMaintenance %></h3>
                <div>
                    <asp:Button ID="btnUpdateMaintenance" runat="server" Text="<%$ Resources:Club, ReportUpdate %>" OnClick="btnUpdateMaintenance_Click" />
                    <asp:Button ID="btnDownloadMaintenance" runat="server" Text="<%$ Resources:Club, ReportDownload %>" Visible="false" OnClick="btnDownloadMaintenance_Click" />
                </div>
                <asp:Panel ID="pnlMaintenanceReport" runat="server" ScrollBars="Auto" >
                    <uc1:MaintenanceReport runat="server" ID="MaintenanceReport" />
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
                <uc1:InsuranceReport runat="server" ID="InsuranceReport" />
            </ContentTemplate>
        </asp:TabPanel>
    </asp:TabContainer>
    <asp:Label ID="lblErr" runat="server" CssClass="error" EnableViewState="False"></asp:Label>
    <uc1:ViewClub ID="ViewClub1" runat="server" LinkToDetails="false" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
</asp:Content>

