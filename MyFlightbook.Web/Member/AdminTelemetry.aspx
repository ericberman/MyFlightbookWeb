<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_AdminTelemetry" Codebehind="AdminTelemetry.aspx.cs" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbGoogleMapManager.ascx" tagname="mfbGoogleMapManager" tagprefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpPageTitle" Runat="Server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cpTopForm" Runat="Server">
    <asp:Panel ID="pnlBulkMigrate" runat="server">
        <asp:UpdatePanel ID="UpdatePanel1" runat="server">
            <ContentTemplate>
                <h2>Migration between database and files</h2>
                <asp:TextBox ID="txtMaxFiles" runat="server" TextMode="Number" Text="50"></asp:TextBox> <asp:CheckBox ID="ckLimitUsername" runat="server" Text="Restrict to user (below)" /><br />
                <cc1:TextBoxWatermarkExtender ID="txtMaxFiles_TextBoxWatermarkExtender" runat="server" BehaviorID="txtMaxFiles_TextBoxWatermarkExtender" TargetControlID="txtMaxFiles" WatermarkCssClass="watermark" WatermarkText="(Limit)" />
                <asp:Button ID="btnMigrateFromDB" runat="server" Text="Migrate from DATABASE to FILES" OnClick="btnMigrateFromDB_Click" /> <asp:Button ID="btnMigrateFromFiles" runat="server" Text="Migrate from FILES to DATABASE" OnClick="btnMigrateFromFiles_Click" />
                <asp:Label ID="lblMigrateStatus" runat="server" Text="" EnableViewState="false"></asp:Label>
                <br />
                <h2>Synchronization, data integrity</h2>
                <div><asp:Button ID="btnFindDupeTelemetry" runat="server" Text="Find flights in both DB and disk" OnClick="btnFindDupeTelemetry_Click" />
                    <asp:Button ID="btnFindOrphanedRefs" runat="server" Text="Find orphaned references" OnClick="btnFindOrphanedRefs_Click" />
                    <asp:Button ID="btnFindOrphanedFiles" runat="server" Text="Find orphaned files" OnClick="btnFindOrphanedFiles_Click" /></div>
                <br />
                <asp:GridView ID="gvSetManagement" runat="server" AutoGenerateColumns="true">
                    <EmptyDataTemplate>
                        <p>No issues found!</p>
                    </EmptyDataTemplate>
                </asp:GridView>
            </ContentTemplate>
        </asp:UpdatePanel>
        <asp:UpdateProgress ID="UpdateProgress1" runat="server" AssociatedUpdatePanelID="UpdatePanel1">
            <ProgressTemplate>
                <asp:Image ID="imgProgress" ImageUrl="~/images/ajax-loader.gif" runat="server" />
            </ProgressTemplate>
        </asp:UpdateProgress>
    </asp:Panel>
    <h2>View telemetry for user</h2>
    <asp:Panel ID="pnlUser" runat="server" DefaultButton="btnRefresh">
        <asp:TextBox ID="txtUser" runat="server"></asp:TextBox>
        <cc1:TextBoxWatermarkExtender ID="txtUser_TextBoxWatermarkExtender" runat="server" BehaviorID="txtUser_TextBoxWatermarkExtender" TargetControlID="txtUser" WatermarkCssClass="watermark" WatermarkText="(Username)" />
        <asp:Button ID="btnRefresh" runat="server" Text="Refresh" OnClick="btnRefresh_Click" />    <asp:Label ID="lblErr" CssClass="error" EnableViewState="false" runat="server" Text=""></asp:Label>
    </asp:Panel>
    <asp:Panel ID="pnlMaps" Visible="false" runat="server" Width="400px" style="margin-top: 30px; margin-bottom: 30px">
        <div>Raw:</div>
        <div><uc1:mfbGoogleMapManager ID="mfbGMapStraight" runat="server" /></div>
        <div>Compressed/uncompressed:</div>
        <div><uc1:mfbGoogleMapManager ID="mfbGMapReconstituded" runat="server" /></div>
    </asp:Panel>
    <h2>Antipodes (just for fun)</h2>
    <p>Upload a telemetry file and view it side-by-side with its antipodes</p>
    <asp:FileUpload ID="FileUpload1" runat="server" />
    <asp:Button ID="btnViewAntipodes" runat="server" Text="View Antipodes" OnClick="btnViewAntipodes_Click" />

    <cc1:AlwaysVisibleControlExtender ID="AlwaysVisibleControlExtender1" runat="server" TargetControlID="pnlMaps" HorizontalSide="Right" VerticalSide="Middle" />
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cpMain" Runat="Server">
    <asp:GridView ID="gvData" OnRowCommand="gvData_RowCommand" AutoGenerateColumns="false" runat="server">
        <Columns>
            <asp:ButtonField ButtonType="Link" DataTextField="FlightID" CommandName="MapEm" HeaderText="FlightID" />
            <asp:BoundField DataField="Uncompressed" DataFormatString="{0:#,###}" HeaderText="Uncompressed size" ItemStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="Compressed" DataFormatString="{0:#,###}" HeaderText="Compressed size" ItemStyle-HorizontalAlign="Right"/>
            <asp:BoundField DataField="GoogleDataSize" DataFormatString="{0:#,###}" HeaderText="Google size" ItemStyle-HorizontalAlign="Right"/>
            <asp:BoundField DataField="CachedDistance" DataFormatString="{0:#,###.0nm}" HeaderText="Distance" ItemStyle-HorizontalAlign="Right"/>
            <asp:TemplateField>
                <ItemTemplate>
                    <%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:#.0}%", 100.0 * Convert.ToInt32(Eval("Compressed")) / Convert.ToInt32(Eval("Uncompressed"))) %> / 
                    <%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:#.0}%", 100.0 * Convert.ToInt32(Eval("GoogleDataSize")) / Convert.ToInt32(Eval("Uncompressed"))) %> / 
                    <%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:#.0}%", 100.0 * Convert.ToInt32(Eval("GoogleDataSize")) / Convert.ToInt32(Eval("Compressed"))) %>
                </ItemTemplate>
                <HeaderTemplate>
                    Compression ratios <br />
                    (C/U, G/U, G/C)
                </HeaderTemplate>
            </asp:TemplateField>
            <asp:ButtonField ButtonType="Button" Text="DB to Disk" CommandName="FromFlights" />
            <asp:ButtonField ButtonType="Button" Text="Disk to DB" CommandName="ToFlights" />
        </Columns>
    </asp:GridView>
</asp:Content>

