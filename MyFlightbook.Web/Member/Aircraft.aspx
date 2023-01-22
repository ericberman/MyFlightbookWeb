<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    Codebehind="Aircraft.aspx.cs" Inherits="MyFlightbook.MemberPages.MyAircraft" Title="" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/popmenu.ascx" tagname="popmenu" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbHoverImageList.ascx" tagname="mfbHoverImageList" tagprefix="uc3" %>
<%@ Register Src="~/Controls/AircraftControls/AircraftList.ascx" TagPrefix="uc1" TagName="AircraftList" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="Localize2" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftHeader %>"></asp:Localize> <asp:Label ID="lblAdminMode" runat="server" Text=" - ADMIN MODE (No images or delete)" Visible="false"></asp:Label></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div style="width: 100%;">
        <p><% =Resources.LocalizedText.MyAircraftDesc %></p>
        <div class="calloutSmall shadowed" style="vertical-align:middle; height: 20pt; line-height: 20pt; width: 90%; display:flex; margin-right: auto; margin-left: auto; margin-bottom: 5pt; font-weight:bold;" >
            <asp:HyperLink style="flex:auto;" ID="btnAddNew" runat="server" NavigateUrl="~/Member/EditAircraft.aspx?id=-1">
                <asp:Image ID="imgAdd" runat="server" ImageUrl="~/images/add.png" style="margin-right: 15px; margin-left: 0px; vertical-align:middle;" />
                <asp:Label ID="lblAdd" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftAddAircraft %>" />
            </asp:HyperLink>
            <asp:HyperLink style="flex:auto; text-align:center;" ID="lnkImportAircraft" runat="server" NavigateUrl="~/Member/ImpAircraft.aspx">
                <asp:Image ID="imgImport" runat="server" ImageUrl="~/images/import.png" style="margin-right: 5px; vertical-align:middle;" />
                <asp:Label ID="lblImport" runat="server" Text="<%$ Resources:Aircraft, ImportTitle %>" />
            </asp:HyperLink>
            <asp:LinkButton style="flex:auto; text-align: right" ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click">
                <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="margin-right: 15px; margin-left:5px; vertical-align:middle;" />
                <asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:Aircraft, DownloadCSV %>"></asp:Localize>
            </asp:LinkButton>
        </div>
        <div style="margin-top: 15pt; margin-bottom: 15pt; text-align:center;">
            <% =Resources.Aircraft.ViewAircraftPrompt %>
            <asp:DropDownList ID="cmbAircraftGrouping" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbAircraftGrouping_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:Aircraft, ViewAircraftActive %>" Value="Activity"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftAll %>" Value="All"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftModel %>" Value="Model"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftICAO %>" Value="ICAO"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftCategoryClass %>" Value="CategoryClass"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftRecency %>" Value="Recency"></asp:ListItem>
            </asp:DropDownList>
            <asp:Label ID="lblNumAircraft" runat="server" />
        </div>
    </div>
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <asp:HiddenField ID="hdnStatsFetched" runat="server" />
            <div style="max-width:800px">
                <asp:GridView ID="gvAircraftToDownload" runat="server" AutoGenerateColumns="false" Visible="false" EnableViewState="false">
                    <Columns>
                        <asp:BoundField HeaderText="Aircraft ID" DataField="AircraftID" />
                        <asp:BoundField HeaderText="Tail Number" DataField="TailNumber" />
                        <asp:BoundField HeaderText="Display Tail Number" DataField="DisplayTailNumber" />
                        <asp:BoundField HeaderText="Training Device Kind" DataField="InstanceTypeDescription" />
                        <asp:BoundField HeaderText="Category/Class" DataField="CategoryClassDisplay" />
                        <asp:TemplateField HeaderText="Manufacturer">
                            <ItemTemplate>
                                <asp:Label ID="lblMan" runat="server" Text='<%#: MakeModel.GetModel((int) Eval("ModelID")).ManufacturerDisplay %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField HeaderText="Model" DataField="ModelDescription" />
                        <asp:TemplateField HeaderText="Type designation">
                            <ItemTemplate>
                                <asp:Label ID="lblModel" runat="server" Text='<%#: MakeModel.GetModel((int) Eval("ModelID")).TypeName %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Model full name">
                            <ItemTemplate>
                                <asp:Label ID="lblFullName" runat="server" Text='<%#: MakeModel.GetModel((int) Eval("ModelID")).ModelDisplayNameNoCatclass %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnual %>">
                            <ItemTemplate>
                                <asp:Label ID="lblAnnual" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastAnnual")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAnnualDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblAnnual" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextAnnual) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponder %>">
                            <ItemTemplate>
                                <asp:Label ID="lblXPonder" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastTransponder")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceTransponderDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblXPonderDue" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextTransponder) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStatic %>">
                            <ItemTemplate>
                                <asp:Label ID="lblPitot" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastStatic")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenancePitotStaticDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblPitotDue" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextStatic) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeter %>">
                            <ItemTemplate>
                                <asp:Label ID="lblAltimeter" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastAltimeter")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceAltimeterDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblAltimeterDue" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextAltimeter) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELT %>">
                            <ItemTemplate>
                                <asp:Label ID="lblElt" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastELT")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceELTDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblEltDue" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextELT) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVOR %>">
                            <ItemTemplate>
                                <asp:Label ID="lblVOR" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastVOR")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceVORDue %>">
                            <ItemTemplate>
                                <asp:Label ID="lblVORDue" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextVOR) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100 %>">
                            <ItemTemplate>
                                <asp:Label ID="lbl100" runat="server" Text='<%# ((Aircraft) Container.DataItem).Maintenance.Last100.FormatDecimal(false) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, Maintenance100Due %>">
                            <ItemTemplate>
                                <asp:Label ID="lblNext100" runat="server" Text='<%# ValueString(Eval("Maintenance.Next100")) %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOil %>">
                            <ItemTemplate>
                                <asp:Label ID="lblLastOil" runat="server" Text='<%# ValueString (Eval("LastOilChange")) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue25 %>">
                            <ItemTemplate>
                                <asp:Label ID="lblOil25" runat="server" Text='<%# ValueString (Eval("LastOilChange"), 25) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue50 %>">
                            <ItemTemplate>
                                <asp:Label ID="lblOil50" runat="server" Text='<%# ValueString (Eval("LastOilChange"), 50) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceOilDue100 %>">
                            <ItemTemplate>
                                <asp:Label ID="lblOil100" runat="server" Text='<%# ValueString (Eval("LastOilChange"), 100) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceEngine %>">
                            <ItemTemplate>
                                <asp:Label ID="lblLastEngine" runat="server" Text='<%# ValueString (Eval("LastNewEngine")) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Aircraft, MaintenanceRegistration %>">
                            <ItemTemplate>
                                <asp:Label ID="lblRegistration" runat="server" Text='<%# ValueString (Eval("RegistrationDue")) %>' /></ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="<%$ Resources:Currency, deadlinesHeaderDeadlines %>">
                            <ItemTemplate>
                                <asp:Label ID="lblDeadlines" runat="server" Text='<%#: MyFlightbook.Currency.DeadlineCurrency.CoalescedDeadlinesForAircraft(Page.User.Identity.Name, (int) Eval("AircraftID")) %>' />
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Frequently Used">
                            <ItemTemplate>
                                <asp:Label ID="lblFrequent" runat="server" Text='<%# (bool) Eval("HideFromSelection") ? string.Empty : "yes" %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField HeaderText="Public Notes" DataField="PublicNotes" HtmlEncode="true" />
                        <asp:BoundField HeaderText="Private Notes" DataField="PrivateNotes" HtmlEncode="true" />
                        <asp:BoundField HeaderText="FlightCount" DataField="Stats.UserFlights" />
                        <asp:BoundField HeaderText="Hours" DataField="Stats.Hours" DataFormatString="{0:0.0#}" />
                        <asp:TemplateField HeaderText="First Flight">
                            <ItemTemplate>
                                <asp:Label ID="lblFirstFlight" runat="server" Text='<%# ((Aircraft) Container.DataItem).Stats.EarliestDate.HasValue ? ((Aircraft) Container.DataItem).Stats.EarliestDate.Value.ToShortDateString() : string.Empty %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last Flight">
                            <ItemTemplate>
                                <asp:Label ID="lblLast" runat="server" Text='<%# ((Aircraft) Container.DataItem).Stats.LatestDate.HasValue ? ((Aircraft) Container.DataItem).Stats.LatestDate.Value.ToShortDateString() : string.Empty %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:Repeater ID="rptAircraftGroups" runat="server" OnItemDataBound="rptAircraftGroups_ItemDataBound">
                    <ItemTemplate>
                        <h2><asp:Label ID="lblGroupNames" runat="server" Text='<%# Eval("GroupTitle") %>' /> 
                            <asp:Label ID="lblGroupCount" runat="server" style="font-size: smaller" 
                                Text='<%# String.IsNullOrEmpty((string) Eval("GroupTitle")) ? string.Empty : String.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Aircraft.MyAircraftAircraftGroupCount, ((IEnumerable<Aircraft>)Eval("MatchingAircraft")).Count()) %>' /></h2>
                        <uc1:AircraftList runat="server" ID="AircraftList" OnAircraftDeleted="AircraftList_AircraftDeleted" OnMigrateAircraft="AircraftList_MigrateAircraft" />
                    </ItemTemplate>
                </asp:Repeater>
            </div>
            <asp:Panel ID="pnlMigrate" runat="server"  DefaultButton="btnMigrate" style="display:none;" Visible="false">
                <div style="max-width: 450px">
                    <div><asp:Label ID="lblMigrate" runat="server" /></div>
                    <div style="margin-left:auto; margin-right: auto;">
                        <div><asp:DropDownList ID="cmbMigr" runat="server" DataTextField="DisplayTailnumber" DataValueField="AircraftID" /></div>
                        <div><asp:CheckBox ID="ckDelAfterMigr" runat="server" Text="<%$ Resources:Aircraft, editAircraftRemoveAfterMigrate %>" /></div>
                    </div>
                    <div style="margin-top: 5px; text-align:center;">
                        <asp:Button ID="btnMigrate" runat="server" Text="<%$ Resources:Aircraft, editAircraftMigrateReplace %>" OnClick="btnMigrate_Click" />&nbsp;&nbsp;
                    </div>
                    <asp:HiddenField ID="hdnMigSrc" runat="server" />
                    <cc1:ConfirmButtonExtender ID="cbeMig" TargetControlID="btnMigrate" ConfirmText="<%$ Resources:Aircraft, editAircraftMigrateConfirm %>" runat="server" />
                </div>
            </asp:Panel>
            <script language="javascript" type="text/javascript">
                function pageLoad() {
                    $(function () {
                        if (document.getElementById('<%=pnlMigrate.ClientID %>'))
                            showModalById('<%=pnlMigrate.ClientID %>', '<% =Resources.Aircraft.editAircraftMigrate %>', 450);
                    });
                }
            </script>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">

</asp:Content>
