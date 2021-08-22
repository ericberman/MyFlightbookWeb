<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/MasterPage.master" Title="Admin - Aircraft" CodeBehind="AdminAircraft.aspx.cs" Inherits="MyFlightbook.Web.Admin.AdminAircraft" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="~/Controls/mfbTooltip.ascx" TagPrefix="uc1" TagName="mfbTooltip" %>
<asp:Content ID="Content2" ContentPlaceHolderID="cpPageTitle" runat="Server">
    Admin Tools - Aircraft
</asp:Content>
<asp:Content ID="Content1" ContentPlaceHolderID="cpTopForm" runat="Server">
    <script>
        function convertOandI(sender, idAircraft) {
            var params = new Object();
            params.idAircraft = idAircraft;
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Admin/AdminAircraft.aspx/ConvertOandI") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
                });
        }
        function trimLeadingN(sender, idAircraft) {
            var params = new Object();
            params.idAircraft = idAircraft;
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Admin/AdminAircraft.aspx/TrimLeadingN") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) {  },
                    success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
                });
        }
        function migrateGeneric(sender, idAircraft) {
            var params = new Object();
            params.idAircraft = idAircraft;
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Admin/AdminAircraft.aspx/MigrateGeneric") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
                });
        }
        function trimN0(sender, idAircraft) {
            var params = new Object();
            params.idAircraft = idAircraft;
            var d = JSON.stringify(params);
            $.ajax(
                {
                    url: '<% =ResolveUrl("~/Admin/AdminAircraft.aspx/TrimN0") %>',
                    type: "POST", data: d, dataType: "json", contentType: "application/json",
                    error: function (xhr, status, error) {
                        window.alert(xhr.responseJSON.Message);
                    },
                    complete: function (response) { },
                    success: function (response) { document.getElementById(sender).parentElement.parentElement.className = 'handled'; }
                });
        }
    </script>
    <style type="text/css">
        .adm .admItem {
        }
        .adm .admItem:after {
            content: " | ";
        }
        .adm .admItem:last-child:after {
            content: "";
        }
        .handled {
            background-color: lightgray;
            color: darkgray;
        }
    </style>
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
                <tr style="vertical-align: top">
                    <td>
                        <div>
                            <asp:Button ID="btnMapModels" runat="server" Width="100%" OnClick="btnMapModels_Click" Text="Bulk-map models" /></div>
                    </td>
                    <td>
                        <div>
                            <asp:FileUpload ID="fuMapModels" runat="server" /></div>
                        Bulk map aircraft models from spreadsheet.
                                    <uc1:mfbtooltip runat="server" id="mfbTooltip">
                                        <TooltipBody>
                                            <div>CSV spreadsheet needs two columns:</div>
                                            <ul>
                                                <li>idaircraft - the ID of the aircraft to map</li>
                                                <li>idModelProper - the ID of the model to which it should be mapped.</li>
                                            </ul>
                                        </TooltipBody>
                                    </uc1:mfbtooltip>
                        <div>
                            <asp:Label runat="server" CssClass="error" EnableViewState="false" ID="lblMapModelErr"></asp:Label></div>
                    </td>
                </tr>
            </table>
            <asp:Panel ID="pnlFindAircraft" runat="server" DefaultButton="btnFindAircraftByTail">
                Find aircraft by tail (use * and ? as wildcards):
                            <asp:TextBox ID="txtTailToFind" runat="server"></asp:TextBox>
                <asp:Button ID="btnFindAircraftByTail" runat="server"
                    OnClick="btnFindAircraftByTail_Click" Text="Find" />
            </asp:Panel>
            <asp:UpdateProgress ID="updprgAircraft" runat="server"
                AssociatedUpdatePanelID="updpanelAircraft">
                <ProgressTemplate>
                    <p>Evaluating aircraft for issues...</p>
                    <p>
                        <asp:Image ID="imgProgress" runat="server"
                            ImageUrl="~/images/ajax-loader.gif" />
                    </p>
                </ProgressTemplate>
            </asp:UpdateProgress>
            <div>
                <asp:Label ID="lblAircraftStatus" runat="server" EnableViewState="false" Font-Bold="true"></asp:Label></div>
            <asp:MultiView ID="mvAircraftIssues" runat="server">
                <asp:View ID="vwDupeAircraft" runat="server">
                    <p>
                        Potential duplicate aircraft:
                    </p>
                    <asp:GridView ID="gvDupeAircraft" runat="server" AutoGenerateColumns="False"
                        OnRowCommand="gvDupeAircraft_RowCommand">
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
                                No potential duplicates found!
                            </p>
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
                        Duplicate sims:
                    </p>
                    <asp:GridView ID="gvDupeSims" runat="server" AutoGenerateColumns="False"
                        OnRowCommand="gvDupeSims_RowCommand">
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
                                No potential duplicates found!
                            </p>
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
                        Aircraft that may be invalid:
                    </p>
                    <asp:GridView ID="gvInvalidAircraft" runat="server" AutoGenerateColumns="False"
                        EnableViewState="false">
                        <Columns>
                            <asp:TemplateField HeaderText="Aircraft">
                                <ItemTemplate>
                                    <div>
                                        <a href='../Member/EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1'
                                            target="_blank"><%# Eval("TailNumber") %></a>
                                    </div>
                                    <div><%# MakeModel.GetModel((int) Eval("ModelID")).DisplayName %></div>
                                    <div><%# Eval("InstanceTypeDescription") %></div>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="ErrorString" HeaderText="Validation Error" />
                        </Columns>
                        <EmptyDataTemplate>
                            <p class="success">
                                No invalid aircraft!
                            </p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:View>
                <asp:View ID="vwAllSims" runat="server">
                    <p>
                        All sims:
                    </p>
                    <asp:Label ID="lblSimsFound" runat="server" Text=""></asp:Label>
                    <asp:GridView ID="gvSims" runat="server" AllowSorting="true"
                        AutoGenerateColumns="false" OnRowCommand="gvSims_RowCommand">
                        <Columns>
                            <asp:BoundField DataField="AircraftID" HeaderText="ID"
                                SortExpression="AircraftID" />
                            <asp:TemplateField HeaderText="Aircraft" SortExpression="TailNumber">
                                <ItemTemplate>
                                    <a href='../Member/EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1'
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
                    <p>Orphaned Aircraft
                        <asp:Button ID="btnDeleteAllOrphans" runat="server" Text="Delete all orphans" OnClick="btnDeleteAllOrphans_Click" /></p>
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
                                    <a href='../Member/EditAircraft.aspx?id=<%# Eval("idAircraft") %>&amp;a=1'
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
                                No orphaned aircraft found!
                            </p>
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
                                    <a href='../Member/EditAircraft.aspx?id=<%# Eval("AircraftID") %>&amp;a=1&amp;gencandidate=1'
                                        target="_blank"><%# Eval("TailNumber") %></a>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                        <EmptyDataTemplate>
                            <p>
                                No matching aircraft found!
                            </p>
                        </EmptyDataTemplate>
                    </asp:GridView>
                </asp:View>
                <asp:View ID="vwPseudoGeneric" runat="server">
                    <asp:GridView ID="gvPseudoGeneric" runat="server" AutoGenerateColumns="false" OnRowDataBound="gvPseudoGeneric_RowDataBound">
                        <Columns>
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <asp:HyperLInk ID="lblTailnumber" Font-Bold="true" runat="server" Text='<%# Eval("Tailnumber") %>' Target="_blank" NavigateUrl='<%# String.Format(System.Globalization.CultureInfo.InvariantCulture, "~/Member/EditAircraft.aspx?id={0}&a=1&genCandidate=1", Eval("idaircraft")) %>' />
                                    <asp:Label ID="lblManufacturer" runat="server" Text='<%# Eval("manufacturer") %>'></asp:Label>
                                    <asp:Label ID="lblModel" runat="server" Text='<%# Eval("model") %>'></asp:Label>
                                </ItemTemplate>
                            </asp:TemplateField>
                            <asp:BoundField DataField="numFlights" HeaderText="numFlights" />
                            <asp:TemplateField>
                                <ItemTemplate>
                                    <div class="adm">
                                        <asp:HyperLink ID="lnkViewFixedTail" Target="_blank" runat="server" CssClass="admItem" />
                                        <asp:HyperLink ID="lnkRemoveLeadingN" Visible="false" runat="server" Text="Remove Leading N" CssClass="admItem" />
                                        <asp:HyperLink ID="lnkConvertOandI" Visible="false" runat="server" Text="Convert O/I to 0/1" CssClass="admItem" />
                                        <asp:HyperLink ID="lnkN0ToN" Visible="false" runat="server" Text="N0 → N" CssClass="admItem" />
                                        <asp:HyperLink ID="lnkMigrateGeneric" runat="server" Text="Migrate Generic" CssClass="admItem" />
                                    </div>
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
    OR ac.tailnumber RLIKE '^N.*[ioIO]'
    OR ac.tailnumber RLIKE '^N-?0'
    OR modelTails.tailnumber IS NOT NULL
    OR REPLACE(RIGHT(ac.tailnumber, LENGTH(ac.tailnumber) - 1), '-', '') = REPLACE(RIGHT(m.model, LENGTH(m.model) - 1), '-', '')
    OR (LEFT(ac.tailnumber, 3) &lt;&gt; 'SIM' AND (LEFT(REPLACE(ac.tailnumber, '-', ''), 4) = LEFT(man.manufacturer, 4)))
    OR (ac.instancetype=1 AND REPLACE(ac.tailnumber, '-', '') RLIKE 'SIM|FTD|ATD|FFS|REDB|FRAS|ELIT|CAE|ALSIM|FLIG|SAFE|PREC|TRUF|FMX|GROU|VARI|MISC|NONE|UNKN|FAA')
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
</asp:Content>
<asp:Content runat="server" ID="content3" ContentPlaceHolderID="cpMain">
    <asp:UpdatePanel ID="UpdatePanel4" runat="server">
        <ContentTemplate>
            <div style="padding: 5px;">
                <asp:HiddenField ID="hdnLastCountryEdited" runat="server" />
                <asp:HiddenField ID="hdnLastCountryResult" runat="server" />
                <asp:GridView ID="gvCountryCodes" runat="server" AllowSorting="True" OnRowEditing="gvCountryCodes_RowEditing"
                    AutoGenerateEditButton="true" CellPadding="3" AutoGenerateColumns="false" OnRowCommand="gvCountryCodes_RowCommand"
                    OnRowDataBound="gvCountryCodes_RowDataBound" DataKeyNames="ID" OnRowUpdating="gvCountryCodes_RowUpdating">
                    <Columns>
                        <asp:BoundField DataField="ID" HeaderText="ID" SortExpression="ID" ReadOnly="true" />
                        <asp:BoundField DataField="Prefix" HeaderText="Prefix" SortExpression="Prefix" />
                        <asp:BoundField DataField="CountryName" HeaderText="Country Name" SortExpression="CountryName" />
                        <asp:BoundField DataField="Locale" HeaderText="Locale" SortExpression="Locale" ConvertEmptyStringToNull="false" />
                        <asp:BoundField DataField="RegistrationURLTemplate" />
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
                                <div>
                                    <asp:Label ID="lblHyphenResult" runat="server" Font-Bold="true" Visible="false"></asp:Label></div>
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
</asp:Content>
