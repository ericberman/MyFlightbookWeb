<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="Member_Admin" Title="Administer MyFlightbook" Codebehind="Admin.aspx.cs" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbImpersonate.ascx" TagName="mfbImpersonate" TagPrefix="uc1" %>
<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="cc1" %>
<%@ Register src="../Controls/mfbEditEndorsement.ascx" tagname="mfbEditEndorsement" tagprefix="uc2" %>
<%@ Register src="../Controls/mfbDecimalEdit.ascx" tagname="mfbDecimalEdit" tagprefix="uc3" %>
<%@ Register src="../Controls/Expando.ascx" tagname="Expando" tagprefix="uc4" %>
<%@ Register Src="~/Controls/mfbTypeInDate.ascx" TagPrefix="uc1" TagName="mfbTypeInDate" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>

<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <asp:MultiView ID="mvAdmin" runat="server" ActiveViewIndex="0">
        <asp:View ID="vwUsers" runat="server">
            <h2>User Management</h2>
            <asp:GridView ID="gvLockedUsers" runat="server" AutoGenerateColumns="False" 
                DataSourceID="sqlDSLockedUsers" onrowcommand="UnlockUser" >
                <Columns>
                    <asp:ButtonField ButtonType="Button" CommandName="Unlock" Text="Unlock" />
                    <asp:BoundField DataField="Username" HeaderText="Username" 
                        SortExpression="Username" />
                    <asp:BoundField DataField="Email" HeaderText="Email" SortExpression="Email" />
                    <asp:BoundField DataField="LastLockedOutDate" HeaderText="LastLockedOutDate" 
                        SortExpression="LastLockedOutDate" />
                    <asp:BoundField DataField="FailedPasswordAttemptCount" 
                        HeaderText="FailedPasswordAttemptCount" 
                        SortExpression="FailedPasswordAttemptCount" />
                    <asp:BoundField DataField="FailedPasswordAttemptWindowStart" 
                        HeaderText="FailedPasswordAttemptWindowStart" 
                        SortExpression="FailedPasswordAttemptWindowStart" />
                    <asp:BoundField DataField="FailedPasswordAnswerAttemptCount" 
                        HeaderText="FailedPasswordAnswerAttemptCount" 
                        SortExpression="FailedPasswordAnswerAttemptCount" />
                    <asp:BoundField DataField="FailedPasswordAnswerAttemptWindowStart" 
                        HeaderText="FailedPasswordAnswerAttemptWindowStart" 
                        SortExpression="FailedPasswordAnswerAttemptWindowStart" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSLockedUsers" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT Username, Email, LastLockedOutDate, FailedPasswordAttemptCount, FailedPasswordAttemptWindowStart, FailedPasswordAnswerAttemptCount, FailedPasswordAnswerAttemptWindowStart FROM users WHERE IsLockedOut&lt;&gt;0">
            </asp:SqlDataSource>
            <uc1:mfbImpersonate ID="mfbImpersonate1" runat="server" />
            <asp:SqlDataSource ID="sqlDSDupeUsers" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="select count(username) as numaccounts, username from users group by username having numaccounts &gt; 1;">
            </asp:SqlDataSource>
            <h3>Dupe users:</h3>
            <asp:GridView ID="gvDupeUsers" DataSourceID="sqlDSDupeUsers" runat="server">
                <EmptyDataTemplate>
                    <p>No duplicate users found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwAircraft" runat="server">
            <h2>Aircraft</h2>
            <asp:UpdatePanel ID="updpanelAircraft" runat="server">
                <Triggers>
                    <asp:PostBackTrigger ControlID="btnMapModels" />
                    <asp:PostBackTrigger ControlID="btnManageCountryCodes" />
                </Triggers>
                <ContentTemplate>
                        <table>
                            <tr>
                                <td>
                                    <asp:Button ID="btnRefreshDupes" runat="server" Width="100%" Text="Dupe Aircraft" OnClick="btnRefreshDupes_Click" /></td>
                                <td>View potentially duplicate aircraft</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnDupeSims" runat="server" Width="100%" Text="Dupe Sims" OnClick="btnRefreshDupeSims_Click" /></td>
                                <td>View and reconcile duplicate sims</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnRefreshInvalid" runat="server" Width="100%" Text="Invalid Aircraft" OnClick="btnRefreshInvalid_Click" /></td>
                                <td>Perform validity check on ALL aircraft</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnAllSims" runat="server" Width="100%" Text="ALL Sims" OnClick="btnRefreshAllSims_Click" /></td>
                                <td>View all sims</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnOrphans" runat="server" Width="100%" Text="Orphaned Aircraft" OnClick="btnOrphans_Click" /></td>
                                <td>View aircraft that are no longer used by any pilot and can be deleted</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnPseudoGeneric" runat="server" Width="100%" Text="Pseudo-generic Aircraft" OnClick="btnPseudoGeneric_Click" /></td>
                                <td>View aircraft that are suspected of having a made-up tailnumber</td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnManageCountryCodes" runat="server" Width="100%" OnClick="btnManageCountryCodes_Click" Text="Country Codes" /></td>
                                <td>Country codes
                                </td>
                            </tr>
                            <tr>
                                <td>
                                    <asp:Button ID="btnCleanUpMaintenance" runat="server" Width="100%" OnClick="btnCleanUpMaintenance_Click" Text="Clean up Maint." /></td>
                                <td>Remove maintainence for virtual aircraft (sims and generic).
                                </td>
                            </tr>
                            <tr style="vertical-align:top">
                                <td>
                                    <div><asp:Button ID="btnMapModels" runat="server" Width="100%" OnClick="btnMapModels_Click" Text="Bulk-map models" /></div>
                                </td>
                                <td>
                                    <div><asp:FileUpload ID="fuMapModels" runat="server" /></div>
                                    Bulk map aircraft models from spreadsheet.
                                    <uc1:mfbTooltip runat="server" ID="mfbTooltip">
                                        <TooltipBody>
                                            <div>CSV spreadsheet needs two columns:</div>
                                            <ul>
                                                <li>idaircraft - the ID of the aircraft to map</li>
                                                <li>idModelProper - the ID of the model to which it should be mapped.</li>
                                            </ul>
                                        </TooltipBody>
                                    </uc1:mfbTooltip>
                                    <div><asp:Label runat="server" CssClass="error" EnableViewState="false" ID="lblMapModelErr"></asp:Label></div>
                                </td>
                            </tr>
                        </table>
                        <asp:Panel ID="pnlFindAircraft" runat="server" DefaultButton="btnFindAircraftByTail">
                            Find aircraft by tail (use % as wildcard):
                            <asp:TextBox ID="txtTailToFind" runat="server"></asp:TextBox>
                            <asp:Button ID="btnFindAircraftByTail" runat="server" 
                                onclick="btnFindAircraftByTail_Click" Text="Find" />
                        </asp:Panel>
                        <asp:UpdateProgress ID="updprgAircraft" runat="server" 
                            AssociatedUpdatePanelID="updpanelAircraft">
                            <ProgressTemplate>
                                <p>Evaluating aircraft for issues...</p>
                                <p><asp:Image ID="imgProgress" runat="server" 
                                        ImageUrl="~/images/ajax-loader.gif" /></p>
                            </ProgressTemplate>
                        </asp:UpdateProgress>
                        <div><asp:Label ID="lblAircraftStatus" runat="server" EnableViewState="false" Font-Bold="true"></asp:Label></div>
                        <asp:MultiView ID="mvAircraftIssues" runat="server">
                            <asp:View ID="vwDupeAircraft" runat="server">
                                <p>
                                    Potential duplicate aircraft:</p>
                                <asp:GridView ID="gvDupeAircraft" runat="server" AutoGenerateColumns="False" 
                                    onrowcommand="gvDupeAircraft_RowCommand">
                                    <Columns>
                                        <asp:BoundField DataField="TailNumber" HeaderText="Tail Number" />
                                        <asp:BoundField DataField="NormalTail" HeaderText="Normalized Tail" />
                                        <asp:HyperLinkField DataNavigateUrlFields="idaircraft" 
                                            DataNavigateUrlFormatString="~/Member/EditAircraft.aspx?id={0}&amp;a=1" 
                                            DataTextField="idaircraft" HeaderText="Aircraft ID" Target="_blank" />
                                        <asp:HyperLinkField DataNavigateUrlFields="idmodel" 
                                            DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}&amp;a=1" 
                                            DataTextField="idmodel" HeaderText="Model ID" Target="_blank" />
                                        <asp:BoundField DataField="version" HeaderText="Version" />
                                        <asp:BoundField DataField="ModelCommonName" HeaderText="Model Name" />
                                        <asp:BoundField DataField="instancetype" HeaderText="Instance Type" />
                                        <asp:BoundField DataField="numFlights" HeaderText="# of Flights" />
                                        <asp:BoundField DataField="numUsers" HeaderText="# of Users" />
                                        <asp:ButtonField CommandName="ResolveAircraft" Text="Consolidate" />
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:Label ID="lblError" runat="server" CssClass="error" 
                                                    EnableViewState="false"></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <p class="success">
                                            No potential duplicates found!</p>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                                <asp:SqlDataSource ID="sqlDupeAircraft" runat="server" 
                                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT 
    ac.TailNumber,
    UPPER(Replace(ac.tailnumber, '-', '')) AS NormalTail, 
    ac.idaircraft,
    ac.idmodel,
    ac.version,
    CONCAT(manufacturers.manufacturer, ' ', models.model, ' ' , models.typename, ' ', models.modelname) AS 'ModelCommonName',
    ac.instancetype,
    (SELECT COUNT(f.idFlight) FROM flights f WHERE f.idaircraft=ac.idaircraft) AS numFlights,
    (SELECT COUNT(ua.username) FROM useraircraft ua WHERE ua.idAircraft=ac.idaircraft) AS numUsers
FROM Aircraft ac
    INNER JOIN models ON ac.idmodel=models.idmodel 
    INNER JOIN manufacturers ON manufacturers.idManufacturer=models.idmanufacturer 
WHERE UPPER(Replace(ac.tailnumber, '-', '')) IN 
    (SELECT NormalizedTail FROM 
        (SELECT UPPER(REPLACE(ac.tailnumber, '-', '')) AS NormalizedTail,
             CONCAT(UPPER(REPLACE(ac.tailnumber, '-', '')), ',', Version) AS TailMatch,
             COUNT(idAircraft) AS cAircraft 
         FROM Aircraft ac 
         GROUP BY TailMatch 
         HAVING cAircraft &gt; 1) AS Dupes)
ORDER BY NormalTail ASC, numUsers DESC, idaircraft ASC"></asp:SqlDataSource>
                            </asp:View>
                            <asp:View ID="vwDupeSims" runat="server">
                                <p>
                                    Duplicate sims:</p>
                                <asp:GridView ID="gvDupeSims" runat="server" AutoGenerateColumns="False" 
                                    onrowcommand="gvDupeSims_RowCommand">
                                    <Columns>
                                        <asp:BoundField DataField="instancetype" HeaderText="Instance Type" />
                                        <asp:BoundField DataField="idmodel" HeaderText="Model ID" />
                                        <asp:BoundField DataField="idaircraft" HeaderText="Aircraft ID" />
                                        <asp:BoundField DataField="ModelCommonName" HeaderText="Model Name" />
                                        <asp:BoundField DataField="TailNumber" HeaderText="Tail Number" />
                                        <asp:HyperLinkField DataNavigateUrlFields="idaircraft" 
                                            DataNavigateUrlFormatString="~/Member/EditAircraft.aspx?id={0}&amp;a=1" 
                                            DataTextField="idaircraft" HeaderText="Aircraft ID" Target="_blank" />
                                        <asp:HyperLinkField DataNavigateUrlFields="idmodel" 
                                            DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}&amp;a=1" 
                                            DataTextField="idmodel" HeaderText="Model ID" Target="_blank" />
                                        <asp:BoundField DataField="ModelCommonName" HeaderText="Model Name" />
                                        <asp:BoundField DataField="numFlights" HeaderText="# of Flights" />
                                        <asp:BoundField DataField="numUsers" HeaderText="# of Users" />
                                        <asp:ButtonField CommandName="ResolveAircraft" Text="Keep This" />
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:Label ID="lblError" runat="server" CssClass="error" 
                                                    EnableViewState="false"></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <p class="success">
                                            No potential duplicates found!</p>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                                <asp:SqlDataSource ID="sqlDupeSims" runat="server" 
                                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT 
      ac.TailNumber, 
      UPPER(Replace(ac.tailnumber, '-', '')) AS NormalTail, 
      ac.idaircraft,
      ac.idmodel,
      CONCAT(manufacturers.manufacturer, ' ', models.model, ' ' , models.typename, ' ', models.modelname) AS 'ModelCommonName',
      ac.instancetype,
      (SELECT COUNT(f.idFlight) FROM flights f WHERE f.idaircraft=ac.idaircraft) AS numFlights,
      (SELECT COUNT(ua.username) FROM useraircraft ua WHERE ua.idAircraft=ac.idaircraft) AS numUsers
    FROM Aircraft ac
    INNER JOIN models ON ac.idmodel=models.idmodel 
    INNER JOIN manufacturers ON manufacturers.idManufacturer=models.idmanufacturer 
        INNER JOIN (SELECT 
            count(ac.idaircraft) AS numAircraft,
            ac.idmodel,
            ac.instancetype
            FROM aircraft ac
        WHERE ac.instancetype &lt;&gt; 1
        GROUP BY instancetype, idmodel
        HAVING numAircraft &gt; 1 
        ORDER BY numAircraft DESC) AS dupeSims 
    ON (ac.idmodel=dupeSims.idmodel AND ac.instancetype=dupeSims.instancetype)
    ORDER BY ac.instancetype, ac.idmodel"></asp:SqlDataSource>
                            </asp:View>
                            <asp:View ID="vwInvalidAircraft" runat="server">
                                <p>
                                    Aircraft that may be invalid:</p>
                                <asp:GridView ID="gvInvalidAircraft" runat="server" AutoGenerateColumns="False" 
                                    EnableViewState="false">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Aircraft">
                                            <ItemTemplate>
                                                <div><a href='EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1' 
                                                    target="_blank"><%# Eval("TailNumber") %></a></div>
                                                <div><%# MakeModel.GetModel((int) Eval("ModelID")).DisplayName %></div>
                                                <div><%# Eval("InstanceTypeDescription") %></div>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="ErrorString" HeaderText="Validation Error" />
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <p class="success">
                                            No invalid aircraft!</p>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </asp:View>
                            <asp:View ID="vwAllSims" runat="server">
                                <p>
                                    All sims:</p>
                                <asp:Label ID="lblSimsFound" runat="server" Text=""></asp:Label>
                                <asp:GridView ID="gvSims" runat="server" AllowSorting="true" 
                                    AutoGenerateColumns="false" onrowcommand="gvSims_RowCommand">
                                    <Columns>
                                        <asp:BoundField DataField="AircraftID" HeaderText="ID" 
                                            SortExpression="AircraftID" />
                                        <asp:TemplateField HeaderText="Aircraft" SortExpression="TailNumber">
                                            <ItemTemplate>
                                                <a href='EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1' 
                                                    target="_blank"><%# Eval("TailNumber") %></a>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <%# Eval("ModelDescription") %><%# Eval("ModelCommonName") %>
                                                <%# Eval("InstanceTypeDescription") %>
                                                <asp:Label ID="lblProposedRename" runat="server" Font-Bold="true" Text=""></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:ButtonField ButtonType="Link" CommandName="Preview" 
                                            HeaderText="Suggest new tail" Text="Preview" />
                                        <asp:ButtonField ButtonType="Link" CommandName="Rename" Text="Rename" />
                                    </Columns>
                                </asp:GridView>
                            </asp:View>
                            <asp:View ID="vwOrphans" runat="server">
                                <p>Orphaned Aircraft <asp:Button ID="btnDeleteAllOrphans" runat="server" Text="Delete all orphans" OnClick="btnDeleteAllOrphans_Click" /></p>
                                <asp:GridView ID="gvOrphanedAircraft" runat="server" 
                                    AutoGenerateColumns="false" OnRowCommand="gvOrphanedAircraft_RowCommand" 
                                    DataKeyNames="idAircraft">
                                    <Columns>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:LinkButton ID="lnkDelete" CommandArgument='<%# Eval("idAircraft") %>' CommandName="_Delete" runat="server" Text="Delete"></asp:LinkButton>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField HeaderText="Tailnumber">
                                            <ItemTemplate>
                                                <a href='EditAircraft.aspx?id=<%# Eval("idAircraft") %>&amp;a=1' 
                                                    target="_blank"><%# Eval("TailNumber") %></a>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="idmodel" HeaderText="Model ID" />
                                        <asp:BoundField DataField="model" HeaderText="Model" />
                                        <asp:BoundField DataField="modelname" HeaderText="ModelName" />
                                        <asp:BoundField DataField="typename" HeaderText="TypeName" />
                                        <asp:BoundField DataField="version" HeaderText="Version" />
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <p class="success">
                                            No orphaned aircraft found!</p>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                                <asp:SqlDataSource ID="sqlOrphanedAircraft" runat="server" 
                                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT ac.*, m.* 
                                        FROM aircraft ac 
                                        INNER JOIN models m ON ac.idmodel=m.idmodel
                                        WHERE ac.idaircraft in (
                                        SELECT ac.idaircraft
                                        FROM aircraft ac 
                                        LEFT JOIN flights f ON f.idaircraft=ac.idaircraft
                                        LEFT JOIN useraircraft ua ON ua.idaircraft=ac.idaircraft
                                        WHERE f.idflight IS NULL AND ua.idaircraft IS NULL)">
                                    <DeleteParameters>
                                        <asp:Parameter Direction="Input" Name="idaircraft" Type="Int32" />
                                    </DeleteParameters>
                                </asp:SqlDataSource>
                            </asp:View>
                            <asp:View ID="vwMatchingAircraft" runat="server">
                                <asp:GridView ID="gvFoundAircraft" runat="server" AutoGenerateColumns="false">
                                    <Columns>
                                        <asp:TemplateField HeaderText="Tailnumber">
                                            <ItemTemplate>
                                                <a href='EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1&amp;gencandidate=1' 
                                                    target="_blank"><%# Eval("TailNumber") %></a>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                    <EmptyDataTemplate>
                                        <p>
                                            No matching aircraft found!</p>
                                    </EmptyDataTemplate>
                                </asp:GridView>
                            </asp:View>
                            <asp:View ID="vwPseudoGeneric" runat="server">
                                <asp:GridView ID="gvPseudoGeneric" runat="server" AutoGenerateColumns="false" 
                                    EnableViewState="false" onrowdatabound="gvPseudoGeneric_RowDataBound">
                                    <Columns>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:Label ID="lblTailnumber" Font-Bold="true" runat="server" Text='<%# Eval("Tailnumber") %>'></asp:Label>
                                                <asp:Label ID="lblManufacturer" runat="server" Text='<%# Eval("manufacturer") %>'></asp:Label>
                                                <asp:Label ID="lblModel" runat="server" Text='<%# Eval("model") %>'></asp:Label>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:BoundField DataField="numFlights" HeaderText="numFlights" />
                                        <asp:HyperLinkField Text="View" Target="_blank" DataNavigateUrlFormatString="~/Member/EditAircraft.aspx?id={0}&a=1&genCandidate=1" DataNavigateUrlFields="idaircraft" />
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkViewFixedTail" Target="_blank" runat="server"></asp:HyperLink>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                    </Columns>
                                </asp:GridView>
                                <asp:SqlDataSource ID="sqlPseudoGeneric" runat="server" 
                                    ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                                    ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                                    SelectCommand="SELECT ac.tailnumber, ac.idaircraft, m.model, m.idmodel, man.manufacturer, count(f.idflight) AS numFlights
FROM aircraft ac 
INNER JOIN models m ON ac.idmodel=m.idmodel 
INNER JOIN manufacturers man ON m.idmanufacturer=man.idmanufacturer 
LEFT JOIN (select idaircraft, tailnumber, m.idmodel, model, modelname 
           from aircraft ac inner join models m
           ON m.idmodel=ac.idmodel AND LEFT(REPLACE(ac.tailnumber, '-', ''), 4)=LEFT(REPLACE(m.model, '-',''), 4)) modelTails 
       ON ac.idaircraft=modelTails.idaircraft
LEFT JOIN Flights f ON f.idaircraft=ac.idaircraft
WHERE
	ac.tailnumber RLIKE '^N-?[ABD-FH-KM-QT-WYZ][-0-9A-Z]+'
    OR modelTails.tailnumber IS NOT NULL
    OR REPLACE(RIGHT(ac.tailnumber, LENGTH(ac.tailnumber) - 1), '-', '') = REPLACE(RIGHT(m.model, LENGTH(m.model) - 1), '-', '')
    OR (ac.instancetype=1 AND REPLACE(ac.tailnumber, '-', '') RLIKE 'SIM|FTD|ATD|FFS|REDB|FRAS|ELIT|CAE|ALSIM|FLIG|SAFE|PREC|TRUF|GROU|VARI|MISC|NONE|UNKN')
GROUP BY ac.idaircraft
ORDER BY tailnumber ASC"></asp:SqlDataSource>
                            </asp:View>
                            <asp:View ID="vwMapModels" runat="server">
                                <asp:GridView ID="gvMapModels" runat="server" OnRowCommand="gvMapModels_RowCommand" AutoGenerateColumns="false">
                                    <Columns>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkRegistration" runat="server" NavigateUrl='<%# Aircraft.LinkForTailnumberRegistry(((AircraftAdminModelMapping) Container.DataItem).aircraft.TailNumber) %>' Target="_blank" Text="Registration" Visible='<%# !String.IsNullOrEmpty(Aircraft.LinkForTailnumberRegistry(((AircraftAdminModelMapping) Container.DataItem).aircraft.TailNumber)) %>'></asp:HyperLink>
                                            </ItemTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkAircraft" Text="<%# ((AircraftAdminModelMapping) Container.DataItem).aircraft.DisplayTailnumber %>" Target="_blank"
                                                    NavigateUrl='<%# "~/Member/EditAircraft.aspx?id=" + ((AircraftAdminModelMapping) Container.DataItem).aircraft.AircraftID.ToString() %>' runat="server"></asp:HyperLink>
                                            </ItemTemplate>
                                            <HeaderTemplate>
                                                Aircraft
                                            </HeaderTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkModel" Text="<%# ((AircraftAdminModelMapping) Container.DataItem).currentModel.ModelDisplayName %>" Target="_blank"
                                                    NavigateUrl='<%# "~/Member/EditMake.aspx?id=" + ((AircraftAdminModelMapping) Container.DataItem).currentModel.MakeModelID.ToString() %>' runat="server"></asp:HyperLink>
                                            </ItemTemplate>
                                            <HeaderTemplate>
                                                Current Model
                                            </HeaderTemplate>
                                        </asp:TemplateField>
                                        <asp:TemplateField>
                                            <ItemTemplate>
                                                <asp:HyperLink ID="lnkModel" Text="<%# ((AircraftAdminModelMapping) Container.DataItem).targetModel.ModelDisplayName %>" Target="_blank"
                                                    NavigateUrl='<%# "~/Member/EditMake.aspx?id=" + ((AircraftAdminModelMapping) Container.DataItem).targetModel.MakeModelID.ToString() %>' runat="server"></asp:HyperLink>
                                            </ItemTemplate>
                                            <HeaderTemplate>
                                                Target Model Model
                                            </HeaderTemplate>
                                        </asp:TemplateField>
                                        <asp:ButtonField CommandName="_MapModel" ButtonType="Link" Text="Map it" />
                                    </Columns>
                                </asp:GridView>
                            </asp:View>
                        </asp:MultiView>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwModels" runat="server">
        </asp:View>
        <asp:View ID="vwManufacturers" runat="server">
            <h2>
                Manufacturers</h2>
            <asp:UpdatePanel ID="UpdatePanel3" runat="server">
                <ContentTemplate>
                    <p>
                        Possible dupes:
                        <asp:SqlDataSource ID="sqlDataSourceDupeMan" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT m.*
                FROM manufacturers m
                WHERE m.manufacturer IN (SELECT manufacturer FROM (SELECT * FROM manufacturers m2 GROUP BY manufacturer HAVING COUNT(manufacturer) &gt; 1) as dupes);">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlDupeManstoMatch" runat="server" 
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT idmanufacturer,
                CAST(CONCAT(idmanufacturer, ' - ', manufacturer) AS CHAR) AS 'DisplayName'
                FROM manufacturers
                WHERE manufacturer IN
                  (SELECT manufacturer FROM manufacturers GROUP BY manufacturer HAVING count(manufacturer) &gt; 1) ORDER BY idmanufacturer;">
                        </asp:SqlDataSource>
                        <asp:SqlDataSource ID="sqlModelsToRemap" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" runat="server"></asp:SqlDataSource>
                        <asp:GridView ID="gvDupeMan" runat="server" AutoGenerateColumns="False" 
                            DataSourceID="sqlDataSourceDupeMan" EnableModelValidation="True">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" 
                                    InsertVisible="False" SortExpression="idManufacturer" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                                    SortExpression="manufacturer" />
                            </Columns>
                        </asp:GridView>
                    </p>
                    <p>
                        Keep
                        <asp:DropDownList ID="cmbManToKeep" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        And kill
                        <asp:DropDownList ID="cmbManToKill" runat="server" DataSourceID="sqlDupeManstoMatch"
                            DataTextField="DisplayName" DataValueField="idmanufacturer">
                        </asp:DropDownList>
                        (will be deleted)
                        <asp:Button ID="btnPreviewMan" runat="server" OnClick="btnPreviewDupeMans_Click" Text="Preview"
                            ValidationGroup="PreviewDupeMans" />
                        <asp:CustomValidator ID="CustomValidator2" runat="server" ValidationGroup="PreviewDupeMans"
                            ErrorMessage="These don't appear to be duplicates" 
                            OnServerValidate="ValidateDupeMans"></asp:CustomValidator>
                    </p>
                    <asp:Label ID="lblPreviewDupeMan" runat="server" Text=""></asp:Label>
                    <asp:Panel ID="pnlPreviewDupeMan" runat="server" Visible="false">
                        <asp:GridView ID="gvModelsToRemap" runat="server" 
                            DataSourceID="sqlModelsToRemap">
                        </asp:GridView>
                        <asp:Button ID="btnDeleteDupeMan" runat="server" 
                            Text="Delete Duplicate Manufacturer" OnClick="btnDeleteDupeMan_Click"
                            ValidationGroup="PreviewDupeMans" />
                    </asp:Panel>
                </ContentTemplate>
            </asp:UpdatePanel>
            <asp:Label ID="lblError" runat="server" CssClass="error"></asp:Label><br />
        </asp:View>
        <asp:View ID="vwProperties" runat="server">
            <h2>
                <asp:Label ID="lblCustomProperties" runat="server" Text=""></asp:Label><asp:Label
                    ID="lblCustomPropsText" runat="server" Text=""></asp:Label></h2>
            <asp:Panel ID="pnlAddProp" runat="server" DefaultButton="btnNewCustomProp">
                <table>
                    <tr>
                        <td>Title:</td>
                        <td><asp:TextBox ID="txtCustomPropTitle" runat="server" Width="300px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>Format:</td>
                        <td><asp:TextBox ID="txtCustomPropFormat" runat="server" Width="300px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>Description:</td>
                        <td><asp:TextBox ID="txtCustomPropDesc" runat="server" Width="300px" TextMode="MultiLine"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td>Type:</td>
                        <td>
                            <asp:DropDownList ID="cmbCustomPropType" runat="server">
                                <asp:ListItem Text="Integer" Value="0"></asp:ListItem>
                                <asp:ListItem Text="Decimal" Value="1"></asp:ListItem>
                                <asp:ListItem Text="Boolean" Value="2"></asp:ListItem>
                                <asp:ListItem Text="Date" Value="3"></asp:ListItem>
                                <asp:ListItem Text="DateTime" Value="4"></asp:ListItem>
                                <asp:ListItem Text="String" Value="5"></asp:ListItem>
                                <asp:ListItem Text="Currency" Value="6"></asp:ListItem>
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr style="vertical-align:top">
                        <td>Flags:</td>
                        <td>
                            <asp:Label ID="lblFlags" runat="server" Text="0"></asp:Label>
                            <asp:CheckBoxList ID="CheckBoxList1" RepeatColumns="2" runat="server" OnSelectedIndexChanged="CheckBoxList1_SelectedIndexChanged"
                                AutoPostBack="True">
                                <asp:ListItem Text="Is a BFR" Value="1"></asp:ListItem>
                                <asp:ListItem Text="Is a IPC" Value="2"></asp:ListItem>
                                <asp:ListItem Text="Unusual Attitude - Descending" Value="4"></asp:ListItem>
                                <asp:ListItem Text="Unusual Attitude - Ascending" Value="8"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Take-off" Value="16"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Landing" Value="32"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Hovering" Value="64"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Departure / Arrival" Value="128"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Transitions" Value="256"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Proficiency Check" Value="512"></asp:ListItem>
                                <asp:ListItem Text="Night Vision - Night Vision Time" Value="32768"></asp:ListItem>
                                <asp:ListItem Text="Glider - Instrument Maneuvers" Value="1024"></asp:ListItem>
                                <asp:ListItem Text="Glider - Instrument Maneuvers for passengers" Value="2048"></asp:ListItem>                    
                                <asp:ListItem Text="Is an approach" Value="4096"></asp:ListItem>                    
                                <asp:ListItem Text="Don't add to totals" Value="8192"></asp:ListItem>                    
                                <asp:ListItem Text="Night-time Takeoff" Value="16384"></asp:ListItem>
                                <asp:ListItem Text="PIC Proficiency Check" Value="65536"></asp:ListItem>
                                <asp:ListItem Text="Base Check" Value="131072"></asp:ListItem>
                                <asp:ListItem Text="Solo" Value="262144"></asp:ListItem>
                                <asp:ListItem Text="Glider Ground Launch" Value="524288"></asp:ListItem>
                                <asp:ListItem Text="Exclude from MRU" Value="1048576"></asp:ListItem>
                                <asp:ListItem Text="Decimal is not time" Value="2097152"></asp:ListItem>
                                <asp:ListItem Text="UAS Launch" Value="4194304"></asp:ListItem>
                                <asp:ListItem Text="UAS Recovery" Value="8388608"></asp:ListItem>
                                <asp:ListItem Text="Known property" Value="16777216"></asp:ListItem>
                                <asp:ListItem Text="No autocomplete" Value="33554432"></asp:ListItem>
                                <asp:ListItem Text="Convert to Caps" Value="67108864"></asp:ListItem>
                            </asp:CheckBoxList>
                        </td>
                    </tr>
                </table>
                <asp:Button ID="btnNewCustomProp" runat="server" Text="Add a custom property" OnClick="btnNewCustomProp_Click" />
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwEndorsementTemplates" runat="server">
            <asp:Panel ID="pnlEditEndorsement" runat="server" DefaultButton="btnAddTemplate">
                <h2><asp:Label ID="lblEndorsements" runat="server" Text=""></asp:Label></h2>
                <p>To edit: </p>
                <ul>
                    <li>{x} for a single-line entry with watermark &quot;x&quot;</li>
                    <li>{Freeform} for freeform multi-line text (no watermark prompt)</li>
                    <li>{Date} for the date (prefilled)</li>
                    <li>{Student} for the Student&#39;s name (pre-filled)</li>
                    <li>{x/y/z} for a drop-down of choices x, y, and z</li>
                </ul>
                <table>
                    <tr><td>Title:</td><td>
                        <asp:TextBox ID="txtEndorsementTitle" runat="server" Width="400px"></asp:TextBox></td></tr>
                    <tr><td>FARRef:</td><td>
                        <asp:TextBox ID="txtEndorsementFAR" runat="server" Width="400px"></asp:TextBox></td></tr>
                    <tr><td>Template Text:</td><td>
                        <asp:TextBox ID="txtEndorsementTemplate" runat="server" Rows="3" 
                            TextMode="MultiLine"  Width="400px"></asp:TextBox></td></tr>
                </table>
                <asp:Button ID="btnAddTemplate" runat="server" 
                    Text="Add an endorsment template" onclick="btnAddTemplate_Click" />
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwImages" runat="server">
            <h2>Images</h2>
            <p>
                <asp:Button ID="btnDeleteOrphans" runat="server" Text="Delete Orphans" 
                    onclick="btnDeleteOrphans_Click" /><asp:Label ID="lblDeleted" runat="server" Text=""></asp:Label>
                <asp:Button ID="btnDeleteS3Debug" runat="server" 
                    onclick="btnDeleteS3Debug_Click" Text="Delete DEBUG S3 Images" /> 
            </p>
            <table>
                <tr>
                    <td>
                        <asp:HyperLink ID="lnkFlightImages" NavigateUrl="~/Member/AdminImages.aspx?r=Flight" Target="_blank" runat="server">Review Flight Images</asp:HyperLink>
                    </td>
                    <td>
                        <asp:Button ID="btnSyncFlight" runat="server" Text="Sync Flight Images to DB" 
                            onclick="btnSyncFlight_Click" />
                    </td>
                    <td>
                        <asp:Button ID="btnDelS3FlightOrphans" runat="server" OnClick="btnDelS3FlightOrphans_Click" Text="Delete Orphan S3 Images" />
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:HyperLink ID="lnkAircraftImages" NavigateUrl="~/Member/AdminImages.aspx?r=Aircraft" Target="_blank" runat="server">Review Aircraft Images</asp:HyperLink>
                    </td>
                    <td>
                        <asp:Button ID="btnSyncAircraftImages" runat="server" 
                            Text="Sync Aircraft Images to DB" onclick="btnSyncAircraftImages_Click" />
                    </td>
                    <td>
                        <asp:Button ID="btnDelS3AircraftOrphans" runat="server" OnClick="btnDelS3AircraftOrphans_Click" Text="Delete Orphan S3 Images" />
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:HyperLink ID="lnkEndorsementImages" NavigateUrl="~/Member/AdminImages.aspx?r=Endorsement" Target="_blank" runat="server">Review Endorsements</asp:HyperLink>
                    </td>
                    <td>
                        <asp:Button ID="btnSyncEndorsements" runat="server" 
                            Text="Sync Endorsement Images to DB" onclick="btnSyncEndorsements_Click" />
                    </td>
                    <td>
                        <asp:Button ID="btnDelS3EndorsementOrphans" runat="server" OnClick="btnDelS3EndorsementOrphans_Click" Text="Delete Orphan S3 Images" />
                    </td>
                </tr>
                <tr>
                    <td>
                        <asp:HyperLink ID="lnkBasicMedImages" NavigateUrl="~/Member/AdminImages.aspx?r=Endorsement" Target="_blank" runat="server">Review BasicMed</asp:HyperLink>
                    </td>
                    <td>
                        <asp:Button ID="btnSyncBasicMed" runat="server" 
                            Text="Sync BasicMed Images to DB" onclick="btnSyncBasicMed_Click" />
                    </td>
                    <td>
                        <asp:Button ID="btnDelS3BasicMedOrphans" runat="server" OnClick="btnDelS3BasicMedOrphans_Click" Text="Delete Orphan S3 Images" />
                    </td>
                </tr>
            </table>
            <asp:CheckBox ID="ckPreviewOnly" runat="server" Text="Preview Only (no changes made)" />
            <asp:PlaceHolder ID="plcDBSync" runat="server"></asp:PlaceHolder>
            <h3>Images to fix:</h3>
            <asp:SqlDataSource ID="sqlDSImagesToFix" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                UpdateCommand="UPDATE images SET ThumbWidth=?thumbWidth, ThumbHeight=?thumbHeight WHERE VirtPathID=?VirtpathID AND ImageKey=?imagekey AND ThumbFilename=?thumbfilename"
                SelectCommand="SELECT CONCAT('~/images/', ELT(VirtPathID + 1, 'Flights/', 'Aircraft/ID/', 'Endorsements/'), ImageKey, '/', ThumbFilename) AS URL, images.* from images where imageType=1 OR thumbwidth=0 OR thumbheight=0 OR islocal&lt;&gt;0">
                <UpdateParameters>
                    <asp:Parameter Name="thumbWidth" Type="Int32"  Direction="InputOutput" />
                    <asp:Parameter Name="thumbHeight" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="virtpathID" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="imagekey" Type="String" Size="50"  Direction="InputOutput" />
                    <asp:Parameter Name="thumbfilename" Type="String" Size="50"  Direction="InputOutput" />
                </UpdateParameters>
            </asp:SqlDataSource>
            <asp:GridView ID="gvImagesToFix" AutoGenerateColumns="false" AutoGenerateEditButton="True" EnableModelValidation="true" DataKeyNames="VirtPathID,ImageKey,ThumbFilename" DataSourceID="sqlDSImagesToFix" runat="server">
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <asp:HyperLink ID="lnkImage" runat="server" NavigateUrl='<%# Eval("URL") %>' Target="_blank">
                                <asp:Image ID="Image1" runat="server" ImageUrl='<%# Eval("URL") %>' />
                            </asp:HyperLink>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="VirtPathID" HeaderText="VirtPathID" />
                    <asp:BoundField DataField="ImageKey" HeaderText="ImageKey"/>
                    <asp:BoundField DataField="ThumbFileName" HeaderText="ThumbFileName"/>
                    <asp:BoundField DataField="ImageType" HeaderText="ImageType"/>
                    <asp:BoundField DataField="ThumbWidth" HeaderText="ThumbWidth"/>
                    <asp:BoundField DataField="ThumbHeight" HeaderText="ThumbHeight" />
                    <asp:BoundField DataField="Comment" HeaderText="Comment"/>
                    <asp:BoundField DataField="Latitude" HeaderText="Latitude" />
                    <asp:BoundField DataField="Longitude" HeaderText="Longitude" />
                    <asp:BoundField DataField="IsLocal" HeaderText="IsLocal" />
                </Columns>
                <EmptyDataTemplate>
                    <p>(No problematic images found)</p>
                </EmptyDataTemplate>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwAchievements" runat="server">
            <asp:Button ID="btnInvalidateUserAchievements" runat="server" OnClick="btnInvalidateUserAchievements_Click" Text="Invalidate Badge Cache" />
            &nbsp;(if any badge criteria changes, use this to set everybody to &quot;needs computed&quot;)

            <h2>Airport-list achievements:</h2>
            <table width="400px">
                <tr>
                    <td valign="top"><b>Title</b></td>
                    <td><asp:TextBox ID="txtAirportAchievementName" runat="server" /></td>
                </tr>
                <tr>
                    <td valign="top"><b>Binary (all or nothing)?</b></td>
                    <td><asp:CheckBox ID="ckBinaryAchievement" runat="server" /></td>
                </tr>
                <tr>
                    <td valign="top"><b>Bronze Level:</b></td>
                    <td>
                        <uc3:mfbDecimalEdit ID="mfbDecEditBronze" DefaultValueInt="0" EditingMode="Integer" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td valign="top"><b>Silver Level:</b></td>
                    <td>
                        <uc3:mfbDecimalEdit ID="mfbDecEditSilver" DefaultValueInt="0" EditingMode="Integer" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td valign="top"><b>Gold Level:</b></td>
                    <td>
                        <uc3:mfbDecimalEdit ID="mfbDecEditGold" DefaultValueInt="0" EditingMode="Integer" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td valign="top"><b>Platinum Level:</b></td>
                    <td>
                        <uc3:mfbDecimalEdit ID="mfbDecEditPlatinum" DefaultValueInt="0" EditingMode="Integer" runat="server" />
                    </td>
                </tr>
                <tr>
                    <td valign="top"><b>Overlay</b></td>
                    <td><asp:TextBox ID="txtOverlay" runat="server" /></td>
                </tr>
                <tr>
                    <td valign="top"><b>Airport Codes</b></td>
                    <td><asp:TextBox ID="txtAirportAchievementList" TextMode="MultiLine" runat="server" Width="300px"></asp:TextBox></td>
                </tr>
            </table>
            <asp:Button ID="btnAddAirportAchievement" OnClick="btnAddAirportAchievement_Click" runat="server" Text="Add" />
            <asp:SqlDataSource ID="sqlDSAirportAchievements" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                UpdateCommand="UPDATE airportlistachievement SET idAchievement=?idAchievement, name=?name, overlayname=?overlayname, airportcodes=?airportcodes, thresholdBronze=?thresholdBronze, thresholdSilver=?thresholdSilver, thresholdGold=?thresholdGold, thresholdPlatinum=?thresholdPlatinum, fBinaryOnly=?fBinaryOnly WHERE idAchievement=?idAchievement"
                SelectCommand="SELECT * FROM airportlistachievement">
                <UpdateParameters>
                    <asp:Parameter Name="idAchievement" Type="Int32"  Direction="InputOutput" />
                    <asp:Parameter Name="name" Type="String" Size="100"  Direction="InputOutput" />
                    <asp:Parameter Name="overlayname" Type="String" Size="45"  Direction="InputOutput" />
                    <asp:Parameter Name="airportcodes" Type="String" Size="1000"  Direction="InputOutput" />
                    <asp:Parameter Name="thresholdBronze" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="thresholdSilver" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="thresholdGold" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="thresholdPlatinum" Type="Int32" Direction="InputOutput" />
                    <asp:Parameter Name="fBinaryOnly" Type="Int16" Direction="InputOutput" />
                </UpdateParameters>
            </asp:SqlDataSource>
            <asp:UpdatePanel ID="UpdatePanel2" runat="server">
                <ContentTemplate>
                    <asp:GridView ID="gvAirportAchievements" runat="server" AutoGenerateColumns="false" AutoGenerateEditButton="true" CellPadding="5" DataSourceID="sqlDSAirportAchievements">
                <EmptyDataTemplate>
                    (No airportlist achievements defined)
                </EmptyDataTemplate>
                <Columns>
                    <asp:TemplateField>
                        <ItemTemplate>
                            <table>
                                <tr>
                                    <td>
                                        <asp:Image ID="imgOverlay" ImageUrl='<%# String.Format("~/images/BadgeOverlays/{0}", Eval("overlayname")) %>' runat="server" />
                                    </td>
                                    <td>
                                        <asp:Label ID="txtID" runat="server" Text='<%# Bind("idAchievement") %>' Font-Bold="true" /> - <asp:Label  ID="txtTitle" runat="server" Text='<%# Bind("name") %>' />
                                        <br />
                                        <asp:HyperLink ID="lnkViewAirports" Target="_blank" runat="server" NavigateUrl='<%# String.Format("~/public/maproute2.aspx?Airports={0}", HttpUtility.UrlEncode(Eval("airportcodes").ToString())) %>'>
                                            <asp:Label  ID="txtText" runat="server" Width="300px" Text='<%# Bind("airportcodes")%>' />
                                        </asp:HyperLink><asp:Label ID="lblLevels" runat="server" Text='<%# Convert.ToInt32(Eval("fBinaryOnly")) != 0 ? "Binary Only" : String.Format("{0}, {1}, {2}, {3}", Eval("thresholdBronze"), Eval("thresholdSilver"), Eval("thresholdGold"), Eval("thresholdPlatinum"))  %>'></asp:Label></td></tr></table></ItemTemplate><EditItemTemplate>
                            <table width="400px">
                                <tr>
                                    <td valign="top"><b>ID</b></td><td><asp:Label ID="txtID" runat="server" Text='<%# Bind("idAchievement") %>' /></td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Title</b></td><td><asp:TextBox ID="txtTitle" runat="server" Text='<%# Bind("name") %>' /></td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Binary (all or nothing)?</b></td><td><asp:TextBox ID="txtBinaryOnly" runat="server" Text='<%# Bind("fBinaryOnly") %>' /></td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Bronze Level:</b></td><td>
                                        <asp:TextBox ID="txtBronze" runat="server" Text='<%# Bind("thresholdBronze") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Silver Level:</b></td><td>
                                        <asp:TextBox ID="txtSilver" runat="server" Text='<%# Bind("thresholdSilver") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Gold Level:</b></td><td>
                                        <asp:TextBox ID="txtGold" runat="server" Text='<%# Bind("thresholdGold") %>' /><br />
                                    </td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Platinum Level:</b></td><td>
                                        <asp:TextBox ID="txtPlatinum" runat="server" Text='<%# Bind("thresholdPlatinum") %>' />
                                    </td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Overlay</b></td><td><asp:TextBox ID="txtOverlay" runat="server" Text='<%# Bind("overlayname") %>' /></td>
                                </tr>
                                <tr>
                                    <td valign="top"><b>Airport Codes</b></td><td><asp:TextBox ID="txtText" runat="server" Width="300px" Text='<%# Bind("airportcodes")%>' TextMode="MultiLine" /></td>
                                </tr>
                            </table>
                        </EditItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwDonations" runat="server">
            <asp:Panel ID="Panel2" runat="server" DefaultButton="btnFindDonations">
                <p><asp:Button ID="btnResetGratuities" runat="server" OnClick="btnResetGratuities_Click" Text="Reset Gratuities" /><asp:CheckBox ID="ckResetGratuityReminders" runat="server" Text="Reset gratuity reminders too" /></p>
                <p>View donations for: <asp:TextBox ID="txtDonationUser" runat="server"></asp:TextBox><asp:CheckBoxList ID="ckTransactionTypes" runat="server" RepeatColumns="4"><asp:ListItem Selected="True" Text="Payments" Value="0"></asp:ListItem><asp:ListItem Selected="True" Text="Refunds" Value="1"></asp:ListItem><asp:ListItem Text="Adjustments" Value="2"></asp:ListItem><asp:ListItem Text="Test Transactions" Value="3"></asp:ListItem></asp:CheckBoxList><asp:Button ID="btnFindDonations" runat="server" onclick="btnFindDonations_Click" Text="Find" /></p><p>
                    <asp:Button ID="btnComputeStats" runat="server" onclick="btnComputeStats_Click" 
                        Text="Fix Donation Fees" />
                    <asp:PlaceHolder ID="plcPayments" runat="server"></asp:PlaceHolder>
                    <asp:SqlDataSource ID="sqlDSTotalPayments" runat="server" DataSourceMode="DataReader"
                        ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                        SelectCommand="SELECT CAST(CONCAT(YEAR(Date), '-', LPAD(MONTH(Date), 2, '0')) AS CHAR) AS MonthPeriod, SUM(Amount) AS Gross, SUM(Fee) AS Fee, SUM(Amount - Fee) AS Net, SUM(IF(TransactionID='', 0, Amount-FEE)) AS NetPaypal From Payments WHERE TransactionType IN (0, 1) GROUP BY MonthPeriod ORDER BY MonthPeriod ASC">
                    </asp:SqlDataSource>
                </p>
            </asp:Panel>
            <cc1:CollapsiblePanelExtender Collapsed="true" ID="CollapsiblePanelExtender1" runat="server" TargetControlID="pnlTestTransaction" CollapseControlID="lblShowHideTestTransaction" ExpandControlID="lblShowHideTestTransaction"
                TextLabelID="lblShowHideTestTransaction" CollapsedText="Click to show" ExpandedText="Click to hide"></cc1:CollapsiblePanelExtender>
            <p><asp:Label ID="lblHeader" runat="server" Font-Bold="true" Text="Test transaction"></asp:Label>&nbsp;<asp:Label ID="lblShowHideTestTransaction" runat="server" Text="show/hide"></asp:Label></p><asp:Panel ID="pnlTestTransaction" Height="0px" style="overflow:hidden;" DefaultButton="btnEnterTestTransaction" runat="server">
                <table>
                    <tr>
                        <td>Date: </td><td>
                            <uc1:mfbTypeInDate runat="server" ID="dateTestTransaction" DefaultType="Today" />
                        </td>
                    </tr>
                    <tr>
                        <td>Username:</td><td>
                            <asp:TextBox ID="txtTestTransactionUsername" runat="server"></asp:TextBox><asp:RequiredFieldValidator ID="RequiredFieldValidator1" runat="server" ControlToValidate="txtTestTransactionUsername" ErrorMessage="Username required"></asp:RequiredFieldValidator></td></tr><tr>
                        <td>Amount: </td><td>
                            <uc3:mfbDecimalEdit ID="decTestTransactionAmount" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>Fee: </td><td>
                            <uc3:mfbDecimalEdit ID="decTestTransactionFee" runat="server" />
                        </td>
                    </tr>
                    <tr>
                        <td>Transaction Type: </td><td>
                            <asp:DropDownList ID="cmbTestTransactionType" runat="server">
                                <asp:ListItem Text="Payment" Value="0"></asp:ListItem><asp:ListItem Text="Refund" Value="1"></asp:ListItem><asp:ListItem Text="Adjustment" Value="2"></asp:ListItem><asp:ListItem Text="TestTransaction" Value="3"  Selected="True"></asp:ListItem></asp:DropDownList></td></tr><tr>
                        <td>Notes:</td><td>
                            <asp:TextBox ID="txtTestTransactionNotes" runat="server"></asp:TextBox></td></tr><tr>
                        <td></td>
                        <td><asp:Button ID="btnEnterTestTransaction" runat="server" Text="Enter" OnClick="btnEnterTestTransaction_Click" /></td>
                    </tr>
                </table>
            </asp:Panel>
        </asp:View>
        <asp:View ID="vwMisc" runat="server">
            <h2>Fix duplicate properties on flights</h2><asp:SqlDataSource ID="sqlDSDupeProps" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select fp.idflight, fp.idproptype, count(fp.idProp) AS numProps, cast(group_concat(fp.idprop) as char) AS PropIDs
    from flightproperties fp
    group by fp.idflight, fp.idproptype
    having numProps &gt; 1;"></asp:SqlDataSource>
            <asp:GridView ID="gvDupeProps" runat="server" AutoGenerateColumns="False" 
                EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                    <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                        SortExpression="idproptype" />
                    <asp:BoundField DataField="numProps" HeaderText="numProps" 
                        SortExpression="numProps" />
                    <asp:BoundField DataField="PropIDs" 
                        HeaderText="PropIDs" 
                        SortExpression="PropIDs" />
                </Columns>
                <EmptyDataTemplate>
                    <p class="success">No duplicate properties found.</p></EmptyDataTemplate></asp:GridView><h2>Empty properties</h2><asp:SqlDataSource ID="sqlDSEmptyProps" runat="server"
            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
            SelectCommand="SELECT * FROM flightproperties WHERE intvalue = 0 AND decvalue = 0.0 AND (datevalue IS NULL OR YEAR(datevalue) &lt; 10) AND (stringvalue = '' OR stringvalue IS NULL);" >
            </asp:SqlDataSource>
            <asp:GridView ID="gvEmptyProps" runat="server" AutoGenerateColumns="False" 
                EnableModelValidation="True">
                <Columns>
                    <asp:HyperLinkField DataTextField="idflight" DataNavigateUrlFields="idflight" DataTextFormatString="{0}" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1&oldProps=1" Target="_blank" HeaderText="Flight" SortExpression="idflight" />
                    <asp:BoundField DataField="idproptype" HeaderText="idproptype" 
                        SortExpression="idproptype" />
                </Columns>
                <EmptyDataTemplate>
                    <p class="success">No empty properties found.</p>
                </EmptyDataTemplate>
            </asp:GridView>
            <div><asp:Button ID="btnRefreshProps" runat="server" Text="Refresh empty/dupe props" OnClick="btnRefreshProps_Click" /></div>
            <h2>Invalid signatures</h2><p>
                        <asp:Button ID="btnRefreshInvalidSigs" runat="server" OnClick="btnRefreshInvalidSigs_Click" Text="Refresh" /></p>
            <p><asp:Label ID="lblSigResults" runat="server" Text=""></asp:Label></p>
            <p><asp:Button ID="btnFlushCache" runat="server" Text="Flush Cache" OnClick="btnFlushCache_Click" /><span class="fineprint">Removes all entries from the cache; will make things slow, but useful for picking up DB changes or debugging</span></p>
        </asp:View>
    </asp:MultiView>
    <asp:HiddenField ID="hdnActiveTab" runat="server" />
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <asp:MultiView ID="mvMain" runat="server">
        <asp:View ID="vwMainUsers" runat="server">
        </asp:View>
        <asp:View ID="vwMainAircraft" runat="server">
            <asp:UpdatePanel ID="UpdatePanel4" runat="server">
                <ContentTemplate>
                    <div style="padding:5px;">
                        <asp:HiddenField ID="hdnLastCountryEdited" runat="server" />
                        <asp:HiddenField ID="hdnLastCountryResult" runat="server" />
                        <asp:GridView ID="gvCountryCodes" runat="server" AllowSorting="True" OnRowEditing="gvCountryCodes_RowEditing"
                            AutoGenerateEditButton="true" CellPadding="3" AutoGenerateColumns="false" OnRowCommand="gvCountryCodes_RowCommand"
                            OnRowDataBound="gvCountryCodes_RowDataBound" DataKeyNames="ID" OnRowUpdating="gvCountryCodes_RowUpdating"
                            >
                            <Columns>
                                <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" ReadOnly="true" />
                                <asp:BoundField DataField="Prefix" HeaderText="Prefix" SortExpression="Prefix" />
                                <asp:BoundField DataField="CountryName" HeaderText="Country Name" SortExpression="CountryName" />
                                <asp:BoundField DataField="Locale" HeaderText="Locale" SortExpression="Locale" ConvertEmptyStringToNull="false" />
                                <asp:BoundField DataField="RegistrationURLTemplate"   />
                                <asp:TemplateField HeaderText="Template Mode" SortExpression="RegistrationURLTemplateMode">
                                    <ItemTemplate>
                                        <%# ((CountryCodePrefix.RegistrationTemplateMode) Convert.ToUInt32(Eval("TemplateType"), System.Globalization.CultureInfo.InvariantCulture)).ToString() %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:RadioButtonList ID="rblTemplateType" runat="server">
                                            <asp:ListItem Text="None" Value="0"></asp:ListItem>
                                            <asp:ListItem Text="Whole Tailnumber" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Suffix Only (only what follows dash)" Value="2"></asp:ListItem>
                                            <asp:ListItem Text="Whole - with dash" Value="3"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </EditItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField HeaderText="Hyphen Rules" SortExpression="HyphenPref">
                                    <ItemTemplate>
                                        <%# ((CountryCodePrefix.HyphenPreference) Convert.ToUInt32(Eval("HyphenPref"), System.Globalization.CultureInfo.InvariantCulture)).ToString() %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:RadioButtonList ID="rblHyphenPref" runat="server">
                                            <asp:ListItem Text="None" Value="0"></asp:ListItem>
                                            <asp:ListItem Text="Hyphenate" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="No Hyphen" Value="2"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </EditItemTemplate>
                                </asp:TemplateField>
                                <asp:TemplateField>
                                    <ItemTemplate>
                                        <asp:Button ID="btnFixHyphens" runat="server" Text="Fix Aircraft Hyphenation" CommandArgument='<%# Eval("Prefix") %>' CommandName="fixHyphens" />
                                        <div><asp:Label ID="lblHyphenResult" runat="server" Font-Bold="true" Visible="false"></asp:Label></div>
                                    </ItemTemplate>
                                </asp:TemplateField>
                            </Columns>
                        </asp:GridView>
                        <asp:SqlDataSource ID="sqlDSCountryCode" runat="server"
                            ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                            ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                            SelectCommand="SELECT * FROM countrycodes ORDER BY ID ASC"
                            UpdateCommand="UPDATE countrycodes SET Prefix=?Prefix, CountryName=?CountryName, Locale=?Locale, RegistrationURLTemplate=?RegistrationURLTemplate, TemplateType=?TemplateType, HyphenPref=?HyphenPref WHERE ID=?ID">
                            <UpdateParameters>
                                <asp:Parameter Name="Prefix" Type="String" Size="10" Direction="InputOutput" />
                                <asp:Parameter Name="CountryName" Type="String" Size="80" Direction="InputOutput" />
                                <asp:Parameter Name="Locale" Type="String" Size="3" Direction="InputOutput" ConvertEmptyStringToNull="false" />
                                <asp:Parameter Name="RegistrationURLTemplate" Type="String" Size="512" Direction="InputOutput" />
                                <asp:Parameter Name="TemplateType" Type="Int16" Direction="InputOutput" />
                                <asp:Parameter Name="HyphenPref" Type="Int16" Direction="InputOutput" />
                                <asp:Parameter Name="ID" Type="Int32" Direction="Input" />
                            </UpdateParameters>
                        </asp:SqlDataSource>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainModels" runat="server">
            <h2>Makes/models that should be sims:</h2>
            <asp:GridView ID="gvAircraftShouldBeSims" runat="server" AutoGenerateColumns="False" 
                DataSourceID="sqlSimMakes" EnableModelValidation="True">
                <Columns>
                    <asp:BoundField DataField="idmodel" 
                        DataFormatString="&lt;a href=&quot;EditMake.aspx?id={0}&quot; target=&quot;_blank&quot;&gt;Edit&lt;/a&gt;" 
                        HtmlEncode="False" HtmlEncodeFormatString="False" ReadOnly="True" />
                    <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="model" />
                    <asp:BoundField DataField="typename" HeaderText="typename" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="modelname" HeaderText="modelname" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="idcategoryclass" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" 
                        SortExpression="fSimOnly" />
                </Columns>
                <EmptyDataTemplate>
                    <div class="success">(No suspect makes/models found)</div>
                </EmptyDataTemplate>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlSimMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT man.manufacturer, m.*
FROM models m INNER JOIN manufacturers man ON m.idmanufacturer=man.idManufacturer
WHERE man.DefaultSim &lt;&gt; 0 AND m.fSimOnly= 0"></asp:SqlDataSource>
            <h2>Orphaned makes/models (i.e., no airplanes using them):</h2>
            <asp:GridView ID="gvOrphanMakes" runat="server"
                AutoGenerateDeleteButton="True" DataKeyNames="idmodel,idmanufacturer" 
                DataSourceID="sqlDSOrphanMakes" EnableModelValidation="True" OnRowDeleting="gvOrphanMakes_RowDeleting"
                AutoGenerateColumns="False">
                <Columns>
                    <asp:BoundField DataField="NumberAircraft" HeaderText="NumberAircraft" 
                        ReadOnly="True" SortExpression="NumberAircraft" />
                    <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" 
                        SortExpression="manufacturer" />
                    <asp:BoundField DataField="model" HeaderText="model" SortExpression="model" />
                    <asp:BoundField DataField="typename" HeaderText="typename" 
                        SortExpression="typename" />
                    <asp:BoundField DataField="idmodel" HeaderText="idmodel" ReadOnly="True" 
                        SortExpression="idmodel" />
                    <asp:BoundField DataField="idcategoryclass" HeaderText="idcategoryclass" 
                        SortExpression="idcategoryclass" />
                    <asp:BoundField DataField="idmanufacturer" HeaderText="idmanufacturer" 
                        ReadOnly="True" SortExpression="idmanufacturer" />
                    <asp:BoundField DataField="modelname" HeaderText="modelname" 
                        SortExpression="modelname" />
                    <asp:BoundField DataField="fcomplex" HeaderText="fcomplex" 
                        SortExpression="fcomplex" />
                    <asp:BoundField DataField="fHighPerf" HeaderText="fHighPerf" 
                        SortExpression="fHighPerf" />
                    <asp:BoundField DataField="fTailwheel" HeaderText="fTailwheel" 
                        SortExpression="fTailwheel" />
                    <asp:BoundField DataField="fConstantProp" HeaderText="fConstantProp" 
                        SortExpression="fConstantProp" />
                    <asp:BoundField DataField="fTurbine" HeaderText="fTurbine" 
                        SortExpression="fTurbine" />
                    <asp:BoundField DataField="fRetract" HeaderText="fRetract" 
                        SortExpression="fRetract" />
                    <asp:BoundField DataField="fCowlFlaps" HeaderText="fCowlFlaps" 
                        SortExpression="fCowlFlaps" />
                    <asp:BoundField DataField="fGlassOnly" HeaderText="fGlassOnly" SortExpression="fGlassOnly" />
                    <asp:BoundField DataField="fSimOnly" HeaderText="fSimOnly" SortExpression="fSimOnly" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSOrphanMakes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT COUNT(ac.idaircraft) AS NumberAircraft, man.manufacturer, m.*
        FROM models m
        LEFT JOIN aircraft ac ON m.idmodel=ac.idmodel
        INNER JOIN manufacturers man ON m.idmanufacturer=man.idmanufacturer
        GROUP BY m.idmodel
        HAVING NumberAircraft=0"
                DeleteCommand="DELETE FROM models WHERE idmodel=?idmodel"
                    >
                    <DeleteParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                    </DeleteParameters>
            </asp:SqlDataSource>    
            <br />
            <h2>Review Type Designations</h2>
            <asp:Button ID="btnRefreshReview" runat="server" Text="Refresh" OnClick="btnRefreshReview_Click" />
            <asp:GridView ID="gvReviewTypes" runat="server" DataKeyNames="idmodel" AutoGenerateColumns="false" AutoGenerateEditButton="True">
                <Columns>
                    <asp:HyperLinkField HeaderText="ModelID" Target="_blank" DataNavigateUrlFields="idmodel" DataTextField="idmodel" DataNavigateUrlFormatString="~/Member/EditMake.aspx?id={0}" />
                    <asp:BoundField DataField="idmodel" HeaderText="modelid" Visible="false" ReadOnly="true" />
                    <asp:BoundField DataField="catclass" HeaderText="catclass" ReadOnly="true" />
                    <asp:BoundField DataField="manufacturer" ReadOnly="true" HeaderText="Manufacturer" />
                    <asp:BoundField DataField="model" ReadOnly="true" HeaderText="Model Name" />
                    <asp:BoundField DataField="typename" HeaderText="Type name" />
                </Columns>
            </asp:GridView>
            <asp:SqlDataSource ID="sqlDSReviewTypes" runat="server" 
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" 
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="select m.idmodel, man.manufacturer, m.model, m.typename, cc.catclass
from models m
inner join manufacturers man on m.idmanufacturer=man.idmanufacturer
inner join categoryclass cc on m.idcategoryclass=cc.idcatclass
where m.typename &lt;&gt; ''
order by cc.idcatclass ASC, man.manufacturer asc, m.model asc, m.typename asc;"
                UpdateCommand="UPDATE Models SET typename=?typename WHERE idmodel=?idmodel">
                <UpdateParameters>
                        <asp:Parameter Type="Int32" Direction="Input" Name="idmodel" />
                        <asp:Parameter Type="String" Direction="Input" Name="typename" />
                </UpdateParameters>
            </asp:SqlDataSource>  
            <h2>Models that are potential dupes:</h2><asp:SqlDataSource ID="SqlDataSourceDupeModels" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                SelectCommand="SELECT man.manufacturer, 
                    cc.CatClass,
                    CAST(CONCAT(model, CONCAT(' ''', modelname, ''' '), IF(typename='', '', CONCAT('TYPE=', typename)), CONCAT(' - ', idmodel)) AS CHAR) AS 'DisplayName',
                    m.*
                FROM models m
                    INNER JOIN manufacturers man ON m.idmanufacturer = man.idManufacturer
                    INNER JOIN categoryclass cc ON cc.idCatClass=m.idcategoryclass
                WHERE UPPER(REPLACE(REPLACE(CONCAT(m.model,CONVERT(m.idcategoryclass, char),m.typename), '-', ''), ' ', '')) IN
                    (SELECT modelandtype FROM (SELECT model, COUNT(model) AS cModel, UPPER(REPLACE(REPLACE(CONCAT(m2.model,CONVERT(m2.idcategoryclass, char),m2.typename), '-', ''), ' ', '')) AS modelandtype FROM models m2 GROUP BY modelandtype HAVING cModel &gt; 1) as dupes)
                ORDER BY m.model">
            </asp:SqlDataSource>
            <div>
                <p>
                    Keep <asp:DropDownList ID="cmbModelToMergeInto" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    And kill <asp:DropDownList ID="cmbModelToDelete" runat="server" DataSourceID="SqlDataSourceDupeModels"
                        DataTextField="DisplayName" DataValueField="idmodel">
                    </asp:DropDownList>
                    (will be deleted) <asp:Button ID="btnPreview" runat="server" OnClick="btnPreview_Click" Text="Preview"
                        ValidationGroup="PreviewDupes" />
                    <asp:CustomValidator ID="CustomValidator1" runat="server" ValidationGroup="PreviewDupes"
                        ErrorMessage="These don't appear to be duplicates" OnServerValidate="CustomValidator1_ServerValidate"></asp:CustomValidator></p><asp:Label ID="lblPreview" runat="server" Text=""></asp:Label><asp:Panel ID="pnlPreview" runat="server" Visible="false">
                    <asp:GridView ID="gvAirplanesToRemap" runat="server" DataSourceID="sqlAirplanesToReMap">
                    </asp:GridView>
                    <asp:Button ID="btnDeleteDupeMake" runat="server" Text="Delete Duplicate Make" OnClick="btnDeleteDupeMake_Click"
                        ValidationGroup="PreviewDupes" />
                    <br />
                </asp:Panel>
            </div>
            <div>&nbsp;</div><asp:SqlDataSource ID="sqlAirplanesToReMap" DataSourceMode="DataReader" runat="server"
                ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>" ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
                SelectCommand=""></asp:SqlDataSource>
            <asp:GridView ID="gvDupeModels" DataSourceID="SqlDataSourceDupeModels" runat="server" EnableModelValidation="True" AutoGenerateColumns="false">
                <Columns>
                    <asp:BoundField DataField="idmodel" 
                        DataFormatString="&lt;a href=&quot;EditMake.aspx?id={0}&quot; target=&quot;_blank&quot;&gt;{0}&lt;/a&gt;" 
                        HtmlEncode="False" HtmlEncodeFormatString="False" ReadOnly="True" />
                    <asp:BoundField DataField="idmodel"
                        DataFormatString="&lt;a href=&quot;Aircraft.aspx?a=1&m={0}&quot; target=&quot;_blank&quot;&gt;View Aircraft&lt;/a&gt;" 
                        HtmlEncode="False" HtmlEncodeFormatString="False" ReadOnly="True" />
                    <asp:BoundField DataField="Manufacturer" HeaderText="Manufacturer" SortExpression="Manufacturer" />
                    <asp:BoundField DataField="DisplayName" HeaderText="DisplayName" SortExpression="DisplayName" />
                    <asp:BoundField DataField="Family" HeaderText="Family" SortExpression="FamilyName" />
                    <asp:BoundField DataField="CatClass" HeaderText="CatClass" SortExpression="CatClass" />
                    <asp:TemplateField HeaderText="Model Attributes">
                        <ItemTemplate>
                            <%# Convert.ToBoolean(Eval("fComplex")) ? "Complex " : "" %>
                            <%# Convert.ToBoolean(Eval("fHighPerf")) ? "High Perf " : (Convert.ToBoolean(Eval("f200HP")) ? "200hp" : "") %>
                            <%# Convert.ToBoolean(Eval("fTailWheel")) ? "Tailwheel " : "" %>
                            <%# Convert.ToBoolean(Eval("fConstantProp")) ? "Constant Prop " : "" %>
                            <%# Convert.ToBoolean(Eval("fTurbine")) ? "Turbine " : "" %>
                            <%# Convert.ToBoolean(Eval("fRetract")) ? "Retract " : "" %>
                            <%# Convert.ToBoolean(Eval("fCowlFlaps")) ? "Flaps" : "" %>
                            <%# Eval("ArmyMissionDesignSeries").ToString().Length > 0 ? "AMS = " + Eval("ArmyMissionDesignSeries").ToString() : "" %>
                            <%# Convert.ToBoolean(Eval("fSimOnly")) ? "Sim Only " : "" %>
                            <%# Convert.ToBoolean(Eval("fGlassOnly")) ? "Glass Only " : "" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </asp:View>
        <asp:View ID="vwMainManufacturers" runat="server">
            <h2>Existing Manufacturers</h2><asp:UpdatePanel runat="server" ID="updatePanelManufacturers">
                <ContentTemplate>
                    <asp:Panel ID="Panel1" runat="server" DefaultButton="btnAdd">
                        Add a new manufacturer: <asp:TextBox ID="txtNewManufacturer" runat="server"></asp:TextBox><asp:Button ID="btnAdd" runat="server" OnClick="btnManAdd_Click" Text="Add" />
                    </asp:Panel>
                    <br />
                    <asp:GridView ID="gvManufacturers" EnableModelValidation="True" runat="server" OnRowDeleting="ManRowDeleting" OnRowUpdating="ManRowUpdating"
                        AllowSorting="True" AutoGenerateEditButton="True" AutoGenerateDeleteButton="True" OnRowDataBound="gvManufacturers_RowDataBound" 
                        DataSourceID="sqlDSManufacturers" DataKeyNames="idManufacturer" BorderStyle="None" 
                        CellPadding="3" AutoGenerateColumns="False">
                            <Columns>
                                <asp:BoundField DataField="idManufacturer" HeaderText="idManufacturer" SortExpression="idManufacturer" ReadOnly="true" />
                                <asp:BoundField DataField="manufacturer" HeaderText="manufacturer" SortExpression="manufacturer" />
                                <asp:TemplateField HeaderText="Restriction" SortExpression="DefaultSim">
                                    <ItemTemplate>
                                        <%# ((AllowedAircraftTypes) ((UInt32)Eval("DefaultSim"))).ToString() %>
                                    </ItemTemplate>
                                    <EditItemTemplate>
                                        <asp:RadioButtonList ID="rblDefaultSim" runat="server">
                                            <asp:ListItem Text="No Restrictions" Value="0"></asp:ListItem>
                                            <asp:ListItem Text="Sim Only" Value="1"></asp:ListItem>
                                            <asp:ListItem Text="Sim or Generic, but not real" Value="2"></asp:ListItem>
                                        </asp:RadioButtonList>
                                    </EditItemTemplate>
                                </asp:TemplateField>
                                <asp:BoundField DataField="Number of Models" ReadOnly="True" HeaderText="Number of models" SortExpression="Number of Models" />
                            </Columns>
                            <SelectedRowStyle BackColor="#E0E0E0" />
                    </asp:GridView>
                    <asp:SqlDataSource ID="sqlDSManufacturers" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" 
                        SelectCommand="SELECT man.*, COUNT(m.idmodel) AS 'Number of Models'
            FROM manufacturers man
            LEFT JOIN models m ON m.idmanufacturer=man.idmanufacturer
            GROUP BY man.idmanufacturer
            ORDER BY manufacturer ASC"
                        UpdateCommand="UPDATE manufacturers SET Manufacturer=?manufacturer, DefaultSim=?DefaultSim WHERE idManufacturer=?idManufacturer"
                        DeleteCommand="DELETE FROM manufacturers WHERE idManufacturer=?idManufacturer"
                        >
                        <UpdateParameters>
                            <asp:Parameter Name="manufacturer" Type="String" Size="50"  Direction="Input" />
                            <asp:Parameter Name="DefaultSim" Type="Int32" Direction="InputOutput" />
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </UpdateParameters>
                        <DeleteParameters>
                            <asp:Parameter Name="idManufacturer" Type="Int32" Direction="Input" />
                        </DeleteParameters>
                    </asp:SqlDataSource>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainProperties" runat="server">
            <asp:SqlDataSource ID="sqlCustomProps" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM custompropertytypes ORDER BY idPropType DESC"
                UpdateCommand="UPDATE custompropertytypes SET Title=?Title, SortKey=IF(?SortKey IS NULL, '', ?SortKey), FormatString=?FormatString, Description=?Description, Type=?Type, Flags=?Flags WHERE idPropType=?idPropType">
                <UpdateParameters>
                    <asp:Parameter Name="Title" Direction="Input" Type="String" />
                    <asp:Parameter Name="FormatString" Direction="Input" Type="String" />
                    <asp:Parameter Name="Type" Direction="Input" Type="Int32" />
                    <asp:Parameter Name="Flags" Direction="Input" Type="Int32" />
                    <asp:Parameter Name="idPropType" Direction="Input" Type="Int32" />
                    <asp:Parameter Name="Description" Direction="Input" Type="String" />
                    <asp:Parameter Name="SortKey" Direction="Input" Type="String" ConvertEmptyStringToNull="false" />
                </UpdateParameters>
            </asp:SqlDataSource>
            <h2>Existing Props</h2><asp:UpdatePanel ID="UpdatePanel1" runat="server">
                <ContentTemplate>
                    <asp:GridView ID="gvCustomProps" runat="server" AllowSorting="True" DataSourceID="sqlCustomProps"
                        AutoGenerateEditButton="True" AutoGenerateColumns="false" OnRowUpdated="CustPropsRowEdited" 
                        EnableModelValidation="True">
                        <Columns>
                            <asp:BoundField DataField="idPropType" HeaderText="PropertyTypeID" />
                            <asp:BoundField DataField="Title" HeaderText="Title" />
                            <asp:BoundField DataField="SortKey" HeaderText="SortKey" />
                            <asp:BoundField DataField="FormatString" HeaderText="Format String" />
                            <asp:BoundField DataField="Description" HeaderText="Description" />
                            <asp:BoundField DataField="Type" HeaderText="Type" />
                            <asp:BoundField DataField="Flags" HeaderText="Flags" />
                            <asp:TemplateField HeaderText="Description">
                                <ItemTemplate>
                                    <asp:Label ID="lblFlagsDesc" runat="server" Text='<%# CustomPropertyType.AdminFlagsDesc((CFPPropertyType)Convert.ToInt32(Eval("Type")), (UInt32)(Convert.ToInt32(Eval("Flags")))) %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainEndorsements" runat="server">
            <h2>Existing Endorsements</h2><asp:UpdatePanel ID="updPnlEndorsements" runat="server">
                <ContentTemplate>
                    <asp:SqlDataSource ID="sqlEndorsementTemplates" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                        ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>" SelectCommand="SELECT * FROM endorsementtemplates ORDER BY FARRef ASC"
                        UpdateCommand="UPDATE endorsementtemplates SET FARRef=?FARRef, Title=?Title, Text=?Text WHERE ID=?id">
                        <UpdateParameters>
                            <asp:Parameter Name="FARRef" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                            <asp:Parameter Name="Title" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                            <asp:Parameter Name="Text"  DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                            <asp:Parameter Name="id" Direction="Input" Type="Int32" />
                        </UpdateParameters>
                    </asp:SqlDataSource>
                    <asp:GridView ID="gvEndorsementTemplate" runat="server" DataSourceID="sqlEndorsementTemplates"
                        AutoGenerateEditButton="True" AutoGenerateColumns="false" 
                        EnableModelValidation="True" 
                        onrowdatabound="gvEndorsementTemplate_RowDataBound" CellPadding="5">
                        <Columns>
                            <asp:TemplateField HeaderText="Endorsement Template Data">
                                <ItemStyle VerticalAlign="Top" />
                                <ItemTemplate>
                                    <table width="600px">
                                        <tr>
                                            <td valign="top"><b>ID</b></td><td><asp:Label ID="Label1" runat="server"><%# Eval("id") %></asp:Label></td></tr><tr>
                                            <td valign="top"><b>Title</b></td><td><asp:Label ID="Label2" runat="server"><%# Eval("Title") %></asp:Label></td></tr><tr>
                                            <td valign="top"><b>FAR</b></td><td><asp:Label ID="Label3" runat="server"><%# Eval("FARRef") %></asp:Label></td></tr><tr>
                                            <td valign="top"><b>Template</b></td><td><asp:Label ID="Label4" runat="server"><%# Eval("Text") %></asp:Label></td></tr></table></ItemTemplate><EditItemTemplate>
                                    <table width="400px">
                                        <tr>
                                            <td valign="top"><b>ID</b></td><td><asp:Label ID="txtID" runat="server" Text='<%# Bind("id") %>' /></td>
                                        </tr>
                                        <tr>
                                            <td valign="top"><b>Title</b></td><td><asp:TextBox ID="txtTitle" runat="server" Text='<%# Bind("Title") %>' /></td>
                                        </tr>
                                        <tr>
                                            <td valign="top"><b>FAR</b></td><td><asp:TextBox ID="txtFAR" runat="server" Text='<%# Bind("FARRef") %>' /></td>
                                        </tr>
                                        <tr>
                                            <td valign="top"><b>Template</b></td><td><asp:TextBox ID="txtText" runat="server" Width="300px" Text='<%# Bind("Text")%>' TextMode="MultiLine" /></td>
                                        </tr>
                                    </table>
                                </EditItemTemplate>
                            </asp:TemplateField>
                            <asp:TemplateField HeaderText="Preview">
                                <ItemTemplate>
                                    <uc2:mfbEditEndorsement ID="mfbEditEndorsement1" PreviewMode="true" runat="server" />
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>
                </ContentTemplate>
            </asp:UpdatePanel>
        </asp:View>
        <asp:View ID="vwMainImages" runat="server">
        </asp:View>
        <asp:View ID="vwMainAchievements" runat="server">
        </asp:View>
        <asp:View ID="vwMainDonations" runat="server">
            <asp:SqlDataSource ID="sqlDSDonations" runat="server" ConnectionString="<%$ ConnectionStrings:logbookConnectionString %>"
                ProviderName="<%$ ConnectionStrings:logbookConnectionString.ProviderName %>"
                SelectCommand=""
                UpdateCommand="UPDATE payments SET Notes=?Notes WHERE idPayments=?idPayments" >
                <SelectParameters>
                    <asp:Parameter Name="user" DefaultValue="%" />
                </SelectParameters>
                <UpdateParameters>
                    <asp:Parameter Name="Notes" DefaultValue="" Direction="Input" ConvertEmptyStringToNull="false" Type="String" />
                    <asp:Parameter Name="idPayments" Direction="Input" Type="Int32" />
                </UpdateParameters>
            </asp:SqlDataSource>
            <asp:GridView ID="gvDonations" runat="server" GridLines="None" DataSourceID="sqlDSDonations" CellPadding="5" EnableModelValidation="true" 
                ShowHeader="true" AllowPaging="true" OnPageIndexChanging="gvDonations_PageIndexChanging" PageSize="25" AutoGenerateColumns="false" 
                onrowdatabound="gvDonations_RowDataBound">
                <Columns>
                    <asp:TemplateField HeaderText="ID" SortExpression="idPayments" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelPayment" runat="server" Text='<%# Bind("idPayments") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Date" SortExpression="Date" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelDate" runat="server" Text='<%# Bind("Date") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Username" SortExpression="Username" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelUser" runat="server" Text='<%# Bind("Username") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Amount" SortExpression="Amount" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelAmount" runat="server" Text='<%# Bind("Amount") %>'></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Fee" SortExpression="Fee" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelFee" runat="server" Text='<%# Bind("Fee") %>'></asp:Label>&nbsp;</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Net" SortExpression="Net" ItemStyle-VerticalAlign="Top">
                        <ItemTemplate>
                            <asp:Label ID="LabelNet" runat="server" Text='<%# Bind("Net") %>'></asp:Label>&nbsp;</ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="TransactionType" SortExpression="TYPE">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <asp:Label ID="lblTransactionType" runat="server" Text=""></asp:Label></ItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="Notes" SortExpression="">
                        <ItemStyle VerticalAlign="Top" />
                        <ItemTemplate>
                            <asp:Label ID="lblNotes" runat="server" Text='<%# Bind("Notes") %>'></asp:Label></ItemTemplate><EditItemTemplate>
                            <asp:TextBox ID="txtNotes" runat="server" Text='<%# Bind("Notes") %>' TextMode="MultiLine"></asp:TextBox></EditItemTemplate></asp:TemplateField><asp:TemplateField HeaderText="TransactionData" SortExpression="">
                        <ItemStyle VerticalAlign="Top" HorizontalAlign="Left" Width="350px" />
                        <ItemTemplate>
                            <asp:Label ID="lblTransactionID" runat="server" Font-Bold="true" Text='<%# Bind("TransactionID") %>'></asp:Label><asp:Panel ID="pnlDecoded" runat="server">
                                <asp:PlaceHolder ID="plcDecoded" runat="server"></asp:PlaceHolder>
                                <asp:Label ID="lblTxNotes" runat="server" Text="Decode"></asp:Label><br /></asp:Panel><cc1:CollapsiblePanelExtender ID="cpeNotes" runat="server"
                                CollapseControlID="lblTransactionID" SuppressPostBack="True"
                                ExpandControlID="lblTransactionID" TargetControlID="pnlDecoded"
                                Collapsed="True">
                            </cc1:CollapsiblePanelExtender>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
            <asp:HiddenField ID="hdnLastDonationSortExpr" runat="server" />
            <asp:HiddenField ID="hdnLastDonationSortDir" runat="server" />
        </asp:View>
        <asp:View ID="vwMainMisc" runat="server">
            <asp:GridView ID="gvInvalidSignatures" runat="server" AutoGenerateColumns="false" OnRowCommand="gvInvalidSignatures_RowCommand">
                <Columns>
                    <asp:HyperLinkField DataNavigateUrlFields="FlightID" DataNavigateUrlFormatString="~/Member/LogbookNew.aspx/{0}?a=1" DataTextField="FlightID" DataTextFormatString="{0}" Target="_blank" />
                    <asp:TemplateField>
                        <ItemTemplate>
                            <%# Eval("User") %><br />
                            <%# ((DateTime) Eval("Date")).ToShortDateString() %><br />
                            Saved State: <%# Eval("CFISignatureState") %><br /><%# Eval("AdminSignatureSanityCheckState").ToString() %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                        <ItemTemplate>
                            <asp:Label ID="Label5" runat="server" Width="60px" Text="Saved:"></asp:Label><%# Eval("DecryptedFlightHash") %><br /><asp:Label ID="Label6" runat="server" Width="60px" Text="Current:"></asp:Label><%# Eval("DecryptedCurrentHash") %></ItemTemplate></asp:TemplateField><asp:TemplateField>
                        <ItemTemplate>
                            <asp:Button ID="btnSetValidity" runat="server" Text="Fix" CommandArgument='<%# Bind("FlightID") %>' CommandName="FixValidity" /><br />
                            <asp:Button ID="btnForceValidSig" runat="server" Text="Force Valid" CommandArgument='<%# Bind("FlightID") %>' CommandName="ForceValidity" />
                            </ItemTemplate></asp:TemplateField></Columns></asp:GridView></asp:View></asp:MultiView></asp:Content>