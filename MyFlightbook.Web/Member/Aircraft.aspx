<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_Aircraft" Title="" Codebehind="Aircraft.aspx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register Src="../Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/popmenu.ascx" tagname="popmenu" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbHoverImageList.ascx" tagname="mfbHoverImageList" tagprefix="uc3" %>
<%@ Register Src="~/Controls/AircraftControls/AircraftList.ascx" TagPrefix="uc1" TagName="AircraftList" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="Localize2" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftHeader %>"></asp:Localize> <asp:Label ID="lblAdminMode" runat="server" Text=" - ADMIN MODE (No images or delete)" Visible="false"></asp:Label></asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <div>
        <p><% =Resources.LocalizedText.MyAircraftDesc %></p>
        <asp:Panel ID="pnlGrouping" runat="server">
            <% =Resources.Aircraft.ViewAircraftPrompt %>
            <asp:DropDownList ID="cmbAircraftGrouping" runat="server" AutoPostBack="true" OnSelectedIndexChanged="cmbAircraftGrouping_SelectedIndexChanged">
                <asp:ListItem Selected="True" Text="<%$ Resources:Aircraft, ViewAircraftActive %>" Value="Activity"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftAll %>" Value="All"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftModel %>" Value="Model"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftICAO %>" Value="ICAO"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftCategoryClass %>" Value="CategoryClass"></asp:ListItem>
                <asp:ListItem Text="<%$ Resources:Aircraft, ViewAircraftRecency %>" Value="Recency"></asp:ListItem>
            </asp:DropDownList>
        </asp:Panel>
        <p>
            <asp:Button ID="btnAddNew" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftAddAircraft %>" 
            onclick="btnAddNew_Click" />
            <% =Resources.Aircraft.ImportPrompt %>
            <asp:HyperLink ID="lnkImportAircraft" runat="server" Text="<%$ Resources:Aircraft, ImportTitle %>" NavigateUrl="~/Member/ImpAircraft.aspx"></asp:HyperLink>
        </p>
        <asp:Panel ID="pnlDownload" runat="server">
            <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click">
                <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" style="padding-right: 5px; vertical-align:middle;" />
                <asp:Image ID="imgCSVIcon" runat="server" ImageUrl="~/images/csvicon_med.png" style="padding-right: 5px; vertical-align: middle" />
                <asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:Aircraft, DownloadCSV %>"></asp:Localize>
            </asp:LinkButton>
        </asp:Panel>
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
                                <asp:Label ID="lblMan" runat="server" Text='<%# MakeModel.GetModel((int) Eval("ModelID")).ManufacturerDisplay %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField HeaderText="Model" DataField="ModelDescription" />
                        <asp:TemplateField HeaderText="Type designation">
                            <ItemTemplate>
                                <asp:Label ID="lblModel" runat="server" Text='<%# MakeModel.GetModel((int) Eval("ModelID")).TypeName %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Model full name">
                            <ItemTemplate>
                                <asp:Label ID="lblFullName" runat="server" Text='<%# MakeModel.GetModel((int) Eval("ModelID")).ModelDisplayNameNoCatclass %>'></asp:Label>
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
                                <asp:Label ID="lblDeadlines" runat="server" Text='<%# MyFlightbook.FlightCurrency.DeadlineCurrency.CoalescedDeadlinesForAircraft(Page.User.Identity.Name, (int) Eval("AircraftID")) %>' />
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
                        <h2><asp:Label ID="lblGroupNames" runat="server" Text='<%# Eval("GroupTitle") %>'></asp:Label></h2>
                        <uc1:AircraftList runat="server" ID="AircraftList" OnAircraftDeleted="AircraftList_AircraftDeleted" OnFavoriteChanged="AircraftList_FavoriteChanged" OnAircraftPrefChanged="AircraftList_AircraftPrefChanged" />
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
