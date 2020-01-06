<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_Airports" Title="Airports I've Visited" culture="auto" meta:resourcekey="PageResource1" Codebehind="Airports.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server"><asp:Localize ID="locVAHeader" runat="server" Text="Visited Airports" 
            meta:resourcekey="locVAHeaderResource1"></asp:Localize>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvVisitedAirports" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwVisitedAirports" runat="server">
            <p>
                <asp:Label ID="lblNumAirports" runat="server" Font-Bold="True" 
                    meta:resourcekey="lblNumAirportsResource1"></asp:Label>  
                &nbsp;<asp:Label ID="lblNote" runat="server" Font-Bold="True" 
                    Text="<%$ Resources:LocalizedText, Note %>" meta:resourcekey="lblNoteResource1"></asp:Label> 
                <asp:Localize ID="locVANote" runat="server" 
                    Text="you may have visited more airports than this; this is only the count of distinct 3- or 4-letter codes within the 'Route' field of flights in your account." 
                    meta:resourcekey="locVANoteResource1"></asp:Localize>
            </p>
            <div>
                <asp:Button ID="btnChangeQuery" runat="server" Text="Change Query..." 
                    onclick="btnChangeQuery_Click" 
                    meta:resourcekey="btnChangeQueryResource1" />
                <uc2:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
            </div>
            <asp:Panel ID="Panel1" runat="server" ScrollBars="Vertical" Height="300px" 
            meta:resourcekey="Panel1Resource1">
                <asp:GridView ID="gvAirports" runat="server" AllowSorting="True" 
                    AutoGenerateColumns="False" EnableModelValidation="True" OnRowDataBound="gvAirports_DataBound"
                    EnableViewState="False"  BorderStyle="None" onsorting="gvAirports_Sorting" onsorted="gvAirports_Sorted"
                        CellPadding="3" GridLines="None" 
                    meta:resourcekey="gvAirportsResource1">
                        <AlternatingRowStyle BackColor="#E0E0E0" />
                        <Columns>
                        <asp:TemplateField HeaderText="Facility" 
                            meta:resourceKey="TemplateFieldResource1" SortExpression="Code">
                                <ItemTemplate>
                                    <asp:PlaceHolder ID="plcZoomCode" runat="server"></asp:PlaceHolder>
                                </ItemTemplate>
                            </asp:TemplateField>
                        <asp:BoundField DataField="FacilityName" HeaderText="Facility Name" 
                            meta:resourceKey="BoundFieldResource1" SortExpression="FacilityName" />
                        <asp:TemplateField meta:resourceKey="TemplateFieldResource2" 
                            SortExpression="NumberOfVisits">
                                <HeaderTemplate>
                                <asp:LinkButton ID="lnkVisits" runat="server" CommandArgument="NumberOfVisits" 
                                    CommandName="Sort" meta:resourceKey="lnkVisitsResource1" Text="Visits"></asp:LinkButton>
                                <span style="font-weight:normal"><uc5:mfbTooltip ID="mfbTooltip1" runat="server" BodyContent="<%$ Resources:Airports, vistedAirportsCountTip %>" /></span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("NumberOfVisits") %>
                                </ItemTemplate>
                            </asp:TemplateField>
                        <asp:BoundField DataField="EarliestVisitDate" DataFormatString="{0:d}" 
                            HeaderText="Date of First Visit" meta:resourceKey="BoundFieldResource2" 
                            SortExpression="EarliestVisitDate" />
                        <asp:BoundField DataField="LatestVisitDate" DataFormatString="{0:d}" 
                            HeaderText="Date of Last Visit" meta:resourceKey="BoundFieldResource3" 
                            SortExpression="LatestVisitDate" />
                        <asp:HyperLinkField DataNavigateUrlFields="AllCodes" 
                            DataNavigateUrlFormatString="~/Member/LogbookNew.aspx?ap={0}" 
                            meta:resourceKey="HyperLinkFieldResource1" ShowHeader="False" 
                            Text="View Flights" />
                        </Columns>
                    <HeaderStyle HorizontalAlign="Center"></HeaderStyle>
                        <RowStyle VerticalAlign="Top" />
                    </asp:GridView>
                </asp:Panel>
                <asp:HiddenField ID="hdnLastSortDirection" runat="server" Value="0" />
                <asp:HiddenField ID="hdnLastSortExpression" runat="server" />
                <br />
            <table style="width:100%">
                <tr style="vertical-align:top;">
                    <td style="width:33%; padding: 3px;">
                        <asp:Panel ID="pnlDownload" runat="server">
                            <asp:LinkButton ID="lnkDownloadCSV" runat="server" OnClick="lnkDownloadAirports_Click">
                                <asp:Image ID="imgDownloadCSV" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                                <asp:Image ID="imgCSVIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/csvicon_med.png" style="padding-right: 5px;" />
                                <asp:Localize ID="locDownloadCSV" runat="server" Text="<%$ Resources:Airports, DownloadVisited %>"></asp:Localize>
                            </asp:LinkButton>
                            <asp:GridView ID="gvAirportsDownload" runat="server" AutoGenerateColumns="false" EnableViewState="false">
                                <Columns>
                                    <asp:BoundField DataField="Code" HeaderText="Facility" />
                                    <asp:BoundField DataField="FacilityName" HeaderText="Facility Name" />
                                    <asp:TemplateField HeaderText="Facility Type">
                                        <ItemTemplate>
                                            <asp:Label ID="lblFacilityType" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.FacilityType") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Latitude">
                                        <ItemTemplate>
                                            <asp:Label ID="lblLat" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.LatLong.Latitude") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Longitude">
                                        <ItemTemplate>
                                            <asp:Label ID="lblLon" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.LatLong.Longitude") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="NumberOfVisits" HeaderText="Visits" />
                                    <asp:BoundField DataField="EarliestVisitDate" DataFormatString="{0:d}" HeaderText="First Visit" />
                                    <asp:BoundField DataField="LatestVisitDate" DataFormatString="{0:d}" HeaderText="Last Visit" />
                                </Columns>
                            </asp:GridView>
                        </asp:Panel>
                    </td>
                    <td style="width:33%; padding: 3px;">
                        <asp:Panel ID="pnlViewGoogleEarth" runat="server">
                            <asp:LinkButton ID="lnkViewKML" runat="server" onclick="btnGetTotalKML">
                                <asp:Image ID="imgDownloadKML" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px;" />
                                <asp:Image ID="imgKMLIcon" ImageAlign="Middle" runat="server" ImageUrl="~/images/kmlicon_med.png" style="padding-right: 5px;" />
                                <div style="display:inline-block;vertical-align:middle"><asp:Localize ID="locViewGoogleEarth" runat="server" Text="<%$ Resources:Airports, DownloadKML %>"></asp:Localize><br /><asp:Label ID="locKMLSlow" runat="server" Text="<%$ Resources:Airports, WarningSlow %>"></asp:Label></div> 
                            </asp:LinkButton>
                        </asp:Panel>
                    </td>
                    <td style="width:33%; padding: 3px;">
                        <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                            <ContentTemplate>
                                <div>
                                    <asp:LinkButton ID="btnEstimateDistance" runat="server" EnableViewState="false" onclick="btnEstimateDistance_Click">
                                        <asp:Image ID="Image1" ImageUrl="~/images/download.png" runat="server" ImageAlign="Middle" style="padding-right: 5px; visibility:hidden;" />
                                        <asp:Image ID="Image2" ImageAlign="Middle" runat="server" ImageUrl="~/images/ruler.png" style="padding-right: 5px;" />
                                        <div style="display:inline-block;vertical-align:middle"><asp:Localize ID="locEstimateDistance" runat="server" Text="<%$ Resources:Airports, EstimateDistance %>"></asp:Localize><br /><asp:Label ID="lblSlow" runat="server" Text="<%$ Resources:Airports, WarningSlow %>"></asp:Label></div>
                                    </asp:LinkButton>
                                    <asp:Label ID="lblErr" runat="server" CssClass="error" meta:resourcekey="lblErrResource1"></asp:Label>
                                    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
                                        <ProgressTemplate>
                                            <p>
                                                <asp:Label ID="lblComputing" runat="server" Font-Bold="True" meta:resourcekey="lblComputingResource1" Text="Computing..."></asp:Label>
                                            </p>
                                            <p>
                                                <asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" />
                                            </p>
                                        </ProgressTemplate>
                                    </asp:UpdateProgress>
                                    <asp:Panel ID="pnlDistanceResults" runat="server" EnableViewState="false" meta:resourcekey="pnlDistanceResultsResource1" Visible="False">
                                        <asp:Label ID="lblDistanceEstimate" runat="server" Font-Bold="True" meta:resourcekey="lblDistanceEstimateResource1"></asp:Label>
                                        <br />
                                        <asp:Label ID="lblNote2" runat="server" Font-Bold="True" meta:resourcekey="lblNote2Resource1" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                                        <asp:Localize ID="locDistance" runat="server" meta:resourcekey="locDistanceResource1" Text="Estimate is based on airport-to-airport distance in the route of your flight or telemetry, if present."></asp:Localize>
                                    </asp:Panel>
                                </div>
                            </ContentTemplate>
                        </asp:UpdatePanel>
                    </td>
                </tr>
            </table>
        </asp:View>
        <asp:View ID="vwSearch" runat="server">
            <uc3:mfbSearchForm ID="mfbSearchForm1" runat="server" OnQuerySubmitted="ShowResults" OnReset="ClearForm" InitialCollapseState="true" />
        </asp:View>
    </asp:MultiView>
    <p>
        <asp:HyperLink ID="lnkZoomOut" runat="server" Text="Zoom to fit all airports" 
        meta:resourcekey="lnkZoomOutResource1"></asp:HyperLink>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
    </p>
    <div style="width:100%;">
        <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" AllowResize="false" Height="400px" />
        <br />
        <br />
    </div>
 </asp:Content>

