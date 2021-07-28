<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" CodeBehind="AdminAirportGeocoder.aspx.cs" Inherits="MyFlightbook.Web.Admin.AdminAirportGeocoder" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbGoogleMapManager.ascx" TagPrefix="uc1" TagName="mfbGoogleMapManager" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Reverse Geocode airports
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
     <script> 
//<![CDATA[
        function clickAndZoom(point) 
        {
            getGMap().setCenter(point);
            getGMap().setZoom(10);
        }
//]]>
        </script>
    <h2>Review airport locations</h2>
    <uc1:mfbGoogleMapManager runat="server" ID="mfbGoogleMapManager" AllowResize="false" Height="400px" Width="100%" ShowRoute="false" />
    <h1>Verify airport countries/admin regions</h1>
    <div>
        <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnViewMore">
            View airports matching:
            <table>
                <tr>
                    <td>Country</td>
                    <td>
                        <asp:TextBox ID="txtCountry" runat="server" /> <asp:CheckBox ID="ckNoCountry" runat="server" Text="(Missing)" />
                        <cc1:AutoCompleteExtender ID="aceCountries" runat="server"
                            CompletionInterval="100" CompletionListCssClass="AutoExtender"
                            CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                            CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                            Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestCountries"
                            ServicePath="~/Admin/AdminAirportGeocoder.aspx" TargetControlID="txtCountry" CompletionSetCount="20">
                        </cc1:AutoCompleteExtender>
                    </td>
                </tr>
                <tr>
                    <td>Admin (State/province)</td>
                    <td>
                        <asp:TextBox ID="txtAdmin" runat="server" /> <asp:CheckBox ID="ckNoAdmin1" runat="server" Text="(Missing)" />
                        <cc1:AutoCompleteExtender ID="aceAdmin1" runat="server"
                            CompletionInterval="100" CompletionListCssClass="AutoExtender"
                            CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                            CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                            Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestAdmin"
                            ServicePath="~/Admin/AdminAirportGeocoder.aspx" TargetControlID="txtAdmin" CompletionSetCount="20">
                        </cc1:AutoCompleteExtender>
                    </td>
                </tr>
                <tr>
                    <td>Start</td>
                    <td>
                        <uc1:mfbDecimalEdit runat="server" ID="decStart" DefaultValueInt="0" EditingMode="Integer" IntValue="0" />
                    </td>
                </tr>
                <tr>
                    <td>Limit</td>
                    <td>
                        <uc1:mfbDecimalEdit runat="server" ID="decMaxAirports" DefaultValueInt="200" EditingMode="Integer" IntValue="250" />
                    </td>
                </tr>
            </table>
            <div>
                <asp:Button ID="btnViewMore" runat="server" Text="Get Next Set" OnClick="btnViewMore_Click" />
                <asp:Label ID="lblCurrentRange" runat="server" />
            </div>
        </asp:Panel>
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <asp:GridView ID="gvEdit" runat="server" AutoGenerateColumns="false" AllowPaging="true" PageSize="15" OnRowDataBound="gvEdit_RowDataBound" OnRowCancelingEdit="gvEdit_RowCancelingEdit" AutoGenerateEditButton="true"
                    OnRowEditing="gvEdit_RowEditing" OnRowUpdating="gvEdit_RowUpdating" DataKeyNames="Code,FacilityTypeCode" CellPadding="3"
                    OnPageIndexChanging="gvEdit_PageIndexChanging" >
                    <Columns>
                        <asp:TemplateField HeaderText="Code" SortExpression="AirportID">
                            <ItemTemplate>
                                <asp:HyperLink ID="lnkZoomCode" runat="server" 
                                    Text='<%# Bind("Code") %>' meta:resourcekey="lnkZoomCodeResource1" ></asp:HyperLink>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="FacilityTypeCode" ReadOnly="true" HeaderText="Type" />
                        <asp:BoundField DataField="Name" ReadOnly="true" HeaderText="Name" />
                        <asp:BoundField DataField="UserName" ReadOnly="true" />
                        <asp:TemplateField HeaderText="Country">
                            <ItemTemplate>
                                <asp:Label ID="lblCountry" runat="server" Text='<%# Bind("Country") %>' />
                            </ItemTemplate>
                            <EditItemTemplate>
                                <asp:TextBox ID="txtAdm0" runat="server" Text='<%# Bind("Country") %>' AutoCompleteType="None" />
                                <cc1:AutoCompleteExtender ID="aceEditCntry" runat="server"
                                    CompletionInterval="100" CompletionListCssClass="AutoExtender"
                                    CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                                    CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                                    Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestCountries"
                                    ServicePath="~/Admin/AdminAirportGeocoder.aspx" TargetControlID="txtAdm0" CompletionSetCount="20">
                                </cc1:AutoCompleteExtender>
                            </EditItemTemplate>
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Admin1">
                            <ItemTemplate>
                                <asp:Label ID="lblAdmin" runat="server" Text='<%# Bind("Admin1") %>' />
                            </ItemTemplate>
                            <EditItemTemplate>
                                <asp:TextBox ID="txtEditAdmin1" runat="server" Text='<%# Bind("Admin1") %>' />
                                <cc1:AutoCompleteExtender ID="aceEditAdmn" runat="server"
                                    CompletionInterval="100" CompletionListCssClass="AutoExtender"
                                    CompletionListHighlightedItemCssClass="AutoExtenderHighlight"
                                    CompletionListItemCssClass="AutoExtenderList" DelimiterCharacters=""
                                    Enabled="True" MinimumPrefixLength="2" ServiceMethod="SuggestAdmin"
                                    ServicePath="~/Admin/AdminAirportGeocoder.aspx" TargetControlID="txtEditAdmin1" CompletionSetCount="20">
                                </cc1:AutoCompleteExtender>
                            </EditItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
                <asp:Label ID="lerr" runat="server" CssClass="error" EnableViewState="false" />
            </ContentTemplate>
        </asp:UpdatePanel>
    </div>
    <asp:Panel ID="pnlImportGPX" runat="server" Visible="false">
        <h1>Geolocate based on KML</h1>
        <p>THIS IS HIDDEN BECAUSE IT IS DANGEROUS - ONLY USE ON LOCAL MACHINE WITH LOCAL DATABASE</p>
        <p><asp:FileUpload ID="fuGPX" runat="server" /> <asp:Button ID="btnLocate" runat="server" Text="GeoLocate" OnClick="btnLocate_Click" /></p>
        <div><asp:Label ID="lblAudit" EnableViewState="false" runat="server" style="white-space:pre" /></div>
        <div><asp:Label ID="lblCommands" EnableViewState="false" runat="server" style="white-space:pre" /></div>
    </asp:Panel>
</asp:Content>

