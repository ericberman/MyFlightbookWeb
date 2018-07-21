<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="Aircraft.aspx.cs" Inherits="makes" Title="" %>
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
        <asp:HiddenField ID="hdnStatsFetched" runat="server" />
        <p>
            <asp:Button ID="btnAddNew" runat="server" Text="<%$ Resources:LocalizedText, MyAircraftAddAircraft %>" 
            onclick="btnAddNew_Click" />
            <% =Resources.Aircraft.ImportPrompt %>
            <asp:HyperLink ID="lnkImportAircraft" runat="server" Text="<%$ Resources:Aircraft, ImportTitle %>" NavigateUrl="~/Member/ImpAircraft.aspx"></asp:HyperLink>
        </p>
        <p>
            <asp:Panel ID="pnlDownload" runat="server">
                <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadCSV_Click">
                    <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                    <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_med.png" style="padding-right: 5px;" />
                    <asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:Aircraft, DownloadCSV %>"></asp:Localize>
                </asp:LinkButton>
            </asp:Panel>
        </p>
    </div>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <asp:UpdatePanel ID="UpdatePanel1" runat="server">
        <ContentTemplate>
            <div style="max-width:800px">
                <asp:GridView ID="gvAircraftToDownload" runat="server" AutoGenerateColumns="false" EnableViewState="false">
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
                        <asp:TemplateField HeaderText="Last Annual">
                            <ItemTemplate>
                                <asp:Label ID="lblAnnual" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastAnnual")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last VOR">
                            <ItemTemplate>
                                <asp:Label ID="lblVOR" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastVOR")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last Altimeter">
                            <ItemTemplate>
                                <asp:Label ID="lblAltimeter" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastAltimeter")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last Transponder">
                            <ItemTemplate>
                                <asp:Label ID="lblXPonder" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastTransponder")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last ELT">
                            <ItemTemplate>
                                <asp:Label ID="lblElt" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastELT")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last Pitot Static">
                            <ItemTemplate>
                                <asp:Label ID="lblPitot" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("LastStatic")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next Annual">
                            <ItemTemplate>
                                <asp:Label ID="lblAnnual" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextAnnual) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next VOR">
                            <ItemTemplate>
                                <asp:Label ID="lblVOR" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextVOR) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next Altimeter">
                            <ItemTemplate>
                                <asp:Label ID="lblAltimeter" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextAltimeter) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next Transponder">
                            <ItemTemplate>
                                <asp:Label ID="lblXPonder" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextTransponder) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next ELT">
                            <ItemTemplate>
                                <asp:Label ID="lblElt" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextELT) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Next Pitot Static">
                            <ItemTemplate>
                                <asp:Label ID="lblPitot" runat="server" Text='<%# FormatOptionalDate(((Aircraft) Container.DataItem).Maintenance.NextStatic) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Last100">
                            <ItemTemplate>
                                <asp:Label ID="lbl100" runat="server" Text='<%# Eval("Last100").FormatDecimal(false) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Oil Change">
                            <ItemTemplate>
                                <asp:Label ID="lblOil" runat="server" Text='<%# Eval("LastOilChange").FormatDecimal(false) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Engine Time">
                            <ItemTemplate>
                                <asp:Label ID="lblEngine" runat="server" Text='<%# Eval("LastNewEngine").FormatDecimal(false) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Registration Renewal Date">
                            <ItemTemplate>
                                <asp:Label ID="lblRegistration" runat="server" Text='<%# FormatOptionalDate((DateTime) Eval("RegistrationDue")) %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Frequently Used">
                            <ItemTemplate>
                                <asp:Label ID="lblFrequent" runat="server" Text='<%# (bool) Eval("HideFromSelection") ? string.Empty : "yes" %>'></asp:Label>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField HeaderText="Public Notes" DataField="PublicNotes" HtmlEncode="true" />
                        <asp:BoundField HeaderText="Private Notes" DataField="PrivateNotes" HtmlEncode="true" />
                    </Columns>
                </asp:GridView>
                <asp:Repeater ID="rptAircraftGroups" runat="server" OnItemDataBound="rptAircraftGroups_ItemDataBound">
                    <ItemTemplate>
                        <h2><asp:Label ID="lblGroupNames" runat="server" Text='<%# Eval("GroupTitle") %>'></asp:Label></h2>
                        <uc1:AircraftList runat="server" ID="AircraftList" OnAircraftDeleted="AircraftList_AircraftDeleted" OnFavoriteChanged="AircraftList_FavoriteChanged" />
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </ContentTemplate>
    </asp:UpdatePanel>
</asp:Content>
