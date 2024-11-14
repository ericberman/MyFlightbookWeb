<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Trace="false"
    Inherits="MyFlightbook.Web.Member.EditAircraft" Title="" Codebehind="EditAircraft.aspx.cs" %>
<%@ Register Src="../Controls/mfbEditAircraft.ascx" TagName="mfbEditAircraft" TagPrefix="uc1" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<asp:Content ID="ContentHead" ContentPlaceHolderID="cpPageTitle" runat="server">
    <asp:Label ID="lblAddEdit1" runat="server" Text="Label"></asp:Label>
    <asp:Localize ID="locAircraftHeader" Text="<%$ Resources:Aircraft, AircraftHeader %>" runat="server"></asp:Localize> 
    <asp:Label ID="lblTail" runat="server" Text=""></asp:Label> <asp:Label ID="lblAdminMode" runat="server" Text=" - ADMIN MODE" Visible="false"></asp:Label>
</asp:Content>
<asp:Content ID="ContentTopForm" ContentPlaceHolderID="cpTopForm" runat="server">
    <script src='<%= ResolveUrl("~/public/Scripts/daypilot-all.min.js?v=20210821") %>'></script>
    <script src='<%= ResolveUrl("~/public/Scripts/mfbcalendar.js?v=4") %>'></script>
    <div>
        <uc1:mfbEditAircraft id="MfbEditAircraft1" runat="server" OnAircraftUpdated="AircraftUpdated"></uc1:mfbEditAircraft>
    </div>
    <asp:HiddenField ID="hdnReturnURL" runat="server" />
    <asp:HiddenField ID="hdnAdminMode" runat="server" />
    <asp:Panel ID="pnlAdminUserFlights" runat="server" Visible="false">
        <p>Clone an aircraft if the tailnumber can represent more than just this aircraft.  E.g., if N12345 used to be a piper cub and was re-assigned to a C-172, clone it to assign to the new aircraft.</p>
        <p>When cloning an aircraft:</p>
        <ul>
            <li>Select the NEW model above, then hit "Create new version".  The new version will be created using the specified model above.</li>
            <li>If you know that specific users belong in the newly created version, select them below PRIOR to cloning; they will be migrated automatically.</li>
        </ul>
        <asp:Button ID="btnMigrateGeneric" runat="server" Text="Migrate to Generic" Visible="false" />
        <asp:Button ID="btnMigrateSim" runat="server" Text="Migrate to Sim" Visible="false" />
        <asp:Button ID="btnAdminCloneThis" runat="server" Text="Create New Version" CausesValidation="true" ValidationGroup="adminclone" onclick="btnAdminCloneThis_Click" />
        <asp:Button ID="btnMakeDefault" runat="server" Text="MakeDefault" CausesValidation="true" Visible="false" />
        <asp:CustomValidator ID="valModelSelected" runat="server" CssClass="error" 
            ErrorMessage="Please select a new (different) model to which to clone this aircraft" 
            onservervalidate="valModelSelected_ServerValidate" 
            ValidationGroup="adminclone" Display="Dynamic"></asp:CustomValidator>
            <asp:Label ID="lblErr" CssClass="error" runat="server" Text="" EnableViewState="false"></asp:Label>
        <asp:SqlDataSource ID="sqlDSFlightsPerUser" runat="server" OnSelecting="sqlDSFlightsPerUser_Selecting" 
            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="
/* Union of an inner join with a not-exists is orders of magnitude faster than a single left join, for reasons I don't quite understand */
SELECT 
    u.email AS 'Email Address',
    u.username AS User,
    u.FirstName,
    u.LastName,
    COUNT(f.idflight) AS 'Number of Flights'
FROM
    Useraircraft ua
        INNER JOIN
    users u ON ua.username = u.username
        LEFT JOIN
    flights f ON (f.username = ua.username)
WHERE
    ua.idaircraft = ?idaircraft
        AND f.idaircraft = ?idaircraft
GROUP BY ua.username 
UNION SELECT 
    u.email AS 'Email Address',
    u.username AS User,
    u.FirstName,
    u.LastName,
    0 AS 'Number of Flights'
FROM
    Useraircraft ua
        INNER JOIN
    users u ON ua.username = u.username
WHERE
    ua.idaircraft = ?idaircraft
        AND NOT EXISTS( SELECT 
            *
        FROM
            flights f
        WHERE
            f.username = u.username
                AND f.idaircraft = ?idaircraft)
GROUP BY ua.username
ORDER BY user ASC">
        </asp:SqlDataSource>
        <asp:GridView ID="gvFlightsPerUser" runat="server" AutoGenerateColumns="False" 
            EnableModelValidation="True">
            <Columns>
                <asp:BoundField DataField="User" HeaderText="User" SortExpression="User" />
                <asp:BoundField DataField="Email Address" HeaderText="Email Address" 
                    SortExpression="Email Address" />
                <asp:BoundField DataField="FirstName" HeaderText="First Name" SortExpression="FirstName" />
                <asp:BoundField DataField="LastName" HeaderText="Last Name" SortExpression="LastName" />
                <asp:BoundField DataField="Number of Flights" HeaderText="Number of Flights" 
                    SortExpression="Number of Flights" />
                <asp:TemplateField HeaderText="Migrate to new aircraft?">
                    <ItemTemplate>
                        <asp:HiddenField ID="hdnUsername" Value='<%# Eval("User") %>' runat="server" />
                        <asp:CheckBox ID="ckMigrateUser" runat="server" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
        </asp:GridView>
    </asp:Panel>
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
</asp:Content>
