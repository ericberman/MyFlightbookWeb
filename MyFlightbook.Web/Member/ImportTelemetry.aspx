<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_ImportTelemetry" Codebehind="ImportTelemetry.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>
<%@ Register Src="~/Controls/mfbDecimalEdit.ascx" TagPrefix="uc1" TagName="mfbDecimalEdit" %>
<%@ Register Src="~/Controls/ClubControls/TimeZone.ascx" TagPrefix="uc1" TagName="TimeZone" %>

<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
    <asp:Label ID="lblTitle" runat="server" Text=""></asp:Label>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <h2><% =Resources.FlightData.ImportHeaderBulkUpload %></h2>
    <div><% =Resources.FlightData.ImportBulkUploadDescription %></div>
    <div>
        <uc1:TimeZone runat="server" ID="TimeZone1" AutoPostBack="true" />
    </div>
    <script>
        function refreshStatus(sender, e) {
            window.location = "<% = Request.Url.AbsoluteUri %>";
         }
    </script>
    <asp:AjaxFileUpload ID="AjaxFileUpload1" runat="server" OnClientUploadCompleteAll="refreshStatus" CssClass="mfbDefault"
            ThrobberID="myThrobber" MaximumNumberOfFiles="20" OnUploadComplete="AjaxFileUpload1_UploadComplete" />
    <asp:Image ID="myThrobber" ImageUrl="~/images/ajax-loader.gif" runat="server" style="display:None" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:GridView ID="gvResults" ShowFooter="false" ShowHeader="true" runat="server" ViewStateMode="Enabled" Width="100%" EnableViewState="true" GridLines="None" AutoGenerateColumns="false">
        <Columns>
            <asp:BoundField DataField="TelemetryFileName" HeaderText="Filename" ItemStyle-VerticalAlign="Top" HeaderStyle-HorizontalAlign="Left" />
            <asp:BoundField DataField="DateDisplay" HeaderText="Date" ItemStyle-VerticalAlign="Top" HeaderStyle-HorizontalAlign="Left" />
            <asp:TemplateField HeaderText="Status" HeaderStyle-HorizontalAlign="Left" >
                <ItemTemplate>
                    <div class='<%# Eval("CssClass") %>'><%# Eval("Status") %></div>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Matching Flight" HeaderStyle-HorizontalAlign="Left" >
                <ItemTemplate>
                    <asp:Panel ID="pnlMatch" runat="server" Visible='<%# Eval("Success") %>'>
                        <asp:HyperLink ID="lnkMatch" NavigateUrl='<%# Eval("MatchHREF") %>' Target="_blank" runat="server"><%# Eval("MatchedFlightDescription") %></asp:HyperLink>
                    </asp:Panel>
                </ItemTemplate>
                <ItemStyle VerticalAlign="Top" />
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>

