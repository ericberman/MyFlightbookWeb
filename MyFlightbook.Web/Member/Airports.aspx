<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Codebehind="Airports.aspx.cs" Inherits="Member_Airports" culture="auto" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register src="../Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc1" %>
<%@ Register src="../Controls/mfbSearchForm.ascx" tagname="mfbSearchForm" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbTooltip.ascx" tagname="mfbTooltip" tagprefix="uc5" %>
<%@ Register src="../Controls/mfbQueryDescriptor.ascx" tagname="mfbQueryDescriptor" tagprefix="uc2" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Localize ID="locVAHeader" runat="server" Text="<%$ Resources:Airports, visitedAirportTitle %>"></asp:Localize>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <asp:MultiView ID="mvVisitedAirports" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwVisitedAirports" runat="server">
            <p>
                <asp:Label ID="lblNumAirports" runat="server" Font-Bold="True"></asp:Label>  
                &nbsp;<asp:Label ID="lblNote" runat="server" Font-Bold="True" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label> 
                <asp:Localize ID="locVANote" runat="server" Text="<%$ Resources:Airports, airportVisitedAirportsNote %>"></asp:Localize>
            </p>
            <div>
                <asp:Button ID="btnChangeQuery" runat="server" Text="<%$ Resources:LocalizedText, ChangeQuery %>" onclick="btnChangeQuery_Click" />
                <uc2:mfbQueryDescriptor ID="mfbQueryDescriptor1" runat="server" ShowEmptyFilter="true" OnQueryUpdated="mfbQueryDescriptor1_QueryUpdated" />
            </div>
            <asp:Panel ID="Panel1" runat="server" ScrollBars="Vertical" Height="300px">
                <asp:GridView ID="gvAirports" runat="server" AllowSorting="True" 
                    AutoGenerateColumns="False" EnableModelValidation="True" OnRowDataBound="gvAirports_DataBound"
                    EnableViewState="False"  BorderStyle="None" onsorting="gvAirports_Sorting"
                        CellPadding="3" GridLines="None">
                        <AlternatingRowStyle BackColor="#E0E0E0" />
                        <Columns>
                            <asp:TemplateField HeaderText="<%$ Resources:Airports, airportCode %>" SortExpression="Code">
                                <ItemTemplate>
                                    <asp:PlaceHolder ID="plcZoomCode" runat="server"></asp:PlaceHolder>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="FacilityName" HeaderText="<%$ Resources:Airports, airportName %>" SortExpression="FacilityName" />
                            <asp:TemplateField SortExpression="NumberOfVisits">
                                <HeaderTemplate>
                                    <asp:LinkButton ID="lnkVisits" runat="server" CommandArgument="NumberOfVisits" CommandName="Sort" Text="<%$ Resources:Airports, airportVisits %>"></asp:LinkButton>
                                    <span style="font-weight:normal"><uc5:mfbTooltip ID="mfbTooltip1" runat="server" BodyContent="<%$ Resources:Airports, vistedAirportsCountTip %>" /></span>
                                </HeaderTemplate>
                                <ItemTemplate>
                                    <%# Eval("NumberOfVisits") %>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="EarliestVisitDate" DataFormatString="{0:d}" HeaderText="<%$ Resources:Airports, airportEarliestVisit %>" SortExpression="EarliestVisitDate" />
                            <asp:BoundField DataField="LatestVisitDate" DataFormatString="{0:d}" HeaderText="<%$ Resources:Airports, airportLatestVisit %>" SortExpression="LatestVisitDate" />
                            <asp:HyperLinkField DataNavigateUrlFields="AllCodes" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx?ap={0}" ShowHeader="False" Text="<%$ Resources:Airports, airportViewFlights %>" />
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
                                    <asp:BoundField DataField="Code" HeaderText="<%$ Resources:Airports, airportCode %>" />
                                    <asp:BoundField DataField="FacilityName" HeaderText="<%$ Resources:Airports, airportName %>" />
                                    <asp:TemplateField HeaderText="<%$ Resources:Airports, airportType %>">
                                        <ItemTemplate>
                                            <asp:Label ID="lblFacilityType" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.FacilityType") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="<%$ Resources:Airports, airportLatitude %>">
                                        <ItemTemplate>
                                            <asp:Label ID="lblLat" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.LatLong.Latitude") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="<%$ Resources:Airports, airportLongitude %>">
                                        <ItemTemplate>
                                            <asp:Label ID="lblLon" runat="server" Text='<%# DataBinder.Eval(Container.DataItem, "Airport.LatLong.Longitude") %>'></asp:Label>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:BoundField DataField="NumberOfVisits" HeaderText="<%$ Resources:Airports, airportVisits %>" />
                                    <asp:BoundField DataField="EarliestVisitDate" DataFormatString="{0:d}" HeaderText="<%$ Resources:Airports, airportEarliestVisit %>" />
                                    <asp:BoundField DataField="LatestVisitDate" DataFormatString="{0:d}" HeaderText="<%$ Resources:Airports, airportLatestVisit %>" />
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
                                    <asp:Label ID="lblErr" runat="server" CssClass="error"></asp:Label>
                                    <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel2">
                                        <ProgressTemplate>
                                            <p>
                                                <asp:Label ID="lblComputing" runat="server" Font-Bold="True" Text="<%$ Resources:Airports, visitedAirportComputing %>"></asp:Label>
                                            </p>
                                            <p>
                                                <asp:Image ID="imgProgress" runat="server" ImageUrl="~/images/ajax-loader.gif" />
                                            </p>
                                        </ProgressTemplate>
                                    </asp:UpdateProgress>
                                    <asp:Panel ID="pnlDistanceResults" runat="server" EnableViewState="false" Visible="False">
                                        <asp:Label ID="lblDistanceEstimate" runat="server" Font-Bold="True"></asp:Label>
                                        <br />
                                        <asp:Label ID="lblNote2" runat="server" Font-Bold="True" Text="<%$ Resources:LocalizedText, Note %>"></asp:Label>
                                        <asp:Localize ID="locDistance" runat="server" Text="Estimate is based on airport-to-airport distance in the route of your flight or telemetry, if present."></asp:Localize>
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
        <asp:HyperLink ID="lnkZoomOut" runat="server" Text="<%$ Resources:Airports, MapZoomAllAirports %>"></asp:HyperLink>
    </p>
    <div style="width:100%;">
        <uc1:mfbGoogleMapManager ID="mfbGoogleMapManager1" runat="server" AllowResize="false" Height="400px" />
        <br />
        <br />
    </div>
 </asp:Content>

